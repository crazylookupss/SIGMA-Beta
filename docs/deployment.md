# SIGMA Deployment Guide

| Metadata | Value |
|----------|-------|
| **Version** | 2.0.0 |
| **Last Updated** | 2026-05-25 |

---

## 1. Prerequisites

| Dependency | Version | Purpose |
|-----------|---------|---------|
| .NET SDK | 10.0.x | Build and run the application |
| Entra ID subscription | Any | Microsoft Graph access |
| Redis (optional) | 6.x+ | L2 cache for HybridCache |

---

## 2. Local Development Setup

```bash
# 1. Clone and restore
git clone <repo-url>
cd SIGMA
dotnet restore

# 2. Build
dotnet build

# 3. Configure secrets
dotnet user-secrets init --project src/SIGMA.Api
dotnet user-secrets set "AzureAd:TenantId" "<tenant-id>" --project src/SIGMA.Api
dotnet user-secrets set "AzureAd:ClientId" "<api-app-client-id>" --project src/SIGMA.Api
dotnet user-secrets set "AzureAd:ClientSecret" "<api-app-client-secret>" --project src/SIGMA.Api
dotnet user-secrets set "Entra:TenantId" "<tenant-id>" --project src/SIGMA.Api
dotnet user-secrets set "Entra:ClientId" "<api-app-client-id>" --project src/SIGMA.Api
dotnet user-secrets set "Entra:ClientSecret" "<api-app-client-secret>" --project src/SIGMA.Api

# 4. Run
dotnet run --project src/SIGMA.Api
```

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

```yaml
name: CI
on: pull_request
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
      - run: dotnet format --verify-no-changes
```

---

## 7. Change Log

| Date | Version | Changes |
|------|---------|---------|
| 2026-05-25 | 2.0.0 | Updated for Microsoft.Identity.Web auth, added env var reference |
| 2026-05-22 | 1.0.0 | Initial deployment guide |
