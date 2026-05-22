# SIGMA Deployment Guide

| Metadata | Value |
|----------|-------|
| **Version** | 1.0.0 |
| **Last Updated** | 2026-05-22 |
| **Owner** | SIGMA Team |

---

## 1. Prerequisites

| Dependency | Version | Purpose |
|-----------|---------|---------|
| .NET SDK | 10.0.x (LTS) | Build and run the application |
| Visual Studio | 2026 (recommended) | Development IDE |
| Git | Latest | Version control |
| Entra ID subscription | Any | Microsoft Graph access |
| Redis (optional) | 6.x+ | L2 cache for HybridCache |

---

## 2. Local Development Setup

### 2.1 One-Time Setup

```bash
# 1. Clone repository
git clone <repo-url>
cd SIGMA

# 2. Restore dependencies
dotnet restore

# 3. Build
dotnet build

# 4. Configure secrets
dotnet user-secrets init --project src/SIGMA.Api
dotnet user-secrets set "Entra:TenantId" "<your-tenant-id>" --project src/SIGMA.Api
dotnet user-secrets set "Entra:ClientId" "<your-client-id>" --project src/SIGMA.Api
dotnet user-secrets set "Entra:ClientSecret" "<your-client-secret>" --project src/SIGMA.Api
dotnet user-secrets set "Authentication:ApiKey" "<your-api-key>" --project src/SIGMA.Api
```

### 2.2 Run

```bash
dotnet run --project src/SIGMA.Api
```

The API starts on `http://localhost:5000`. Open `http://localhost:5000/scalar` for the interactive API reference.

### 2.3 Configuration Files

| File | Purpose | In Git? |
|------|---------|---------|
| `appsettings.json` | Shared defaults | Yes |
| `appsettings.Development.json` | Local overrides (local dev) | **No** (in .gitignore) |
| User Secrets | Secrets for local dev | No |

---

## 3. Configuration Reference

### 3.1 Entra Settings (`Entra` section)

```json
{
  "Entra": {
    "TenantId": "00000000-0000-0000-0000-000000000000",
    "ClientId": "11111111-1111-1111-1111-111111111111",
    "ClientSecret": "",
    "Scopes": ["https://graph.microsoft.com/.default"]
  }
}
```

| Property | Required | Description |
|----------|----------|-------------|
| `TenantId` | Yes | Entra directory (tenant) ID |
| `ClientId` | Yes | App registration client ID |
| `ClientSecret` | Yes | App registration client secret |
| `Scopes` | Yes | Graph API scopes (default: `https://graph.microsoft.com/.default`) |

### 3.2 Authentication Settings (`Authentication` section)

```json
{
  "Authentication": {
    "ApiKey": "change-me-in-production",
    "Jwt": {
      "Authority": "https://login.microsoftonline.com/<tenant-id>",
      "Audience": "https://sigma-api"
    }
  }
}
```

### 3.3 Caching Settings (future)

```json
{
  "HybridCache": {
    "DefaultEntryExpiration": "00:05:00",
    "MaximumPayloadBytes": 1048576
  }
}
```

---

## 4. Production Deployment

### 4.1 Recommended: Azure App Service

1. **Create App Service** in Azure Portal
   - Runtime stack: .NET 10
   - Region: Choose closest to your data

2. **Configure Application Settings** (App Settings blade):
   ```
   Entra__TenantId = <value>
   Entra__ClientId = <value>
   Entra__ClientSecret = <value>
   Authentication__ApiKey = <value>
   ```

3. **Deploy**:
   ```bash
   dotnet publish src/SIGMA.Api -c Release -o ./publish
   # Zip publish folder and deploy via Azure CLI / GitHub Actions
   ```

4. **Apply Managed Identity** (preferred over client secret):
   - Enable System-Assigned Managed Identity on the App Service
   - Remove `ClientSecret` from config
   - Update `GraphClientService.cs` to use `DefaultAzureCredential`

### 4.2 Alternative: Docker / Container Apps

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
ENTRYPOINT ["dotnet", "SIGMA.Api.dll"]
```

### 4.3 Alternative: On-Premises / Windows Server

```bash
dotnet publish src/SIGMA.Api -c Release -r win-x64 --self-contained -o ./publish
# Copy ./publish to target server
# Run: dotnet SIGMA.Api.dll
# Configure as Windows Service using sc.exe
```

---

## 5. CI/CD Pipeline (GitHub Actions)

### 5.1 CI Pipeline (PRs)

```yaml
# .github/workflows/ci.yml
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
      - run: dotnet test --no-build -c Release
      - run: dotnet format --verify-no-changes
```

### 5.2 CD Pipeline (Merge to main)

```yaml
# .github/workflows/cd.yml
name: CD
on:
  push:
    branches: [main]
jobs:
  deploy:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"
      - run: dotnet publish src/SIGMA.Api -c Release -o publish
      - uses: azure/webapps-deploy@v3
        with:
          app-name: sigma-api
          slot-name: production
          package: ./publish
```

---

## 6. Environment Matrix

| Environment | Purpose | Config Source | Cache | Auth Method |
|-------------|---------|--------------|-------|-------------|
| `Development` | Local dev | User Secrets / appsettings.Development.json | In-memory only | API Key |
| `Staging` | Pre-prod validation | App Settings / Key Vault | Redis optional | API Key + JWT |
| `Production` | Live | Azure Key Vault | Redis required | JWT only |

---

## 7. Monitoring & Logging

| Tool | Purpose | How to Enable |
|------|---------|--------------|
| OpenTelemetry | Traces, metrics, logs | Add `OpenTelemetry.Extensions.Hosting` package |
| Application Insights | Azure monitoring | Add `Azure.Monitor.OpenTelemetry.AspNetCore` |
| Console logging | Local debugging | Built-in — log level in `appsettings.json` |

---

## 8. Change Log

| Date | Version | Author | Changes |
|------|---------|--------|---------|
| 2026-05-22 | 1.0.0 | SIGMA Team | Initial deployment guide |
