---
title: "Bash Scripting"
description: "Shell scripting patterns and best practices"
weight: 4
---

# Bash Scripting

Learn how to use Entra Token CLI in Bash scripts with proper error handling, caching, and best practices.

---

## Token Caching

Implement token caching to improve performance and reduce authentication requests.

```bash {linenos=inline}
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

---

## Error Handling

Implement robust error handling with retries.

```bash {linenos=inline}
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

---

## Multi-API Script

Work with multiple APIs using different tokens.

```bash {linenos=inline}
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

---

## Conditional Token Refresh

Only refresh tokens when necessary.

```bash {linenos=inline}
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

## Parallel API Calls

Make multiple API calls concurrently.

```bash {linenos=inline}
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

---

## Secure Token Storage

Use secure temporary files for token storage.

```bash {linenos=inline}
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

---

## API Rate Limiting

Handle API rate limiting with exponential backoff.

```bash {linenos=inline}
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

---

## Pagination

Handle paginated API responses.

```bash {linenos=inline}
#!/bin/bash
TOKEN=$(entratool get-token -p graph-admin --silent)
URL="https://graph.microsoft.com/v1.0/users"

while [ -n "$URL" ]; do
  response=$(curl -s -H "Authorization: Bearer $TOKEN" "$URL")
  
  # Process users
  echo "$response" | jq -r '.value[].displayName'
  
  # Get next page URL
  URL=$(echo "$response" | jq -r '.["@odata.nextLink"] // empty')
done
```

---

## Complete Example Script

A production-ready script combining multiple patterns.

```bash {linenos=inline}
#!/bin/bash
set -euo pipefail

# Configuration
PROFILE="my-profile"
TOKEN_CACHE="/tmp/entratool-${PROFILE}.token"
LOG_FILE="/var/log/my-script.log"

# Logging function
log() {
  echo "[$(date +'%Y-%m-%d %H:%M:%S')] $*" | tee -a "$LOG_FILE"
}

# Get cached or fresh token
get_token() {
  if [ -f "$TOKEN_CACHE" ]; then
    if entratool discover -f "$TOKEN_CACHE" &>/dev/null; then
      cat "$TOKEN_CACHE"
      return 0
    fi
  fi
  
  entratool get-token -p "$PROFILE" --silent | tee "$TOKEN_CACHE"
  chmod 600 "$TOKEN_CACHE"
}

# Call API with error handling
call_api() {
  local url=$1
  local token=$2
  
  response=$(curl -s -w "\n%{http_code}" \
    -H "Authorization: Bearer $token" \
    "$url")
  
  http_code=$(echo "$response" | tail -n1)
  body=$(echo "$response" | sed '$d')
  
  if [ "$http_code" = "200" ]; then
    echo "$body"
    return 0
  else
    log "ERROR: API call failed with HTTP $http_code"
    return 1
  fi
}

# Main
main() {
  log "Starting script..."
  
  # Get token
  if ! TOKEN=$(get_token); then
    log "FATAL: Could not get token"
    exit 1
  fi
  
  # Call API
  if result=$(call_api "https://graph.microsoft.com/v1.0/me" "$TOKEN"); then
    log "SUCCESS: API call completed"
    echo "$result" | jq
  else
    log "FATAL: API call failed"
    exit 1
  fi
  
  log "Script completed successfully"
}

main "$@"
```

---

## Best Practices

### Always Use set -euo pipefail

```bash {linenos=inline}
#!/bin/bash
set -euo pipefail  # Exit on error, undefined var, pipe failure
```

### Use Silent Mode for Scripts

```bash {linenos=inline}
TOKEN=$(entratool get-token -p my-profile --silent)
# vs
TOKEN=$(entratool get-token -p my-profile)  # May include extra output
```

### Secure Token Storage

```bash {linenos=inline}
# Good: Restricted permissions
chmod 600 token.txt

# Bad: World readable
chmod 644 token.txt
```

### Clean Up Temporary Files

```bash {linenos=inline}
# Use trap to ensure cleanup
TOKEN_FILE=$(mktemp)
trap "rm -f $TOKEN_FILE" EXIT
```

---

## Next Steps

- [PowerShell Scripting](/docs/recipes/powershell-scripts/)
- [Security Hardening](/docs/recipes/security-hardening/)
- [Common Patterns](/docs/recipes/common-patterns/)
