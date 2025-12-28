---
title: "Working with Tokens"
description: "Inspect, validate, and use access tokens effectively"
weight: 30
---

# Working with Tokens

Once you have an access token, learn how to inspect, validate, refresh, and use it effectively with APIs.

---

## Quick Reference

| Task | Command |
|------|---------|
| Inspect token | `entratool inspect -t TOKEN` |
| Inspect from file | `entratool inspect -f token.txt` |
| Discover token info | `entratool discover -t TOKEN` |
| Refresh token | `entratool refresh -p PROFILE` |
| Check expiration | `entratool discover -t TOKEN \| jq .exp` |

---

## Token Inspection

### Decode JWT Claims

```bash {linenos=inline}
entratool inspect -t "eyJ0eXAiOiJKV1QiLCJh..."
```

**Output:**
```json
{
  "header": {
    "typ": "JWT",
    "alg": "RS256",
    "kid": "abc123..."
  },
  "payload": {
    "aud": "https://graph.microsoft.com",
    "iss": "https://sts.windows.net/12345678-1234-1234-1234-123456789abc/",
    "iat": 1701432000,
    "nbf": 1701432000,
    "exp": 1701435600,
    "appid": "12345678-1234-1234-1234-123456789abc",
    "appidacr": "1",
    "idp": "https://sts.windows.net/...",
    "oid": "87654321-4321-4321-4321-cba987654321",
    "rh": "...",
    "sub": "...",
    "tid": "12345678-1234-1234-1234-123456789abc",
    "uti": "...",
    "ver": "1.0",
    "scp": "User.Read Mail.Read",
    "roles": []
  },
  "signature": "..."
}
```

### Inspect from File

```bash {linenos=inline}
entratool get-token -p myprofile --silent > token.txt
entratool inspect -f token.txt
```

### Inspect from Pipeline

```bash {linenos=inline}
entratool get-token -p myprofile --silent | entratool inspect
```

[Detailed inspection guide →](/docs/user-guide/working-with-tokens/inspecting/)

---

## Understanding Token Claims

### Essential Claims

| Claim | Description | Example |
|-------|-------------|---------|
| `aud` | Audience (API) | `https://graph.microsoft.com` |
| `iss` | Issuer (Entra ID) | `https://sts.windows.net/...` |
| `iat` | Issued at (Unix timestamp) | `1701432000` |
| `exp` | Expiration (Unix timestamp) | `1701435600` |
| `appid` | Application ID | `12345678-...` |
| `tid` | Tenant ID | `87654321-...` |

### Permission Claims

**Delegated permissions (user context):**
```json
{
  "scp": "User.Read Mail.Read Calendars.Read"
}
```

**Application permissions (app context):**
```json
{
  "roles": ["User.Read.All", "Mail.Send"]
}
```

### User Claims

```json
{
  "unique_name": "user@contoso.com",
  "upn": "user@contoso.com",
  "name": "John Doe",
  "oid": "87654321-4321-4321-4321-cba987654321"
}
```

---

## Token Discovery

### Quick Token Info

```bash {linenos=inline}
entratool discover -t "eyJ0eXAiOiJKV1QiLCJh..."
```

**Output:**
```
Token Information:
  Audience: https://graph.microsoft.com
  Issued: 2024-12-01 10:00:00 UTC
  Expires: 2024-12-01 11:00:00 UTC
  Time remaining: 45 minutes
  Scopes: User.Read Mail.Read
  Application ID: 12345678-1234-1234-1234-123456789abc
  Tenant ID: 87654321-4321-4321-4321-cba987654321
```

### Check if Token is Valid

```bash {linenos=inline}
entratool discover -t "eyJ0eXAiOiJKV1QiLCJh..." && echo "Valid" || echo "Expired"
```

**Exit codes:**
- `0`: Token is valid
- `1`: Token is expired or invalid

### Expiration Check

```bash {linenos=inline}
# Get expiration timestamp
EXP=$(entratool inspect -t "$TOKEN" | jq -r .payload.exp)

# Convert to readable date
date -r $EXP  # macOS
date -d @$EXP  # Linux
```

[Detailed discovery guide →](/docs/user-guide/working-with-tokens/discovering/)

---

## Token Refresh

### When to Refresh

**Refresh token when:**
- Token is expired or about to expire
- You need a fresh token with updated permissions

**Check expiration:**
```bash {linenos=inline}
entratool discover -t "$TOKEN"
```

### Refresh Command

```bash {linenos=inline}
entratool refresh -p myprofile
```

**Requirements:**
- Original token acquired with `offline_access` scope
- Refresh token available in cache

**Output:**
```
Refreshing token for profile 'myprofile'...
✓ Token refreshed successfully

eyJ0eXAiOiJKV1QiLCJh...
```

### Refresh vs New Token

| Operation | Refresh | New Token |
|-----------|---------|-----------|
| **Speed** | Faster | Slower |
| **User interaction** | None | May require |
| **Requirements** | Refresh token | Profile + credentials |
| **Use case** | Token expired | First request or refresh unavailable |

**When refresh isn't available:**
- Client Credentials flow (no refresh tokens)
- Refresh token expired (>90 days of inactivity)

[Detailed refresh guide →](/docs/user-guide/working-with-tokens/refreshing/)

---

## Using Tokens with APIs

### Microsoft Graph API

```bash {linenos=inline}
TOKEN=$(entratool get-token -p graph-profile --silent)

curl -H "Authorization: Bearer $TOKEN" \
     https://graph.microsoft.com/v1.0/me
```

**Example response:**
```json
{
  "@odata.context": "https://graph.microsoft.com/v1.0/$metadata#users/$entity",
  "id": "87654321-4321-4321-4321-cba987654321",
  "displayName": "John Doe",
  "mail": "john.doe@contoso.com",
  "userPrincipalName": "john.doe@contoso.com"
}
```

### Azure Resource Manager

```bash {linenos=inline}
TOKEN=$(entratool get-token -p azure-profile --silent)

curl -H "Authorization: Bearer $TOKEN" \
     "https://management.azure.com/subscriptions?api-version=2020-01-01"
```

### Custom APIs

```bash {linenos=inline}
TOKEN=$(entratool get-token -p custom-api --silent)

curl -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"key": "value"}' \
     https://api.contoso.com/v1/resources
```

---

## Token Validation

### Client-Side Validation

**Check expiration:**
```bash {linenos=inline}
# Extract exp claim
EXP=$(entratool inspect -t "$TOKEN" | jq -r .payload.exp)

# Compare with current time
NOW=$(date +%s)
if [ $EXP -lt $NOW ]; then
  echo "Token expired"
else
  echo "Token valid for $(( ($EXP - $NOW) / 60 )) minutes"
fi
```

**Verify audience:**
```bash {linenos=inline}
AUD=$(entratool inspect -t "$TOKEN" | jq -r .payload.aud)
if [ "$AUD" = "https://graph.microsoft.com" ]; then
  echo "Valid for Graph API"
else
  echo "Wrong audience: $AUD"
fi
```

### Server-Side Validation

APIs validate tokens by:
1. Verifying signature using Microsoft's public keys
2. Checking expiration (`exp` claim)
3. Validating audience (`aud` claim)
4. Checking issuer (`iss` claim)

**Microsoft Graph validates:**
- Token signature
- Expiration time
- Audience = `https://graph.microsoft.com`
- Issuer from trusted Entra ID tenant

---

## Token Expiration

### Typical Lifetimes

| Token Type | Lifetime |
|------------|----------|
| **Access token** | 1 hour |
| **Refresh token** | 90 days (or until revoked) |
| **ID token** | 1 hour |

### Handling Expiration

**Pattern 1: Check before use**
```bash {linenos=inline}
if entratool discover -f token.txt &>/dev/null; then
  TOKEN=$(cat token.txt)
else
  TOKEN=$(entratool get-token -p myprofile --silent)
  echo "$TOKEN" > token.txt
fi
```

**Pattern 2: Catch API errors**
```bash {linenos=inline}
TOKEN=$(cat token.txt)
RESPONSE=$(curl -s -w "%{http_code}" \
  -H "Authorization: Bearer $TOKEN" \
  https://graph.microsoft.com/v1.0/me)

if [[ "$RESPONSE" == *"401"* ]]; then
  # Token expired, get new one
  TOKEN=$(entratool get-token -p myprofile --silent)
  echo "$TOKEN" > token.txt
  
  # Retry request
  curl -H "Authorization: Bearer $TOKEN" \
       https://graph.microsoft.com/v1.0/me
fi
```

**Pattern 3: Preemptive refresh**
```bash {linenos=inline}
# Refresh if less than 5 minutes remaining
EXP=$(entratool inspect -f token.txt | jq -r .payload.exp)
NOW=$(date +%s)
REMAINING=$(( ($EXP - $NOW) / 60 ))

if [ $REMAINING -lt 5 ]; then
  entratool get-token -p myprofile --silent > token.txt
fi
```

[Detailed expiration guide →](/docs/user-guide/working-with-tokens/expiration/)

---

## Token Storage

### Temporary Storage

```bash {linenos=inline}
# In-memory (session only)
export TOKEN=$(entratool get-token -p myprofile --silent)

# File (with restricted permissions)
entratool get-token -p myprofile --silent > /tmp/token.txt
chmod 600 /tmp/token.txt
```

### Cache Location

**MSAL token cache:**
- **Windows:** `%LOCALAPPDATA%\.msal\cache`
- **macOS/Linux:** `~/.msal/cache`

**Contains:**
- Access tokens
- Refresh tokens
- ID tokens
- Account information

**Security:**
- Encrypted on Windows (DPAPI)
- Encrypted on macOS (Keychain)
- ⚠️ Obfuscated on Linux (not secure)

### Clearing Cache

```bash {linenos=inline}
# Delete cache directory
rm -rf ~/.msal/cache

# Next token request will require re-authentication
entratool get-token -p myprofile
```

---

## Token Security

### ✅ Best Practices

**Do:**
- ✅ Request tokens on-demand
- ✅ Use HTTPS for all API requests
- ✅ Check token expiration before use
- ✅ Store tokens with restrictive permissions (600)
- ✅ Clear tokens after use
- ✅ Use environment variables for temporary storage
- ✅ Rotate credentials regularly

**Don't:**
- ❌ Hard-code tokens in source code
- ❌ Commit tokens to git repositories
- ❌ Share tokens via email/chat
- ❌ Log tokens in plaintext
- ❌ Store tokens long-term
- ❌ Use tokens across environments (dev/prod)

### Secure Token Handling

```bash {linenos=inline}
#!/bin/bash
set -e

# Secure temp file
TOKEN_FILE=$(mktemp)
trap "rm -f $TOKEN_FILE" EXIT

# Get token
entratool get-token -p myprofile --silent > "$TOKEN_FILE"
chmod 600 "$TOKEN_FILE"

# Use token
TOKEN=$(cat "$TOKEN_FILE")
curl -H "Authorization: Bearer $TOKEN" https://api.example.com

# File automatically deleted on exit
```

---

## Common Patterns

### Pattern 1: Token in Script

```bash {linenos=inline}
#!/bin/bash
set -euo pipefail

get_token() {
  entratool get-token -p "$1" --silent
}

TOKEN=$(get_token "myprofile")

# Use token
curl -H "Authorization: Bearer $TOKEN" \
     https://graph.microsoft.com/v1.0/me
```

### Pattern 2: Cached Token

```bash {linenos=inline}
#!/bin/bash

TOKEN_CACHE="/tmp/my-token-cache.txt"
TOKEN_MAX_AGE=3000  # 50 minutes

get_fresh_token() {
  entratool get-token -p myprofile --silent > "$TOKEN_CACHE"
  chmod 600 "$TOKEN_CACHE"
}

# Check if cache exists and is fresh
if [ -f "$TOKEN_CACHE" ]; then
  FILE_AGE=$(( $(date +%s) - $(stat -f %m "$TOKEN_CACHE") ))  # macOS
  # FILE_AGE=$(( $(date +%s) - $(stat -c %Y "$TOKEN_CACHE") ))  # Linux
  
  if [ $FILE_AGE -gt $TOKEN_MAX_AGE ]; then
    get_fresh_token
  fi
else
  get_fresh_token
fi

TOKEN=$(cat "$TOKEN_CACHE")
```

### Pattern 3: Token Rotation

```bash {linenos=inline}
#!/bin/bash

# Function to get valid token
get_valid_token() {
  local profile=$1
  local token_file="/tmp/token-$profile.txt"
  
  # Check if token exists and is valid
  if [ -f "$token_file" ] && entratool discover -f "$token_file" &>/dev/null; then
    cat "$token_file"
  else
    # Get fresh token
    entratool get-token -p "$profile" --silent | tee "$token_file"
    chmod 600 "$token_file"
  fi
}

# Use it
TOKEN=$(get_valid_token "myprofile")
curl -H "Authorization: Bearer $TOKEN" https://api.example.com
```

### Pattern 4: Multi-Profile Management

```bash {linenos=inline}
#!/bin/bash

declare -A TOKENS

get_token_for_profile() {
  local profile=$1
  
  if [ -z "${TOKENS[$profile]}" ]; then
    TOKENS[$profile]=$(entratool get-token -p "$profile" --silent)
  fi
  
  echo "${TOKENS[$profile]}"
}

# Use different profiles
GRAPH_TOKEN=$(get_token_for_profile "graph-profile")
AZURE_TOKEN=$(get_token_for_profile "azure-profile")

curl -H "Authorization: Bearer $GRAPH_TOKEN" https://graph.microsoft.com/v1.0/me
curl -H "Authorization: Bearer $AZURE_TOKEN" https://management.azure.com/subscriptions
```

---

## Troubleshooting

### "Invalid token"

**Cause:** Token is malformed or corrupted

**Fix:**
```bash {linenos=inline}
# Validate token format
entratool inspect -t "$TOKEN"

# If invalid, get fresh token
TOKEN=$(entratool get-token -p myprofile --silent)
```

### "Token expired"

**Cause:** Token lifetime exceeded

**Fix:**
```bash {linenos=inline}
# Get new token
TOKEN=$(entratool get-token -p myprofile --silent)
```

### "Insufficient privileges"

**Cause:** Token lacks required permissions

**Fix:**
1. Check token scopes:
   ```bash
   entratool inspect -t "$TOKEN" | jq .payload.scp
   ```
2. Request token with correct scopes:
   ```bash
   entratool get-token -p myprofile \
     --scope "https://graph.microsoft.com/User.Read Mail.Send"
   ```

### "Invalid audience"

**Cause:** Token issued for different API

**Fix:**
```bash {linenos=inline}
# Check audience
entratool inspect -t "$TOKEN" | jq .payload.aud

# Get token for correct API
entratool get-token -p myprofile \
  --scope "https://correct-api.com/.default"
```

---

## Next Steps

- [Token Inspection (Detailed)](/docs/user-guide/working-with-tokens/inspecting/)
- [Token Refresh (Detailed)](/docs/user-guide/working-with-tokens/refreshing/)
- [Token Expiration Handling](/docs/user-guide/working-with-tokens/expiration/)
- [API Integration Recipes](/docs/recipes/)
