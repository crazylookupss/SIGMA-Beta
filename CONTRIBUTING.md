# Contributing to SIGMA API

Thank you for your interest in contributing! We welcome contributions from the community.

## How to Contribute

1. **Fork the repository** and create a feature branch from `main`.
2. **Make your changes** following the project's coding conventions.
3. **Run the build** to ensure zero warnings and zero errors:
   ```bash
   dotnet build
   ```
4. **Submit a pull request** with a clear description of the change.

## Development Setup

See [docs/development.md](docs/development.md) for setup instructions.

## Code Standards

- File-scoped namespaces
- Clean Architecture: Domain → Application → Infrastructure → Api
- Result pattern for all operations
- Custom CQRS without MediatR
- Prefer `record` over `class` for DTOs and entities

## Pull Request Guidelines

- Keep PRs focused on a single concern
- Write clear commit messages following conventional commits
- Ensure the build passes with zero warnings
- Update documentation if adding or changing features

## Reporting Issues

Open a GitHub issue with:
- A clear title and description
- Steps to reproduce (if bug)
- Expected vs actual behavior

## Code of Conduct

This project follows the [Contributor Covenant](CODE_OF_CONDUCT.md). Be respectful and inclusive.
