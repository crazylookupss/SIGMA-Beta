# SIGMA API Reference

| Metadata | Value |
|----------|-------|
| **Version** | 1.0.0 |
| **Base URL** | `http://localhost:5000/api/v1` |
| **Last Updated** | 2026-05-22 |

---

## 1. Authentication

All endpoints (except `/health`) require authentication. Choose one:

| Scheme | Header | Example |
|--------|--------|---------|
| API Key | `X-API-Key: <key>` | `X-API-Key: sigma-dev-key` |
| JWT Bearer | `Authorization: Bearer <token>` | `Authorization: Bearer eyJ...` |

---

## 2. Health Check

Check if the service is running.

```
GET /api/v1/health
```

**Auth:** None (anonymous)

**Response 200:**
```json
{
  "status": "healthy",
  "timestamp": "2026-05-22T10:00:00Z"
}
```

---

## 3. Entra — Users

### 3.1 List Users

```
GET /api/v1/entra/users
```

**Query Parameters:**

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `$select` | string | Comma-separated properties | `$select=id,displayName,mail` |
| `$filter` | string | OData filter expression | `$filter=startswith(displayName,'A')` |
| `$top` | int | Page size (max 999) | `$top=50` |
| `$skip` | int | Results to skip | `$skip=100` |
| `$count` | bool | Include total count | `$count=true` |

**Example Request:**
```bash
curl -H "X-API-Key: sigma-dev-key" \
  "http://localhost:5000/api/v1/entra/users?$top=10&$count=true&$select=id,displayName,userPrincipalName"
```

**Response 200:**
```json
{
  "data": [
    {
      "id": "aaaaaaaa-0000-1111-2222-bbbbbbbbbbbb",
      "displayName": "John Doe",
      "userPrincipalName": "john.doe@contoso.com",
      "givenName": "John",
      "surname": "Doe",
      "jobTitle": "Software Engineer",
      "mail": "john.doe@contoso.com",
      "mobilePhone": "+1 555-0123",
      "officeLocation": "Building 3",
      "preferredLanguage": "en-US",
      "businessPhone": "+1 555-0124",
      "accountEnabled": true,
      "userType": "Member"
    }
  ],
  "@nextLink": "https://graph.microsoft.com/v1.0/users?$top=10&$skiptoken=X",
  "count": 345
}
```

### 3.2 Get User by ID

```
GET /api/v1/entra/users/{id}
```

**Parameters:**

| Parameter | Type | Location | Description |
|-----------|------|----------|-------------|
| `id` | string | Path | User object ID (GUID) |
| `$select` | string | Query | Comma-separated properties |

**Example Request:**
```bash
curl -H "X-API-Key: sigma-dev-key" \
  "http://localhost:5000/api/v1/entra/users/aaaaaaaa-0000-1111-2222-bbbbbbbbbbbb?$select=id,displayName,mail"
```

**Response 200:**
```json
{
  "data": {
    "id": "aaaaaaaa-0000-1111-2222-bbbbbbbbbbbb",
    "displayName": "John Doe",
    "userPrincipalName": "john.doe@contoso.com",
    "givenName": "John",
    "surname": "Doe",
    "jobTitle": "Software Engineer",
    "mail": "john.doe@contoso.com",
    "mobilePhone": "+1 555-0123",
    "officeLocation": "Building 3",
    "preferredLanguage": "en-US",
    "businessPhone": "+1 555-0124",
    "accountEnabled": true,
    "userType": "Member"
  }
}
```

**Response 404:**
```json
{
  "type": "https://tools.ietf.org/html/rfc9457",
  "title": "User.NotFound",
  "status": 404,
  "detail": "User with id 'aaaaaaaa-0000-1111-2222-bbbbbbbbbbbb' not found."
}
```

---

## 4. Entra — Groups

### 4.1 List Groups

```
GET /api/v1/entra/groups
```

**Query Parameters:** Same as List Users (`$select`, `$filter`, `$top`, `$skip`, `$count`)

**Example Request:**
```bash
curl -H "X-API-Key: sigma-dev-key" \
  "http://localhost:5000/api/v1/entra/groups?$filter=securityEnabled eq true&$top=20"
```

**Response 200:**
```json
{
  "data": [
    {
      "id": "cccccccc-2222-3333-4444-dddddddddddd",
      "displayName": "Engineering Team",
      "description": "All engineering staff",
      "mail": "engineering@contoso.com",
      "mailEnabled": false,
      "securityEnabled": true,
      "mailNickname": "Engineering",
      "groupTypes": ["Unified"],
      "visibility": "Public",
      "createdDateTime": "2024-01-15T08:00:00Z",
      "memberCount": 42
    }
  ],
  "@nextLink": "https://graph.microsoft.com/v1.0/groups?$top=20&$skiptoken=X",
  "count": 15
}
```

### 4.2 Get Group by ID

```
GET /api/v1/entra/groups/{id}
```

**Parameters:**

| Parameter | Type | Location | Description |
|-----------|------|----------|-------------|
| `id` | string | Path | Group object ID (GUID) |
| `$select` | string | Query | Comma-separated properties |

---

## 5. Entra — Service Principals (Enterprise Apps)

### 5.1 List Service Principals

```
GET /api/v1/entra/service-principals
```

**Query Parameters:** Same as List Users (`$select`, `$filter`, `$top`, `$skip`, `$count`)

**Example Request:**
```bash
curl -H "X-API-Key: sigma-dev-key" \
  "http://localhost:5000/api/v1/entra/service-principals?$select=id,displayName,appId,accountEnabled&$top=10"
```

**Response 200:**
```json
{
  "data": [
    {
      "id": "eeeeeeee-4444-5555-6666-ffffffffffff",
      "appId": "11111111-2222-3333-4444-555555555555",
      "displayName": "AzurePortal",
      "appDisplayName": "Azure Portal",
      "servicePrincipalType": "Application",
      "accountEnabled": true,
      "publisherName": "Microsoft Services",
      "signInAudience": "AzureADMultipleOrgs",
      "tags": ["WindowsAzureActiveDirectoryIntegratedApp"],
      "appOwnerOrganizationId": "00000000-0000-0000-0000-000000000000",
      "createdDateTime": "2023-06-01T12:00:00Z"
    }
  ],
  "@nextLink": null,
  "count": 88
}
```

### 5.2 Get Service Principal by ID

```
GET /api/v1/entra/service-principals/{id}
```

**Parameters:**

| Parameter | Type | Location | Description |
|-----------|------|----------|-------------|
| `id` | string | Path | Service principal object ID (GUID) |
| `$select` | string | Query | Comma-separated properties |

---

## 6. Error Responses

All errors follow [RFC 9457](https://tools.ietf.org/html/rfc9457) Problem Details format:

| HTTP Status | Error Code | Description |
|-------------|------------|-------------|
| 400 | `Validation.*` | Invalid request parameters |
| 401 | — | Missing or invalid authentication |
| 403 | — | Insufficient permissions |
| 404 | `*.NotFound` | Resource not found |
| 502 | `GraphError.*` | Microsoft Graph API error |

**Generic error response:**
```json
{
  "type": "https://tools.ietf.org/html/rfc9457",
  "title": "User.NotFound",
  "status": 404,
  "detail": "User with id 'aaaaaaaa-0000-1111-2222-bbbbbbbbbbbb' not found."
}
```

---

## 7. Rate Limiting

Rate limits are inherited from Microsoft Graph API:

| Operation | Limit |
|-----------|-------|
| List queries | 100 requests per 10 seconds per app per tenant |
| Item queries | 1000 requests per 10 seconds per app per tenant |

---

## 8. Change Log

| Date | Version | Author | Changes |
|------|---------|--------|---------|
| 2026-05-22 | 1.0.0 | SIGMA Team | Initial API reference |
