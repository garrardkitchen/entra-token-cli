---
title: "Profile Issues"
description: "Troubleshooting profile configuration and access problems"
weight: 10
---

# Profile Issues

Common problems with profile configuration, access, and management.

## Profile Not Found

### Problem

```bash
$ entratool get-token --profile production
Error: profile 'production' not found
```

### Solutions

#### 1. List Available Profiles

```bash
# See all configured profiles
entratool list-profiles

# Check if profile exists with different name
entratool list-profiles | grep -i prod
```

#### 2. Create Missing Profile

```bash
# Create the profile
entratool create-profile --name production

# Or use interactive mode
entratool create-profile
```

#### 3. Use Default Profile

```bash
# If you meant to use default profile
entratool get-token

# Check default profile
entratool list-profiles | grep default
```

## Profile Already Exists

### Problem

```bash
$ entratool create-profile --name dev
Error: profile 'dev' already exists
```

### Solutions

#### 1. Use Existing Profile

```bash
# View existing profile
entratool show-profile --name dev

# Edit if needed
entratool edit-profile --name dev
```

#### 2. Delete and Recreate

```bash
# Delete existing profile
entratool delete-profile --name dev

# Create new one
entratool create-profile --name dev
```

#### 3. Use Different Name

```bash
# Create with different name
entratool create-profile --name dev-new
entratool create-profile --name dev-v2
```

## Invalid Profile Configuration

### Problem

```bash
$ entratool get-token --profile myapp
Error: invalid profile configuration: missing tenant_id
```

### Solutions

#### 1. Check Profile Configuration

```bash
# View profile details
entratool show-profile --name myapp

# Look for missing fields:
# - tenant_id
# - client_id
# - auth_method
```

#### 2. Edit Profile

```bash
# Interactive edit
entratool edit-profile --name myapp

# Or delete and recreate
entratool delete-profile --name myapp
entratool create-profile --name myapp
```

#### 3. Validate Required Fields

```bash
# Profile must have:
# - Tenant ID (GUID)
# - Client ID (GUID)
# - Auth method (client_secret or certificate)
# - Scopes (optional, has defaults)

# Example valid configuration:
{
  "name": "myapp",
  "tenant_id": "12345678-1234-1234-1234-123456789012",
  "client_id": "87654321-4321-4321-4321-210987654321",
  "auth_method": "client_secret",
  "scopes": ["https://graph.microsoft.com/.default"]
}
```

## Cannot Access Secure Storage

### Problem

**Windows:**
```bash
Error: failed to access DPAPI encrypted storage
```

**macOS:**
```bash
Error: failed to access Keychain: user canceled
```

**Linux:**
```bash
Error: permission denied accessing /home/user/.entratool/profiles/
```

### Solutions

#### Windows (DPAPI)

```powershell
# 1. Check user profile
whoami

# 2. Verify storage location
$env:LOCALAPPDATA\EntraTokenCLI\profiles

# 3. Check permissions
Get-Acl "$env:LOCALAPPDATA\EntraTokenCLI\profiles" | Format-List

# 4. Recreate profile if user changed
entratool delete-profile --name myapp
entratool create-profile --name myapp
```

#### macOS (Keychain)

```bash
# 1. Allow Keychain access
# Click "Always Allow" when prompted

# 2. Reset Keychain permissions
security delete-generic-password -s "com.garrardkitchen.entratool-cli" -a "myapp"
entratool create-profile --name myapp

# 3. Check Keychain Access app
open -a "Keychain Access"
# Search for: entratool-cli

# 4. Unlock Keychain if locked
security unlock-keychain ~/Library/Keychains/login.keychain-db
```

#### Linux (File Permissions)

```bash
# 1. Check permissions
ls -la ~/.entratool/profiles/

# 2. Fix permissions
chmod 700 ~/.entratool/profiles/
chmod 600 ~/.entratool/profiles/*

# 3. Check ownership
ls -la ~/.entratool/
chown -R $USER:$USER ~/.entratool/

# 4. Recreate if needed
rm -rf ~/.entratool/profiles/
entratool create-profile --name myapp
```

## Profile Corruption

### Problem

```bash
$ entratool get-token --profile prod
Error: failed to parse profile: invalid JSON
```

### Solutions

#### 1. View Raw Profile

```bash
# Windows
type %LOCALAPPDATA%\EntraTokenCLI\profiles\prod.json

# macOS/Linux
cat ~/.entratool/profiles/prod.json
```

#### 2. Delete Corrupted Profile

```bash
# Delete via CLI
entratool delete-profile --name prod

# Or manually
# Windows
del %LOCALAPPDATA%\EntraTokenCLI\profiles\prod.*

# macOS/Linux
rm ~/.entratool/profiles/prod.*
```

#### 3. Recreate Profile

```bash
entratool create-profile --name prod
```

## Migration Issues

### Problem

```bash
# After moving to new machine
$ entratool get-token --profile myapp
Error: cannot decrypt token
```

### Solutions

#### 1. Understand Encryption

Tokens are encrypted per-machine and per-user:
- **Windows**: DPAPI (machine-specific)
- **macOS**: Keychain (can sync via iCloud)
- **Linux**: Machine ID + User ID

#### 2. Export and Import Profiles

```bash
# On old machine - export (configuration only, not tokens)
entratool export-profile --name myapp > myapp-profile.json

# On new machine - import
entratool import-profile < myapp-profile.json

# Re-authenticate on new machine
entratool get-token --profile myapp --force
```

#### 3. Recreate Profiles

```bash
# Easiest solution: recreate profiles on new machine
entratool create-profile --name myapp
# Enter same tenant ID, client ID, etc.
```

## Permission Denied

### Problem

```bash
$ entratool create-profile
Error: permission denied: cannot write to profiles directory
```

### Solutions

#### 1. Check Directory Permissions

```bash
# Linux/macOS
ls -ld ~/.entratool/
mkdir -p ~/.entratool/profiles
chmod 700 ~/.entratool/profiles

# Windows (PowerShell)
Test-Path "$env:LOCALAPPDATA\EntraTokenCLI"
New-Item -ItemType Directory -Force -Path "$env:LOCALAPPDATA\EntraTokenCLI\profiles"
```

#### 2. Check Disk Space

```bash
# Linux/macOS
df -h ~

# Windows
Get-PSDrive C
```

#### 3. Run as User (Not Root)

```bash
# Don't use sudo
sudo entratool create-profile  # ❌ Wrong

# Run as regular user
entratool create-profile       # ✅ Correct
```

## Profile Name Conflicts

### Problem

```bash
$ entratool create-profile --name "My Profile"
Error: invalid profile name: spaces not allowed
```

### Solutions

#### 1. Use Valid Names

```bash
# Valid profile names:
entratool create-profile --name my-profile
entratool create-profile --name my_profile
entratool create-profile --name myProfile
entratool create-profile --name myprofile

# Invalid (will fail):
entratool create-profile --name "my profile"  # spaces
entratool create-profile --name "my/profile"  # slashes
entratool create-profile --name "my@profile"  # special chars
```

#### 2. Naming Conventions

```bash
# Recommended patterns:
entratool create-profile --name production
entratool create-profile --name dev
entratool create-profile --name staging

# By purpose:
entratool create-profile --name graph-reader
entratool create-profile --name azure-deployer

# By environment:
entratool create-profile --name prod-api
entratool create-profile --name dev-testing
```

## Duplicate Tenant/Client Combinations

### Problem

Multiple profiles with same tenant/client but different configurations cause confusion.

### Solutions

#### 1. Use Descriptive Names

```bash
# Instead of:
entratool create-profile --name app1
entratool create-profile --name app2

# Use descriptive names:
entratool create-profile --name graph-readonly
entratool create-profile --name graph-admin
```

#### 2. List Profiles with Details

```bash
# Show all profiles with details
entratool list-profiles --verbose

# Or manually check
entratool show-profile --name profile1
entratool show-profile --name profile2
```

## Diagnostic Steps

### General Troubleshooting

```bash
# 1. List all profiles
entratool list-profiles

# 2. Check specific profile
entratool show-profile --name myapp

# 3. Verify token generation works
entratool get-token --profile myapp --output json

# 4. Check profile file exists
# Windows
dir %LOCALAPPDATA%\EntraTokenCLI\profiles\

# macOS/Linux
ls -la ~/.entratool/profiles/

# 5. Test with default profile
entratool get-token
```

### Verify Azure Configuration

```bash
# Check app registration exists
az ad app show --id YOUR_CLIENT_ID

# Verify credentials
az ad app credential list --id YOUR_CLIENT_ID

# Check permissions
az ad app permission list --id YOUR_CLIENT_ID
```

## Prevention

### Best Practices

```bash
# 1. Use descriptive profile names
entratool create-profile --name prod-graph-api

# 2. Document your profiles
cat > profiles.md << 'EOF'
# Entra Token Profiles

- production: Production Graph API access
- dev: Development environment
- cicd: CI/CD pipeline authentication
EOF

# 3. Backup profile configurations
entratool export-profile --name production > backup/production.json

# 4. Regular validation
entratool get-token --profile production --force
```

## See Also

- [Managing Profiles](/docs/user-guide/managing-profiles/) - Profile management guide
- [Authentication Issues](/docs/troubleshooting/authentication/) - Authentication problems
- [Platform Guides](/docs/platform-guides/) - Platform-specific information
