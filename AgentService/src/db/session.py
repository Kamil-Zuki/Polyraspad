"""Database session and connection management."""

from contextlib import asynccontextmanager
from typing import AsyncGenerator
from sqlalchemy import event, text
from sqlalchemy.ext.asyncio import (
    AsyncEngine,
    AsyncSession,
    async_sessionmaker,
    create_async_engine,
)
from src.config import settings
from src.db.models import Base

# Engine configuration
engine: AsyncEngine = create_async_engine(
    settings.DATABASE_URL,
    echo=False,
    future=True,
    pool_pre_ping=True,
)

if engine.dialect.name == "sqlite":
    @event.listens_for(engine.sync_engine, "connect")
    def _sqlite_connect(dbapi_connection, connection_record):
        try:
            cursor = dbapi_connection.cursor()
            cursor.execute("ATTACH DATABASE ':memory:' AS internal")
            cursor.close()
        except Exception:
            pass

async_session_factory = async_sessionmaker(
    engine,
    class_=AsyncSession,
    expire_on_commit=False,
)


async def init_db(custom_engine: AsyncEngine | None = None) -> None:
    """Initialize database schema and tables."""
    eng = custom_engine or engine
    async with eng.begin() as conn:
        if eng.dialect.name == "postgresql":
            await conn.execute(text("CREATE SCHEMA IF NOT EXISTS internal;"))
        await conn.run_sync(Base.metadata.create_all)


@asynccontextmanager
async def get_db(custom_factory: async_sessionmaker[AsyncSession] | None = None) -> AsyncGenerator[AsyncSession, None]:
    """Provide an async transactional database session context."""
    factory = custom_factory or async_session_factory
    async with factory() as session:
        try:
            yield session
        except Exception:
            await session.rollback()
            raise
        finally:
            await session.close()
