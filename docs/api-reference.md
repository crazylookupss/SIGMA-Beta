# SIGMA API Reference

| Metadata | Value |
|----------|-------|
| **Version** | 1.0.0 |
| **Base URL** | `http://localhost:5107` |
| **Last Updated** | 2026-05-25 |

---

## 1. Authentication

All endpoints (except `/api/v1/health`) require a valid JWT Bearer token issued by Microsoft Entra ID.

| Scheme | Header | Example |
|--------|--------|---------|
| JWT Bearer | `Authorization: Bearer <token>` | `Authorization: Bearer eyJ...` |

The token must include the `access_as_user` scope and a valid `oid` (object ID) claim (enforced by `DelegatedUserPolicy`).

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
  "timestamp": "2026-05-25T12:00:00Z"
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
curl -H "Authorization: Bearer <access-token>" \
  "http://localhost:5107/api/v1/entra/users?$top=10&$count=true&$select=id,displayName,userPrincipalName"
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
curl -H "Authorization: Bearer <access-token>" \
  "http://localhost:5107/api/v1/entra/users/aaaaaaaa-0000-1111-2222-bbbbbbbbbbbb?$select=id,displayName,mail"
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
curl -H "Authorization: Bearer <access-token>" \
  "http://localhost:5107/api/v1/entra/groups?$filter=securityEnabled eq true&$top=20"
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

### 5.3 Get Service Principal Dashboard

Returns aggregated metrics and sign-in trend datasets for enterprise applications.

```
GET /api/v1/entra/service-principals/dashboard
```

**Response 200:**
```json
{
  "data": {
    "totalCount": 88,
    "activeCount": 85,
    "disabledCount": 3,
    "donutStatusSegments": [
      { "label": "Active", "value": 85 },
      { "label": "Disabled", "value": 3 }
    ],
    "signInTrendSeries": [
      { "date": "2026-05-24", "count": 120 },
      { "date": "2026-05-25", "count": 145 }
    ]
  }
}
```

### 5.4 Get Service Principal Linked Application

Returns the application registration object linked to the service principal.

```
GET /api/v1/entra/service-principals/{id}/application
```

**Response 200:**
```json
{
  "data": {
    "id": "bbbbbbbb-1111-2222-3333-cccccccccccc",
    "appId": "11111111-2222-3333-4444-555555555555",
    "displayName": "SIGMA API Gateway",
    "signInAudience": "AzureADMyOrg"
  }
}
```

### 5.5 Get Service Principal Assignments

Returns users and groups assigned to this enterprise application.

```
GET /api/v1/entra/service-principals/{id}/assignments
```

**Response 200:**
```json
{
  "data": [
    {
      "id": "asdfasdf-1111-2222-3333-asdfasdfasdf",
      "principalId": "aaaaaaaa-0000-1111-2222-bbbbbbbbbbbb",
      "principalType": "User",
      "principalDisplayName": "John Doe",
      "resourceId": "eeeeeeee-4444-5555-6666-ffffffffffff"
    }
  ]
}
```

### 5.6 Get Service Principal Owners

Returns the owners of the enterprise application.

```
GET /api/v1/entra/service-principals/{id}/owners
```

**Response 200:**
```json
{
  "data": [
    {
      "id": "aaaaaaaa-0000-1111-2222-bbbbbbbbbbbb",
      "displayName": "John Doe",
      "userPrincipalName": "john.doe@contoso.com"
    }
  ]
}
```

### 5.7 Get Service Principal Sign-ins

Returns recent sign-in activities for this enterprise application (requires Entra ID P1/P2).

```
GET /api/v1/entra/service-principals/{id}/signins
```

**Response 200:**
```json
{
  "data": [
    {
      "id": "signin-1111",
      "createdDateTime": "2026-05-25T12:00:00Z",
      "userPrincipalName": "john.doe@contoso.com",
      "appDisplayName": "SIGMA Portal",
      "ipAddress": "192.168.1.1",
      "status": {
        "errorCode": 0,
        "failureReason": null
      }
    }
  ]
}
```

---

## 6. Entra — App Registrations (Applications)

### 6.1 List App Registrations

```
GET /api/v1/entra/applications
```

**Query Parameters:** Same as List Users (`$select`, `$filter`, `$top`, `$skip`, `$count`)

**Response 200:**
```json
{
  "data": [
    {
      "id": "bbbbbbbb-1111-2222-3333-cccccccccccc",
      "appId": "11111111-2222-3333-4444-555555555555",
      "displayName": "SIGMA API Gateway",
      "signInAudience": "AzureADMyOrg",
      "createdDateTime": "2026-01-10T14:30:00Z"
    }
  ],
  "count": 42
}
```

### 6.2 Get App Registration by ID

```
GET /api/v1/entra/applications/{id}
```

**Parameters:**

| Parameter | Type | Location | Description |
|-----------|------|----------|-------------|
| `id` | string | Path | Application object ID (GUID) |
| `$select` | string | Query | Comma-separated properties |

### 6.3 Get App Registration Statistics

Returns aggregated credential health metrics and security stats for all registrations.

```
GET /api/v1/entra/applications/statistics
```

**Response 200:**
```json
{
  "data": {
    "totalCount": 42,
    "activeCount": 40,
    "expiredCredentialsCount": 2,
    "expiringSoonCredentialsCount": 5
  }
}
```

### 6.4 Get App Registration Owners

Returns the list of owners (users and service principals) of the registration.

```
GET /api/v1/entra/applications/{id}/owners
```

**Response 200:**
```json
{
  "data": [
    {
      "id": "aaaaaaaa-0000-1111-2222-bbbbbbbbbbbb",
      "displayName": "John Doe",
      "userPrincipalName": "john.doe@contoso.com"
    }
  ]
}
```

### 6.5 Get App Registration Linked Service Principals

Returns all enterprise applications linked to this app registration.

```
GET /api/v1/entra/applications/{id}/service-principals
```

**Response 200:**
```json
{
  "data": [
    {
      "id": "eeeeeeee-4444-5555-6666-ffffffffffff",
      "appId": "11111111-2222-3333-4444-555555555555",
      "displayName": "AzurePortal"
    }
  ]
}
```

### 6.6 Get App Registration Credentials

Returns the status, expiration dates, and risk assessment for certificate and secret keys.

```
GET /api/v1/entra/applications/{id}/credentials
```

**Response 200:**
```json
{
  "data": {
    "passwordCredentials": [
      {
        "keyId": "key-123",
        "displayName": "prod-secret",
        "startDateTime": "2026-01-01T00:00:00Z",
        "endDateTime": "2026-12-31T23:59:59Z",
        "hint": "abc",
        "isExpired": false,
        "isExpiringSoon": false
      }
    ],
    "keyCredentials": []
  }
}
```

### 6.7 Get App Registration Permissions

Returns required resource access settings (requested API permissions).

```
GET /api/v1/entra/applications/{id}/permissions
```

**Response 200:**
```json
{
  "data": [
    {
      "resourceAppId": "00000003-0000-0000-c000-000000000000",
      "resourceAccess": [
        {
          "id": "311a71aa-2db4-459b-97c1-d4016b341483",
          "type": "Role"
        }
      ]
    }
  ]
}
```

### 6.8 Get App Registration Sign-ins

Returns recent sign-in history events for the app registration (requires P1/P2).

```
GET /api/v1/entra/applications/{id}/signins
```

---

### 6.9 Get App Registration Audit Logs

Returns directory audit log entries for changes and activities on the application registration.

```
GET /api/v1/entra/applications/{id}/audit-logs
```

**Response 200:**
```json
{
  "data": [
    {
      "id": "audit-123",
      "activityDateTime": "2026-05-25T14:00:00Z",
      "activityDisplayName": "Update application",
      "loggedBy": "admin@contoso.com"
    }
  ]
}
```

### 6.10 Get App Registration Manifest

Returns the raw JSON application manifest directly from Microsoft Entra ID.

```
GET /api/v1/entra/applications/{id}/manifest
```

**Response 200:**
```json
{
  "id": "bbbbbbbb-1111-2222-3333-cccccccccccc",
  "appId": "11111111-2222-3333-4444-555555555555",
  "acceptMappedClaims": null,
  "accessTokenAcceptedVersion": 2
}
```

### 6.11 Get App Registration Service Principal Reference

Returns the primary linked service principal reference (enterprise application) in the home tenant.

```
GET /api/v1/entra/applications/{id}/service-principal-ref
```

**Response 200:**
```json
{
  "data": {
    "id": "eeeeeeee-4444-5555-6666-ffffffffffff",
    "appId": "11111111-2222-3333-4444-555555555555",
    "displayName": "AzurePortal"
  }
}
```

---

## 7. Entra — Tenant

### 7.1 Get Tenant Details

Returns live organizational connection metadata and object statistics from the active Entra ID tenant.

```
GET /api/v1/entra/tenant
```

**Response 200:**
```json
{
  "data": {
    "tenantId": "00000000-0000-0000-0000-000000000000",
    "displayName": "Contoso Corp",
    "verifiedDomains": [
      { "name": "contoso.com", "isDefault": true }
    ]
  }
}
```

---

## 8. Error Responses

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

## 9. Rate Limiting

Rate limits are inherited from Microsoft Graph API:

| Operation | Limit |
|-----------|-------|
| List queries | 100 requests per 10 seconds per app per tenant |
| Item queries | 1000 requests per 10 seconds per app per tenant |

---

## 10. Change Log

| Date | Version | Author | Changes |
|------|---------|--------|---------|
| 2026-05-22 | 1.0.0 | SIGMA Team | Initial API reference |
| 2026-05-25 | 1.1.0 | SIGMA Team | Documented dashboard, credential, assignment, and application sub-routes |
