# SIGMA Deployment Guide

| Metadata | Value |
|----------|-------|
| **Version** | 3.0.0 |
| **Last Updated** | 2026-06-01 |

---

## 1. Prerequisites

| Dependency | Version | Purpose |
|-----------|---------|---------|
| .NET SDK | 10.0.x | Build and run the application |
| Entra ID subscription | Any | Microsoft Graph access |
| Redis (optional) | 6.x+ | Distributed cache for multi-instance deployment |

---

## 2. Local Development Setup

```bash
# 1. Clone and restore
git clone <repo-url>
cd SIGMA
dotnet restore

# 2. Build
dotnet build

# 3. Configure secrets (copy template and fill in values)
cp src/SIGMA.Api/appsettings.json src/SIGMA.Api/appsettings.Local.json
# Edit appsettings.Local.json with your real TenantId, ClientId, ClientSecret

# 4. Run
dotnet run --project src/SIGMA.Api
```

> **Note:** `appsettings.Local.json` is gitignored and overrides the placeholder values in `appsettings.json`. Never commit real secrets.

The API starts on `http://localhost:5107`. Open `http://localhost:5107/scalar` for the interactive API reference.

---

## 3. Configuration

All settings are configurable via environment variables using `__` (double underscore) separators.

### Required Variables

| Variable | Description |
|----------|-------------|
| `AzureAd__TenantId` | Entra ID tenant ID |
| `AzureAd__ClientId` | API app client ID |
| `AzureAd__ClientSecret` | API app client secret |
| `AzureAd__Audience` | API audience (`api://<api-app-client-id>`) |
| `Entra__TenantId` | Entra ID tenant ID (for Graph calls) |
| `Entra__ClientId` | API app client ID (for Graph calls) |
| `Entra__ClientSecret` | API app client secret (for Graph calls) |

### Optional Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `Kestrel__Endpoints__Http__Url` | `http://0.0.0.0:8080` | HTTP binding address |
| `ForwardedHeaders__Enabled` | `true` | Enable behind reverse proxy |
| `Security__SensitiveScope` | `sensitive_selfservice` | Scope for sensitive operations |
| `Redis__Enabled` | `false` | Enable Redis distributed cache |
| `Redis__Connection` | `localhost:6379` | Redis connection string |
| `Redis__InstanceName` | `sigma_` | Redis key prefix |

---

## 4. Azure App Service

1. **Create App Service** with .NET 10 runtime stack
2. **Configure Application Settings**:

   ```
   AzureAd__TenantId = <tenant-id>
   AzureAd__ClientId = <api-app-client-id>
   AzureAd__ClientSecret = <secret>
   Entra__TenantId = <tenant-id>
   Entra__ClientId = <api-app-client-id>
   Entra__ClientSecret = <secret>
   ```

3. **Deploy**:

   ```bash
   dotnet publish src/SIGMA.Api -c Release -o ./publish
   ```

4. **Managed Identity** (preferred over client secret):
   - Enable System-Assigned Managed Identity on the App Service
   - Grant Graph permissions via managed identity
   - Remove `ClientSecret` from config

---

## 5. Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish src/SIGMA.Api -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "SIGMA.Api.dll"]
```

---

## 6. CI/CD (GitHub Actions)

The CI pipeline runs on every push to `main`/`develop` and on pull requests:

| Job | Runner | Steps |
|-----|--------|-------|
| **Build & Verify** | `windows-latest` | Checkout, setup .NET 10, restore, build, test with coverage, format check, dependency audit |
| **Secret Scanning** | `ubuntu-latest` | TruffleHog (verified secrets only) |

```yaml
# Simplified CI workflow
jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"
      - run: dotnet restore
      - run: dotnet build --no-restore -c Release
      - run: dotnet test --no-build -c Release --collect:"XPlat Code Coverage"
      - run: dotnet format --verify-no-changes --no-restore

  security:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: trufflesecurity/trufflehog@main
        with:
          extra_args: --only-verified
```

---

## 7. Change Log

| Date | Version | Changes |
|------|---------|---------|
| 2026-06-01 | 3.0.0 | Added Redis config, appsettings.Local.json pattern, updated CI pipeline |
| 2026-05-25 | 2.0.0 | Updated for Microsoft.Identity.Web auth, added env var reference |
| 2026-05-22 | 1.0.0 | Initial deployment guide |
