---
title: "Custom APIs"
description: "Authenticate and call your own protected APIs"
weight: 9
---

# Custom APIs

Learn how to use Entra Auth Cli to authenticate and call your own custom APIs protected by Microsoft Entra ID.

---

## Overview

Entra Auth Cli can generate tokens for any API protected by Entra ID:
- **Internal APIs** - Your organization's microservices
- **Partner APIs** - Third-party services
- **Multi-tenant APIs** - APIs serving multiple organizations

**Authentication scenarios:**
- Service-to-service (client credentials)
- User-delegated (on-behalf-of)
- API chaining (downstream APIs)

---

## Prerequisites

### Register Your API in Azure

1. Go to [Azure Portal](https://portal.azure.com) → **Entra ID** → **App registrations**
2. Click **New registration**
3. Enter name: `MyCustomAPI`
4. Select appropriate account types
5. Click **Register**

### Expose API Scopes

1. In your API app registration, go to **Expose an API**
2. Set **Application ID URI**: `api://your-api-id` (or custom domain)
3. Click **Add a scope**:
   - **Scope name**: `access_as_user` (or custom)
   - **Who can consent**: Admins and users
   - **Display name**: "Access My API"
   - **Description**: "Allows the app to access My API"
4. Click **Add scope**

### Register Client App

1. Create another app registration for the client
2. Go to **API permissions**
3. Click **Add a permission** → **My APIs**
4. Select your API and the scope you created
5. Click **Grant admin consent** (if required)

---

## Service-to-Service (Client Credentials)

### API Configuration

Your API's `appsettings.json`:

```json {linenos=inline}
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-api-client-id",
    "Audience": "api://your-api-id"
  }
}
```

### Create Profile for API Access

```bash {linenos=inline}
entra-auth-cli config create
```

**Configuration:**
```
Profile name: my-api-client
Tenant ID: contoso.onmicrosoft.com
Client ID: <client-app-id>
Authentication method: ClientSecret
Client secret: ****
Scopes: api://your-api-id/.default
```

### Get Token and Call API

```bash {linenos=inline}
# Get token for your API
TOKEN=$(entra-auth-cli get-token -p my-api-client)

# Call your API
curl -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     https://api.mycompany.com/v1/data
```

---

## User-Delegated Access

### API Configuration with Delegated Scopes

```bash {linenos=inline}
entra-auth-cli config create
```

**Configuration:**
```
Profile name: my-api-user
Tenant ID: contoso.onmicrosoft.com
Client ID: <client-app-id>
Authentication method: PasswordlessCertificate (or other user auth)
Scopes: api://your-api-id/access_as_user
Default OAuth2 flow: InteractiveBrowser
```

### Get Token with User Context

```bash {linenos=inline}
# User signs in via browser
entra-auth-cli get-token -p my-api-user

# Token includes user identity and delegated permissions
curl -H "Authorization: Bearer $TOKEN" \
     https://api.mycompany.com/v1/me
```

---

## Common Scenarios

### RESTful API

```bash {linenos=inline}
#!/bin/bash
TOKEN=$(entra-auth-cli get-token -p my-api-client)

# GET request
curl -H "Authorization: Bearer $TOKEN" \
     https://api.mycompany.com/v1/resources

# POST request
curl -X POST \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"name":"example","value":123}' \
     https://api.mycompany.com/v1/resources

# PUT request
curl -X PUT \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"name":"updated","value":456}' \
     https://api.mycompany.com/v1/resources/123

# DELETE request
curl -X DELETE \
     -H "Authorization: Bearer $TOKEN" \
     https://api.mycompany.com/v1/resources/123
```

### GraphQL API

```bash {linenos=inline}
#!/bin/bash
TOKEN=$(entra-auth-cli get-token -p graphql-api)

curl -X POST \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d '{
       "query": "{ users { id name email } }"
     }' \
     https://api.mycompany.com/graphql
```

### gRPC API

```bash {linenos=inline}
#!/bin/bash
TOKEN=$(entra-auth-cli get-token -p grpc-api)

# Using grpcurl with token
grpcurl \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"user_id": "123"}' \
  api.mycompany.com:443 \
  myapi.UserService/GetUser
```

---

## Multi-Environment Setup

### Development Environment

```bash {linenos=inline}
entra-auth-cli config create
# Name: myapi-dev
# Client ID: <dev-client-id>
# Scopes: api://dev-myapi/.default

TOKEN=$(entra-auth-cli get-token -p myapi-dev)
curl -H "Authorization: Bearer $TOKEN" https://dev-api.mycompany.com/health
```

### Staging Environment

```bash {linenos=inline}
entra-auth-cli config create
# Name: myapi-staging
# Client ID: <staging-client-id>
# Scopes: api://staging-myapi/.default

TOKEN=$(entra-auth-cli get-token -p myapi-staging)
curl -H "Authorization: Bearer $TOKEN" https://staging-api.mycompany.com/health
```

### Production Environment

```bash {linenos=inline}
entra-auth-cli config create
# Name: myapi-prod
# Client ID: <prod-client-id>
# Scopes: api://prod-myapi/.default

TOKEN=$(entra-auth-cli get-token -p myapi-prod)
curl -H "Authorization: Bearer $TOKEN" https://api.mycompany.com/health
```

---

## API Testing Scripts

### Basic Health Check

```bash {linenos=inline}
#!/bin/bash
set -euo pipefail

PROFILE="my-api-client"
API_URL="https://api.mycompany.com"

# Get token
TOKEN=$(entra-auth-cli get-token -p "$PROFILE")

# Check health endpoint
response=$(curl -s -w "\n%{http_code}" \
  -H "Authorization: Bearer $TOKEN" \
  "$API_URL/health")

http_code=$(echo "$response" | tail -n1)
body=$(echo "$response" | sed '$d')

if [ "$http_code" = "200" ]; then
  echo "✓ API is healthy"
  echo "$body" | jq
else
  echo "✗ API health check failed: HTTP $http_code"
  exit 1
fi
```

### Integration Test Suite

```bash {linenos=inline}
#!/bin/bash
set -euo pipefail

PROFILE="my-api-test"
API_URL="https://api.mycompany.com/v1"

# Get token once
TOKEN=$(entra-auth-cli get-token -p "$PROFILE")

test_get() {
  echo "Testing GET /resources..."
  response=$(curl -s -w "\n%{http_code}" \
    -H "Authorization: Bearer $TOKEN" \
    "$API_URL/resources")
  
  http_code=$(echo "$response" | tail -n1)
  [ "$http_code" = "200" ] && echo "✓ GET test passed" || echo "✗ GET test failed"
}

test_post() {
  echo "Testing POST /resources..."
  response=$(curl -s -w "\n%{http_code}" \
    -X POST \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d '{"name":"test","value":123}' \
    "$API_URL/resources")
  
  http_code=$(echo "$response" | tail -n1)
  [ "$http_code" = "201" ] && echo "✓ POST test passed" || echo "✗ POST test failed"
}

test_authentication() {
  echo "Testing authentication..."
  response=$(curl -s -w "\n%{http_code}" \
    "$API_URL/resources")
  
  http_code=$(echo "$response" | tail -n1)
  [ "$http_code" = "401" ] && echo "✓ Auth test passed" || echo "✗ Auth test failed"
}

# Run tests
test_get
test_post
test_authentication

echo "All tests completed"
```

---

## PowerShell Integration

```powershell {linenos=inline}
# Get token
$token = entra-auth-cli get-token -p my-api-client

# Set headers
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# GET request
$response = Invoke-RestMethod `
    -Uri "https://api.mycompany.com/v1/resources" `
    -Headers $headers `
    -Method Get

$response | ConvertTo-Json

# POST request
$body = @{
    name = "example"
    value = 123
} | ConvertTo-Json

$response = Invoke-RestMethod `
    -Uri "https://api.mycompany.com/v1/resources" `
    -Headers $headers `
    -Method Post `
    -Body $body

$response | ConvertTo-Json
```

---

## API Scope Patterns

### Default Scope

```bash {linenos=inline}
# Request all permissions exposed by API
entra-auth-cli get-token -p my-api \
  -s "api://your-api-id/.default"
```

### Specific Scopes

```bash {linenos=inline}
# Request specific scopes
entra-auth-cli get-token -p my-api \
  -s "api://your-api-id/read api://your-api-id/write"
```

### Multiple APIs

```bash {linenos=inline}
# Get tokens for multiple APIs
TOKEN_API1=$(entra-auth-cli get-token -p api1-client)
TOKEN_API2=$(entra-auth-cli get-token -p api2-client)

# Call first API
curl -H "Authorization: Bearer $TOKEN_API1" https://api1.mycompany.com/data

# Call second API
curl -H "Authorization: Bearer $TOKEN_API2" https://api2.mycompany.com/data
```

---

## Troubleshooting

### Invalid Audience

**Problem:** API returns 401 with "The audience is invalid"

**Solution:**
- Verify Application ID URI matches in both API and token request
- Check `aud` claim in token: `echo "$TOKEN" | entra-auth-cli inspect -`
- Ensure API is configured to accept tokens with correct audience

### Scope Not Found

**Problem:** `AADSTS70011: The provided value for the input parameter 'scope' is not valid`

**Solution:**
```bash {linenos=inline}
# Verify scope format
# Correct: api://your-api-id/scope-name
# Correct: api://your-api-id/.default

# Check exposed scopes in Azure Portal
entra-auth-cli discover -t your-tenant-id -s "YourAPI*"
```

### Missing Permissions

**Problem:** `Insufficient privileges to complete the operation`

**Solution:**
- Grant API permissions in client app registration
- Ensure admin consent is provided (if required)
- Verify user has necessary roles (for delegated access)

---

## See Also

- [Scopes](/docs/core-concepts/scopes/) - Understanding API scopes
- [Service Principal Authentication](/docs/recipes/service-principals/) - Non-interactive authentication
- [Microsoft Graph](/docs/recipes/microsoft-graph/) - Example API integration
- [Security Hardening](/docs/recipes/security-hardening/) - Secure API access patterns
