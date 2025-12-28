---
title: "Client Credentials Flow"
description: "Service-to-service authentication without user interaction"
weight: 10
---

# Client Credentials Flow

The Client Credentials flow is designed for service-to-service authentication where no user interaction is required. This is the most common flow for automation, CI/CD pipelines, and background services.

## Overview

**Use this flow when:**
- Your application runs without user interaction
- You're authenticating a service, not a user
- You need application-level permissions
- Running in CI/CD pipelines or automated scripts

**Authentication methods:**
- **Client Secret** - Shared secret (less secure, easier setup)
- **Certificate** - Public/private key pair (more secure, recommended for production)

## Quick Start

### Using Client Secret

```bash {linenos=inline}
# Create profile with client secret
entratool create-profile

# Generate token
entratool get-token --profile myapp
```

### Using Certificate

```bash {linenos=inline}
# Create profile with certificate
entratool create-profile --use-certificate

# Generate token
entratool get-token --profile myapp-cert
```

## Configuration

### Profile Setup

When creating a profile for client credentials flow:

```bash {linenos=inline}
entratool create-profile
```

You'll be prompted for:
- **Profile Name**: Identifier for this configuration
- **Tenant ID**: Your Microsoft Entra tenant ID
- **Client ID**: Application (client) ID from Azure
- **Authentication Method**: Client secret or certificate
- **Scopes**: Default API permissions

### Azure App Registration

Required Azure configuration:

1. **Create App Registration**
   ```bash
   # Using Azure CLI
   az ad app create --display-name "My Service App"
   ```

2. **Configure API Permissions**
   - Add application permissions (not delegated)
   - Grant admin consent
   - Common scopes:
     - `https://graph.microsoft.com/.default`
     - `https://management.azure.com/.default`

3. **Add Credentials**
   
   **Option A: Client Secret**
   ```bash
   az ad app credential reset --id <app-id>
   ```
   
   **Option B: Certificate**
   ```bash
   az ad app credential reset --id <app-id> --cert @cert.pem
   ```

## Usage Examples

### Basic Token Request

```bash {linenos=inline}
# Using default profile
entratool get-token

# Using specific profile
entratool get-token --profile production

# Override scopes
entratool get-token --scope https://graph.microsoft.com/.default
```

### With Microsoft Graph

```bash {linenos=inline}
# Get token for Graph API
TOKEN=$(entratool get-token --scope https://graph.microsoft.com/.default --output json | jq -r .access_token)

# Use token
curl -H "Authorization: Bearer $TOKEN" \
  https://graph.microsoft.com/v1.0/users
```

### With Azure Management

```bash {linenos=inline}
# Get token for Azure Management
TOKEN=$(entratool get-token --scope https://management.azure.com/.default --output json | jq -r .access_token)

# List subscriptions
curl -H "Authorization: Bearer $TOKEN" \
  https://management.azure.com/subscriptions?api-version=2020-01-01
```

### In Scripts

```bash {linenos=inline}
#!/bin/bash
set -euo pipefail

# Function to get token with error handling
get_token() {
    local scope="${1:-https://graph.microsoft.com/.default}"
    local max_retries=3
    local retry=0
    
    while [ $retry -lt $max_retries ]; do
        if TOKEN=$(entratool get-token --scope "$scope" --output json 2>/dev/null); then
            echo "$TOKEN" | jq -r .access_token
            return 0
        fi
        retry=$((retry + 1))
        sleep $((retry * 2))
    done
    
    echo "Failed to get token after $max_retries attempts" >&2
    return 1
}

# Use the token
if TOKEN=$(get_token); then
    curl -H "Authorization: Bearer $TOKEN" \
      https://graph.microsoft.com/v1.0/me
fi
```

## CI/CD Integration

### GitHub Actions

```yaml
name: Deploy with Token

on: [push]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Install Entra Token CLI
        run: |
          wget https://github.com/garrardkitchen/entratool-cli/releases/latest/download/entratool-linux-amd64
          chmod +x entratool-linux-amd64
          sudo mv entratool-linux-amd64 /usr/local/bin/entratool
      
      - name: Create Profile
        env:
          TENANT_ID: ${{ secrets.AZURE_TENANT_ID }}
          CLIENT_ID: ${{ secrets.AZURE_CLIENT_ID }}
          CLIENT_SECRET: ${{ secrets.AZURE_CLIENT_SECRET }}
        run: |
          entratool create-profile \
            --name ci \
            --tenant-id "$TENANT_ID" \
            --client-id "$CLIENT_ID" \
            --client-secret "$CLIENT_SECRET" \
            --scope https://management.azure.com/.default
      
      - name: Get Token and Deploy
        run: |
          TOKEN=$(entratool get-token --profile ci --output json | jq -r .access_token)
          # Use token for deployment
```

### Azure Pipelines

```yaml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: Bash@3
  displayName: 'Install Entra Token CLI'
  inputs:
    targetType: 'inline'
    script: |
      wget https://github.com/garrardkitchen/entratool-cli/releases/latest/download/entratool-linux-amd64
      chmod +x entratool-linux-amd64
      sudo mv entratool-linux-amd64 /usr/local/bin/entratool

- task: Bash@3
  displayName: 'Get Token'
  env:
    TENANT_ID: $(AZURE_TENANT_ID)
    CLIENT_ID: $(AZURE_CLIENT_ID)
    CLIENT_SECRET: $(AZURE_CLIENT_SECRET)
  inputs:
    targetType: 'inline'
    script: |
      entratool create-profile \
        --name pipeline \
        --tenant-id "$TENANT_ID" \
        --client-id "$CLIENT_ID" \
        --client-secret "$CLIENT_SECRET"
      
      TOKEN=$(entratool get-token --profile pipeline --output json | jq -r .access_token)
      echo "##vso[task.setvariable variable=ACCESS_TOKEN;isSecret=true]$TOKEN"

- task: Bash@3
  displayName: 'Deploy Application'
  inputs:
    targetType: 'inline'
    script: |
      # Use $(ACCESS_TOKEN) in subsequent commands
      curl -H "Authorization: Bearer $(ACCESS_TOKEN)" ...
```

## Security Best Practices

### Certificate-Based Authentication

**Always prefer certificates over client secrets in production:**

```bash {linenos=inline}
# Create certificate
openssl req -x509 -newkey rsa:4096 -keyout key.pem -out cert.pem -days 365 -nodes

# Create profile with certificate
entratool create-profile \
  --name secure-app \
  --use-certificate \
  --certificate-path cert.pem \
  --private-key-path key.pem
```

### Secret Management

**Don't hardcode secrets:**

```bash {linenos=inline}
# ❌ Bad - hardcoded
entratool create-profile --client-secret "my-secret-123"

# ✅ Good - from environment
entratool create-profile --client-secret "${CLIENT_SECRET}"

# ✅ Better - from secure vault
CLIENT_SECRET=$(az keyvault secret show --vault-name myvault --name client-secret --query value -o tsv)
entratool create-profile --client-secret "${CLIENT_SECRET}"
```

### Least Privilege

Only request the permissions you need:

```bash {linenos=inline}
# ❌ Too broad
entratool get-token --scope https://graph.microsoft.com/.default

# ✅ Specific permission
entratool get-token --scope https://graph.microsoft.com/User.Read.All
```

## Troubleshooting

### "Unauthorized" Errors

**Problem:** `401 Unauthorized` when using token

**Solutions:**
1. Verify application permissions in Azure Portal
2. Ensure admin consent is granted
3. Check token scopes match API requirements
4. Verify application has correct role assignments

```bash {linenos=inline}
# Inspect token to verify scopes
entratool inspect --profile myapp
```

### Certificate Not Found

**Problem:** Profile created but certificate not accessible

**Solutions:**
1. Verify certificate path is absolute
2. Check file permissions (readable by user)
3. Ensure certificate format is correct (PEM/PFX)

```bash {linenos=inline}
# Verify certificate
openssl x509 -in cert.pem -text -noout
```

### Token Expired

**Problem:** Token works initially but fails later

**Solution:** Tokens expire after 1 hour. Implement refresh logic:

```bash {linenos=inline}
# Check if token is still valid
is_token_valid() {
    local token="$1"
    local exp=$(echo "$token" | jq -R 'split(".") | .[1] | @base64d | fromjson | .exp')
    local now=$(date +%s)
    [ "$exp" -gt "$now" ]
}

# Get new token if expired
if ! is_token_valid "$TOKEN"; then
    TOKEN=$(entratool get-token --profile myapp --output json | jq -r .access_token)
fi
```

## Performance Optimization

### Token Caching

Entra Token CLI automatically caches tokens. Reuse tokens within their lifetime:

```bash {linenos=inline}
# First call gets new token
entratool get-token --profile myapp  # ~500ms

# Subsequent calls use cached token
entratool get-token --profile myapp  # ~50ms
```

### Parallel Requests

When making multiple API calls with the same token:

```bash {linenos=inline}
# Get token once
TOKEN=$(entratool get-token --output json | jq -r .access_token)

# Use for multiple parallel requests
{
  curl -H "Authorization: Bearer $TOKEN" https://graph.microsoft.com/v1.0/users &
  curl -H "Authorization: Bearer $TOKEN" https://graph.microsoft.com/v1.0/groups &
  curl -H "Authorization: Bearer $TOKEN" https://graph.microsoft.com/v1.0/applications &
  wait
}
```

## Next Steps

- [Certificate Authentication](/docs/certificates/overview/) - Use certificates instead of secrets
- [CI/CD Integration](/docs/recipes/cicd-integration/) - Complete CI/CD examples
- [Security Hardening](/docs/recipes/security-hardening/) - Production security checklist
- [Microsoft Graph Recipes](/docs/recipes/microsoft-graph/) - Common Graph API patterns
