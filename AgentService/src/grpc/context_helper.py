"""Helper functions for extracting authentication and identity from gRPC ServerCallContext."""

import uuid
import grpc


async def get_user_id(context: grpc.ServicerContext) -> uuid.UUID:
    """Extract user_id from gRPC invocation metadata."""
    metadata = dict(context.invocation_metadata())
    user_id_str = metadata.get("user_id") or metadata.get("x-user-id")

    if user_id_str:
        try:
            return uuid.UUID(str(user_id_str).strip())
        except ValueError:
            await context.abort(grpc.StatusCode.UNAUTHENTICATED, "Invalid user ID format in request context")

    await context.abort(grpc.StatusCode.UNAUTHENTICATED, "User ID not found in request context")


def get_roles(context: grpc.ServicerContext) -> list[str]:
    """Extract roles list from gRPC invocation metadata."""
    metadata = dict(context.invocation_metadata())
    roles_str = metadata.get("roles") or metadata.get("x-user-role") or ""

    if roles_str:
        return [r.strip() for r in roles_str.split(",") if r.strip()]

    return []
