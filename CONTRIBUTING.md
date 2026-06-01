# Contributing to SIGMA API

Thank you for your interest in contributing to SIGMA API! We welcome feedback, issue reports, and pull requests from the community.

## Prerequisites

Before you begin, ensure you have:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Git](https://git-scm.com/)
- A code editor (Visual Studio 2026 or VS Code with C# Dev Kit)
- Microsoft Entra ID tenant with admin access (for testing)

## Development Setup

1. **Fork and clone the repository**:
   ```bash
   git clone https://github.com/<your-username>/SIGMA-Beta.git
   cd SIGMA-Beta
   ```

2. **Restore and build**:
   ```bash
   dotnet restore
   dotnet build
   ```

3. **Configure secrets** (copy template and fill in your values):
   ```bash
   cp src/SIGMA.Api/appsettings.json src/SIGMA.Api/appsettings.Local.json
   # Edit appsettings.Local.json with your TenantId, ClientId, ClientSecret
   ```

4. **Run the API**:
   ```bash
   dotnet run --project src/SIGMA.Api
   ```

## How to Contribute

1. **Fork the repository** and create a feature branch from `main`.
2. **Make your changes** following the project's coding conventions.
3. **Run tests** to verify your changes:
   ```bash
   dotnet test -c Release
   ```
4. **Verify build and formatting**:
   ```bash
   dotnet build --no-restore -c Release
   dotnet format --verify-no-changes
   ```
5. **Submit a pull request** with a clear description of the change.

## Code Standards

| Rule | Standard |
|------|----------|
| Target framework | `net10.0` |
| Nullable reference types | Enabled |
| File-scoped namespaces | Required |
| Primary constructors | Preferred for simple DI |
| `record` over `class` | For DTOs, entities, queries |
| Architecture | Clean Architecture (Domain -> Application -> Infrastructure -> Api) |
| CQRS | Custom implementation (no MediatR) |
| Error handling | Result pattern with typed errors |

## Testing

- **Framework**: xUnit with Coverlet for code coverage
- **Test naming**: `{Method}_{Scenario}_Should{Expected}`
- **Run all tests**: `dotnet test`
- **Run with coverage**: `dotnet test --collect:"XPlat Code Coverage"`
- **CI requirement**: All tests must pass before merge

## Commit Convention

We use [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <description>

feat(entra): add list users endpoint
fix(graph): handle 404 from Graph API
docs(readme): add setup instructions
test(entra): add unit tests for list users handler
```

**Types:** `feat`, `fix`, `refactor`, `docs`, `test`, `chore`, `perf`

## Pull Request Guidelines

- Keep PRs focused on a single concern
- Write clear commit messages following conventional commits
- Ensure the build passes with zero warnings
- Update documentation if adding or changing features
- Link related issues in the PR description

## Reporting Issues

Open a GitHub issue using the provided templates:
- **Bug Report**: For reporting bugs
- **Feature Request**: For suggesting new features

Please do **not** report security vulnerabilities through public issues. See [SECURITY.md](SECURITY.md) for responsible disclosure.

## Code of Conduct

This project follows the [Contributor Covenant](CODE_OF_CONDUCT.md). Be respectful and inclusive.

## Questions?

If you have questions about contributing, feel free to open a [Discussion](https://github.com/crazylookupss/SIGMA-Beta/discussions) or reach out to the maintainers.
