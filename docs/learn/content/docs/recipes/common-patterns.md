---
title: "Common Patterns"
description: "Frequently used patterns and techniques"
weight: 7
---

# Common Patterns

Frequently used patterns and techniques for working with Entra Token CLI.

---

## Pattern: API Rate Limiting

Handle API rate limiting with exponential backoff.

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
      wait_time=$(( 2 ** retry ))
      echo "Rate limited, waiting $wait_time seconds..." >&2
      sleep $wait_time
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

# Usage
TOKEN=$(entratool get-token -p my-profile --silent)
call_api_with_rate_limit "https://graph.microsoft.com/v1.0/users" "$TOKEN"
```

---

## Pattern: Parallel API Calls

Make multiple API calls concurrently for better performance.

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

---

## Pattern: Conditional Token Refresh

Only refresh tokens when they're close to expiration.

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

# Usage
TOKEN=$(get_valid_token "my-profile")
```

---

## Pattern: Batch API Requests

Process items in batches to avoid rate limits.

```bash
#!/bin/bash

process_batch() {
  local items=("$@")
  local token=$(entratool get-token -p my-profile --silent)
  local batch_size=20
  
  for ((i=0; i<${#items[@]}; i+=batch_size)); do
    batch=("${items[@]:i:batch_size}")
    
    echo "Processing batch $(( i / batch_size + 1))..."
    
    for item in "${batch[@]}"; do
      curl -s -H "Authorization: Bearer $token" \
        "https://api.example.com/items/$item" &
    done
    
    # Wait for batch to complete
    wait
    
    # Rate limit pause between batches
    sleep 1
  done
}

# Usage
items=(item1 item2 item3 ... item100)
process_batch "${items[@]}"
```

---

## Pattern: Circuit Breaker

Prevent cascading failures with a circuit breaker pattern.

```bash
#!/bin/bash

FAILURE_THRESHOLD=5
TIMEOUT_SECONDS=60
FAILURE_COUNT=0
CIRCUIT_OPEN_UNTIL=0

call_api_with_circuit_breaker() {
  local url=$1
  local token=$2
  local now=$(date +%s)
  
  # Check if circuit is open
  if [ $now -lt $CIRCUIT_OPEN_UNTIL ]; then
    echo "Circuit breaker open, skipping request" >&2
    return 1
  fi
  
  # Try API call
  response=$(curl -s -w "\n%{http_code}" \
    -H "Authorization: Bearer $token" \
    "$url")
  
  http_code=$(echo "$response" | tail -n1)
  body=$(echo "$response" | sed '$d')
  
  if [ "$http_code" = "200" ]; then
    # Success: reset failure count
    FAILURE_COUNT=0
    echo "$body"
    return 0
  else
    # Failure: increment counter
    FAILURE_COUNT=$((FAILURE_COUNT + 1))
    
    if [ $FAILURE_COUNT -ge $FAILURE_THRESHOLD ]; then
      # Open circuit
      CIRCUIT_OPEN_UNTIL=$((now + TIMEOUT_SECONDS))
      echo "Circuit breaker opened for $TIMEOUT_SECONDS seconds" >&2
    fi
    
    return 1
  fi
}

# Usage
TOKEN=$(entratool get-token -p my-profile --silent)
call_api_with_circuit_breaker "https://api.example.com/data" "$TOKEN"
```

---

## Pattern: Request Deduplication

Prevent duplicate requests with request hashing.

```bash
#!/bin/bash

declare -A REQUEST_CACHE

call_api_deduplicated() {
  local url=$1
  local token=$2
  local cache_key=$(echo "$url" | md5sum | cut -d' ' -f1)
  
  # Check if request was recently made
  if [ -n "${REQUEST_CACHE[$cache_key]}" ]; then
    echo "Using cached response for $url" >&2
    echo "${REQUEST_CACHE[$cache_key]}"
    return 0
  fi
  
  # Make request
  response=$(curl -s -H "Authorization: Bearer $token" "$url")
  
  # Cache response (with 5 minute TTL)
  REQUEST_CACHE[$cache_key]="$response"
  
  # Schedule cache cleanup (background)
  (sleep 300; unset REQUEST_CACHE[$cache_key]) &
  
  echo "$response"
}

# Usage
TOKEN=$(entratool get-token -p my-profile --silent)
call_api_deduplicated "https://api.example.com/data" "$TOKEN"
call_api_deduplicated "https://api.example.com/data" "$TOKEN"  # Uses cache
```

---

## Pattern: Retry with Jitter

Add randomness to retry delays to avoid thundering herd.

```bash
#!/bin/bash

call_api_with_jitter() {
  local url=$1
  local token=$2
  local max_retries=5
  local base_delay=1
  
  for retry in $(seq 0 $max_retries); do
    response=$(curl -s -w "\n%{http_code}" \
      -H "Authorization: Bearer $token" \
      "$url")
    
    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | sed '$d')
    
    if [ "$http_code" = "200" ]; then
      echo "$body"
      return 0
    fi
    
    if [ $retry -lt $max_retries ]; then
      # Calculate delay with exponential backoff and jitter
      delay=$(( base_delay * (2 ** retry) ))
      jitter=$(( RANDOM % delay ))
      wait_time=$(( delay + jitter ))
      
      echo "Retry $((retry + 1))/$max_retries, waiting $wait_time seconds..." >&2
      sleep $wait_time
    fi
  done
  
  echo "Max retries exceeded" >&2
  return 1
}

# Usage
TOKEN=$(entratool get-token -p my-profile --silent)
call_api_with_jitter "https://api.example.com/data" "$TOKEN"
```

---

## Pattern: Health Check

Implement health checks for monitoring.

```bash
#!/bin/bash

health_check() {
  local profile=$1
  local check_url="https://graph.microsoft.com/v1.0/me"
  
  # Try to get token
  if ! token=$(entratool get-token -p "$profile" --silent 2>&1); then
    echo "FAIL: Could not acquire token"
    return 1
  fi
  
  # Try to call API
  http_code=$(curl -s -w "%{http_code}" -o /dev/null \
    -H "Authorization: Bearer $token" \
    "$check_url")
  
  if [ "$http_code" = "200" ]; then
    echo "OK: Profile $profile is healthy"
    return 0
  else
    echo "FAIL: API call returned HTTP $http_code"
    return 1
  fi
}

# Usage
if health_check "my-profile"; then
  echo "System operational"
else
  echo "System degraded"
  # Send alert
fi
```

---

## Pattern: Lazy Initialization

Defer token acquisition until needed.

```bash
#!/bin/bash

TOKEN=""
PROFILE="my-profile"

get_token_lazy() {
  if [ -z "$TOKEN" ]; then
    echo "Acquiring token..." >&2
    TOKEN=$(entratool get-token -p "$PROFILE" --silent)
  fi
  echo "$TOKEN"
}

# Token not acquired until first use
echo "Starting script..."

# First API call acquires token
curl -H "Authorization: Bearer $(get_token_lazy)" \
  https://api.example.com/data1

# Second call reuses token
curl -H "Authorization: Bearer $(get_token_lazy)" \
  https://api.example.com/data2
```

---

## Pattern: Multi-Tenant Support

Handle multiple Azure tenants in one script.

```bash
#!/bin/bash

call_multi_tenant_api() {
  local tenant=$1
  local endpoint=$2
  
  # Get token for specific tenant
  token=$(entratool get-token -p "tenant-$tenant" --silent)
  
  # Call API
  curl -s -H "Authorization: Bearer $token" "$endpoint"
}

# Usage
tenants=("contoso" "fabrikam" "adventureworks")

for tenant in "${tenants[@]}"; do
  echo "Processing tenant: $tenant"
  data=$(call_multi_tenant_api "$tenant" "https://graph.microsoft.com/v1.0/users")
  echo "$data" | jq
done
```

---

## Pattern: Graceful Degradation

Provide fallback behavior when authentication fails.

```bash
#!/bin/bash

get_data_with_fallback() {
  local use_cache=${1:-false}
  local cache_file="/tmp/data-cache.json"
  
  # Try to get fresh data
  if token=$(entratool get-token -p my-profile --silent 2>/dev/null); then
    data=$(curl -s -H "Authorization: Bearer $token" \
      "https://api.example.com/data")
    
    # Cache successful response
    echo "$data" > "$cache_file"
    echo "$data"
    return 0
  fi
  
  # Authentication failed, try cache
  if [ -f "$cache_file" ]; then
    echo "Using cached data (authentication unavailable)" >&2
    cat "$cache_file"
    return 0
  fi
  
  # No cache available
  echo "Data unavailable" >&2
  return 1
}

# Usage
get_data_with_fallback
```

---

## Next Steps

- [Microsoft Graph API](/docs/recipes/microsoft-graph/)
- [Azure Management API](/docs/recipes/azure-management/)
- [Bash Scripting](/docs/recipes/bash-scripts/)
- [Security Hardening](/docs/recipes/security-hardening/)
