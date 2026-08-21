"""SQLAlchemy models for AgentService in schema 'internal'."""

import uuid
from datetime import datetime, timezone
from sqlalchemy import (
    Boolean,
    Column,
    DateTime,
    ForeignKey,
    Index,
    String,
    Text,
    func,
)
from sqlalchemy.dialects.postgresql import JSONB, UUID
from sqlalchemy.orm import DeclarativeBase, relationship
from sqlalchemy.types import JSON, TypeDecorator


class CompatibleUUID(TypeDecorator):
    """Platform-independent GUID/UUID type.
    Uses PostgreSQL's UUID type, otherwise CHAR(36).
    """
    impl = String(36)
    cache_ok = True

    def load_dialect_impl(self, dialect):
        if dialect.name == "postgresql":
            return dialect.type_descriptor(UUID(as_uuid=True))
        return dialect.type_descriptor(String(36))

    def process_bind_param(self, value, dialect):
        if value is None:
            return value
        if isinstance(value, uuid.UUID):
            return str(value) if dialect.name != "postgresql" else value
        return uuid.UUID(value) if dialect.name == "postgresql" else str(value)

    def process_result_value(self, value, dialect):
        if value is None:
            return value
        if isinstance(value, uuid.UUID):
            return value
        return uuid.UUID(value)


class CompatibleJSON(TypeDecorator):
    """Platform-independent JSON type.
    Uses PostgreSQL's JSONB type, otherwise standard JSON.
    """
    impl = JSON
    cache_ok = True

    def load_dialect_impl(self, dialect):
        if dialect.name == "postgresql":
            return dialect.type_descriptor(JSONB)
        return dialect.type_descriptor(JSON)


class Base(DeclarativeBase):
    pass


class CustomScenario(Base):
    __tablename__ = "custom_scenarios"
    __table_args__ = (
        Index("idx_custom_scenarios_user_created", "user_id", "created_at"),
        {"schema": "internal"},
    )

    id = Column(CompatibleUUID, primary_key=True, default=uuid.uuid4)
    user_id = Column(CompatibleUUID, nullable=False)
    title = Column(String, nullable=False)
    description = Column(Text, nullable=True)
    target_skill = Column(String, nullable=False, default="Speaking")
    system_prompt_template = Column(Text, nullable=False)
    difficulty = Column(String, nullable=True)
    goals = Column(CompatibleJSON, nullable=True)
    context_configuration = Column(CompatibleJSON, nullable=True)
    created_at = Column(DateTime(timezone=True), nullable=False, default=lambda: datetime.now(timezone.utc), server_default=func.now())
    updated_at = Column(DateTime(timezone=True), nullable=False, default=lambda: datetime.now(timezone.utc), server_default=func.now(), onupdate=lambda: datetime.now(timezone.utc))

    threads = relationship("AgentThread", back_populates="custom_scenario")


class AgentThread(Base):
    __tablename__ = "agent_threads"
    __table_args__ = (
        Index("idx_agent_threads_user_project_updated", "user_id", "project_id", "updated_at"),
        {"schema": "internal"},
    )

    id = Column(CompatibleUUID, primary_key=True, default=uuid.uuid4)
    user_id = Column(CompatibleUUID, nullable=False)
    project_id = Column(CompatibleUUID, nullable=False)
    title = Column(String, nullable=True)
    agent_id = Column(String, nullable=True)
    system_prompt_override = Column(Text, nullable=True)
    custom_scenario_id = Column(CompatibleUUID, ForeignKey("internal.custom_scenarios.id"), nullable=True)
    created_at = Column(DateTime(timezone=True), nullable=False, default=lambda: datetime.now(timezone.utc), server_default=func.now())
    updated_at = Column(DateTime(timezone=True), nullable=False, default=lambda: datetime.now(timezone.utc), server_default=func.now(), onupdate=lambda: datetime.now(timezone.utc))
    archived_at = Column(DateTime(timezone=True), nullable=True)

    custom_scenario = relationship("CustomScenario", back_populates="threads")
    messages = relationship("AgentMessage", back_populates="thread", cascade="all, delete-orphan")
    runs = relationship("AgentRun", back_populates="thread", cascade="all, delete-orphan")
    artifacts = relationship("AgentArtifact", back_populates="thread", cascade="all, delete-orphan")


class AgentMessage(Base):
    __tablename__ = "agent_messages"
    __table_args__ = (
        Index("idx_agent_messages_thread_created", "thread_id", "created_at"),
        {"schema": "internal"},
    )

    id = Column(CompatibleUUID, primary_key=True, default=uuid.uuid4)
    thread_id = Column(CompatibleUUID, ForeignKey("internal.agent_threads.id"), nullable=False)
    role = Column(String(16), nullable=False)
    content = Column(Text, nullable=False)
    metadata_json = Column(CompatibleJSON, nullable=True)
    created_at = Column(DateTime(timezone=True), nullable=False, default=lambda: datetime.now(timezone.utc), server_default=func.now())

    thread = relationship("AgentThread", back_populates="messages")


class AgentRun(Base):
    __tablename__ = "agent_runs"
    __table_args__ = (
        Index("idx_agent_runs_thread_id", "thread_id"),
        {"schema": "internal"},
    )

    id = Column(CompatibleUUID, primary_key=True, default=uuid.uuid4)
    thread_id = Column(CompatibleUUID, ForeignKey("internal.agent_threads.id"), nullable=False)
    status = Column(String(16), nullable=False, default="completed")
    model = Column(String, nullable=True)
    started_at = Column(DateTime(timezone=True), nullable=False, default=lambda: datetime.now(timezone.utc), server_default=func.now())
    completed_at = Column(DateTime(timezone=True), nullable=True)
    error = Column(Text, nullable=True)

    thread = relationship("AgentThread", back_populates="runs")
    tool_calls = relationship("AgentToolCall", back_populates="run", cascade="all, delete-orphan")
    domain_decision = relationship("AgentDomainDecision", back_populates="run", uselist=False, cascade="all, delete-orphan")
    artifacts = relationship("AgentArtifact", back_populates="run", cascade="all, delete-orphan")


class AgentToolCall(Base):
    __tablename__ = "agent_tool_calls"
    __table_args__ = (
        Index("idx_agent_tool_calls_run_created", "run_id", "created_at"),
        {"schema": "internal"},
    )

    id = Column(CompatibleUUID, primary_key=True, default=uuid.uuid4)
    run_id = Column(CompatibleUUID, ForeignKey("internal.agent_runs.id"), nullable=False)
    tool_name = Column(Text, nullable=False)
    input_json = Column(CompatibleJSON, nullable=False)
    output_json = Column(CompatibleJSON, nullable=False)
    status = Column(String(16), nullable=False, default="completed")
    created_at = Column(DateTime(timezone=True), nullable=False, default=lambda: datetime.now(timezone.utc), server_default=func.now())

    run = relationship("AgentRun", back_populates="tool_calls")


class AgentDomainDecision(Base):
    __tablename__ = "agent_domain_decisions"
    __table_args__ = (
        Index("idx_agent_domain_decisions_run_id", "run_id", unique=True),
        {"schema": "internal"},
    )

    id = Column(CompatibleUUID, primary_key=True, default=uuid.uuid4)
    run_id = Column(CompatibleUUID, ForeignKey("internal.agent_runs.id"), nullable=False, unique=True)
    allowed = Column(Boolean, nullable=False)
    category = Column(String(32), nullable=False)
    reason = Column(Text, nullable=True)
    user_text_preview = Column(Text, nullable=True)
    created_at = Column(DateTime(timezone=True), nullable=False, default=lambda: datetime.now(timezone.utc), server_default=func.now())

    run = relationship("AgentRun", back_populates="domain_decision")


class AgentArtifact(Base):
    __tablename__ = "agent_artifacts"
    __table_args__ = (
        Index("idx_agent_artifacts_thread_created", "thread_id", "created_at"),
        {"schema": "internal"},
    )

    id = Column(CompatibleUUID, primary_key=True, default=uuid.uuid4)
    run_id = Column(CompatibleUUID, ForeignKey("internal.agent_runs.id"), nullable=False)
    thread_id = Column(CompatibleUUID, ForeignKey("internal.agent_threads.id"), nullable=False)
    kind = Column(String(32), nullable=False)
    payload_json = Column(CompatibleJSON, nullable=False)
    created_at = Column(DateTime(timezone=True), nullable=False, default=lambda: datetime.now(timezone.utc), server_default=func.now())

    run = relationship("AgentRun", back_populates="artifacts")
    thread = relationship("AgentThread", back_populates="artifacts")
