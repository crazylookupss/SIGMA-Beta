# SIGMA Authentication

| Metadata | Value |
|----------|-------|
| **Version** | 2.0.0 |
| **Last Updated** | 2026-05-24 |
| **Owner** | SIGMA Team |

---

## 1. Two-App Registration Model

SIGMA uses a **Two-App Registration** architecture for least-privilege security:

| App Registration | Role | Has Graph Permissions? | Auth Method |
|-----------------|------|----------------------|-------------|
| **API App** (`SIGMA-Api`) | Backend API | ✅ Application permissions | `ClientSecretCredential` (Graph calls) |
| **Client App** (`SIGMA-Web`) | Frontend / CLI | ❌ No direct Graph access | User sign-in via MSAL |

```
┌──────────────┐     User Token      ┌──────────────┐    Graph Token     ┌─────────────────┐
│              │   (access_as_user)   │              │ (client_credentials)│                 │
│  Client App  │ ──────────────────▶  │  SIGMA.Api   │ ──────────────────▶│  Microsoft Graph │
│  (Blazor UI) │                     │  (Validator)  │                    │                 │
│              │ ◀────────────────── │              │ ◀──────────────────│                 │
└──────┬───────┘     API Response     └──────────────┘    Graph Data      └─────────────────┘
       │
       │ OIDC Auth Code Flow
       ▼
┌─────────────────────────────────────────────────────────────┐
│  Microsoft Entra ID                                          │
│  - API App exposes: access_as_user (delegated scope)         │
│  - Client App requests: access_as_user on behalf of user     │
│  - API App has: User.Read.All, Group.Read.All (app perms)    │
└─────────────────────────────────────────────────────────────┘
```

### Why Two Apps?

- **Client App** has zero Graph permissions — if compromised, attacker only gets a user token for the API's scope
- **API App** holds all Graph permissions via `ClientSecretCredential` — only the backend can call Graph
- Users authenticate to the **Client App**, not directly to the API
- The API validates that every request has a proper user-delegated token with the required scope

---

## 2. Inbound Authentication (API Validation)

Inbound requests are validated via **JWT Bearer tokens** issued by Microsoft Entra ID.

### 2.1 Configuration

The API authenticates using `Microsoft.Identity.Web` with the `AzureAd` section:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "yourtenant.onmicrosoft.com",
    "TenantId": "<tenant-id>",
    "ClientId": "<api-app-client-id>",
    "ClientSecret": "<api-app-client-secret>",
    "Audience": "api://<api-app-client-id>"
  }
}
```

### 2.2 DelegatedUserPolicy

All protected endpoints require the **`DelegatedUserPolicy`** which validates:

| Requirement | Claim | Description |
|------------|-------|-------------|
| **Scope** | `scp` | Must contain `access_as_user` |
| **Object ID** | `oid` | Must be present (stable user identifier) |

Endpoints that do **not** require this policy:

| Endpoint | Reason |
|----------|--------|
| `POST /api/v1/auth/token` | Bootstrap token acquisition |
| `GET /api/v1/health` | Health probes |

### 2.3 Obtaining a User Token (Client Side)

The Client App acquires a user-delegated token via MSAL (see Client App docs). The token is requested for the scope:

```
api://<api-app-client-id>/access_as_user
```

### 2.4 Using the Token

```bash
curl -H "Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIs..." \
  http://localhost:5107/api/v1/entra/users?$top=5
```

---

## 3. Outbound Authentication (Graph Calls)

The API calls Microsoft Graph using its own credentials, not the user's token.

### 3.1 ClientSecretCredential

```json
{
  "Entra": {
    "TenantId": "<tenant-id>",
    "ClientId": "<api-app-client-id>",
    "ClientSecret": "<api-app-client-secret>",
    "Scopes": ["https://graph.microsoft.com/.default"]
  }
}
```

The `GraphClientService` in `SIGMA.Infrastructure` uses `Azure.Identity.ClientSecretCredential` to acquire tokens for Microsoft Graph.

### 3.2 Required Graph Permissions (Application)

| Permission | Type | Required For |
|-----------|------|-------------|
| `User.Read.All` | Application | List/Get users |
| `Group.Read.All` | Application | List/Get groups |
| `Application.Read.All` | Application | List/Get service principals & app registrations |
| `Directory.Read.All` | Application | Cross-directory queries |

**Grant admin consent** after adding permissions.

---

## 4. Entra ID App Registration Setup

### 4.1 Create/Update the API App

1. Navigate to **Azure Portal → Microsoft Entra ID → App registrations**
2. Click **New registration** (or use existing `SIGMA-Api`)
   - Name: `SIGMA-Api`
   - Supported account types: **Accounts in this organizational directory only** (single tenant)
3. Note the **Application (client) ID** and **Directory (tenant) ID**

#### Expose an API
1. Go to **Expose an API**
2. Set **Application ID URI**: `api://<api-app-client-id>` (use your API's Client ID)
3. Click **Add a scope**:
   - Scope name: `access_as_user`
   - Who can consent: **Admins and users**
   - Admin consent display name: `Access SIGMA API`
   - Admin consent description: `Allows the application to access SIGMA API on behalf of the signed-in user.`
   - State: **Enabled**
4. Click **Add scope**

#### API Permissions (Application)
1. Go to **API Permissions**
2. Click **Add a permission** → **Microsoft Graph** → **Application permissions**
3. Add:
   - `User.Read.All`
   - `Group.Read.All`
   - `Application.Read.All`
   - `Directory.Read.All`
4. Click **Grant admin consent**

#### Client Secret
1. Go to **Certificates & secrets → Client secrets**
2. Click **New client secret**
3. Set description and expiration
4. Copy the value immediately

### 4.2 Create the Client App

1. Navigate to **Azure Portal → Microsoft Entra ID → App registrations**
2. Click **New registration**
   - Name: `SIGMA-Web`
   - Supported account types: **Accounts in this organizational directory only**
   - Redirect URI: **Web** → `https://localhost:7042/signin-oidc`
3. Note the **Application (client) ID**

#### API Permissions (Delegated)
1. Go to **API Permissions**
2. Click **Add a permission** → **APIs my organization uses**
3. Search for your API App (`SIGMA-Api`)
4. Select **Delegated permissions** → `access_as_user`
5. Click **Grant admin consent**

#### Authentication Settings
1. Go to **Authentication**
2. Under **Implicit grant and hybrid flows**: ensure both checkboxes are **unchecked**
3. Under **Front-channel logout URL**: set to `https://localhost:7042/signout-callback-oidc`

---

## 5. Authorization Policies

### DelegatedUserPolicy (Normal Tier)

Applied to all protected endpoints. Requirements:

```csharp
options.AddPolicy("DelegatedUserPolicy", policy =>
{
    policy.RequireScope("access_as_user");
    policy.RequireClaim("oid");
});
```

### SensitiveSelfServicePolicy (Future)

For sensitive operations (delete, write). Requirements (planned):

| Requirement | Detail |
|------------|--------|
| Scopes | `access_as_user` + `sensitive_selfservice` |
| MFA | `amr` claim must contain `mfa` |
| Confirmation | Requires explicit JSON confirmation body |

---

## 6. Environment Configuration

### Development (user-secrets)

```bash
dotnet user-secrets set "AzureAd:TenantId" "<tenant-id>" --project src/SIGMA.Api
dotnet user-secrets set "AzureAd:ClientId" "<api-app-client-id>" --project src/SIGMA.Api
dotnet user-secrets set "AzureAd:ClientSecret" "<api-app-client-secret>" --project src/SIGMA.Api
dotnet user-secrets set "Entra:TenantId" "<tenant-id>" --project src/SIGMA.Api
dotnet user-secrets set "Entra:ClientId" "<api-app-client-id>" --project src/SIGMA.Api
dotnet user-secrets set "Entra:ClientSecret" "<api-app-client-secret>" --project src/SIGMA.Api
```

### Production (Key Vault / Environment Variables)

```bash
# Environment variables
AzureAd__TenantId=<tenant-id>
AzureAd__ClientId=<api-app-client-id>
AzureAd__ClientSecret=<api-app-client-secret>
Entra__TenantId=<tenant-id>
Entra__ClientId=<api-app-client-id>
Entra__ClientSecret=<api-app-client-secret>
```

---

## 7. Security Checklist

- [ ] Two App Registrations created in Entra ID
- [ ] API App: `access_as_user` scope exposed
- [ ] API App: Graph application permissions granted + admin consent
- [ ] Client App: `access_as_user` delegated permission granted + admin consent
- [ ] Client App: redirect URIs configured (HTTPS for production)
- [ ] Client secrets stored in Key Vault (production)
- [ ] Secret rotation schedule defined (every 6 months)
- [ ] `appsettings.Development.json` in `.gitignore`
- [ ] Production uses Managed Identity (preferred) or environment variables

---

## 8. Common Error Scenarios

| Error | Likely Cause | Solution |
|-------|-------------|----------|
| `401 Unauthorized` | Missing/invalid Bearer token | Sign in via Client App |
| `401 Unauthorized` | Token audience mismatch | Check `Audience` in `AzureAd` config matches `api://<client-id>` |
| `403 Forbidden` | Token missing `access_as_user` scope | Check Client App API permissions |
| `403 Forbidden` | Token missing `oid` claim | Ensure user is from the correct tenant |
| `502 Bad Gateway` | Invalid Entra credentials | Verify `Entra:TenantId`, `ClientId`, `ClientSecret` |
| `500 Internal Server Error` | Microsoft.Identity.Web config error | Check `AzureAd` section is populated correctly |

---

## 9. Change Log

| Date | Version | Author | Changes |
|------|---------|--------|---------|
| 2026-05-24 | 2.0.0 | SIGMA Team | Rewrote for Two-App Registration model + DelegatedUserPolicy |
| 2026-05-22 | 1.0.0 | SIGMA Team | Initial authentication docs |
