# SIGMA Authentication

| Metadata | Value |
|----------|-------|
| **Version** | 1.0.0 |
| **Last Updated** | 2026-05-22 |
| **Owner** | SIGMA Team |

---

## 1. Overview

SIGMA supports two authentication schemes for inbound requests:

| Scheme | Method | Best For |
|--------|--------|----------|
| **API Key** | `X-API-Key` header | Internal tools, scripts, CI/CD pipelines |
| **JWT Bearer** | `Authorization: Bearer` header | Enterprise services with OAuth2 infrastructure |

Both schemes are evaluated; a request passes if **either** is valid.

---

## 2. API Key Authentication

### 2.1 Configuration

Set the API key in `appsettings.json`:

```json
{
  "Authentication": {
    "ApiKey": "your-strong-api-key-here"
  }
}
```

**Security rules:**
- Never commit the API key to source control
- Use `appsettings.Development.json` for local dev (already in `.gitignore`)
- For production, use environment variables or a secrets manager

### 2.2 Usage

Include the key in every request header:

```bash
curl -H "X-API-Key: your-strong-api-key-here" \
  http://localhost:5000/api/v1/entra/users?$top=5
```

### 2.3 Implementation

The `ApiKeyAuthenticationHandler` in `SIGMA.Api/Authentication/`:

1. Reads `X-API-Key` from request headers
2. Compares against configured key (constant-time comparison)
3. Creates a `ClaimsPrincipal` with `AuthenticationMethod` claim
4. Returns `AuthenticateResult.Success` or `.Fail()`

---

## 3. JWT Bearer Authentication

### 3.1 Configuration

```json
{
  "Authentication": {
    "Jwt": {
      "Authority": "https://login.microsoftonline.com/<your-tenant-id>",
      "Audience": "https://<your-api-app-id-uri>"
    }
  }
}
```

### 3.2 Obtaining a Token

**Client Credentials Flow (service-to-service):**

```bash
curl -X POST https://login.microsoftonline.com/<tenant-id>/oauth2/v2.0/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=<client-id>" \
  -d "client_secret=<client-secret>" \
  -d "scope=https://<your-api-app-id-uri>/.default" \
  -d "grant_type=client_credentials"
```

**Response:**
```json
{
  "token_type": "Bearer",
  "expires_in": 3599,
  "access_token": "eyJ..."
}
```

### 3.3 Usage

```bash
curl -H "Authorization: Bearer eyJ..." \
  http://localhost:5000/api/v1/entra/users?$top=5
```

---

## 4. Entra ID App Registration Setup

To allow SIGMA to query Microsoft Graph, create an **App Registration** in Entra ID:

### 4.1 Registration Steps

1. Navigate to **Azure Portal → Microsoft Entra ID → App registrations**
2. Click **New registration**
   - Name: `SIGMA-Prod` (or `SIGMA-Dev` for development)
   - Supported account types: **Accounts in this organizational directory only** (single tenant)
   - Redirect URI: (leave blank)
3. Click **Register**
4. Note the **Application (client) ID** and **Directory (tenant) ID**

### 4.2 API Permissions (Application Permissions)

Add the following **Application permissions** (not delegated):

| Permission | Type | Required For |
|-----------|------|-------------|
| `User.Read.All` | Application | List/Get users |
| `Group.Read.All` | Application | List/Get groups |
| `Application.Read.All` | Application | List/Get service principals |
| `Directory.Read.All` | Application | Cross-directory queries |

**Grant admin consent** after adding permissions — this is a one-time step.

### 4.3 Client Secret

1. Navigate to **Certificates & secrets → Client secrets**
2. Click **New client secret**
3. Set description and expiration (recommended: 6 months, with calendar reminder for rotation)
4. Copy the **Value** immediately — it won't be shown again

### 4.4 Configuration File

Populate `appsettings.Development.json`:

```json
{
  "Entra": {
    "TenantId": "<your-tenant-id>",
    "ClientId": "<your-client-id>",
    "ClientSecret": "<your-client-secret>",
    "Scopes": ["https://graph.microsoft.com/.default"]
  }
}
```

### 4.5 Security Checklist

- [ ] Client secret stored in secure location (Azure Key Vault for production)
- [ ] Secret rotation schedule defined (every 6 months)
- [ ] App permissions scoped to read-only
- [ ] Admin consent granted
- [ ] `appsettings.Development.json` in `.gitignore`
- [ ] Production uses Managed Identity (preferred) or environment variables

---

## 5. Common Error Scenarios

| Error | Likely Cause | Solution |
|-------|-------------|----------|
| `401 Unauthorized` | Missing or invalid `X-API-Key` | Check header name and value |
| `401 Unauthorized` | Expired JWT | Obtain a new token |
| `403 Forbidden` | Insufficient Graph permissions | Check app permissions in Entra |
| `502 Bad Gateway` | Invalid Entra credentials | Verify TenantId, ClientId, ClientSecret |

---

## 6. Change Log

| Date | Version | Author | Changes |
|------|---------|--------|---------|
| 2026-05-22 | 1.0.0 | SIGMA Team | Initial authentication docs |
