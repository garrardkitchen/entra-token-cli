---
title: "Service Principal Authentication"
description: "Authenticate using service principals with client secrets and certificates"
weight: 8
---

# Service Principal Authentication

Learn how to use service principals (app registrations) for non-interactive authentication with Entra Auth Cli.

---

## Overview

Service principals are ideal for:
- **Automated scripts** - No user interaction required
- **CI/CD pipelines** - Secure authentication in build processes
- **Background services** - Long-running applications
- **Server-to-server** - API-to-API authentication

**Authentication methods:**
- Client secrets (shared secrets)
- Certificates (more secure)

---

## Prerequisites

### Create App Registration

1. Go to [Azure Portal](https://portal.azure.com) → **Entra ID** → **App registrations**
2. Click **New registration**
3. Enter a name: `MyServicePrincipal`
4. Select **Accounts in this organizational directory only**
5. Click **Register**

### Grant API Permissions

1. In your app registration, go to **API permissions**
2. Click **Add a permission**
3. Select **Microsoft Graph** (or your API)
4. Choose **Application permissions** (not Delegated)
5. Select required permissions (e.g., `User.Read.All`)
6. Click **Grant admin consent**

---

## Client Secret Authentication

### Step 1: Create Client Secret

In your app registration:
1. Go to **Certificates & secrets**
2. Click **New client secret**
3. Enter description: `CLI Access`
4. Set expiration: 6 months (or as per policy)
5. Click **Add**
6. **Copy the secret value immediately** (you won't see it again)

### Step 2: Create Profile

```bash {linenos=inline}
entra-auth-cli config create
```

**Interactive prompts:**
```
Profile name: my-service-principal
Tenant ID: contoso.onmicrosoft.com
Client ID: 12345678-1234-1234-1234-123456789abc
Authentication method: ClientSecret
Client secret: ****
Scopes: https://graph.microsoft.com/.default
```

### Step 3: Get Token

```bash {linenos=inline}
entra-auth-cli get-token -p my-service-principal
```

**Output:**
```
✓ Token acquired successfully
Expires: 2026-01-01 15:30:00 UTC (59 minutes)
Scopes: https://graph.microsoft.com/.default

eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Ij...
```

---

## Certificate Authentication

### Step 1: Create Certificate

```bash {linenos=inline}
# Create self-signed certificate (for testing)
openssl req -x509 -newkey rsa:4096 -keyout key.pem -out cert.pem -days 365 -nodes \
  -subj "/CN=MyServicePrincipal"

# Create PFX from PEM
openssl pkcs12 -export -out cert.pfx -inkey key.pem -in cert.pem -passout pass:YourPassword
```

**For production:** Use certificates from a trusted CA.

### Step 2: Upload Certificate to Azure

In your app registration:
1. Go to **Certificates & secrets**
2. Click **Upload certificate**
3. Select `cert.pem` (the public key)
4. Add description
5. Click **Add**

### Step 3: Create Profile with Certificate

```bash {linenos=inline}
entra-auth-cli config create
```

**Interactive prompts:**
```
Profile name: my-cert-principal
Tenant ID: contoso.onmicrosoft.com
Client ID: 12345678-1234-1234-1234-123456789abc
Authentication method: Certificate
Certificate path: ./cert.pfx
Cache certificate password? Yes
Certificate password: ****
Scopes: https://graph.microsoft.com/.default
```

### Step 4: Get Token

```bash {linenos=inline}
entra-auth-cli get-token -p my-cert-principal
```

---

## Common Scenarios

### Azure Resource Management

```bash {linenos=inline}
# Create profile for Azure Management
entra-auth-cli config create
# Name: azure-mgmt
# Scope: https://management.azure.com/.default

# Get token and list subscriptions
TOKEN=$(entra-auth-cli get-token -p azure-mgmt)
curl -H "Authorization: Bearer $TOKEN" \
  'https://management.azure.com/subscriptions?api-version=2020-01-01'
```

### Microsoft Graph Operations

```bash {linenos=inline}
# Create profile for Graph API
entra-auth-cli config create
# Name: graph-sp
# Scope: https://graph.microsoft.com/.default

# Get token and list users
TOKEN=$(entra-auth-cli get-token -p graph-sp)
curl -H "Authorization: Bearer $TOKEN" \
  'https://graph.microsoft.com/v1.0/users'
```

### Custom API Access

```bash {linenos=inline}
# Create profile for custom API
entra-auth-cli config create
# Name: custom-api
# Scope: api://12345678-1234-1234-1234-123456789abc/.default

# Get token and call API
TOKEN=$(entra-auth-cli get-token -p custom-api)
curl -H "Authorization: Bearer $TOKEN" \
  https://api.mycompany.com/data
```

---

## Automated Scripts

### Simple Script

```bash {linenos=inline}
#!/bin/bash
set -euo pipefail

# Get token
TOKEN=$(entra-auth-cli get-token -p my-service-principal)

# Use token to call API
curl -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     https://api.example.com/endpoint
```

### Script with Error Handling

```bash {linenos=inline}
#!/bin/bash
set -euo pipefail

get_token() {
  local profile=$1
  local max_retries=3
  local retry=0
  
  while [ $retry -lt $max_retries ]; do
    if token=$(entra-auth-cli get-token -p "$profile" 2>&1); then
      echo "$token"
      return 0
    fi
    echo "Retry $((retry + 1))/$max_retries..." >&2
    retry=$((retry + 1))
    sleep 5
  done
  
  echo "Failed to acquire token" >&2
  return 1
}

# Main
TOKEN=$(get_token "my-service-principal")
curl -H "Authorization: Bearer $TOKEN" https://api.example.com/data
```

---

## CI/CD Integration

### GitHub Actions

```yaml {linenos=inline}
name: Deploy with Service Principal

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Install Entra Auth CLI
        run: dotnet tool install -g EntraAuthCli
      
      - name: Create Profile
        run: |
          # Profile must be created before first use
          # Store profile in repository secrets or create on-the-fly
          echo "Note: Profiles should be pre-created and exported"
      
      - name: Get Access Token
        run: |
          TOKEN=$(entra-auth-cli get-token -p ci-deployment)
          echo "::add-mask::$TOKEN"
          echo "TOKEN=$TOKEN" >> $GITHUB_ENV
      
      - name: Deploy Application
        run: |
          curl -H "Authorization: Bearer $TOKEN" \
               -X POST \
               https://api.myapp.com/deploy
```

### Azure DevOps

```yaml {linenos=inline}
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
  - task: DotNetCoreCLI@2
    displayName: 'Install Entra Auth CLI'
    inputs:
      command: 'custom'
      custom: 'tool'
      arguments: 'install -g EntraAuthCli'
  
  - script: |
      TOKEN=$(entra-auth-cli get-token -p $(ProfileName))
      echo "##vso[task.setvariable variable=AccessToken;issecret=true]$TOKEN"
    displayName: 'Get Access Token'
  
  - script: |
      curl -H "Authorization: Bearer $(AccessToken)" \
           https://api.myapp.com/deploy
    displayName: 'Deploy Application'
```

---

## Security Best Practices

### Secret Rotation

```bash {linenos=inline}
# Rotate client secret regularly
# 1. Create new secret in Azure Portal
# 2. Update profile
entra-auth-cli config edit -p my-service-principal
# Enter new secret when prompted

# 3. Delete old secret in Azure Portal after validation
```

### Use Certificates Over Secrets

```bash {linenos=inline}
# Certificates are more secure:
# - Cannot be easily shared or leaked
# - Rotation is more controlled
# - Better audit trail
entra-auth-cli config create
# Choose Certificate authentication method
```

### Least Privilege

- Grant only required permissions
- Use separate service principals for different purposes
- Regularly audit permissions

```bash {linenos=inline}
# Good: Specific profiles for specific tasks
entra-auth-cli config create # name: graph-readonly
entra-auth-cli config create # name: azure-deployer
entra-auth-cli config create # name: keyvault-reader

# Bad: One profile with all permissions
# Avoid overly permissive service principals
```

---

## Troubleshooting

### Authentication Fails

**Problem:** `AADSTS700016: Application not found`

**Solution:**
- Verify Client ID is correct
- Ensure app registration exists in the tenant
- Check tenant ID matches the app registration

### Insufficient Permissions

**Problem:** `Insufficient privileges to complete the operation`

**Solution:**
- Grant required API permissions in Azure Portal
- Ensure admin consent is granted
- Wait 5-10 minutes for permissions to propagate

### Token Expired

**Problem:** API returns 401 Unauthorized

**Solution:**
```bash {linenos=inline}
# Get fresh token
TOKEN=$(entra-auth-cli get-token -p my-service-principal)

# Check token expiration
echo "$TOKEN" | entra-auth-cli inspect - | grep exp
```

---

## See Also

- [Client Credentials Flow](/docs/oauth-flows/client-credentials/) - Detailed OAuth2 flow documentation
- [Certificate Authentication](/docs/certificates/) - Working with certificates
- [CI/CD Integration](/docs/recipes/cicd-integration/) - Automation patterns
- [Security Hardening](/docs/recipes/security-hardening/) - Security best practices
