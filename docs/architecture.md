# SIGMA Architecture

| Metadata | Value |
|----------|-------|
| **Version** | 1.0.0 |
| **Last Updated** | 2026-05-22 |
| **Owner** | SIGMA Team |

---

## 1. Overview

SIGMA is a **wrapper API** that abstracts IAM/IGA/ITSM platform specifics behind a unified REST interface. Phase 1 targets **Microsoft Entra ID** with a read-only, visibility-first approach.

### Core Principles

- **Read-only first** — all current operations are GET-only
- **Provider abstraction** — each identity provider is a separate vertical slice
- **Enterprise readiness** — structured logging, typed errors, dual auth, RFC 9457 problem details
- **No vendor lock-in** — custom CQRS (no MediatR), REST-based Graph client (no SDK dependency)

---

## 2. Solution Architecture

```
┌──────────────────────────────────────────────────────────┐
│                     Consumer / Client                     │
│              (Internal tool, script, service)              │
└──────────────────────────┬───────────────────────────────┘
                           │ HTTPS / JSON
                           │ Auth: X-API-Key | JWT Bearer
                           ▼
┌──────────────────────────────────────────────────────────┐
│  SIGMA.Api (Presentation Layer)                          │
│                                                          │
│  ┌─────────────┐  ┌─────────────────────────────────┐   │
│  │ Auth        │  │ Minimal API Endpoints            │   │
│  │ Handlers    │  │ /api/v1/entra/users              │   │
│  │ (JWT+APIKey)│  │ /api/v1/entra/groups             │   │
│  └─────────────┘  │ /api/v1/entra/service-principals │   │
│                   └────────────┬────────────────────┘   │
│  ┌─────────────────────────────┴────────────────────┐   │
│  │ Middleware                                        │   │
│  │ - ExceptionHandlingMiddleware                     │   │
│  │ - RequestLoggingMiddleware (future)               │   │
│  └──────────────────────────────────────────────────┘   │
└──────────────────────┬───────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────┐
│  SIGMA.Application (Use Case Layer)                      │
│                                                          │
│  ┌──────────────────────────────────────────────────┐   │
│  │ CQRS Dispatcher                                   │   │
│  │                                                   │   │
│  │  Query  ──►  QueryHandler  ──►  Response DTO      │   │
│  │         (assembly-scanned, auto-registered)        │   │
│  └──────────────────────────────────────────────────┘   │
│                                                          │
│  ┌──────────────────────────────────────────────────┐   │
│  │ Abstractions (Interfaces)                         │   │
│  │ - IQuery<TResponse>                               │   │
│  │ - IQueryHandler<TQuery, TResponse>                │   │
│  │ - IQueryDispatcher                                │   │
│  │ - IGraphClientService                             │   │
│  └──────────────────────────────────────────────────┘   │
└──────────────────────┬───────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────┐
│  SIGMA.Infrastructure (External Dependencies)            │
│                                                          │
│  ┌──────────────────────────────────────────────────┐   │
│  │ GraphClientService                                │   │
│  │ - HttpClient + ClientSecretCredential             │   │
│  │ - Token caching (SemaphoreSlim)                   │   │
│  │ - OData query passthrough                         │   │
│  │ - Maps Graph JSON → Domain entities               │   │
│  └──────────────────────────────────────────────────┘   │
│                                                          │
│  ┌──────────────────────────────────────────────────┐   │
│  │ Resilience (future)                               │   │
│  │ - Polly retry/circuit breaker                     │   │
│  │ - HybridCache (L1 memory + L2 Redis)              │   │
│  └──────────────────────────────────────────────────┘   │
└──────────────────────┬───────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────┐
│  SIGMA.Domain (Enterprise Business Rules)                │
│                                                          │
│  - Result<T> / Error (typed error pattern)               │
│  - Entity records (EntraUser, EntraGroup, ...)           │
│  - Zero external dependencies                            │
└──────────────────────────────────────────────────────────┘
```

### Dependency Rule

> Dependencies flow **inward**. Inner layers never know about outer layers.

```
Domain  ←  Application  ←  Infrastructure  ←  Api
(no deps)   (Domain)       (Application)      (App + Infra)
```

---

## 3. CQRS Pattern (No MediatR)

Each read operation is a **vertical slice**: Query → Handler → Response.

```
Features/
└── Entra/
    ├── Users/
    │   ├── ListUsers/
    │   │   ├── ListUsersQuery.cs       # IQuery<Result<PagedResponse<...>>>
    │   │   ├── ListUsersHandler.cs     # IQueryHandler<...>
    │   │   └── ListUsersResponse.cs    # DTO
    │   └── GetUser/
    │       ├── GetUserQuery.cs
    │       ├── GetUserHandler.cs
    │       └── GetUserResponse.cs
    ├── Groups/
    └── ServicePrincipals/
```

### Handler Registration

Handlers are auto-discovered via assembly scanning in `DependencyInjection.cs`:

```csharp
var handlerTypes = assembly.GetTypes()
    .Where(t => t.GetInterfaces()
        .Any(i => i.IsGenericType
            && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)));
```

No manual registration. Add a new handler file, and it's picked up automatically.

---

## 4. Result Pattern

All operations return `Result<T>`:

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }
    public T? Value { get; }
}
```

Errors are typed and map to HTTP status codes:

| ErrorType | HTTP Status | Example |
|-----------|-------------|---------|
| `NotFound` | 404 | User not found |
| `Validation` | 400 | Invalid query parameter |
| `ExternalService` | 502 | Microsoft Graph unavailable |
| `Failure` | 500 | Unexpected internal error |

---

## 5. Provider Abstraction Pattern

Each IAM provider (Entra, Okta, ServiceNow, etc.) follows the same pattern:

```
Features/
├── Entra/                    # Phase 1
│   ├── Users/
│   ├── Groups/
│   └── ServicePrincipals/
├── Okta/                     # Phase 2 (future)
│   ├── Users/
│   └── Groups/
└── ServiceNow/               # Phase 3 (future)
    └── Users/
```

The `IGraphClientService` interface in `Application.Abstractions` defines the contract. New providers implement this interface in `Infrastructure`.

---

## 6. Error Handling Pipeline

```
Request
  │
  ▼
ExceptionHandlingMiddleware
  │  ┌─── catch unhandled exceptions → RFC 9457 ProblemDetails
  │
  ▼
AuthenticationMiddleware (JWT + API Key)
  │
  ▼
AuthorizationMiddleware
  │
  ▼
Minimal API Endpoint ──► CQRS Handler ──► GraphClientService
                                │
                                ▼
                          Result<T>
                           /     \
                     Success    Failure
                        │          │
                  Results.Ok   Results.NotFound / Results.Problem
```

All errors are returned as [RFC 9457](https://tools.ietf.org/html/rfc9457) Problem Details JSON:

```json
{
  "type": "https://tools.ietf.org/html/rfc9457",
  "title": "User.NotFound",
  "status": 404,
  "detail": "User with id '00000000-0000-0000-0000-000000000000' not found."
}
```

---

## 7. Technology Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| Runtime | .NET | 10.0 (LTS) |
| API Framework | ASP.NET Core Minimal APIs | 10.0 |
| Auth (Entra) | Azure.Identity (ClientSecretCredential) | 1.21+ |
| Auth (Inbound) | JWT Bearer + Custom API Key handler | Built-in |
| Caching | Microsoft.Extensions.Caching.Hybrid | 10.6+ |
| Resilience | Polly.Core | 8.6+ |
| API Docs | Scalar.AspNetCore | 2.14+ |
| Validation | FluentValidation | 12.1+ |
| Testing | xUnit + NSubstitute + FluentAssertions | Latest |
| Serialization | System.Text.Json | Built-in |

---

## 8. Change Log

| Date | Version | Author | Changes |
|------|---------|--------|---------|
| 2026-05-22 | 1.0.0 | SIGMA Team | Initial architecture |
