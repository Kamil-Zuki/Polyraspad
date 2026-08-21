"""Main entrypoint for AgentService."""

import asyncio
import logging
import signal
import sys
from pathlib import Path
import grpc
from grpc_health.v1 import health, health_pb2, health_pb2_grpc

# Ensure AgentService root is on sys.path
_root = Path(__file__).resolve().parent
if str(_root) not in sys.path:
    sys.path.insert(0, str(_root))

from src.clients.client_registry import close_all_clients, get_inclusive_client, get_vocabulary_client
from src.clients.access_validator import VocabularyProjectAccessValidator
from src.config import settings
from src.db.session import init_db
from src.grpc.agent_servicer import AgentGrpcServicer
from src.proto import agent_pb2_grpc
from src.services.orchestrator import AgentOrchestrator
from src.services.thread_service import AgentThreadService

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
)
logger = logging.getLogger("agent_service")


async def serve() -> None:
    # 1. Initialize DB schema and tables
    logger.info("Initializing database schema and tables...")
    try:
        await init_db()
        logger.info("Database schema initialized successfully.")
    except Exception as ex:
        logger.warning("Database initialization encountered an error (continuing startup): %s", ex)

    # 2. Pre-warm singleton gRPC clients (so the first request is fast)
    vocabulary_client = await get_vocabulary_client()
    inclusive_client = await get_inclusive_client()
    access_validator = VocabularyProjectAccessValidator(address=settings.VOCABULARY_GRPC_ADDRESS)

    thread_service = AgentThreadService(project_access_validator=access_validator)
    orchestrator = AgentOrchestrator(
        thread_service=thread_service,
        project_access_validator=access_validator,
        vocabulary_client=vocabulary_client,
    )
    servicer = AgentGrpcServicer(thread_service=thread_service, orchestrator=orchestrator)

    # 3. Configure and start async gRPC server
    server = grpc.aio.server(
        options=[
            ("grpc.max_send_message_length", 1000 * 1024 * 1024),
            ("grpc.max_receive_message_length", 1000 * 1024 * 1024),
            # Keep alive: useful in Docker networking
            ("grpc.keepalive_time_ms", 10_000),
            ("grpc.keepalive_timeout_ms", 5_000),
            ("grpc.keepalive_permit_without_calls", True),
        ]
    )

    # Register AgentService
    agent_pb2_grpc.add_AgentServiceServicer_to_server(servicer, server)

    # Register gRPC Health Check service (required for docker-compose service_healthy)
    health_servicer = health.HealthServicer()
    health_pb2_grpc.add_HealthServicer_to_server(health_servicer, server)
    health_servicer.set("", health_pb2.HealthCheckResponse.SERVING)
    health_servicer.set("pvs.agent.grpc.AgentService", health_pb2.HealthCheckResponse.SERVING)

    listen_addr = f"{settings.HOST}:{settings.PORT}"
    server.add_insecure_port(listen_addr)

    logger.info("AgentService listening on %s (port %d)", settings.HOST, settings.PORT)
    await server.start()

    stop_event = asyncio.Event()

    def _signal_handler():
        logger.info("Received termination signal, stopping server...")
        stop_event.set()

    loop = asyncio.get_running_loop()
    for sig in (signal.SIGINT, signal.SIGTERM):
        try:
            loop.add_signal_handler(sig, _signal_handler)
        except NotImplementedError:
            # Windows does not support add_signal_handler for some signals
            pass

    try:
        await stop_event.wait()
    except asyncio.CancelledError:
        pass
    finally:
        logger.info("Setting health status to NOT_SERVING...")
        health_servicer.set("", health_pb2.HealthCheckResponse.NOT_SERVING)
        health_servicer.set("pvs.agent.grpc.AgentService", health_pb2.HealthCheckResponse.NOT_SERVING)

        logger.info("Closing downstream gRPC clients...")
        await close_all_clients()

        logger.info("Shutting down gRPC server...")
        await server.stop(grace=5.0)
        logger.info("AgentService terminated cleanly.")


def main() -> None:
    try:
        asyncio.run(serve())
    except (KeyboardInterrupt, SystemExit):
        logger.info("Service exited.")


if __name__ == "__main__":
    main()
