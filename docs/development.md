# SIGMA Development Guide

| Metadata | Value |
|----------|-------|
| **Version** | 1.0.0 |
| **Last Updated** | 2026-05-22 |
| **Owner** | SIGMA Team |

---

## 1. Repository Setup

### 1.1 Clone & Initialize

```bash
git clone <repo-url>
cd SIGMA

# Restore and build
dotnet restore
dotnet build

# Verify build
dotnet build --no-restore -c Release
```

### 1.2 Secrets Configuration

```bash
dotnet user-secrets init --project src/SIGMA.Api
dotnet user-secrets set "Entra:TenantId" "<tenant-id>" --project src/SIGMA.Api
dotnet user-secrets set "Entra:ClientId" "<client-id>" --project src/SIGMA.Api
dotnet user-secrets set "Entra:ClientSecret" "<client-secret>" --project src/SIGMA.Api
dotnet user-secrets set "SwaggerOAuth:ClientId" "<web-app-client-id>" --project src/SIGMA.Api
```

---

## 2. Git Workflow

### Branch Strategy: GitHub Flow

```
main ──────●────────────────●──────────────●──────────
             \              / \            /
              feature/      feature/       fix/
              entra-users   entra-groups   pagination
```

| Branch | Purpose | Source | Merge Strategy |
|--------|---------|--------|---------------|
| `main` | Production-ready, always deployable | — | Protected, no direct pushes |
| `feature/<name>` | New functionality | `main` | Squash merge via PR |
| `fix/<name>` | Bug fixes | `main` | Squash merge via PR |

### Rules
- Branch lifetime: **1-3 days** maximum
- Always open a **Pull Request** (even for solo development)
- PR must pass **CI checks** before merge
- Use **squash merge** to keep history linear
- `main` is **protected** — no direct commits

### Commit Message Convention

```
<type>(<scope>): <description>

feat(entra): add list users endpoint
feat(entra): add user detail endpoint
fix(graph): handle 404 from Graph API
refactor(caching): extract cache configuration
docs(readme): add setup instructions
test(entra): add unit tests for list users handler
chore(deps): update Microsoft.Graph to 6.1.0
```

**Types:** `feat`, `fix`, `refactor`, `docs`, `test`, `chore`, `perf`

---

## 3. Coding Conventions

### 3.1 C# Standards

| Rule | Standard |
|------|----------|
| Target framework | `net10.0` |
| Nullable reference types | Enabled |
| File-scoped namespaces | Required |
| Primary constructors | Preferred for simple DI |
| `record` over `class` | For DTOs, entities, queries |
| Pattern matching | Prefer `is` / `switch` over `if` |
| `[ ]` over `List<T>()` | Collection expressions where possible |

### 3.2 Architecture Rules

- **Domain** has zero NuGet dependencies
- **Application** only references Domain + DI abstractions + FluentValidation
- **Infrastructure** implements interfaces from Application
- **Api** is the composition root — wires everything together
- No circular dependencies
- No service locator pattern (no `BuildServiceProvider()` in DI registration)

### 3.3 Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Projects | `SIGMA.<Layer>` | `SIGMA.Application` |
| Feature folders | `<Verb><Entity>` | `ListUsers`, `GetUser` |
| Query records | `<Verb><Entity>Query` | `ListUsersQuery` |
| Handler classes | `<Verb><Entity>Handler` | `ListUsersHandler` |
| Response DTOs | `<Verb><Entity>Response` | `ListUsersResponse` |

### 3.4 File Organization

Each feature is a vertical slice in its own folder:

```
Features/
└── Entra/
    └── Users/
        ├── ListUsers/
        │   ├── ListUsersQuery.cs       # Query record
        │   ├── ListUsersHandler.cs     # Handler + mapper
        │   └── ListUsersResponse.cs    # Response DTO
        └── GetUser/
            ├── GetUserQuery.cs
            ├── GetUserHandler.cs
            └── GetUserResponse.cs
```

---

## 4. Testing

### 4.1 Test Projects (future)

```
tests/
├── SIGMA.Application.Tests/     # Unit tests for handlers
└── SIGMA.Api.Tests/             # Integration tests for endpoints
```

### 4.2 Testing Conventions

- **Unit tests**: xUnit + NSubstitute for mocking
- **Integration tests**: xUnit + custom WebApplicationFactory
- **Test naming**: `{Method}_{Scenario}_Should{Expected}`
  - `ListUsers_WhenGraphReturnsData_ShouldReturnSuccess()`
  - `GetUser_WhenUserNotFound_ShouldReturnNotFound()`

### 4.3 Running Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test tests/SIGMA.Application.Tests

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

---

## 5. Adding a New Feature

### Checklist

1. [ ] Create query record in `Application/Features/<Provider>/<Entity>/`
2. [ ] Create handler with `IQueryHandler<,>`
3. [ ] Create response DTO
4. [ ] Add endpoint mapping in `Api/Endpoints/<Provider>/`
5. [ ] Wire endpoint in `Program.cs`
6. [ ] Verify build: `dotnet build`
7. [ ] Update API reference docs
8. [ ] Create PR with `feat(<scope>): description`

---

## 6. Useful Commands

```bash
# Watch mode (auto-restart on file changes)
dotnet watch --project src/SIGMA.Api

# Build with verbose output
dotnet build --verbosity normal

# Check for outdated packages
dotnet list package --outdated

# Format code
dotnet format

# Add new NuGet package
dotnet add src/SIGMA.Infrastructure package <PackageName>

# Add new project
dotnet new classlib -n SIGMA.Xyz -o src/SIGMA.Xyz
dotnet sln add src/SIGMA.Xyz/SIGMA.Xyz.csproj
```

---

## 7. IDE Setup

### Visual Studio 2026 (Recommended)

1. Open `SIGMA.slnx`
2. Install workload: **ASP.NET and web development**
3. Enable **EditorConfig** support (built-in)
4. Install extensions:
   - GitHub Extension for Visual Studio

### VS Code

1. Install C# Dev Kit extension
2. Open the `SIGMA` folder
3. Restore: `dotnet restore`

---

## 8. Change Log

| Date | Version | Author | Changes |
|------|---------|--------|---------|
| 2026-05-22 | 1.0.0 | SIGMA Team | Initial development guide |
