"""Configuration management for AgentService."""

import os
import re
from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict


def _normalize_db_url(raw_url: str | None) -> str:
    """Normalize database URL from standard Postgres URL or ADO.NET connection string format."""
    if not raw_url:
        return "postgresql+asyncpg://postgres:change-me-postgres-password@postgres:5432/agent_service"

    # Check for ADO.NET connection string format: Server=...;Port=...;Database=...;User Id=...;Password=...
    if "Server=" in raw_url or "Database=" in raw_url:
        server_match = re.search(r"Server=([^;]+)", raw_url, re.IGNORECASE)
        port_match = re.search(r"Port=([^;]+)", raw_url, re.IGNORECASE)
        db_match = re.search(r"Database=([^;]+)", raw_url, re.IGNORECASE)
        user_match = re.search(r"(?:User Id|Uid|User)=([^;]+)", raw_url, re.IGNORECASE)
        pwd_match = re.search(r"(?:Password|Pwd)=([^;]+)", raw_url, re.IGNORECASE)

        server = server_match.group(1).strip() if server_match else "localhost"
        port = port_match.group(1).strip() if port_match else "5432"
        db = db_match.group(1).strip() if db_match else "agent_service"
        user = user_match.group(1).strip() if user_match else "postgres"
        pwd = pwd_match.group(1).strip() if pwd_match else "postgres"

        return f"postgresql+asyncpg://{user}:{pwd}@{server}:{port}/{db}"

    # If it's postgresql://, convert to async driver postgresql+asyncpg://
    if raw_url.startswith("postgresql://"):
        return raw_url.replace("postgresql://", "postgresql+asyncpg://", 1)

    if raw_url.startswith("postgres://"):
        return raw_url.replace("postgres://", "postgresql+asyncpg://", 1)

    return raw_url


def _normalize_grpc_address(raw_addr: str | None, default: str) -> str:
    """Normalize gRPC target address by stripping http:// and https:// schemes."""
    if not raw_addr or not str(raw_addr).strip():
        raw_addr = default
    raw_addr = str(raw_addr).strip()
    if raw_addr.startswith("http://"):
        raw_addr = raw_addr[len("http://") :]
    elif raw_addr.startswith("https://"):
        raw_addr = raw_addr[len("https://") :]
    return raw_addr.rstrip("/")


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        extra="ignore"
    )

    # Server settings
    PORT: int = Field(default=5131, validation_alias="PORT")
    HOST: str = Field(default="0.0.0.0", validation_alias="HOST")

    # Database
    DATABASE_URL: str = Field(
        default_factory=lambda: _normalize_db_url(
            os.environ.get("DATABASE_URL")
            or os.environ.get("ConnectionStrings__DefaultConnection")
            or os.environ.get("ConnectionStrings_DefaultConnection")
        )
    )

    # AI Completion Settings
    AI_BASE_URL: str = Field(
        default_factory=lambda: os.environ.get("AI_COMPLETION_BASE_URL")
        or os.environ.get("Ai__BaseUrl")
        or os.environ.get("AI_BASE_URL")
        or "https://api.openai.com/v1"
    )
    AI_API_KEY: str = Field(
        default_factory=lambda: os.environ.get("OPENAI_API_KEY")
        or os.environ.get("Ai__ApiKey")
        or os.environ.get("AI_API_KEY")
        or ""
    )
    AI_MODEL: str = Field(
        default_factory=lambda: os.environ.get("AI_COMPLETION_MODEL")
        or os.environ.get("Ai__Model")
        or os.environ.get("AI_MODEL")
        or "gpt-4o-mini"
    )
    AI_TIMEOUT_SECONDS: int = Field(
        default_factory=lambda: int(
            os.environ.get("Ai__TimeoutSeconds")
            or os.environ.get("AI_TIMEOUT_SECONDS")
            or 120
        )
    )
    AI_ENABLED: bool = Field(
        default_factory=lambda: (
            os.environ.get("AI_COMPLETION_ENABLED")
            or os.environ.get("Ai__Enabled")
            or os.environ.get("AI_ENABLED")
            or "true"
        ).lower() in ("true", "1", "yes")
    )

    # Downstream gRPC services
    VOCABULARY_GRPC_ADDRESS: str = Field(
        default_factory=lambda: _normalize_grpc_address(
            os.environ.get("Vocabulary__GrpcAddress") or os.environ.get("VOCABULARY_GRPC_ADDRESS"),
            "vocabulary-service:5117"
        )
    )
    INCLUSIVE_GRPC_ADDRESS: str = Field(
        default_factory=lambda: _normalize_grpc_address(
            os.environ.get("Inclusive__GrpcAddress") or os.environ.get("INCLUSIVE_GRPC_ADDRESS"),
            "inclusive:40051"
        )
    )


settings = Settings()
