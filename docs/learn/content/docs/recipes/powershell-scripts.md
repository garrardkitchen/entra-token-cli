---
title: "PowerShell Scripting"
description: "PowerShell integration patterns for Windows automation"
weight: 5
---

# PowerShell Scripting

Learn how to integrate Entra Auth Cli with PowerShell for Windows automation and Azure management.

---

## Basic Token Retrieval

```powershell
# Get token
$token = entra-auth-cli get-token -p my-profile

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

---

## Token Caching

Implement token caching for better performance.

```powershell
$tokenCache = "$env:TEMP\entra-auth-cli-token.txt"
$tokenMaxAge = 3000  # 50 minutes

function Get-CachedToken {
    param([string]$Profile)
    
    # Check if cached token is valid
    if (Test-Path $tokenCache) {
        $result = entra-auth-cli inspect - < $tokenCache 2>$null
        if ($LASTEXITCODE -eq 0) {
            return Get-Content $tokenCache -Raw
        }
    }
    
    # Get fresh token
    $token = entra-auth-cli get-token -p $Profile
    $token | Out-File -FilePath $tokenCache -NoNewline
    return $token
}

# Use it
$token = Get-CachedToken -Profile "my-profile"
```

---

## Error Handling

Implement robust error handling with retries.

```powershell
function Get-TokenWithRetry {
    param(
        [string]$Profile,
        [int]$MaxRetries = 3
    )
    
    for ($i = 0; $i -lt $MaxRetries; $i++) {
        try {
            $token = entra-auth-cli get-token -p $Profile 2>&1
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

---

## Multi-API Script

Work with multiple APIs using different tokens.

```powershell
# Get tokens for different APIs
$graphToken = entra-auth-cli get-token -p graph-profile
$azureToken = entra-auth-cli get-token -p azure-profile

# Use Graph API
Write-Host "Fetching user profile..."
$graphHeaders = @{ "Authorization" = "Bearer $graphToken" }
$user = Invoke-RestMethod `
    -Uri "https://graph.microsoft.com/v1.0/me" `
    -Headers $graphHeaders

Write-Host "User: $($user.displayName)"

# Use Azure Management API
Write-Host "Fetching subscriptions..."
$azureHeaders = @{ "Authorization" = "Bearer $azureToken" }
$subs = Invoke-RestMethod `
    -Uri "https://management.azure.com/subscriptions?api-version=2020-01-01" `
    -Headers $azureHeaders

$subs.value | ForEach-Object { Write-Host $_.displayName }
```

---

## Conditional Token Refresh

Only refresh tokens when necessary.

```powershell
function Get-ValidToken {
    param([string]$Profile)
    
    $tokenFile = "$env:TEMP\token-$Profile.txt"
    $minValidity = 300  # 5 minutes
    
    # Check if token exists
    if (Test-Path $tokenFile) {
        # Check expiration
        $token = Get-Content $tokenFile -Raw
        $tokenJson = entra-auth-cli inspect $token 2>$null | ConvertFrom-Json
        if ($tokenJson) {
            $exp = $tokenJson.exp
            $now = [DateTimeOffset]::Now.ToUnixTimeSeconds()
            $remaining = $exp - $now
            
            if ($remaining -gt $minValidity) {
                return $token
            }
        }
    }
    
    # Get fresh token
    $token = entra-auth-cli get-token -p $Profile
    $token | Out-File -FilePath $tokenFile -NoNewline
    return $token
}

$token = Get-ValidToken -Profile "my-profile"
```

---

## Parallel API Calls

Make multiple API calls concurrently using PowerShell jobs.

```powershell
# Get token once
$token = entra-auth-cli get-token -p my-profile
$headers = @{ "Authorization" = "Bearer $token" }

# Start parallel jobs
$jobs = @()
$jobs += Start-Job -ScriptBlock {
    param($headers)
    Invoke-RestMethod -Uri "https://graph.microsoft.com/v1.0/me" -Headers $headers
} -ArgumentList $headers

$jobs += Start-Job -ScriptBlock {
    param($headers)
    Invoke-RestMethod -Uri "https://graph.microsoft.com/v1.0/me/messages" -Headers $headers
} -ArgumentList $headers

$jobs += Start-Job -ScriptBlock {
    param($headers)
    Invoke-RestMethod -Uri "https://graph.microsoft.com/v1.0/me/calendars" -Headers $headers
} -ArgumentList $headers

# Wait for all jobs and get results
$results = $jobs | Wait-Job | Receive-Job
$jobs | Remove-Job

Write-Host "All API calls completed"
$results | ForEach-Object { $_ | ConvertTo-Json }
```

---

## API Pagination

Handle paginated API responses.

```powershell
$token = entra-auth-cli get-token -p graph-admin
$headers = @{ "Authorization" = "Bearer $token" }
$url = "https://graph.microsoft.com/v1.0/users"

while ($url) {
    $response = Invoke-RestMethod -Uri $url -Headers $headers
    
    # Process users
    $response.value | ForEach-Object {
        Write-Host $_.displayName
    }
    
    # Get next page URL
    $url = $response.'@odata.nextLink'
}
```

---

## Rate Limiting

Handle API rate limiting with exponential backoff.

```powershell
function Invoke-ApiWithRateLimit {
    param(
        [string]$Uri,
        [hashtable]$Headers,
        [int]$MaxRetries = 5
    )
    
    for ($retry = 0; $retry -lt $MaxRetries; $retry++) {
        try {
            $response = Invoke-WebRequest -Uri $Uri -Headers $Headers -Method Get
            return $response.Content | ConvertFrom-Json
        } catch {
            if ($_.Exception.Response.StatusCode -eq 429) {
                $waitTime = [Math]::Pow(2, $retry)
                Write-Warning "Rate limited, waiting $waitTime seconds..."
                Start-Sleep -Seconds $waitTime
            } else {
                throw
            }
        }
    }
    
    throw "Max retries exceeded"
}

$token = entra-auth-cli get-token -p my-profile
$headers = @{ "Authorization" = "Bearer $token" }
$result = Invoke-ApiWithRateLimit -Uri "https://graph.microsoft.com/v1.0/users" -Headers $headers
```

---

## Complete Example Script

Production-ready script with logging and error handling.

```powershell
#Requires -Version 5.1

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$Profile,
    
    [Parameter(Mandatory=$false)]
    [string]$LogFile = "$env:TEMP\my-script.log"
)

# Logging function
function Write-Log {
    param([string]$Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    "$timestamp - $Message" | Tee-Object -FilePath $LogFile -Append
}

# Get cached or fresh token
function Get-Token {
    param([string]$Profile)
    
    $tokenCache = "$env:TEMP\entra-auth-cli-$Profile.token"
    
    if (Test-Path $tokenCache) {
        $result = entra-auth-cli inspect - < $tokenCache 2>$null
        if ($LASTEXITCODE -eq 0) {
            return Get-Content $tokenCache -Raw
        }
    }
    
    $token = entra-auth-cli get-token -p $Profile
    $token | Out-File -FilePath $tokenCache -NoNewline
    return $token
}

# Call API with error handling
function Invoke-Api {
    param(
        [string]$Uri,
        [string]$Token
    )
    
    $headers = @{ "Authorization" = "Bearer $Token" }
    
    try {
        $response = Invoke-RestMethod -Uri $Uri -Headers $headers -Method Get
        return $response
    } catch {
        Write-Log "ERROR: API call failed - $_"
        throw
    }
}

# Main
try {
    Write-Log "Starting script..."
    
    # Get token
    $token = Get-Token -Profile $Profile
    if (-not $token) {
        Write-Log "FATAL: Could not get token"
        exit 1
    }
    
    # Call API
    $result = Invoke-Api -Uri "https://graph.microsoft.com/v1.0/me" -Token $token
    Write-Log "SUCCESS: API call completed"
    $result | ConvertTo-Json | Write-Host
    
    Write-Log "Script completed successfully"
} catch {
    Write-Log "FATAL: $_"
    exit 1
}
```

---

## Best Practices

### Use Strict Mode

```powershell
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
```

### Capture Token Output

```powershell
# Get token (output will include token on last line)
$token = entra-auth-cli get-token -p my-profile
# If needed, extract just the token value
$token = ($token -split "`n")[-1].Trim()
```

### Secure Token Storage

```powershell
# Restrict file permissions
$acl = Get-Acl $tokenFile
$acl.SetAccessRuleProtection($true, $false)
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    $env:USERNAME, "FullControl", "Allow"
)
$acl.AddAccessRule($rule)
Set-Acl $tokenFile $acl
```

### Check Exit Codes

```powershell
entra-auth-cli get-token -p my-profile
if ($LASTEXITCODE -ne 0) {
    Write-Error "Token generation failed"
    exit 1
}
```

---

## Next Steps

- [Bash Scripting](/docs/recipes/bash-scripts/)
- [Security Hardening](/docs/recipes/security-hardening/)
- [CI/CD Integration](/docs/recipes/cicd-integration/)
