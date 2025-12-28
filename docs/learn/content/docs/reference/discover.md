---
title: "discover"
description: "Quick token validation and information"
weight: 40
---

# discover

Quickly validate tokens and display essential information without full decoding.

## Synopsis

```bash
entratool discover [flags]
```

## Description

The `discover` command provides a fast way to validate token format and get basic information about tokens without performing full JWT decoding. It's useful for quick checks and validation in scripts.

This command is lighter than `inspect` and focuses on:
- Token format validation
- Basic validity checks
- Quick metadata extraction
- Fast status reporting

## Flags

### Input Options

#### `--profile`, `-p`

Check token from a profile.

```bash
entratool discover --profile production
entratool discover -p dev
```

#### `--token`, `-t`

Check a specific token string.

```bash
entratool discover --token "eyJ0eXAiOiJKV1QiLCJh..."
```

#### `--file`, `-f`

Read token from a file.

```bash
entratool discover --file token.txt
```

### Output Options

#### `--output`, `-o`

Output format.

```bash
entratool discover --output json
entratool discover -o yaml
```

**Options:**
- `text` - Human-readable text (default)
- `json` - JSON format
- `yaml` - YAML format

#### `--quiet`, `-q`

Only output validation result (exit code only).

```bash
if entratool discover --profile myapp --quiet; then
    echo "Token is valid"
fi
```

## Examples

### Basic Usage

```bash
# Check token from profile
entratool discover --profile myapp

# Check specific token
entratool discover --token "eyJ0eXAiOiJKV1Qi..."

# Check token from file
entratool discover --file access_token.txt

# Check token from stdin
entratool get-token | entratool discover
```

### Output Formats

```bash
# Text output (default)
entratool discover --profile myapp

# JSON output
entratool discover --profile myapp --output json

# Quiet mode (exit code only)
entratool discover --profile myapp --quiet
echo $?  # 0 = valid, 1 = invalid
```

### Script Usage

```bash
# Quick validation check
if entratool discover --profile production --quiet; then
    echo "Token valid, proceeding..."
    ./deploy.sh
else
    echo "Token invalid, refreshing..."
    entratool refresh --profile production
fi

# Check before API call
validate_token() {
    local profile="$1"
    if ! entratool discover --profile "$profile" --quiet 2>/dev/null; then
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

```bash
#!/bin/bash

# Validate before expensive operation
if ! entratool discover --profile prod --quiet; then
    echo "Getting fresh token..."
    entratool get-token --profile prod --force
fi

# Proceed with validated token
TOKEN=$(entratool get-token --profile prod --silent)
./expensive-operation.sh "$TOKEN"
```

### Health Check Script

```bash
#!/bin/bash

profiles=("production" "staging" "development")

echo "Token Health Check"
echo "===================="

for profile in "${profiles[@]}"; do
    if entratool discover --profile "$profile" --quiet 2>/dev/null; then
        status="✓ Valid"
        expiry=$(entratool discover --profile "$profile" --output json | jq -r .expires_at)
    else
        status="✗ Invalid"
        expiry="N/A"
    fi
    
    printf "%-15s %s (expires: %s)\n" "$profile" "$status" "$expiry"
done
```

### Monitoring Integration

```bash
#!/bin/bash

# Prometheus metrics format
check_token_validity() {
    local profile="$1"
    
    if output=$(entratool discover --profile "$profile" --output json 2>/dev/null); then
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

```bash
#!/bin/bash
set -euo pipefail

echo "Validating authentication tokens..."

profiles=("cicd-deploy" "cicd-test" "cicd-prod")
failed=0

for profile in "${profiles[@]}"; do
    if entratool discover --profile "$profile" --quiet; then
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

```bash
# Get time until expiration
get_ttl() {
    local profile="$1"
    local ttl=$(entratool discover --profile "$profile" --output json 2>/dev/null | jq -r .expires_in)
    
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
        entratool refresh --profile production
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

```bash
# Benchmark comparison
time entratool discover --profile prod --quiet
# ~10ms

time entratool inspect --profile prod > /dev/null
# ~50ms

# In tight loops, discover is significantly faster
for i in {1..100}; do
    entratool discover --profile prod --quiet
done
# ~1 second

for i in {1..100}; do
    entratool inspect --profile prod > /dev/null
done
# ~5 seconds
```

## Error Handling

```bash
#!/bin/bash

discover_token() {
    local profile="$1"
    local max_retries=3
    local attempt=0
    
    while [ $attempt -lt $max_retries ]; do
        if entratool discover --profile "$profile" --quiet 2>/dev/null; then
            return 0
        fi
        
        attempt=$((attempt + 1))
        if [ $attempt -lt $max_retries ]; then
            echo "Token invalid, attempt $attempt of $max_retries" >&2
            entratool refresh --profile "$profile" 2>/dev/null || entratool get-token --profile "$profile" --force
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

```bash
#!/bin/bash
# /etc/cron.hourly/check-tokens

PROFILES=("app1" "app2" "app3")
LOG_FILE="/var/log/token-check.log"

{
    echo "=== Token Check: $(date) ==="
    
    for profile in "${PROFILES[@]}"; do
        if entratool discover --profile "$profile" --quiet; then
            echo "$profile: OK"
        else
            echo "$profile: INVALID - Attempting refresh"
            if entratool refresh --profile "$profile" 2>&1; then
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

# Install entratool
RUN curl -L https://github.com/garrardkitchen/entra-token-cli/releases/latest/download/entratool-linux-amd64 \
    -o /usr/local/bin/entratool && chmod +x /usr/local/bin/entratool

# Healthcheck using discover
HEALTHCHECK --interval=60s --timeout=10s --start-period=30s --retries=3 \
    CMD entratool discover --profile app --quiet || exit 1

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
        - /usr/local/bin/entratool
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

```bash
# Discover + get-token (with automatic refresh)
if ! entratool discover --profile prod --quiet; then
    entratool get-token --profile prod --force
fi

# Discover + inspect (conditional detailed check)
if ! entratool discover --profile prod --quiet; then
    echo "Token invalid. Details:"
    entratool inspect --profile prod
fi

# Pipeline: discover → refresh → use
entratool discover --profile prod --quiet || entratool refresh --profile prod
TOKEN=$(entratool get-token --profile prod --silent)
```

### Batch Checking

```bash
# Check all profiles
for profile in $(entratool config list); do
    if entratool discover --profile "$profile" --quiet; then
        echo "✓ $profile"
    else
        echo "✗ $profile"
    fi
done
```

### JSON Processing

```bash
# Extract specific fields
EXPIRES_IN=$(entratool discover --profile prod --output json | jq -r .expires_in)
EXPIRED=$(entratool discover --profile prod --output json | jq -r .expired)

# Conditional logic based on fields
if [ "$(entratool discover --profile prod --output json | jq -r .expired)" == "true" ]; then
    entratool refresh --profile prod
fi
```

## See Also

- [inspect](/docs/reference/inspect/) - Full token decoding and inspection
- [get-token](/docs/reference/get-token/) - Generate access tokens
- [refresh](/docs/reference/refresh/) - Refresh expired tokens
- [Generating Tokens](/docs/user-guide/generating-tokens/) - Token generation guide
