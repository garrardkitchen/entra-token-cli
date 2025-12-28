---
title: "get-token"
description: "Generate access tokens using configured profiles"
weight: 10
---

# get-token

Generate Microsoft Entra ID access tokens using configured authentication profiles.

## Synopsis

```bash {linenos=inline}
entra-auth-cli get-token [flags]
```

## Description

The `get-token` command generates access tokens for authenticating with Microsoft APIs. It supports multiple OAuth2 flows and can use profiles configured with different authentication methods.

Tokens are cached and automatically refreshed when needed, making subsequent calls fast and efficient.

## Flags

### Core Options

#### `--profile`, `-p`

Profile name to use for authentication.

```bash {linenos=inline}
entra-auth-cli get-token --profile production
entra-auth-cli get-token -p dev
```

**Default:** `default`

#### `--scope`, `-s`

Override default scopes for this request.

```bash {linenos=inline}
entra-auth-cli get-token --scope "https://graph.microsoft.com/User.Read"
entra-auth-cli get-token -s "User.Read Mail.Read"
```

**Format:** Space-separated list of scopes

#### `--flow`, `-f`

OAuth2 flow to use for authentication.

```bash {linenos=inline}
entra-auth-cli get-token --flow interactive
entra-auth-cli get-token -f device-code
```

**Options:**
- `client-credentials` - Service-to-service (default for app profiles)
- `interactive` - Browser-based user auth
- `device-code` - Device code for limited input
- `authorization-code` - Web app flow (rarely used in CLI)

### Output Options

#### `--output`, `-o`

Output format for the token.

```bash {linenos=inline}
entra-auth-cli get-token --output json
entra-auth-cli get-token -o yaml
```

**Options:**
- `token` - Just the access token (default)
- `json` - Full token response as JSON
- `yaml` - Full token response as YAML

#### `--file`

Save token to file instead of stdout.

```bash {linenos=inline}
entra-auth-cli get-token --file token.txt
entra-auth-cli get-token --output json --file token.json
```

#### `--silent`, `-q`

Suppress all output except the token.

```bash {linenos=inline}
TOKEN=$(entra-auth-cli get-token --silent)
```

### Behavior Options

#### `--force`

Force new token generation (skip cache).

```bash {linenos=inline}
entra-auth-cli get-token --force
```

#### `--no-cache`

Don't cache the generated token.

```bash {linenos=inline}
entra-auth-cli get-token --no-cache
```

#### `--timeout`

Maximum time to wait for token generation.

```bash {linenos=inline}
entra-auth-cli get-token --timeout 30s
entra-auth-cli get-token --timeout 2m
```

**Default:** `60s`

## Examples

### Basic Usage

```bash {linenos=inline}
# Default profile and scopes
entra-auth-cli get-token

# Specific profile
entra-auth-cli get-token --profile production

# With custom scope
entra-auth-cli get-token --scope "https://management.azure.com/.default"
```

### Output Formats

```bash {linenos=inline}
# Token only (default)
entra-auth-cli get-token

# Full JSON response
entra-auth-cli get-token --output json

# Save to file
entra-auth-cli get-token --file access_token.txt
```

### Different Flows

```bash {linenos=inline}
# Client credentials (service account)
entra-auth-cli get-token --profile service-app

# Interactive browser (user auth)
entra-auth-cli get-token --flow interactive

# Device code (headless/SSH)
entra-auth-cli get-token --flow device-code
```

### Script Usage

```bash {linenos=inline}
# Get token in variable
TOKEN=$(entra-auth-cli get-token --silent)

# Use in API call
curl -H "Authorization: Bearer $TOKEN" \
  https://graph.microsoft.com/v1.0/me

# JSON parsing
ACCESS_TOKEN=$(entra-auth-cli get-token --output json | jq -r .access_token)
EXPIRES_AT=$(entra-auth-cli get-token --output json | jq -r .expires_at)
```

### Force Refresh

```bash {linenos=inline}
# Skip cache and get fresh token
entra-auth-cli get-token --force

# Useful when token has wrong permissions
entra-auth-cli get-token --force --scope "Mail.Read Mail.Send"
```

## Output

### Default (Token Only)

```
eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Ik1yNS1BVW...
```

### JSON Format

```json
{
  "token_type": "Bearer",
  "scope": "User.Read Mail.Read",
  "expires_in": 3599,
  "access_token": "eyJ0eXAiOiJKV1QiLCJhbGc...",
  "refresh_token": "0.ARoAv4j5cvGGr...",
  "expires_at": "2025-12-28T15:30:00Z"
}
```

### YAML Format

```yaml
token_type: Bearer
scope: User.Read Mail.Read
expires_in: 3599
access_token: eyJ0eXAiOiJKV1QiLCJhbGc...
refresh_token: 0.ARoAv4j5cvGGr...
expires_at: 2025-12-28T15:30:00Z
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success |
| 1 | General error |
| 2 | Profile not found |
| 3 | Authentication failed |
| 4 | Network error |
| 5 | Timeout |

## Token Caching

Tokens are automatically cached for reuse:

- **Location**: Platform-specific secure storage
  - Windows: DPAPI encrypted
  - macOS: Keychain
  - Linux: Encrypted file
- **Duration**: Until expiration (typically 1 hour)
- **Refresh**: Automatic when expired (if refresh token available)

### Cache Behavior

```bash {linenos=inline}
# First call - generates new token (~500ms)
entra-auth-cli get-token --profile prod

# Subsequent calls - uses cached token (~50ms)
entra-auth-cli get-token --profile prod

# After expiration - automatically refreshes
entra-auth-cli get-token --profile prod
```

## Common Use Cases

### Microsoft Graph API

```bash {linenos=inline}
# Get token for Graph
TOKEN=$(entra-auth-cli get-token \
  --scope "https://graph.microsoft.com/.default" \
  --output json | jq -r .access_token)

# Call Graph API
curl -H "Authorization: Bearer $TOKEN" \
  https://graph.microsoft.com/v1.0/users
```

### Azure Management API

```bash {linenos=inline}
# Get token for Azure
TOKEN=$(entra-auth-cli get-token \
  --scope "https://management.azure.com/.default" \
  --output json | jq -r .access_token)

# List subscriptions
curl -H "Authorization: Bearer $TOKEN" \
  https://management.azure.com/subscriptions?api-version=2020-01-01
```

### CI/CD Pipeline

```bash {linenos=inline}
#!/bin/bash
set -euo pipefail

# Get token (exits on failure)
TOKEN=$(entra-auth-cli get-token \
  --profile cicd \
  --silent \
  --timeout 30s)

# Deploy application
./deploy.sh --token "$TOKEN"
```

### Multiple Scopes

```bash {linenos=inline}
# Request multiple scopes
entra-auth-cli get-token \
  --scope "User.Read Mail.Read Calendars.Read" \
  --output json

# Verify scopes in token
entra-auth-cli get-token --output json | \
  jq -r .scope
```

## Error Handling

### Script Example

```bash {linenos=inline}
#!/bin/bash

get_token_safe() {
    local profile="${1:-default}"
    local max_retries=3
    local attempt=0
    
    while [ $attempt -lt $max_retries ]; do
        if token=$(entra-auth-cli get-token \
            --profile "$profile" \
            --silent 2>/dev/null); then
            echo "$token"
            return 0
        fi
        
        attempt=$((attempt + 1))
        if [ $attempt -lt $max_retries ]; then
            echo "Attempt $attempt failed, retrying..." >&2
            sleep $((attempt * 2))
        fi
    done
    
    echo "Failed to get token after $max_retries attempts" >&2
    return 1
}

# Usage
if TOKEN=$(get_token_safe production); then
    echo "Success: ${TOKEN:0:20}..."
else
    echo "Failed to authenticate"
    exit 1
fi
```

## Tips

### Performance

```bash {linenos=inline}
# Cache tokens in memory for multiple uses
TOKEN=$(entra-auth-cli get-token --silent)
for api in users groups applications; do
    curl -s -H "Authorization: Bearer $TOKEN" \
      "https://graph.microsoft.com/v1.0/$api"
done
```

### Debugging

```bash {linenos=inline}
# Verbose output
entra-auth-cli get-token --output json | jq .

# Force fresh token
entra-auth-cli get-token --force --output json

# Check what's cached
entra-auth-cli inspect --profile myapp
```

### Security

```bash {linenos=inline}
# Don't expose token in command history
TOKEN=$(entra-auth-cli get-token --silent)

# Use token from variable, not command substitution
curl -H "Authorization: Bearer $TOKEN" ...

# Not recommended (token visible in ps output)
curl -H "Authorization: Bearer $(entra-auth-cli get-token)" ...
```

## See Also

- [refresh](/docs/reference/refresh/) - Refresh expired tokens
- [inspect](/docs/reference/inspect/) - Decode and inspect tokens
- [Managing Profiles](/docs/user-guide/managing-profiles/) - Profile management
- [Generating Tokens](/docs/user-guide/generating-tokens/) - Token generation guide
