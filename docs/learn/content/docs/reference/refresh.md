---
title: "refresh"
description: "Refresh expired access tokens using refresh tokens"
weight: 20
---

# refresh

Refresh expired or expiring access tokens using refresh tokens.

## Synopsis

```bash
entratool refresh [flags]
```

## Description

The `refresh` command obtains a new access token using a previously acquired refresh token. This is useful when you have a long-running process that needs to maintain valid tokens without user interaction.

Refresh tokens are only available when using delegated permissions (user authentication flows like interactive or device code).

## Flags

### Core Options

#### `--profile`, `-p`

Profile name to refresh tokens for.

```bash
entratool refresh --profile production
entratool refresh -p dev
```

**Default:** `default`

#### `--output`, `-o`

Output format for the new token.

```bash
entratool refresh --output json
entratool refresh -o yaml
```

**Options:**
- `token` - Just the access token (default)
- `json` - Full token response as JSON
- `yaml` - Full token response as YAML

#### `--silent`, `-q`

Suppress all output except the token.

```bash
TOKEN=$(entratool refresh --silent)
```

## Examples

### Basic Usage

```bash
# Refresh default profile
entratool refresh

# Refresh specific profile
entratool refresh --profile production
```

### Output Formats

```bash
# Token only
entratool refresh

# Full JSON response
entratool refresh --output json

# Get new access token in variable
TOKEN=$(entratool refresh --silent)
```

### Script Usage

```bash
#!/bin/bash

# Check if token needs refresh
if ! entratool inspect --profile myapp 2>/dev/null; then
    echo "Token expired, refreshing..."
    entratool refresh --profile myapp
fi

# Use refreshed token
TOKEN=$(entratool get-token --profile myapp --silent)
```

## Requirements

### Refresh Token Availability

Refresh tokens are only available with:
- **Interactive Browser Flow**
- **Device Code Flow**
- **Authorization Code Flow** (web apps)

Refresh tokens are **NOT** available with:
- Client Credentials Flow (service-to-service)

### Offline Access Scope

For refresh tokens to work, the `offline_access` scope must be included:

```bash
# When creating profile
entratool config create --scope "User.Read offline_access"

# When getting initial token
entratool get-token --scope "User.Read offline_access"
```

## How It Works

1. CLI retrieves stored refresh token for profile
2. Sends refresh token to Microsoft Entra ID
3. Receives new access token (and possibly new refresh token)
4. Updates cached tokens
5. Returns new access token

## Output

Same formats as `get-token`:

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
  "expires_at": "2025-12-28T16:30:00Z"
}
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success |
| 1 | General error |
| 2 | Profile not found |
| 3 | No refresh token available |
| 4 | Refresh token expired/invalid |
| 5 | Network error |

## Common Use Cases

### Long-Running Process

```bash
#!/bin/bash
set -euo pipefail

# Initial authentication
entratool get-token --profile daemon --flow interactive

# Main loop
while true; do
    # Get token (uses cache if valid)
    TOKEN=$(entratool get-token --profile daemon --silent)
    
    # Do work with token
    curl -H "Authorization: Bearer $TOKEN" \
      https://graph.microsoft.com/v1.0/me
    
    # Check if token expires soon
    EXPIRES=$(entratool inspect --profile daemon --output json | jq -r .exp)
    NOW=$(date +%s)
    
    # Refresh if expiring within 5 minutes
    if [ $((EXPIRES - NOW)) -lt 300 ]; then
        echo "Token expiring soon, refreshing..."
        entratool refresh --profile daemon
    fi
    
    sleep 60
done
```

### Proactive Refresh

```bash
#!/bin/bash

# Refresh before token expires
refresh_if_needed() {
    local profile="${1:-default}"
    
    if entratool inspect --profile "$profile" &>/dev/null; then
        local expires=$(entratool inspect --profile "$profile" --output json | jq -r .exp)
        local now=$(date +%s)
        local remaining=$((expires - now))
        
        # Refresh if less than 10 minutes remaining
        if [ $remaining -lt 600 ]; then
            echo "Refreshing token (expires in ${remaining}s)..."
            entratool refresh --profile "$profile"
        fi
    else
        echo "Token invalid or expired, refreshing..."
        entratool refresh --profile "$profile"
    fi
}

# Usage
refresh_if_needed production
TOKEN=$(entratool get-token --profile production --silent)
```

### Scheduled Refresh

```bash
# Cron job to refresh tokens periodically
# Add to crontab: crontab -e

# Refresh every 30 minutes
*/30 * * * * /usr/local/bin/entratool refresh --profile background-job

# Refresh at specific times
0 8,12,17 * * * /usr/local/bin/entratool refresh --profile work-hours
```

## Troubleshooting

### No Refresh Token Available

**Problem:**
```bash
$ entratool refresh --profile myapp
Error: no refresh token available for profile 'myapp'
```

**Cause:** Profile uses client credentials flow (no refresh tokens)

**Solution:**
```bash
# Client credentials profiles don't need refresh
# Just get a new token instead
entratool get-token --profile myapp
```

### Refresh Token Expired

**Problem:**
```bash
$ entratool refresh --profile user-app
Error: refresh token expired or invalid
```

**Solutions:**

```bash
# 1. Re-authenticate with user flow
entratool get-token --profile user-app --flow interactive --force

# 2. Or recreate profile
entratool config delete --name user-app
entratool config create --name user-app
entratool get-token --profile user-app --flow interactive
```

### Missing offline_access Scope

**Problem:** Refresh token not provided by Entra ID

**Solution:**
```bash
# Include offline_access scope
entratool get-token --profile myapp \
  --scope "User.Read offline_access" \
  --flow interactive

# Or update profile default scopes
entratool config edit --name myapp
# Add "offline_access" to scopes
```

## Automatic Refresh

The CLI automatically refreshes tokens when calling `get-token` if:
1. Access token is expired or expiring soon
2. Refresh token is available
3. Profile was authenticated with user flow

```bash
# First call - token expired, automatically refreshes
entratool get-token --profile myapp

# You usually don't need to call refresh manually
# The get-token command handles it automatically
```

## When to Use refresh Explicitly

Explicit `refresh` is useful when:

1. **Proactive refresh before expiration**
   ```bash
   # Refresh before starting long operation
   entratool refresh --profile myapp
   ./long-running-task.sh
   ```

2. **Testing refresh token validity**
   ```bash
   if entratool refresh --profile myapp; then
       echo "Refresh token is valid"
   else
       echo "Need to re-authenticate"
   fi
   ```

3. **Background refresh jobs**
   ```bash
   # Keep tokens fresh in background
   while true; do
       entratool refresh --profile daemon
       sleep 1800  # Every 30 minutes
   done
   ```

## Refresh Token Lifetime

Refresh token lifetimes vary based on configuration:

| Type | Typical Lifetime |
|------|------------------|
| **Public Client** | 90 days inactive / 24 hours active |
| **Web App** | 90 days inactive / Until revoked |
| **Conditional Access** | May require re-auth periodically |

**Note:** Refresh tokens can be revoked by:
- User password change
- Admin action
- Security policies
- Conditional Access policies

## Security Considerations

### Refresh Token Security

```bash
# Refresh tokens are sensitive credentials
# - Never log or expose them
# - Store securely (CLI handles this)
# - Don't share across systems

# ✅ Good - CLI manages storage
entratool refresh --profile myapp

# ❌ Bad - exposing refresh token
export REFRESH_TOKEN=$(...)  # Don't do this
```

### Token Rotation

```bash
# Some configurations rotate refresh tokens
# Old refresh token becomes invalid after use
# CLI handles this automatically

entratool refresh --profile myapp
# CLI stores new refresh token automatically
```

## See Also

- [get-token](/docs/reference/get-token/) - Generate access tokens
- [inspect](/docs/reference/inspect/) - Decode and inspect tokens
- [OAuth Flows](/docs/oauth-flows/) - Authentication flow details
- [Generating Tokens](/docs/user-guide/generating-tokens/) - Token generation guide
