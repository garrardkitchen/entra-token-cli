---
title: "inspect"
description: "Decode and inspect JWT access tokens"
weight: 30
---

# inspect

Decode and inspect JWT (JSON Web Token) access tokens to view claims, expiration, and other metadata.

## Synopsis

```bash {linenos=inline}
entra-auth-cli inspect [flags]
```

## Description

The `inspect` command decodes JWT tokens and displays their contents in a human-readable format. This is useful for debugging authentication issues, verifying token claims, and checking expiration times.

The command can inspect:
- Tokens from profiles (cached tokens)
- Tokens from stdin
- Tokens from files
- Tokens passed as arguments

## Flags

### Input Options

#### `--profile`, `-p`

Inspect token from a profile.

```bash {linenos=inline}
entra-auth-cli inspect --profile production
entra-auth-cli inspect -p dev
```

#### `--token`, `-t`

Inspect a specific token string.

```bash {linenos=inline}
entra-auth-cli inspect --token "eyJ0eXAiOiJKV1QiLCJh..."
```

#### `--file`, `-f`

Read token from a file.

```bash {linenos=inline}
entra-auth-cli inspect --file token.txt
```

### Output Options

#### `--output`, `-o`

Output format.

```bash {linenos=inline}
entra-auth-cli inspect --output json
entra-auth-cli inspect -o yaml
```

**Options:**
- `pretty` - Human-readable format (default)
- `json` - JSON format
- `yaml` - YAML format

#### `--claims`

Show only the claims (payload).

```bash {linenos=inline}
entra-auth-cli inspect --claims
```

#### `--header`

Show only the token header.

```bash {linenos=inline}
entra-auth-cli inspect --header
```

## Examples

### Basic Usage

```bash {linenos=inline}
# Inspect token from profile
entra-auth-cli inspect --profile myapp

# Inspect specific token
entra-auth-cli inspect --token "eyJ0eXAiOiJKV1Qi..."

# Inspect token from file
entra-auth-cli inspect --file access_token.txt

# Inspect token from stdin
entra-auth-cli get-token | entra-auth-cli inspect
```

### Output Formats

```bash {linenos=inline}
# Human-readable (default)
entra-auth-cli inspect --profile myapp

# JSON format
entra-auth-cli inspect --profile myapp --output json

# YAML format
entra-auth-cli inspect --profile myapp --output yaml

# Only claims
entra-auth-cli inspect --profile myapp --claims

# Only header
entra-auth-cli inspect --profile myapp --header
```

### Script Usage

```bash {linenos=inline}
# Check token expiration
EXPIRY=$(entra-auth-cli inspect --profile myapp --output json | jq -r .exp)
NOW=$(date +%s)

if [ $EXPIRY -lt $NOW ]; then
    echo "Token expired!"
    exit 1
fi

# Get specific claim
TENANT=$(entra-auth-cli inspect --profile myapp --output json | jq -r .tid)
echo "Tenant ID: $TENANT"

# Check if token has required scope
SCOPES=$(entra-auth-cli inspect --profile myapp --output json | jq -r '.scp // .roles | join(" ")')
if [[ "$SCOPES" =~ "User.Read" ]]; then
    echo "Token has User.Read permission"
fi
```

## Output

### Pretty Format (Default)

```
Token Type: Bearer
Algorithm: RS256

Header:
  typ: JWT
  alg: RS256
  kid: M5-AUWRy...

Claims:
  aud: https://graph.microsoft.com
  iss: https://sts.windows.net/12345678-1234-1234-1234-123456789012/
  iat: 1703779200
  nbf: 1703779200
  exp: 1703782800
  sub: AAAAAAAAAAAAAAAAAAAAAArQICAg
  tid: 12345678-1234-1234-1234-123456789012
  oid: 87654321-4321-4321-4321-210987654321
  upn: user@contoso.com
  name: John Doe
  scp: User.Read Mail.Read

Expiration:
  Issued At:  2025-12-28 14:00:00 UTC
  Not Before: 2025-12-28 14:00:00 UTC
  Expires At: 2025-12-28 15:00:00 UTC
  Valid For:  42 minutes
```

### JSON Format

```json
{
  "header": {
    "typ": "JWT",
    "alg": "RS256",
    "kid": "M5-AUWRy..."
  },
  "claims": {
    "aud": "https://graph.microsoft.com",
    "iss": "https://sts.windows.net/12345678-1234-1234-1234-123456789012/",
    "iat": 1703779200,
    "nbf": 1703779200,
    "exp": 1703782800,
    "sub": "AAAAAAAAAAAAAAAAAAAAAArQICAg",
    "tid": "12345678-1234-1234-1234-123456789012",
    "oid": "87654321-4321-4321-4321-210987654321",
    "upn": "user@contoso.com",
    "name": "John Doe",
    "scp": "User.Read Mail.Read"
  },
  "signature": "FMh3r8...",
  "expires_in": 2520
}
```

## Common Claims

### Standard JWT Claims

| Claim | Description |
|-------|-------------|
| `iss` | Issuer (Microsoft Entra ID) |
| `aud` | Audience (target API) |
| `sub` | Subject (user/app identifier) |
| `exp` | Expiration time (Unix timestamp) |
| `nbf` | Not before time (Unix timestamp) |
| `iat` | Issued at time (Unix timestamp) |

### Microsoft-Specific Claims

| Claim | Description |
|-------|-------------|
| `tid` | Tenant ID |
| `oid` | Object ID (user/app) |
| `upn` | User Principal Name |
| `name` | Display name |
| `scp` | Scopes (delegated permissions) |
| `roles` | Roles (application permissions) |
| `appid` | Application ID |
| `ver` | Token version |

## Use Cases

### Verify Token Expiration

```bash {linenos=inline}
#!/bin/bash

check_token_valid() {
    local profile="$1"
    
    if ! output=$(entra-auth-cli inspect --profile "$profile" --output json 2>&1); then
        echo "No valid token for $profile"
        return 1
    fi
    
    local exp=$(echo "$output" | jq -r .exp)
    local now=$(date +%s)
    
    if [ $exp -le $now ]; then
        echo "Token expired"
        return 1
    fi
    
    local remaining=$((exp - now))
    echo "Token valid for $remaining seconds"
    return 0
}

# Usage
if check_token_valid production; then
    echo "Proceeding with API call"
else
    echo "Need to refresh token"
    entra-auth-cli refresh --profile production
fi
```

### Extract Specific Claims

```bash {linenos=inline}
# Get tenant ID
TENANT_ID=$(entra-auth-cli inspect --profile myapp --output json | jq -r .tid)

# Get user email
USER_EMAIL=$(entra-auth-cli inspect --profile myapp --output json | jq -r .upn)

# Get scopes
SCOPES=$(entra-auth-cli inspect --profile myapp --output json | jq -r .scp)

# Get expiration
EXPIRES=$(entra-auth-cli inspect --profile myapp --output json | jq -r .exp)
EXPIRES_HR=$(date -r $EXPIRES)
echo "Token expires: $EXPIRES_HR"
```

### Validate Required Permissions

```bash {linenos=inline}
#!/bin/bash

has_permission() {
    local profile="$1"
    local required="$2"
    
    local perms=$(entra-auth-cli inspect --profile "$profile" --output json | \
        jq -r '.scp // .roles // empty')
    
    if [[ "$perms" =~ $required ]]; then
        return 0
    else
        return 1
    fi
}

# Usage
if has_permission production "User.Read"; then
    echo "Has User.Read permission"
else
    echo "Missing User.Read permission"
    exit 1
fi
```

### Debug Authentication Issues

```bash {linenos=inline}
# Full token inspection for debugging
entra-auth-cli inspect --profile problematic-app

# Check what API the token is for
entra-auth-cli inspect --profile myapp --output json | jq -r .aud

# Verify tenant
entra-auth-cli inspect --profile myapp --output json | jq -r .tid

# Check token age
entra-auth-cli inspect --profile myapp --output json | \
    jq -r '"\(.iat) issued, \(.exp) expires"'
```

### Compare Tokens

```bash {linenos=inline}
# Compare tokens from different profiles
echo "Production token:"
entra-auth-cli inspect --profile production --claims

echo -e "\nDevelopment token:"
entra-auth-cli inspect --profile dev --claims

# Compare scopes
diff \
    <(entra-auth-cli inspect --profile production --output json | jq -r .scp) \
    <(entra-auth-cli inspect --profile dev --output json | jq -r .scp)
```

## Troubleshooting

### Invalid Token Format

**Problem:**
```bash {linenos=inline}
$ entra-auth-cli inspect --token "invalid-token"
Error: invalid token format
```

**Solution:**
```bash {linenos=inline}
# Verify token is a JWT (three parts separated by dots)
echo "$TOKEN" | grep -E '^[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+$'

# Check for extra whitespace
TOKEN=$(echo "$TOKEN" | tr -d '[:space:]')
entra-auth-cli inspect --token "$TOKEN"
```

### Token Not Found

**Problem:**
```bash {linenos=inline}
$ entra-auth-cli inspect --profile myapp
Error: no token found for profile 'myapp'
```

**Solution:**
```bash {linenos=inline}
# Generate token first
entra-auth-cli get-token --profile myapp

# Then inspect
entra-auth-cli inspect --profile myapp
```

### Expired Token

**Problem:** Token shows as expired

**Solution:**
```bash {linenos=inline}
# Get fresh token
entra-auth-cli get-token --profile myapp --force

# Verify new token
entra-auth-cli inspect --profile myapp
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success |
| 1 | General error |
| 2 | Profile not found |
| 3 | No token available |
| 4 | Invalid token format |

## Token Structure

JWT tokens consist of three parts separated by dots (`.`):

```
header.payload.signature
```

### Header

Contains token type and signing algorithm:
```json
{
  "typ": "JWT",
  "alg": "RS256",
  "kid": "key-id"
}
```

### Payload (Claims)

Contains token data and metadata:
```json
{
  "aud": "https://graph.microsoft.com",
  "iss": "https://login.microsoftonline.com/...",
  "exp": 1703782800,
  "scp": "User.Read Mail.Read"
}
```

### Signature

Cryptographic signature to verify token integrity (not decoded).

## Security Notes

**Token Inspection is Local:**
- No network calls made
- Token is only decoded, not validated
- Signature verification not performed
- Use only for debugging and informational purposes

**Token Security:**
```bash {linenos=inline}
# ✅ Good - inspect cached token
entra-auth-cli inspect --profile myapp

# ⚠️ Caution - token visible in command history
entra-auth-cli inspect --token "eyJ0..."

# ✅ Better - use file or stdin
cat token.txt | entra-auth-cli inspect
```

## See Also

- [get-token](/docs/reference/get-token/) - Generate access tokens
- [refresh](/docs/reference/refresh/) - Refresh expired tokens
- [JWT.io](https://jwt.io) - Online JWT decoder (use with caution)
- [Generating Tokens](/docs/user-guide/generating-tokens/) - Token generation guide
