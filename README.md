# <img src="docs/images/logo-icon.png" alt="SIGMA Logo" width="36" align="center" /> SIGMA API

> **S**ecure **I**dentity **G**ateway & **M**anagement **A**PI

Enterprise-grade identity gateway API for IAM, IGA, and ITSM platforms.

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
[![CI](https://github.com/crazylookupss/SIGMA-Beta/actions/workflows/ci.yml/badge.svg)](https://github.com/crazylookupss/SIGMA-Beta/actions/workflows/ci.yml)
![Tests](https://img.shields.io/badge/tests-48%20passing-brightgreen)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](CONTRIBUTING.md)
[![Security Policy](https://img.shields.io/badge/security-policy-red.svg)](SECURITY.md)

---

## What is SIGMA?

SIGMA API is an **enterprise identity gateway** that provides a unified REST interface across identity platforms. It uses a **Two-App Registration** security model with **Microsoft.Identity.Web** for JWT validation, **Microsoft Graph** for directory data, and a custom **CQRS** architecture (no MediatR dependency).

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
| **Service Principals** | `GET /api/v1/entra/service-principals/{id}/sso-config` | SAML/OIDC SSO configuration |
| **Service Principals** | `GET /api/v1/entra/service-principals/{id}/proxy-configuration` | Application Proxy settings |
| **Service Principals** | `GET /api/v1/entra/service-principals/{id}/protocol-analysis` | Protocol detection with confidence scoring |
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
┌─────────────────────────────────────────────────────────────────┐
│                         SIGMA Api                                │
│  ┌─────────────┐  ┌──────────────┐  ┌─────────────────────┐    │
│  │  Endpoints   │→│  Application │→│   Infrastructure     │    │
│  │ (Minimal API)│  │   (CQRS)     │  │ (Graph, Cache, Polly)│    │
│  └─────────────┘  └──────────────┘  └─────────────────────┘    │
│         │                                                      │
│         │  Microsoft.Identity.Web (JWT Bearer)                 │
│         │  DelegatedUserPolicy (access_as_user + oid)          │
│         │  SensitiveSelfServicePolicy (write operations)       │
│         │                                                      │
│         │  Outbound: ClientSecretCredential → Microsoft Graph  │
│         │  Circuit Breaker: Polly (50% failure → 30s open)     │
│         │  Cache: ICacheProvider (Memory / Redis)              │
│         └──────────────────────────────────────────────────────│
└─────────────────────────────────────────────────────────────────┘
```

### Design Principles

- **Clean Architecture** — 4 layers (Domain → Application → Infrastructure → Api)
- **Custom CQRS** — No MediatR, no licensing risk
- **Result Pattern** — Typed errors mapping to HTTP status codes
- **Cache-Aside** — `ICacheProvider` with Redis (prod) or in-memory (dev) backends
- **Graph Batching** — `$batch` requests for multi-entity queries (no N+1 fan-out)
- **Circuit Breaker** — Polly v8 prevents hammering a failing Graph API
- **Output Caching** — 60s server-side cache on list endpoints, 300s client cache on detail endpoints

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
# {"status":"healthy","timestamp":"2026-06-02T12:00:00Z"}
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
| **Scalar** | `http://localhost:5107/scalar/v1` | Modern, feature-rich interface |
| **OpenAPI** | `http://localhost:5107/openapi/v1.json` | Raw OpenAPI 3.1 spec |

---

## Security

| Layer | Implementation |
|-------|---------------|
| **Authentication** | Microsoft.Identity.Web JWT Bearer (Two-App Registration) |
| **Authorization** | `DelegatedUserPolicy` (read), `SensitiveSelfServicePolicy` (write) |
| **Rate Limiting** | Fixed window per user (120 req/min), configurable |
| **CORS** | Configurable allowed origins (default: localhost:3000) |
| **Headers** | `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, `X-XSS-Protection`, `Cache-Control` |
| **HTTPS** | HSTS + redirect (non-Development) |
| **Secrets** | `appsettings.Local.json` (gitignored), never committed |
| **Circuit Breaker** | Polly v8 — opens after 50% failure rate, 30s cooldown |
| **Response Compression** | Brotli/deflate enabled for HTTPS |

---

## Performance

| Optimization | Impact |
|-------------|--------|
| **Graph $batch** | 20x fewer API calls for user/owner counts |
| **SemaphoreSlim** | Caps concurrent Graph API calls at 10 |
| **Parallel Enrichment** | `Task.WhenAll` for credential + assignment checks |
| **Output Caching** | 60s server-side cache on list endpoints |
| **Client Cache-Control** | `private, max-age=300` on detail endpoints |
| **Application Cache** | 60s TTL on `GetServicePrincipalsAsync` |
| **Response Compression** | Reduces payload size |

---

## Project Structure

```
SIGMA-Beta/
├── src/
│   ├── SIGMA.Api/              # Minimal API endpoints, middleware, auth
│   ├── SIGMA.Application/      # CQRS handlers, queries, responses
│   ├── SIGMA.Domain/           # Entities, result pattern, error types
│   └── SIGMA.Infrastructure/   # Graph REST client, caching, Polly
├── tests/
│   ├── SIGMA.Application.Tests/    # Unit tests (41 tests)
│   └── SIGMA.Infrastructure.Tests/ # Integration tests (7 tests)
├── docs/
│   ├── architecture.md
│   ├── api-reference.md
│   ├── authentication.md
│   ├── deployment.md
│   ├── providers.md
│   └── development.md
├── .github/
│   ├── workflows/ci.yml        # CI: build, test, audit, secret scan
│   ├── ISSUE_TEMPLATE/
│   └── PULL_REQUEST_TEMPLATE.md
├── Dockerfile
├── docker-compose.yml
├── CONTRIBUTING.md
├── CODE_OF_CONDUCT.md
├── SECURITY.md
├── LICENSE                     # MIT
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

## Testing

```bash
# Run all 48 tests
dotnet test -c Release

# Run with coverage
dotnet test -c Release /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## Related Projects

| Project | Description |
|---------|-------------|
| [SIGMA Web Client](https://github.com/crazylookupss/sigma-next) | Next.js 16 admin dashboard for SIGMA API |

---

## Branching Strategy

**GitHub Flow** — `main` (protected) + short-lived `feature/*` branches merged via squash PRs.

```
main ───────+──────────+──────────+──────────+
             \        / \        / \        /
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
