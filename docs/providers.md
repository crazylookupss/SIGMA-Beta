# SIGMA — Provider Abstraction Pattern

| Metadata | Value |
|----------|-------|
| **Version** | 1.0.0 |
| **Last Updated** | 2026-05-22 |
| **Owner** | SIGMA Team |

---

## 1. Concept

SIGMA wraps multiple IAM/IGA/ITSM platforms behind a unified API. Each platform is a **provider** that implements the same interface contracts. This document describes the pattern for adding new providers.

### Current Providers

| Provider | Phase | Status | Resources |
|----------|-------|--------|-----------|
| Microsoft Entra ID | 1 | ✅ Live | Users, Groups, Service Principals |
| Okta | 2 (planned) | 📋 Planned | Users, Groups |
| ServiceNow | 3 (planned) | 📋 Planned | Users |
| Other (future) | TBD | 🔮 Future | TBD |

---

## 2. Adding a New Provider

### 2.1 Step 1: Define the Client Interface

If the new provider supports the same resource types as Entra, reuse `IGraphClientService`. If it has different capabilities, create a new interface:

```csharp
// Application/Abstractions/IOktaClientService.cs
public interface IOktaClientService
{
    Task<Result<PagedResponse<OktaUser>>> GetUsersAsync(
        string? select, string? filter, int? top, int? skip, bool? count,
        CancellationToken ct = default);

    Task<Result<OktaUser>> GetUserByIdAsync(
        string id, string? select, CancellationToken ct = default);
}
```

### 2.2 Step 2: Create Domain Entities

```csharp
// Domain/Entities/OktaUser.cs
public sealed record OktaUser
{
    public string Id { get; init; } = string.Empty;
    public string? Login { get; init; }
    public string? Email { get; init; }
    public string? DisplayName { get; init; }
    public string? Status { get; init; }
    // Provider-specific properties
}
```

### 2.3 Step 3: Implement the Client

```csharp
// Infrastructure/Okta/OktaClientService.cs
internal sealed class OktaClientService : IOktaClientService
{
    private readonly HttpClient _httpClient;

    public OktaClientService(IConfiguration configuration)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(configuration["Okta:Domain"]!)
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("SSWS", configuration["Okta:ApiToken"]);
    }

    // Implement interface methods...
}
```

### 2.4 Step 4: Register Dependencies

```csharp
// Infrastructure/DependencyInjection.cs
services.AddScoped<IOktaClientService, OktaClientService>();
```

### 2.5 Step 5: Create Feature Slices

```
Features/
└── Okta/
    ├── Users/
    │   ├── ListUsers/
    │   │   ├── ListOktaUsersQuery.cs
    │   │   ├── ListOktaUsersHandler.cs
    │   │   └── ListOktaUsersResponse.cs
    │   └── GetUser/
    │       ├── GetOktaUserQuery.cs
    │       ├── GetOktaUserHandler.cs
    │       └── GetOktaUserResponse.cs
```

### 2.6 Step 6: Add Endpoints

```csharp
// Api/Endpoints/Okta/UserEndpoints.cs
internal static class OktaUserEndpoints
{
    public static RouteGroupBuilder MapOktaUserEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/users", /* handler */);
        return group;
    }
}
```

### 2.7 Step 7: Wire in Program.cs

```csharp
// Program.cs
var okta = api.MapGroup("/okta");
okta.MapOktaUserEndpoints();
```

---

## 3. Provider Routing Strategy

Requests are routed by path prefix:

| Path Prefix | Provider |
|-------------|----------|
| `/api/v1/entra/*` | Microsoft Entra ID |
| `/api/v1/okta/*` | Okta (future) |
| `/api/v1/servicenow/*` | ServiceNow (future) |

Each provider group is independent. There is **no cross-provider aggregation** in Phase 1.

---

## 4. Aggregated Search (Future Phase)

Once multiple providers exist, an aggregated search endpoint can be added:

```
GET /api/v1/search/users?q=john.doe@contoso.com
```

This would fan-out to all configured providers and merge results:

```csharp
// Application/Features/Search/SearchUsersHandler.cs
public async Task<Result<PagedResponse<AggregatedUser>>> Handle(...)
{
    var tasks = new[]
    {
        _entra.GetUsersAsync(filter: $"userPrincipalName eq '{query.Q}'"),
        _okta.GetUsersAsync(filter: $"profile.login eq '{query.Q}'"),
    };

    var results = await Task.WhenAll(tasks);
    // Merge and deduplicate by email/UPN
}
```

---

## 5. Provider Comparison

| Feature | Entra ID | Okta (future) | ServiceNow (future) |
|---------|----------|---------------|---------------------|
| Auth Method | ClientSecretCredential | SSWS API Token | Basic Auth / OAuth |
| Base URL | `graph.microsoft.com` | `{domain}.okta.com` | `{instance}.service-now.com` |
| Rate Limit | 100 req/10s | 600 req/min | Varies by tier |
| User ID Format | GUID | String ID | GUID / sys_id |
| Group Support | ✅ | ✅ | ❌ |
| Service Principal Support | ✅ | ✅ (Apps) | ❌ |

---

## 6. Change Log

| Date | Version | Author | Changes |
|------|---------|--------|---------|
| 2026-05-22 | 1.0.0 | SIGMA Team | Initial provider pattern docs |
