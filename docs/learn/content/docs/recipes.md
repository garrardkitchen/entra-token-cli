---
title: "Recipes & Examples"
description: "Practical examples and common integration patterns"
weight: 40
---

# Recipes & Examples

Real-world examples and patterns for integrating Entra Token CLI into your workflows.

---

## Quick Navigation

### API Integration
- [Microsoft Graph API](/docs/recipes/microsoft-graph/) - User management, email, calendars
- [Azure Management API](/docs/recipes/azure-management/) - Resource management
- [Custom APIs](/docs/recipes/custom-apis/) - Your own API integration

### Automation
- [CI/CD Integration](/docs/recipes/cicd-integration/) - GitHub Actions, Azure Pipelines
- [Bash Scripts](/docs/recipes/bash-scripts/) - Shell scripting patterns
- [PowerShell Scripts](/docs/recipes/powershell-scripts/) - Windows automation

### Security
- [Security Hardening](/docs/recipes/security-hardening/) - Production best practices
- [Secret Rotation](/docs/recipes/secret-rotation/) - Credential lifecycle
- [Multi-Environment Setup](/docs/recipes/multi-environment/) - Dev/staging/prod

### Advanced
- [Token Caching](/docs/recipes/token-caching/) - Optimize performance
- [Error Handling](/docs/recipes/error-handling/) - Robust scripts
- [Multi-Tenant Scenarios](/docs/recipes/multi-tenant/) - Multiple tenants

---

## Microsoft Graph API

### Read User Profile

```bash
#!/bin/bash
TOKEN=$(entratool get-token -p graph-readonly --silent)

curl -H "Authorization: Bearer $TOKEN" \
     https://graph.microsoft.com/v1.0/me | jq
```

### List All Users

```bash
#!/bin/bash
TOKEN=$(entratool get-token -p graph-admin --silent)

curl -H "Authorization: Bearer $TOKEN" \
     'https://graph.microsoft.com/v1.0/users?$select=displayName,mail' | jq
```

### Send Email

```bash
#!/bin/bash
TOKEN=$(entratool get-token -p graph-mail --silent \
  --scope "https://graph.microsoft.com/Mail.Send")

curl -X POST \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d '{
       "message": {
         "subject": "Test Email",
         "body": {
           "contentType": "Text",
           "content": "This is a test email from Entra Token CLI"
         },
         "toRecipients": [
           {
             "emailAddress": {
               "address": "user@contoso.com"
             }
           }
         ]
       },
       "saveToSentItems": "true"
     }' \
     https://graph.microsoft.com/v1.0/me/sendMail
```

[More Microsoft Graph examples →](/docs/recipes/microsoft-graph/)

---

## Azure Management API

### List Subscriptions

```bash
#!/bin/bash
TOKEN=$(entratool get-token -p azure-mgmt --silent \
  --scope "https://management.azure.com/.default")

curl -H "Authorization: Bearer $TOKEN" \
     'https://management.azure.com/subscriptions?api-version=2020-01-01' | jq
```

### List Resource Groups

```bash
#!/bin/bash
TOKEN=$(entratool get-token -p azure-mgmt --silent)
SUBSCRIPTION_ID="12345678-1234-1234-1234-123456789abc"

curl -H "Authorization: Bearer $TOKEN" \
     "https://management.azure.com/subscriptions/$SUBSCRIPTION_ID/resourcegroups?api-version=2021-04-01" | jq
```

### Create Virtual Machine

```bash
#!/bin/bash
TOKEN=$(entratool get-token -p azure-admin --silent)
SUBSCRIPTION_ID="..."
RESOURCE_GROUP="my-rg"
VM_NAME="my-vm"

curl -X PUT \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d @vm-config.json \
     "https://management.azure.com/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Compute/virtualMachines/$VM_NAME?api-version=2021-03-01"
```

[More Azure Management examples →](/docs/recipes/azure-management/)

---

## CI/CD Integration

### GitHub Actions

```yaml
name: Deploy

on: [push]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Install Entra Token CLI
        run: |
          dotnet tool install --global EntraTokenCli
      
      - name: Create Profile
        env:
          CLIENT_ID: ${{ secrets.AZURE_CLIENT_ID }}
          TENANT_ID: ${{ secrets.AZURE_TENANT_ID }}
          CLIENT_SECRET: ${{ secrets.AZURE_CLIENT_SECRET }}
        run: |
          # Create profile non-interactively
          cat > profile.json <<EOF
          {
            "name": "cicd",
            "clientId": "$CLIENT_ID",
            "tenantId": "$TENANT_ID",
            "scope": "https://management.azure.com/.default",
            "useClientSecret": true
          }
          EOF
          entratool config import -f profile.json
          
          # Add secret (requires manual step or env var reading)
          # For this example, we'll use inline token generation
      
      - name: Deploy to Azure
        run: |
          TOKEN=$(entratool get-token -p cicd --silent)
          
          # Use token for deployment
          curl -X POST \
               -H "Authorization: Bearer $TOKEN" \
               -H "Content-Type: application/json" \
               -d @deployment.json \
               "https://management.azure.com/..."
```

### Azure Pipelines

```yaml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
- script: |
    dotnet tool install --global EntraTokenCli
  displayName: 'Install Entra Token CLI'

- task: AzureCLI@2
  inputs:
    azureSubscription: 'MyServiceConnection'
    scriptType: 'bash'
    scriptLocation: 'inlineScript'
    inlineScript: |
      # Create profile
      entratool config create --non-interactive \
        --name cicd \
        --client-id $(ClientId) \
        --tenant-id $(TenantId) \
        --client-secret $(ClientSecret) \
        --scope "https://management.azure.com/.default"
      
      # Get token
      TOKEN=$(entratool get-token -p cicd --silent)
      
      # Deploy
      ./deploy.sh "$TOKEN"
```

[More CI/CD examples →](/docs/recipes/cicd-integration/)

---

## Shell Scripting Patterns

### Token Caching

```bash
#!/bin/bash
set -euo pipefail

TOKEN_CACHE="/tmp/entratool-token-cache.txt"
TOKEN_MAX_AGE=3000  # 50 minutes

get_token() {
  local profile=$1
  
  # Check if cached token exists and is valid
  if [ -f "$TOKEN_CACHE" ]; then
    if entratool discover -f "$TOKEN_CACHE" &>/dev/null; then
      cat "$TOKEN_CACHE"
      return 0
    fi
  fi
  
  # Get fresh token
  entratool get-token -p "$profile" --silent | tee "$TOKEN_CACHE"
  chmod 600 "$TOKEN_CACHE"
}

# Use it
TOKEN=$(get_token "my-profile")
curl -H "Authorization: Bearer $TOKEN" https://api.example.com
```

### Error Handling

```bash
#!/bin/bash
set -euo pipefail

get_token_with_retry() {
  local profile=$1
  local max_retries=3
  local retry=0
  
  while [ $retry -lt $max_retries ]; do
    if TOKEN=$(entratool get-token -p "$profile" --silent 2>&1); then
      echo "$TOKEN"
      return 0
    fi
    
    echo "Retry $((retry + 1))/$max_retries..." >&2
    retry=$((retry + 1))
    sleep 5
  done
  
  echo "Failed to get token after $max_retries attempts" >&2
  return 1
}

# Use it
if TOKEN=$(get_token_with_retry "my-profile"); then
  curl -H "Authorization: Bearer $TOKEN" https://api.example.com
else
  echo "Fatal: Could not acquire token"
  exit 1
fi
```

### Multi-API Script

```bash
#!/bin/bash
set -euo pipefail

# Get tokens for different APIs
GRAPH_TOKEN=$(entratool get-token -p graph-profile --silent)
AZURE_TOKEN=$(entratool get-token -p azure-profile --silent)

# Use Graph API
echo "Fetching user profile..."
USER=$(curl -s -H "Authorization: Bearer $GRAPH_TOKEN" \
  https://graph.microsoft.com/v1.0/me)

USER_ID=$(echo "$USER" | jq -r .id)
echo "User ID: $USER_ID"

# Use Azure Management API
echo "Fetching subscriptions..."
SUBS=$(curl -s -H "Authorization: Bearer $AZURE_TOKEN" \
  'https://management.azure.com/subscriptions?api-version=2020-01-01')

echo "$SUBS" | jq -r '.value[].displayName'
```

[More shell scripting examples →](/docs/recipes/bash-scripts/)

---

## PowerShell Integration

### Basic Token Retrieval

```powershell
# Get token
$token = entratool get-token -p my-profile --silent

# Use with Invoke-RestMethod
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

$response = Invoke-RestMethod `
    -Uri "https://graph.microsoft.com/v1.0/me" `
    -Headers $headers `
    -Method Get

$response | ConvertTo-Json
```

### Token Caching

```powershell
$tokenCache = "$env:TEMP\entratool-token.txt"
$tokenMaxAge = 3000  # 50 minutes

function Get-CachedToken {
    param([string]$Profile)
    
    # Check if cached token is valid
    if (Test-Path $tokenCache) {
        entratool discover -f $tokenCache 2>$null
        if ($LASTEXITCODE -eq 0) {
            return Get-Content $tokenCache -Raw
        }
    }
    
    # Get fresh token
    $token = entratool get-token -p $Profile --silent
    $token | Out-File -FilePath $tokenCache -NoNewline
    return $token
}

# Use it
$token = Get-CachedToken -Profile "my-profile"
```

### Error Handling

```powershell
function Get-TokenWithRetry {
    param(
        [string]$Profile,
        [int]$MaxRetries = 3
    )
    
    for ($i = 0; $i -lt $MaxRetries; $i++) {
        try {
            $token = entratool get-token -p $Profile --silent 2>&1
            if ($LASTEXITCODE -eq 0) {
                return $token
            }
        } catch {
            Write-Warning "Retry $($i + 1)/$MaxRetries..."
            Start-Sleep -Seconds 5
        }
    }
    
    throw "Failed to get token after $MaxRetries attempts"
}

# Use it
try {
    $token = Get-TokenWithRetry -Profile "my-profile"
    # Use token...
} catch {
    Write-Error "Fatal: Could not acquire token"
    exit 1
}
```

[More PowerShell examples →](/docs/recipes/powershell-scripts/)

---

## Security Best Practices

### Secure Token Storage

```bash
#!/bin/bash
set -euo pipefail

# Use secure temp file
TOKEN_FILE=$(mktemp)
trap "rm -f $TOKEN_FILE" EXIT

# Get token with restricted permissions
entratool get-token -p my-profile --silent > "$TOKEN_FILE"
chmod 600 "$TOKEN_FILE"

# Use token
TOKEN=$(cat "$TOKEN_FILE")
curl -H "Authorization: Bearer $TOKEN" https://api.example.com

# File automatically deleted on exit
```

### Profile Separation

```bash
# Development
entratool config create
# Name: dev-graph
# Client ID: <dev-app-id>
# Scope: https://graph.microsoft.com/User.Read

# Production
entratool config create
# Name: prod-graph
# Client ID: <prod-app-id>
# Scope: https://graph.microsoft.com/.default

# Never mix environments
entratool get-token -p dev-graph   # For development
entratool get-token -p prod-graph  # For production
```

### Secret Rotation

```bash
#!/bin/bash

rotate_secret() {
  local profile=$1
  local new_secret=$2
  
  echo "Rotating secret for profile: $profile"
  
  # Update profile
  entratool config edit -p "$profile" <<EOF
3
$new_secret
q
EOF
  
  # Test new secret
  if entratool get-token -p "$profile" --silent > /dev/null; then
    echo "✓ Secret rotation successful"
    return 0
  else
    echo "✗ Secret rotation failed"
    return 1
  fi
}

# Usage
NEW_SECRET="new-secret-from-azure-portal"
rotate_secret "my-service-principal" "$NEW_SECRET"
```

[More security examples →](/docs/recipes/security-hardening/)

---

## Common Patterns

### Pattern: API Rate Limiting

```bash
#!/bin/bash

call_api_with_rate_limit() {
  local url=$1
  local token=$2
  local max_retries=5
  local retry=0
  
  while [ $retry -lt $max_retries ]; do
    response=$(curl -s -w "\n%{http_code}" \
      -H "Authorization: Bearer $token" \
      "$url")
    
    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | sed '$d')
    
    if [ "$http_code" = "200" ]; then
      echo "$body"
      return 0
    elif [ "$http_code" = "429" ]; then
      # Rate limited, wait and retry
      echo "Rate limited, waiting..." >&2
      sleep $(( 2 ** retry ))
      retry=$((retry + 1))
    else
      echo "Error: HTTP $http_code" >&2
      echo "$body" >&2
      return 1
    fi
  done
  
  echo "Max retries exceeded" >&2
  return 1
}

TOKEN=$(entratool get-token -p my-profile --silent)
call_api_with_rate_limit "https://graph.microsoft.com/v1.0/users" "$TOKEN"
```

### Pattern: Parallel API Calls

```bash
#!/bin/bash

# Get token once
TOKEN=$(entratool get-token -p my-profile --silent)

# Parallel API calls
{
  curl -s -H "Authorization: Bearer $TOKEN" \
    https://graph.microsoft.com/v1.0/me > user.json &
  
  curl -s -H "Authorization: Bearer $TOKEN" \
    https://graph.microsoft.com/v1.0/me/messages > messages.json &
  
  curl -s -H "Authorization: Bearer $TOKEN" \
    https://graph.microsoft.com/v1.0/me/calendars > calendars.json &
}

# Wait for all to complete
wait

echo "All API calls completed"
```

### Pattern: Conditional Token Refresh

```bash
#!/bin/bash

get_valid_token() {
  local profile=$1
  local token_file="/tmp/token-$profile.txt"
  local min_validity=300  # 5 minutes
  
  # Check if token exists
  if [ -f "$token_file" ]; then
    # Check expiration
    exp=$(entratool inspect -f "$token_file" 2>/dev/null | jq -r .payload.exp)
    now=$(date +%s)
    remaining=$(( exp - now ))
    
    if [ $remaining -gt $min_validity ]; then
      # Token still valid
      cat "$token_file"
      return 0
    fi
  fi
  
  # Get fresh token
  entratool get-token -p "$profile" --silent | tee "$token_file"
  chmod 600 "$token_file"
}

TOKEN=$(get_valid_token "my-profile")
```

---

## Next Steps

### Explore More Recipes

- [Microsoft Graph Integration](/docs/recipes/microsoft-graph/)
- [Azure Management API](/docs/recipes/azure-management/)
- [CI/CD Integration](/docs/recipes/cicd-integration/)
- [Security Hardening](/docs/recipes/security-hardening/)

### Learn Core Concepts

- [OAuth2 Flows](/docs/core-concepts/oauth2-flows/)
- [Scopes & Permissions](/docs/core-concepts/scopes/)
- [Secure Storage](/docs/core-concepts/secure-storage/)

### Reference Documentation

- [Command Reference](/docs/reference/)
- [Configuration Reference](/docs/reference/configuration/)
