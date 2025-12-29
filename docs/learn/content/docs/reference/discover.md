---
title: "discover"
description: "Discover Azure AD app registrations"
weight: 40
---

# discover

Discover and search for Azure AD app registrations in your tenant.

## Synopsis

```bash {linenos=inline}
entra-auth-cli discover [options]
```

## Description

The `discover` command helps you find Azure AD app registrations in a tenant. You can search by wildcard patterns to locate specific applications.

## Flags

#### `-t`, `--tenant`

Tenant ID to search in (required for tenant-specific searches).

```bash {linenos=inline}
entra-auth-cli discover -t contoso.onmicrosoft.com
entra-auth-cli discover --tenant "12345678-1234-1234-1234-123456789012"
```

#### `-s`, `--search`

Search pattern with wildcard support.

```bash {linenos=inline}
entra-auth-cli discover -s "MyApp*"
entra-auth-cli discover -s "*Test*"
entra-auth-cli discover -t contoso.onmicrosoft.com -s "Prod*"
```

## Examples

### Search for Apps

```bash {linenos=inline}
# Search all apps matching pattern
entra-auth-cli discover -s "MyApp*"

# Search in specific tenant
entra-auth-cli discover -t contoso.onmicrosoft.com -s "*API*"

# Find test apps
entra-auth-cli discover -s "*Test*"
```

## See Also

- [get-token](/docs/reference/get-token/) - Generate access tokens
- [inspect](/docs/reference/inspect/) - Inspect and decode JWT tokens

## Examples

### Basic Usage

```bash {linenos=inline}
# Check token from profile
entra-auth-cli discover

# Check specific token
entra-auth-cli discover --token "eyJ0eXAiOiJKV1Qi..."

# Check token from file
entra-auth-cli discover --file access_token.txt

# Check token from stdin
entra-auth-cli get-token | entra-auth-cli discover
```

### Output Formats

```bash {linenos=inline}
# Text output (default)
entra-auth-cli discover

# JSON output
entra-auth-cli discover --output json

# Quiet mode (exit code only)
echo $?  # 0 = valid, 1 = invalid
```

### Script Usage

```bash {linenos=inline}
# Quick validation check
    echo "Token valid, proceeding..."
    ./deploy.sh
else
    echo "Token invalid, refreshing..."
    entra-auth-cli refresh --profile production
fi

# Check before API call
validate_token() {
    local profile="$1"
        echo "Invalid token for $profile" >&2
        return 1
    fi
    return 0
}

# Usage
validate_token production && api_call
```

## Output

### Text Format (Default)

```
Token Status: Valid
Format: JWT
Type: Bearer
Expires: 2025-12-28 15:30:00 UTC
Remaining: 42 minutes
Profile: production
```

### JSON Format

```json
{
  "valid": true,
  "format": "JWT",
  "type": "Bearer",
  "expires_at": "2025-12-28T15:30:00Z",
  "expires_in": 2520,
  "expired": false,
  "profile": "production"
}
```

### YAML Format

```yaml
valid: true
format: JWT
type: Bearer
expires_at: 2025-12-28T15:30:00Z
expires_in: 2520
expired: false
profile: production
```

### Quiet Mode

No output, only exit code:
- `0` = Token is valid
- `1` = Token is invalid or expired

## Validation Checks

The discover command validates:

1. **Format**: Token is a valid JWT structure (three base64 parts)
2. **Expiration**: Token has not expired
3. **Structure**: Token can be parsed
4. **Type**: Token is a Bearer token

**Not validated:**
- Signature (no cryptographic verification)
- Issuer
- Audience
- Claims content

## Use Cases

### Pre-Flight Validation

```bash {linenos=inline}
#!/bin/bash

# Validate before expensive operation
    echo "Getting fresh token..."
    entra-auth-cli get-token --profile prod --force
fi

# Proceed with validated token
TOKEN=$(entra-auth-cli get-token --profile prod)
./expensive-operation.sh "$TOKEN"
```

### Health Check Script

```bash {linenos=inline}
#!/bin/bash

profiles=("production" "staging" "development")

echo "Token Health Check"
echo "===================="

for profile in "${profiles[@]}"; do
        status="✓ Valid"
        expiry=$(entra-auth-cli discover --output json | jq -r .expires_at)
    else
        status="✗ Invalid"
        expiry="N/A"
    fi
    
    printf "%-15s %s (expires: %s)\n" "$profile" "$status" "$expiry"
done
```

### Monitoring Integration

```bash {linenos=inline}
#!/bin/bash

# Prometheus metrics format
check_token_validity() {
    local profile="$1"
    
    if output=$(entra-auth-cli discover --output json 2>/dev/null); then
        local expires_in=$(echo "$output" | jq -r .expires_in)
        local expired=$(echo "$output" | jq -r .expired)
        
        echo "token_valid{profile=\"$profile\"} 1"
        echo "token_expires_in_seconds{profile=\"$profile\"} $expires_in"
        echo "token_expired{profile=\"$profile\"} $([[ "$expired" == "true" ]] && echo 1 || echo 0)"
    else
        echo "token_valid{profile=\"$profile\"} 0"
    fi
}

# Export metrics
for profile in production staging dev; do
    check_token_validity "$profile"
done
```

### CI/CD Pipeline Check

```bash {linenos=inline}
#!/bin/bash
set -euo pipefail

echo "Validating authentication tokens..."

profiles=("cicd-deploy" "cicd-test" "cicd-prod")
failed=0

for profile in "${profiles[@]}"; do
        echo "✓ $profile: Valid"
    else
        echo "✗ $profile: Invalid or expired"
        failed=1
    fi
done

if [ $failed -eq 1 ]; then
    echo "Some tokens are invalid. Please re-authenticate."
    exit 1
fi

echo "All tokens valid. Proceeding with deployment."
```

### Quick Expiration Check

```bash {linenos=inline}
# Get time until expiration
get_ttl() {
    local profile="$1"
    local ttl=$(entra-auth-cli discover --output json 2>/dev/null | jq -r .expires_in)
    
    if [ "$ttl" != "null" ] && [ -n "$ttl" ]; then
        echo "$ttl"
        return 0
    else
        return 1
    fi
}

# Usage
if ttl=$(get_ttl production); then
    echo "Token expires in $ttl seconds"
    
    if [ $ttl -lt 300 ]; then
        echo "Token expiring soon, refreshing..."
        entra-auth-cli refresh --profile production
    fi
fi
```

## Comparison with inspect

| Feature | discover | inspect |
|---------|----------|---------|
| **Speed** | Fast | Slower |
| **Output** | Basic info | Full token details |
| **Claims** | Not shown | All claims shown |
| **Use Case** | Quick validation | Debugging |
| **Exit Code** | Validity status | Always 0 (unless error) |

Use `discover` for:
- Quick validation checks
- Script conditionals
- Monitoring/health checks
- Fast status reporting

Use `inspect` for:
- Debugging authentication issues
- Viewing all token claims
- Detailed token analysis

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Token is valid |
| 1 | Token is invalid or expired |
| 2 | Profile not found |
| 3 | No token available |
| 4 | Invalid token format |

## Performance

`discover` is optimized for speed:

```bash {linenos=inline}
# Benchmark comparison
# ~10ms

time entra-auth-cli inspect --profile prod > /dev/null
# ~50ms

# In tight loops, discover is significantly faster
for i in {1..100}; do
done
# ~1 second

for i in {1..100}; do
    entra-auth-cli inspect --profile prod > /dev/null
done
# ~5 seconds
```

## Error Handling

```bash {linenos=inline}
#!/bin/bash

discover_token() {
    local profile="$1"
    local max_retries=3
    local attempt=0
    
    while [ $attempt -lt $max_retries ]; do
            return 0
        fi
        
        attempt=$((attempt + 1))
        if [ $attempt -lt $max_retries ]; then
            echo "Token invalid, attempt $attempt of $max_retries" >&2
            entra-auth-cli refresh --profile "$profile" 2>/dev/null || entra-auth-cli get-token --profile "$profile" --force
            sleep 2
        fi
    done
    
    return 1
}

# Usage
if discover_token production; then
    echo "Token ready"
else
    echo "Failed to get valid token"
    exit 1
fi
```

## Automation Examples

### Cron Job Validation

```bash {linenos=inline}
#!/bin/bash
# /etc/cron.hourly/check-tokens

PROFILES=("app1" "app2" "app3")
LOG_FILE="/var/log/token-check.log"

{
    echo "=== Token Check: $(date) ==="
    
    for profile in "${PROFILES[@]}"; do
            echo "$profile: OK"
        else
            echo "$profile: INVALID - Attempting refresh"
            if entra-auth-cli refresh --profile "$profile" 2>&1; then
                echo "$profile: Refreshed successfully"
            else
                echo "$profile: FAILED to refresh - Manual intervention needed"
            fi
        fi
    done
    
    echo ""
} >> "$LOG_FILE"
```

### Docker Healthcheck

```dockerfile
FROM ubuntu:22.04

# Install entra-auth-cli
RUN curl -L https://github.com/garrardkitchen/entra-token-cli/releases/latest/download/entra-auth-cli-linux-amd64 \
    -o /usr/local/bin/entra-auth-cli && chmod +x /usr/local/bin/entra-auth-cli

# Healthcheck using discover
HEALTHCHECK --interval=60s --timeout=10s --start-period=30s --retries=3 \

CMD ["/app/start.sh"]
```

### Kubernetes Liveness Probe

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: app-with-token-check
spec:
  containers:
  - name: app
    image: myapp:latest
    livenessProbe:
      exec:
        command:
        - /usr/local/bin/entra-auth-cli
        - discover
        - --profile
        - k8s-app
        - --quiet
      initialDelaySeconds: 30
      periodSeconds: 60
      timeoutSeconds: 10
      failureThreshold: 3
```

## Tips

### Combine with Other Commands

```bash {linenos=inline}
# Discover + get-token (with automatic refresh)
    entra-auth-cli get-token --profile prod --force
fi

# Discover + inspect (conditional detailed check)
    echo "Token invalid. Details:"
    entra-auth-cli inspect --profile prod
fi

# Pipeline: discover → refresh → use
TOKEN=$(entra-auth-cli get-token --profile prod)
```

### Batch Checking

```bash {linenos=inline}
# Check all profiles
for profile in $(entra-auth-cli config list); do
        echo "✓ $profile"
    else
        echo "✗ $profile"
    fi
done
```

### JSON Processing

```bash {linenos=inline}
# Extract specific fields
EXPIRES_IN=$(entra-auth-cli discover --output json | jq -r .expires_in)
EXPIRED=$(entra-auth-cli discover --output json | jq -r .expired)

# Conditional logic based on fields
if [ "$(entra-auth-cli discover --output json | jq -r .expired)" == "true" ]; then
    entra-auth-cli refresh --profile prod
fi
```

## See Also

- [inspect](/docs/reference/inspect/) - Full token decoding and inspection
- [get-token](/docs/reference/get-token/) - Generate access tokens
- [refresh](/docs/reference/refresh/) - Refresh expired tokens
- [Generating Tokens](/docs/user-guide/generating-tokens/) - Token generation guide
