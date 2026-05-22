# SIGMA

> **S**ecure **I**dentity **G**ateway & **M**anagement **A**PI
>
> Enterprise-grade wrapper API for IAM, IGA, and ITSM platforms.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![Build](https://img.shields.io/badge/build-passing-brightgreen)
![Status](https://img.shields.io/badge/status-development-yellow)
![License](https://img.shields.io/badge/license-MIT-blue)

---

## Overview

SIGMA is a **unified API abstraction layer** for identity and access management platforms. It provides a consistent REST interface across multiple providers (Entra ID, Okta, ServiceNow, etc.) with a **read-only, visibility-first** approach.

**Phase 1** targets Microsoft Entra ID with the following capabilities:

| Resource | Endpoint | Description |
|----------|----------|-------------|
| Users | `GET /api/v1/entra/users` | Paginated list with OData filtering |
| Users | `GET /api/v1/entra/users/{id}` | Single user details |
| Groups | `GET /api/v1/entra/groups` | Paginated list with OData filtering |
| Groups | `GET /api/v1/entra/groups/{id}` | Single group details |
| Service Principals | `GET /api/v1/entra/service-principals` | Paginated list of enterprise apps |
| Service Principals | `GET /api/v1/entra/service-principals/{id}` | Single service principal details |

---

## Architecture

```
┌─────────────┐     ┌──────────────┐     ┌────────────────┐     ┌──────────┐
│  SIGMA.Api  │────▶│  Application │────▶│ Infrastructure │────▶│  Domain  │
│ (Endpoints) │     │   (CQRS)     │     │   (Graph API)  │     │(Entities)│
└─────────────┘     └──────────────┘     └────────────────┘     └──────────┘
```

- **Clean Architecture** with 4 layers (Domain → Application → Infrastructure → Api)
- **Custom CQRS** without MediatR (no licensing risk)
- **Result pattern** with typed errors mapping to HTTP status codes
- **Direct REST calls** to Microsoft Graph (no SDK dependency)

---

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Microsoft Entra ID app registration with read-only permissions

### Setup

```bash
# Clone and restore
git clone <repo-url>
cd SIGMA
dotnet restore

# Configure Entra credentials
dotnet user-secrets init --project src/SIGMA.Api
dotnet user-secrets set "Entra:TenantId" "<your-tenant-id>" --project src/SIGMA.Api
dotnet user-secrets set "Entra:ClientId" "<your-client-id>" --project src/SIGMA.Api
dotnet user-secrets set "Entra:ClientSecret" "<your-client-secret>" --project src/SIGMA.Api
dotnet user-secrets set "Authentication:ApiKey" "<your-api-key>" --project src/SIGMA.Api

# Run
dotnet run --project src/SIGMA.Api
```

### Verify

```bash
curl -H "X-API-Key: <your-api-key>" http://localhost:5000/api/v1/health
```

---

## API Documentation

Two interactive API reference UIs are available in development mode:

| Tool | URL | Description |
|------|-----|-------------|
| **Swagger UI** | `http://localhost:5000/swagger` | Classic Swagger interface |
| **Scalar** | `http://localhost:5000/scalar` | Modern, feature-rich interface |

Both read from the same OpenAPI spec at `/openapi/v1.json`.

---

## Authentication

| Scheme | Header | Example |
|--------|--------|---------|
| API Key | `X-API-Key: <key>` | Internal tools, scripts |
| JWT Bearer | `Authorization: Bearer <token>` | Enterprise services |

---

## Project Structure

```
SIGMA/
├── src/
│   ├── SIGMA.Api/              # Minimal API endpoints, middleware, auth
│   ├── SIGMA.Application/      # CQRS handlers, queries, responses
│   ├── SIGMA.Domain/           # Entities, result pattern, error types
│   └── SIGMA.Infrastructure/   # Graph REST client, configuration
├── tests/                      # Unit & integration tests (future)
├── docs/
│   ├── architecture.md
│   ├── api-reference.md
│   ├── authentication.md
│   ├── deployment.md
│   ├── providers.md
│   └── development.md
├── .github/workflows/          # CI/CD pipelines (future)
├── .gitignore
├── SIGMA.slnx
└── README.md
```

---

## Documentation

Detailed documentation is available in the `docs/` directory:

| Document | Description |
|----------|-------------|
| [Architecture](docs/architecture.md) | Layer design, CQRS pattern, error handling |
| [API Reference](docs/api-reference.md) | Full endpoint specs with examples |
| [Authentication](docs/authentication.md) | Setup guide for API Key and JWT |
| [Deployment](docs/deployment.md) | Local, Azure, and Docker deployment |
| [Providers](docs/providers.md) | Adding new IAM providers |
| [Development](docs/development.md) | Coding conventions, Git workflow, testing |

---

## Branching Strategy

**GitHub Flow** — `main` (protected) + short-lived `feature/*` branches merged via squash PRs.

```
main ──────●─────────●─────────●─────────●
             \       / \       / \       /
              feature/  feature/  fix/
              users     groups    pagination
```

---

## Roadmap

| Phase | Scope | Status |
|-------|-------|--------|
| **1** | Microsoft Entra ID (Users, Groups, Service Principals) | ✅ Complete |
| **2** | Okta provider | 📋 Planned |
| **3** | ServiceNow / ITSM providers | 📋 Planned |
| **4** | Aggregated search across providers | 🔮 Future |
| **5** | Write operations with approval workflows | 🔮 Future |

---

## License

This project is licensed under the MIT License.
