# SIGMA API

> **S**ecure **I**dentity **G**ateway & **M**anagement **A**PI
>
> Enterprise-grade identity gateway API for IAM, IGA, and ITSM platforms.

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
[![CI](https://github.com/crazylookupss/SIGMA-Beta/actions/workflows/ci.yml/badge.svg)](https://github.com/crazylookupss/SIGMA-Beta/actions/workflows/ci.yml)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](CONTRIBUTING.md)
[![Security Policy](https://img.shields.io/badge/security-policy-red.svg)](SECURITY.md)

---

## Overview

SIGMA API is an **enterprise identity gateway** that provides a consistent REST interface across identity platforms. It uses a **Two-App Registration** security model with **Microsoft.Identity.Web** for JWT validation, **Microsoft Graph** for directory data, and a custom **CQRS** architecture.

**Phase 1** targets Microsoft Entra ID (read-only, visibility-first):

| Resource | Endpoint | Description |
|----------|----------|-------------|
| **Health** | `GET /api/v1/health` | Service health status & timestamp |
| **Tenant** | `GET /api/v1/entra/tenant` | Tenant metadata, organization info & connection statistics |
| **Users** | `GET /api/v1/entra/users` | Paginated list with OData filtering |
| **Users** | `GET /api/v1/entra/users/{id}` | Single user details |
| **Groups** | `GET /api/v1/entra/groups` | Paginated list with OData filtering |
| **Groups** | `GET /api/v1/entra/groups/{id}` | Single group details |
| **Service Principals** | `GET /api/v1/entra/service-principals` | Paginated list of enterprise apps |
| **Service Principals** | `GET /api/v1/entra/service-principals/{id}` | Single service principal details |
| **Service Principals** | `GET /api/v1/entra/service-principals/dashboard` | Dashboard analytics & metrics |
| **Service Principals** | `GET /api/v1/entra/service-principals/{id}/application` | Linked App Registration details |
| **Service Principals** | `GET /api/v1/entra/service-principals/{id}/assignments` | Users and groups assigned to this app |
| **Service Principals** | `GET /api/v1/entra/service-principals/{id}/owners` | Owners of the enterprise app |
| **Service Principals** | `GET /api/v1/entra/service-principals/{id}/signins` | Recent sign-in activity (requires P1/P2) |
| **App Registrations** | `GET /api/v1/entra/applications` | Paginated list of app registrations |
| **App Registrations** | `GET /api/v1/entra/applications/{id}` | Single app registration details |
| **App Registrations** | `GET /api/v1/entra/applications/statistics` | Aggregated app registration statistics |
| **App Registrations** | `GET /api/v1/entra/applications/{id}/owners` | Owners of the App Registration |
| **App Registrations** | `GET /api/v1/entra/applications/{id}/service-principals` | Linked enterprise applications |
| **App Registrations** | `GET /api/v1/entra/applications/{id}/credentials` | Certificate and secret health/expiry status |
| **App Registrations** | `GET /api/v1/entra/applications/{id}/permissions` | Required API permissions |
| **App Registrations** | `GET /api/v1/entra/applications/{id}/signins` | Recent sign-in activity (requires P1/P2) |
| **App Registrations** | `GET /api/v1/entra/applications/{id}/audit-logs` | Recent directory audit and sign-in logs |
| **App Registrations** | `GET /api/v1/entra/applications/{id}/manifest` | Raw JSON application manifest |
| **App Registrations** | `GET /api/v1/entra/applications/{id}/service-principal-ref` | Primary linked enterprise application reference |
| **Governance** | `GET /api/v1/governance/findings` | All governance findings |
| **Governance** | `GET /api/v1/governance/findings/summary` | Summary counts by severity |
| **Governance** | `GET /api/v1/governance/findings/category/{category}` | Findings filtered by category |
| **Governance** | `GET /api/v1/governance/findings/severity/{severity}` | Findings filtered by severity |

---

## Architecture

```
+-------------+     +--------------+     +----------------+     +----------+
|  SIGMA.Api  |---->|  Application |---->| Infrastructure |---->|  Domain  |
| (Endpoints) |     |   (CQRS)     |     |   (Graph API)  |     |(Entities)|
+-------------+     +--------------+     +----------------+     +----------+
       |
       | Microsoft.Identity.Web (JWT validation)
       v
+----------------------------------------------------------------------+
| Inbound: DelegatedUserPolicy (access_as_user + oid)                  |
| Outbound: ClientSecretCredential -> Microsoft Graph                  |
| Two-App Registration: API App (graph perms) + Client App (user)      |
+----------------------------------------------------------------------+
```

- **Clean Architecture** with 4 layers (Domain -> Application -> Infrastructure -> Api)
- **Custom CQRS** without MediatR (no licensing risk)
- **Result pattern** with typed errors mapping to HTTP status codes
- **Microsoft.Identity.Web** for JWT Bearer validation
- **Two-App Registration** security model
- **ICacheProvider** abstraction with Redis (production) or in-memory (local dev) backends
- **Graph $batch** for optimized multi-entity queries (no N+1 fan-out)

---

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Microsoft Entra ID tenant with admin access
- Two App Registrations (see [Authentication](docs/authentication.md))

### Setup

```bash
# Clone and restore
git clone https://github.com/crazylookupss/SIGMA-Beta.git
cd SIGMA-Beta
dotnet restore

# Configure Entra credentials (copy template and fill in values)
cp src/SIGMA.Api/appsettings.json src/SIGMA.Api/appsettings.Local.json
# Edit appsettings.Local.json with your real TenantId, ClientId, ClientSecret

# Run
dotnet run --project src/SIGMA.Api
```

> **Note:** `appsettings.Local.json` is gitignored and overrides the placeholder values in `appsettings.json`. Never commit real secrets.

### Verify

```bash
curl http://localhost:5107/api/v1/health
# {"status":"healthy","timestamp":"2026-05-24T12:00:00Z"}
```

### Docker

```bash
docker compose up --build
```

---

## API Documentation

Two interactive API reference UIs are available in development mode:

| Tool | URL | Description |
|------|-----|-------------|
| **Swagger UI** | `http://localhost:5107/swagger` | Classic Swagger interface (OAuth2 enabled) |
| **Scalar** | `http://localhost:5107/scalar` | Modern, feature-rich interface |

Both read from the same OpenAPI spec at `/openapi/v1.json`.

---

## Authentication

SIGMA uses a **Two-App Registration** model (see [full authentication docs](docs/authentication.md)):

| Component | App Registration | Purpose |
|-----------|----------------|---------|
| **API** | `SIGMA-Api` | Validates JWT, calls Microsoft Graph |
| **Client** | `SIGMA-Web` | Signs in users, obtains delegated tokens |

**Authorization policy:** `DelegatedUserPolicy` requires:
- `access_as_user` scope in the JWT
- `oid` (object ID) claim present

---

## Project Structure

```
SIGMA-Beta/
├── src/
│   ├── SIGMA.Api/              # Minimal API endpoints, middleware, auth
│   ├── SIGMA.Application/      # CQRS handlers, queries, responses
│   ├── SIGMA.Domain/           # Entities, result pattern, error types
│   └── SIGMA.Infrastructure/   # Graph REST client, caching, configuration
├── tests/
│   ├── SIGMA.Application.Tests/    # Unit tests (protocol detectors, governance)
│   └── SIGMA.Infrastructure.Tests/ # Integration tests (cache providers)
├── docs/
│   ├── architecture.md
│   ├── api-reference.md
│   ├── authentication.md       # -> Two-App Registration model
│   ├── deployment.md
│   ├── providers.md
│   └── development.md
├── .github/
│   ├── workflows/ci.yml        # CI pipeline (build, test, audit, secret scan)
│   ├── ISSUE_TEMPLATE/         # Bug report & feature request
│   └── PULL_REQUEST_TEMPLATE.md
├── Dockerfile
├── docker-compose.yml
├── .gitignore
├── SIGMA.slnx
├── CONTRIBUTING.md
├── CODE_OF_CONDUCT.md
├── SECURITY.md
├── LICENSE
└── README.md
```

---

## Documentation

| Document | Description |
|----------|-------------|
| [Architecture](docs/architecture.md) | Layer design, CQRS pattern, error handling |
| [API Reference](docs/api-reference.md) | Full endpoint specs with examples |
| [Authentication](docs/authentication.md) | Two-App Registration model, setup guide |
| [Deployment](docs/deployment.md) | Local, Azure, and Docker deployment |
| [Providers](docs/providers.md) | Adding new IAM providers |
| [Development](docs/development.md) | Coding conventions, Git workflow, testing |

---

## Related Projects

| Project | Description |
|---------|-------------|
| [SIGMA Web Client](https://github.com/crazylookupss/sigma-next) | Next.js 16 admin dashboard for SIGMA API |

---

## Branching Strategy

**GitHub Flow** -- `main` (protected) + short-lived `feature/*` branches merged via squash PRs.

```
main ------+---------+---------+---------+
             \       / \       / \       /
              feature/  feature/  fix/
              users     groups    pagination
```

---

## Roadmap

| Phase | Scope | Status |
|-------|-------|--------|
| **1** | Microsoft Entra ID (Users, Groups, Service Principals, App Registrations) | Complete |
| **2** | Okta provider | Planned |
| **3** | ServiceNow / ITSM providers | Planned |
| **4** | Aggregated search across providers | Future |
| **5** | Write operations with approval workflows | Future |

---

## Contributing

We welcome contributions! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.
Please report security issues via [SECURITY.md](SECURITY.md).

---

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
