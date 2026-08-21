# Contributing to Polyraspad

First of all, thank you for your interest in contributing to Polyraspad! We welcome contributions from everyone.

## Getting Started

1. **Read the Guidelines:** Before contributing, please read our operating guide [AGENTS.md](AGENTS.md) located at the root of the repository. It contains all the necessary information about the repository map, technology stack, service architecture, and development workflow.
2. **Local Setup:** Follow the instructions in the main [README.md](README.md) to get your local development environment up and running using Docker Compose.
3. **Find an Issue:** Look for open issues or open a new issue to discuss the feature or bug you want to work on.

## Pull Request Process

1. Fork the repository and create your branch from `master`.
2. If you've added code that should be tested, add tests.
3. Ensure the test suite passes.
4. Update documentation if necessary.
5. Issue a pull request!

## Code Style

- **C#**: Nullable reference types enabled everywhere. Use async/await without `ConfigureAwait(false)`. Follow the existing options and DI patterns.
- **TypeScript/Next.js**: Strict TypeScript. Use Server Components by default. Tailwind for styling.
- **Python**: Use `pytest` for tests. Follow standard Python async best practices where applicable.

For a full breakdown of the code style, please refer to the `AGENTS.md` file.
