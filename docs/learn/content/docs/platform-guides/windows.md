---
title: "Windows"
description: "Windows-specific configuration and token storage"
weight: 10
---

# Windows Platform Guide

Complete guide for using Entra Token CLI on Windows, including DPAPI-based secure storage, PowerShell integration, and Windows-specific features.

## Overview

Entra Token CLI leverages Windows security features for optimal token protection:

- **DPAPI Encryption**: Windows Data Protection API for secure token storage
- **Per-User Isolation**: Tokens encrypted per-user, per-machine
- **Native Integration**: Works with Windows Credential Manager
- **PowerShell Support**: First-class PowerShell scripting support

## Installation

### Using Winget

```powershell {linenos=inline}
# Install using Windows Package Manager
winget install GarrardKitchen.EntraTokenCLI
```

### Manual Installation

```powershell {linenos=inline}
# Download latest release
$version = "1.0.0"
$url = "https://github.com/garrardkitchen/entratool-cli/releases/download/v$version/entratool-windows-amd64.exe"

# Download
Invoke-WebRequest -Uri $url -OutFile "$env:TEMP\entratool.exe"

# Move to Program Files
Move-Item "$env:TEMP\entratool.exe" "C:\Program Files\EntraToken\entratool.exe" -Force

# Add to PATH
$path = [Environment]::GetEnvironmentVariable("Path", "User")
[Environment]::SetEnvironmentVariable("Path", "$path;C:\Program Files\EntraToken", "User")
```

### Verify Installation

```powershell {linenos=inline}
# Check version
entratool --version

# Test basic functionality
entratool --help
```

## Token Storage

### DPAPI Encryption

Tokens are encrypted using Windows DPAPI:

```powershell {linenos=inline}
# Tokens stored at:
$env:LOCALAPPDATA\EntraTokenCLI\profiles\

# Each profile has encrypted token file:
# - profile-name.json (configuration)
# - profile-name.token (encrypted token)
```

**Security characteristics:**
- Encrypted per-user, per-machine
- Cannot be decrypted on different machine or by different user
- Protected by Windows user account
- Survives password changes

### View Storage Location

```powershell {linenos=inline}
# Check storage path
$storagePath = "$env:LOCALAPPDATA\EntraTokenCLI\profiles"
Get-ChildItem $storagePath

# Example output:
# Directory: C:\Users\jsmith\AppData\Local\EntraTokenCLI\profiles
#
# Mode                 LastWriteTime         Length Name
# ----                 -------------         ------ ----
# -a----        12/28/2025   2:30 PM            456 default.json
# -a----        12/28/2025   2:30 PM           2048 default.token
# -a----        12/28/2025   3:15 PM            512 production.json
# -a----        12/28/2025   3:15 PM           2048 production.token
```

### Security Permissions

```powershell {linenos=inline}
# Check file permissions
Get-Acl "$env:LOCALAPPDATA\EntraTokenCLI\profiles\default.token" | Format-List

# Verify only current user has access
$acl = Get-Acl "$env:LOCALAPPDATA\EntraTokenCLI\profiles\default.token"
$acl.Access | Where-Object { $_.IdentityReference -eq "$env:USERDOMAIN\$env:USERNAME" }
```

## PowerShell Integration

### Basic Usage

```powershell {linenos=inline}
# Get token
$token = entratool get-token --output json | ConvertFrom-Json
$accessToken = $token.access_token

# Use token in API call
$headers = @{
    Authorization = "Bearer $accessToken"
}
$response = Invoke-RestMethod -Uri "https://graph.microsoft.com/v1.0/me" -Headers $headers
$response
```

### Function Wrapper

```powershell {linenos=inline}
function Get-EntraToken {
    [CmdletBinding()]
    param(
        [string]$Profile = "default",
        [string]$Scope,
        [switch]$Force
    )
    
    $arguments = @("get-token", "--profile", $Profile, "--output", "json")
    
    if ($Scope) {
        $arguments += @("--scope", $Scope)
    }
    
    if ($Force) {
        $arguments += "--force"
    }
    
    try {
        $output = & entratool @arguments 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to get token: $output"
        }
        
        $token = $output | ConvertFrom-Json
        return $token.access_token
    }
    catch {
        Write-Error "Error getting token: $_"
        return $null
    }
}

# Usage
$token = Get-EntraToken -Profile "production"
$token = Get-EntraToken -Scope "https://graph.microsoft.com/User.Read"
```

### Error Handling

```powershell {linenos=inline}
function Invoke-EntraTokenCommand {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments,
        [int]$MaxRetries = 3
    )
    
    $attempt = 0
    while ($attempt -lt $MaxRetries) {
        try {
            $output = & entratool @Arguments 2>&1
            if ($LASTEXITCODE -eq 0) {
                return $output
            }
            
            $attempt++
            if ($attempt -lt $MaxRetries) {
                Write-Warning "Attempt $attempt failed. Retrying..."
                Start-Sleep -Seconds ($attempt * 2)
            }
        }
        catch {
            $attempt++
            if ($attempt -lt $MaxRetries) {
                Write-Warning "Error: $_. Retrying..."
                Start-Sleep -Seconds ($attempt * 2)
            }
            else {
                throw
            }
        }
    }
    
    throw "Command failed after $MaxRetries attempts"
}

# Usage
$result = Invoke-EntraTokenCommand -Arguments @("get-token", "--output", "json")
```

### Microsoft Graph Module Integration

```powershell {linenos=inline}
# Install Microsoft Graph PowerShell SDK
Install-Module Microsoft.Graph -Scope CurrentUser

# Get token from Entra Token CLI
$token = entratool get-token --scope https://graph.microsoft.com/.default --output json | 
    ConvertFrom-Json | Select-Object -ExpandProperty access_token

# Connect to Microsoft Graph
$secureToken = ConvertTo-SecureString $token -AsPlainText -Force
Connect-MgGraph -AccessToken $secureToken

# Use Graph cmdlets
Get-MgUser -Top 10
Get-MgGroup -Top 10

# Disconnect
Disconnect-MgGraph
```

## Windows-Specific Features

### Task Scheduler Integration

```powershell {linenos=inline}
# Create scheduled task for token refresh
$action = New-ScheduledTaskAction -Execute "entratool" -Argument "refresh --profile production"

$trigger = New-ScheduledTaskTrigger -Daily -At 6:00AM

$principal = New-ScheduledTaskPrincipal -UserId "$env:USERDOMAIN\$env:USERNAME" -LogonType S4U

$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries

Register-ScheduledTask -TaskName "RefreshEntraToken" -Action $action -Trigger $trigger -Principal $principal -Settings $settings

# Run task manually
Start-ScheduledTask -TaskName "RefreshEntraToken"

# Check task history
Get-ScheduledTaskInfo -TaskName "RefreshEntraToken"
```

### Windows Service Integration

```powershell {linenos=inline}
# Example Windows Service using Entra Token CLI
# Service.ps1

Add-Type -TypeDefinition @"
using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Timers;

public class EntraTokenService : ServiceBase {
    private Timer timer;
    
    public EntraTokenService() {
        ServiceName = "EntraTokenRefresh";
    }
    
    protected override void OnStart(string[] args) {
        timer = new Timer(3600000); // 1 hour
        timer.Elapsed += RefreshToken;
        timer.Start();
    }
    
    protected override void OnStop() {
        timer?.Stop();
        timer?.Dispose();
    }
    
    private void RefreshToken(object sender, ElapsedEventArgs e) {
        Process.Start(new ProcessStartInfo {
            FileName = "entratool",
            Arguments = "refresh --profile production",
            UseShellExecute = false,
            RedirectStandardOutput = true
        });
    }
}
"@

# Install service
$serviceName = "EntraTokenRefresh"
New-Service -Name $serviceName -BinaryPathName "C:\Path\To\Service.exe" -StartupType Automatic
Start-Service $serviceName
```

### Event Log Integration

```powershell {linenos=inline}
# Log Entra Token CLI operations to Windows Event Log
function Write-EntraTokenLog {
    param(
        [Parameter(Mandatory)]
        [string]$Message,
        [ValidateSet('Information', 'Warning', 'Error')]
        [string]$EntryType = 'Information'
    )
    
    $source = "EntraTokenCLI"
    $logName = "Application"
    
    # Create source if it doesn't exist
    if (-not [System.Diagnostics.EventLog]::SourceExists($source)) {
        New-EventLog -LogName $logName -Source $source
    }
    
    Write-EventLog -LogName $logName -Source $source -EventId 1000 -EntryType $EntryType -Message $Message
}

# Usage
Write-EntraTokenLog -Message "Token refreshed successfully for profile: production"
Write-EntraTokenLog -Message "Failed to refresh token" -EntryType Error
```

## Common Use Cases

### Azure DevOps Integration

```powershell {linenos=inline}
# Azure DevOps pipeline task
# build.ps1

param(
    [string]$TenantId = $env:AZURE_TENANT_ID,
    [string]$ClientId = $env:AZURE_CLIENT_ID,
    [string]$ClientSecret = $env:AZURE_CLIENT_SECRET
)

# Create profile
entratool create-profile `
    --name azdo `
    --tenant-id $TenantId `
    --client-id $ClientId `
    --client-secret $ClientSecret `
    --scope "https://management.azure.com/.default"

# Get token
$token = entratool get-token --profile azdo --output json | ConvertFrom-Json
$accessToken = $token.access_token

# Deploy to Azure
$headers = @{
    Authorization = "Bearer $accessToken"
    "Content-Type" = "application/json"
}

$body = @{
    location = "eastus"
    properties = @{
        template = Get-Content -Path "template.json" | ConvertFrom-Json
    }
} | ConvertTo-Json -Depth 10

Invoke-RestMethod -Method Post `
    -Uri "https://management.azure.com/subscriptions/$subscriptionId/resourcegroups/$resourceGroup/providers/Microsoft.Resources/deployments/$deploymentName?api-version=2021-04-01" `
    -Headers $headers `
    -Body $body
```

### PowerShell Module

```powershell {linenos=inline}
# EntraToken.psm1

function Connect-EntraToken {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ProfileName,
        [switch]$Force
    )
    
    $arguments = @("get-token", "--profile", $ProfileName, "--output", "json")
    if ($Force) { $arguments += "--force" }
    
    $output = & entratool @arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Authentication failed: $output"
    }
    
    $token = $output | ConvertFrom-Json
    $script:CurrentToken = $token.access_token
    $script:CurrentProfile = $ProfileName
    
    Write-Verbose "Connected to profile: $ProfileName"
}

function Get-CurrentEntraToken {
    if (-not $script:CurrentToken) {
        throw "Not connected. Run Connect-EntraToken first."
    }
    return $script:CurrentToken
}

function Invoke-EntraGraphRequest {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Uri,
        [string]$Method = "GET",
        [object]$Body
    )
    
    $token = Get-CurrentEntraToken
    $headers = @{
        Authorization = "Bearer $token"
        "Content-Type" = "application/json"
    }
    
    $params = @{
        Uri = $Uri
        Method = $Method
        Headers = $headers
    }
    
    if ($Body) {
        $params.Body = $Body | ConvertTo-Json -Depth 10
    }
    
    Invoke-RestMethod @params
}

Export-ModuleMember -Function Connect-EntraToken, Get-CurrentEntraToken, Invoke-EntraGraphRequest

# Usage:
# Import-Module .\EntraToken.psm1
# Connect-EntraToken -ProfileName "production"
# Invoke-EntraGraphRequest -Uri "https://graph.microsoft.com/v1.0/me"
```

## Security Best Practices

### Secure Profile Creation

```powershell {linenos=inline}
# Read sensitive data securely
$tenantId = Read-Host -Prompt "Tenant ID"
$clientId = Read-Host -Prompt "Client ID"
$clientSecret = Read-Host -Prompt "Client Secret" -AsSecureString

# Convert SecureString to plain text (only for immediate use)
$bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($clientSecret)
$plainSecret = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)

try {
    entratool create-profile `
        --name "secure-profile" `
        --tenant-id $tenantId `
        --client-id $clientId `
        --client-secret $plainSecret
}
finally {
    # Clear sensitive data
    [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    $plainSecret = $null
}
```

### Credential Manager Integration

```powershell {linenos=inline}
# Store credentials in Windows Credential Manager
cmdkey /generic:EntraTokenCLI /user:$clientId /pass:$clientSecret

# Retrieve from Credential Manager
$credential = Get-StoredCredential -Target "EntraTokenCLI"
$clientSecret = $credential.GetNetworkCredential().Password

# Use in profile creation
entratool create-profile `
    --name "from-credman" `
    --client-secret $clientSecret
```

## Troubleshooting

### DPAPI Errors

**Problem:** "Unable to decrypt token"

**Solutions:**

```powershell {linenos=inline}
# Check user profile
whoami
echo $env:USERNAME

# Verify profile ownership
Get-Acl "$env:LOCALAPPDATA\EntraTokenCLI\profiles\default.token" | Select-Object Owner

# Recreate profile if ownership changed
entratool delete-profile --name default
entratool create-profile --name default
```

### Path Issues

**Problem:** "entratool is not recognized"

**Solutions:**

```powershell {linenos=inline}
# Check PATH
$env:Path -split ';' | Select-String -Pattern "EntraToken"

# Add to PATH permanently
$path = [Environment]::GetEnvironmentVariable("Path", "User")
[Environment]::SetEnvironmentVariable("Path", "$path;C:\Program Files\EntraToken", "User")

# Add to PATH for current session
$env:Path += ";C:\Program Files\EntraToken"

# Verify
Get-Command entratool
```

### PowerShell Execution Policy

**Problem:** "Cannot run scripts"

**Solutions:**

```powershell {linenos=inline}
# Check current policy
Get-ExecutionPolicy

# Set policy for current user
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Or bypass for specific script
PowerShell -ExecutionPolicy Bypass -File .\script.ps1
```

## Performance Optimization

### Token Caching

```powershell {linenos=inline}
# Cache token in memory for script duration
$script:TokenCache = @{}

function Get-CachedToken {
    param([string]$Profile = "default")
    
    $now = Get-Date
    if ($script:TokenCache.ContainsKey($Profile)) {
        $cached = $script:TokenCache[$Profile]
        if ($cached.ExpiresAt -gt $now.AddMinutes(5)) {
            return $cached.Token
        }
    }
    
    # Get fresh token
    $tokenJson = entratool get-token --profile $Profile --output json | ConvertFrom-Json
    $token = $tokenJson.access_token
    $expiresAt = $now.AddSeconds($tokenJson.expires_in)
    
    $script:TokenCache[$Profile] = @{
        Token = $token
        ExpiresAt = $expiresAt
    }
    
    return $token
}
```

### Parallel Execution

```powershell {linenos=inline}
# Parallel API calls with same token
$token = Get-CachedToken
$headers = @{ Authorization = "Bearer $token" }

$users, $groups, $apps = Invoke-Parallel {
    Invoke-RestMethod -Uri "https://graph.microsoft.com/v1.0/users" -Headers $using:headers
    Invoke-RestMethod -Uri "https://graph.microsoft.com/v1.0/groups" -Headers $using:headers
    Invoke-RestMethod -Uri "https://graph.microsoft.com/v1.0/applications" -Headers $using:headers
}
```

## Next Steps

- [macOS Platform Guide](/docs/platform-guides/macos/) - macOS-specific features
- [Linux Platform Guide](/docs/platform-guides/linux/) - Linux-specific features
- [PowerShell Recipes](/docs/recipes/powershell-scripts/) - Advanced PowerShell examples
- [Security Hardening](/docs/recipes/security-hardening/) - Security best practices
