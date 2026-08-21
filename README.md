# Polyraspad

Polyraspad is an open-source language-learning platform built around a LingQ-style reader model. 
It features a microservice architecture with a Next.js frontend, ASP.NET Core backend services, a Python AI/Scheduling service, and a browser extension for content capture.

## 🏗 Architecture & Tech Stack

- **Frontend:** Next.js 16 (App Router), React 19, Tailwind CSS v4.
- **Backend Services:** ASP.NET Core (.NET 8/10), gRPC, Entity Framework Core.
- **AI & Scheduling Service:** Python, LangGraph, FSRS (Free Spaced Repetition Scheduler), NLTK.
- **Infrastructure:** PostgreSQL, Redis, MinIO (S3-compatible object storage), Docker Compose.
- **Capture Extension:** Chrome Manifest V3 extension for capturing subtitles, audio, and creating flashcards directly from Netflix/YouTube/etc.

## 🚀 Getting Started (Local Development)

The entire application stack can be run locally using Docker Compose.

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop) installed and running.
- Node.js 22 (for local frontend development, optional).
- .NET 8 / .NET 10 SDKs (for local backend development, optional).

### Setup & Run

1. Clone the repository recursively (since it uses submodules):
   ```bash
   git clone --recursive https://github.com/Kamil-Zuki/Polyraspad.git
   cd Polyraspad
   ```

2. Configure environment variables:
   Copy the example environment file and fill in the required secrets (e.g., JWT secret, PostgreSQL password, API keys).
   ```bash
   cp .env.example .env
   ```

3. Build and start the infrastructure and services:
   ```bash
   docker compose up --build -d
   ```

4. Verify the deployment:
   ```bash
   curl http://localhost:5000/healthz
   ```
   The frontend should now be accessible at `http://localhost:3000`.

## 📚 Documentation

For an in-depth understanding of the codebase, project rules, and development workflows, please read the [AGENTS.md](AGENTS.md) file. This file acts as the repository-level operating guide.

Authoritative documentation for specific microservices and product features can be found in the `Docs/` directory.

## 🤝 Contributing

Contributions are always welcome! Please read our [Contributing Guidelines](CONTRIBUTING.md) before submitting a pull request.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
