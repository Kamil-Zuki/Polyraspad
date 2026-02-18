-- Create databases for authorization-module and vocabulary-service.
-- Runs once on first container start (docker-entrypoint-initdb.d).
CREATE DATABASE "auth-module";
CREATE DATABASE vocabulary_service;
