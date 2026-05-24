-- Create databases for authorization-module, vocabulary-service, and agent-service.
-- Runs once on first container start (docker-entrypoint-initdb.d).
CREATE DATABASE "auth-module";
CREATE DATABASE vocabulary_service;
CREATE DATABASE agent_service;
