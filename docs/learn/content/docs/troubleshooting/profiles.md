---
title: "Profile Issues"
description: "Troubleshooting profile configuration and access problems"
weight: 10
---

# Profile Issues

Common problems with profile configuration, access, and management.

## Profile Not Found

### Problem

```bash {linenos=inline}
$ entra-auth-cli get-token --profile production
Error: profile 'production' not found
```

### Solutions

#### 1. List Available Profiles

```bash {linenos=inline}
# See all configured profiles
entra-auth-cli list-profiles

# Check if profile exists with different name
entra-auth-cli list-profiles | grep -i prod
```

#### 2. Create Missing Profile

```bash {linenos=inline}
# Create the profile
entra-auth-cli create-profile --name production

# Or use interactive mode
entra-auth-cli create-profile
```

#### 3. Use Default Profile

```bash {linenos=inline}
# If you meant to use default profile
entra-auth-cli get-token

# Check default profile
entra-auth-cli list-profiles | grep default
```

## Profile Already Exists

### Problem

```bash {linenos=inline}
$ entra-auth-cli create-profile --name dev
Error: profile 'dev' already exists
```

### Solutions

#### 1. Use Existing Profile

```bash {linenos=inline}
# View existing profile
entra-auth-cli show-profile --name dev

# Edit if needed
entra-auth-cli edit-profile --name dev
```

#### 2. Delete and Recreate

```bash {linenos=inline}
# Delete existing profile
entra-auth-cli delete-profile --name dev

# Create new one
entra-auth-cli create-profile --name dev
```

#### 3. Use Different Name

```bash {linenos=inline}
# Create with different name
entra-auth-cli create-profile --name dev-new
entra-auth-cli create-profile --name dev-v2
```

## Invalid Profile Configuration

### Problem

```bash {linenos=inline}
$ entra-auth-cli get-token --profile myapp
Error: invalid profile configuration: missing tenant_id
```

### Solutions

#### 1. Check Profile Configuration

```bash {linenos=inline}
# View profile details
entra-auth-cli show-profile --name myapp

# Look for missing fields:
# - tenant_id
# - client_id
# - auth_method
```

#### 2. Edit Profile

```bash {linenos=inline}
# Interactive edit
entra-auth-cli edit-profile --name myapp

# Or delete and recreate
entra-auth-cli delete-profile --name myapp
entra-auth-cli create-profile --name myapp
```

#### 3. Validate Required Fields

```bash {linenos=inline}
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
```bash {linenos=inline}
Error: failed to access DPAPI encrypted storage
```

**macOS:**
```bash {linenos=inline}
Error: failed to access Keychain: user canceled
```

**Linux:**
```bash {linenos=inline}
Error: permission denied accessing /home/user/.entra-auth-cli/profiles/
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
entra-auth-cli delete-profile --name myapp
entra-auth-cli create-profile --name myapp
```

#### macOS (Keychain)

```bash {linenos=inline}
# 1. Allow Keychain access
# Click "Always Allow" when prompted

# 2. Reset Keychain permissions
security delete-generic-password -s "com.garrardkitchen.entra-auth-cli-cli" -a "myapp"
entra-auth-cli create-profile --name myapp

# 3. Check Keychain Access app
open -a "Keychain Access"
# Search for: entra-auth-cli-cli

# 4. Unlock Keychain if locked
security unlock-keychain ~/Library/Keychains/login.keychain-db
```

#### Linux (File Permissions)

```bash {linenos=inline}
# 1. Check permissions
ls -la ~/.entra-auth-cli/profiles/

# 2. Fix permissions
chmod 700 ~/.entra-auth-cli/profiles/
chmod 600 ~/.entra-auth-cli/profiles/*

# 3. Check ownership
ls -la ~/.entra-auth-cli/
chown -R $USER:$USER ~/.entra-auth-cli/

# 4. Recreate if needed
rm -rf ~/.entra-auth-cli/profiles/
entra-auth-cli create-profile --name myapp
```

## Profile Corruption

### Problem

```bash {linenos=inline}
$ entra-auth-cli get-token --profile prod
Error: failed to parse profile: invalid JSON
```

### Solutions

#### 1. View Raw Profile

```bash {linenos=inline}
# Windows
type %LOCALAPPDATA%\EntraTokenCLI\profiles\prod.json

# macOS/Linux
cat ~/.entra-auth-cli/profiles/prod.json
```

#### 2. Delete Corrupted Profile

```bash {linenos=inline}
# Delete via CLI
entra-auth-cli delete-profile --name prod

# Or manually
# Windows
del %LOCALAPPDATA%\EntraTokenCLI\profiles\prod.*

# macOS/Linux
rm ~/.entra-auth-cli/profiles/prod.*
```

#### 3. Recreate Profile

```bash {linenos=inline}
entra-auth-cli create-profile --name prod
```

## Migration Issues

### Problem

```bash {linenos=inline}
# After moving to new machine
$ entra-auth-cli get-token --profile myapp
Error: cannot decrypt token
```

### Solutions

#### 1. Understand Encryption

Tokens are encrypted per-machine and per-user:
- **Windows**: DPAPI (machine-specific)
- **macOS**: Keychain (can sync via iCloud)
- **Linux**: Machine ID + User ID

#### 2. Export and Import Profiles

```bash {linenos=inline}
# On old machine - export (configuration only, not tokens)
entra-auth-cli export-profile --name myapp > myapp-profile.json

# On new machine - import
entra-auth-cli import-profile < myapp-profile.json

# Re-authenticate on new machine
entra-auth-cli get-token --profile myapp --force
```

#### 3. Recreate Profiles

```bash {linenos=inline}
# Easiest solution: recreate profiles on new machine
entra-auth-cli create-profile --name myapp
# Enter same tenant ID, client ID, etc.
```

## Permission Denied

### Problem

```bash {linenos=inline}
$ entra-auth-cli create-profile
Error: permission denied: cannot write to profiles directory
```

### Solutions

#### 1. Check Directory Permissions

```bash {linenos=inline}
# Linux/macOS
ls -ld ~/.entra-auth-cli/
mkdir -p ~/.entra-auth-cli/profiles
chmod 700 ~/.entra-auth-cli/profiles

# Windows (PowerShell)
Test-Path "$env:LOCALAPPDATA\EntraTokenCLI"
New-Item -ItemType Directory -Force -Path "$env:LOCALAPPDATA\EntraTokenCLI\profiles"
```

#### 2. Check Disk Space

```bash {linenos=inline}
# Linux/macOS
df -h ~

# Windows
Get-PSDrive C
```

#### 3. Run as User (Not Root)

```bash {linenos=inline}
# Don't use sudo
sudo entra-auth-cli create-profile  # ❌ Wrong

# Run as regular user
entra-auth-cli create-profile       # ✅ Correct
```

## Profile Name Conflicts

### Problem

```bash {linenos=inline}
$ entra-auth-cli create-profile --name "My Profile"
Error: invalid profile name: spaces not allowed
```

### Solutions

#### 1. Use Valid Names

```bash {linenos=inline}
# Valid profile names:
entra-auth-cli create-profile --name my-profile
entra-auth-cli create-profile --name my_profile
entra-auth-cli create-profile --name myProfile
entra-auth-cli create-profile --name myprofile

# Invalid (will fail):
entra-auth-cli create-profile --name "my profile"  # spaces
entra-auth-cli create-profile --name "my/profile"  # slashes
entra-auth-cli create-profile --name "my@profile"  # special chars
```

#### 2. Naming Conventions

```bash {linenos=inline}
# Recommended patterns:
entra-auth-cli create-profile --name production
entra-auth-cli create-profile --name dev
entra-auth-cli create-profile --name staging

# By purpose:
entra-auth-cli create-profile --name graph-reader
entra-auth-cli create-profile --name azure-deployer

# By environment:
entra-auth-cli create-profile --name prod-api
entra-auth-cli create-profile --name dev-testing
```

## Duplicate Tenant/Client Combinations

### Problem

Multiple profiles with same tenant/client but different configurations cause confusion.

### Solutions

#### 1. Use Descriptive Names

```bash {linenos=inline}
# Instead of:
entra-auth-cli create-profile --name app1
entra-auth-cli create-profile --name app2

# Use descriptive names:
entra-auth-cli create-profile --name graph-readonly
entra-auth-cli create-profile --name graph-admin
```

#### 2. List Profiles with Details

```bash {linenos=inline}
# Show all profiles with details
entra-auth-cli list-profiles --verbose

# Or manually check
entra-auth-cli show-profile --name profile1
entra-auth-cli show-profile --name profile2
```

## Diagnostic Steps

### General Troubleshooting

```bash {linenos=inline}
# 1. List all profiles
entra-auth-cli list-profiles

# 2. Check specific profile
entra-auth-cli show-profile --name myapp

# 3. Verify token generation works
entra-auth-cli get-token --profile myapp --output json

# 4. Check profile file exists
# Windows
dir %LOCALAPPDATA%\EntraTokenCLI\profiles\

# macOS/Linux
ls -la ~/.entra-auth-cli/profiles/

# 5. Test with default profile
entra-auth-cli get-token
```

### Verify Azure Configuration

```bash {linenos=inline}
# Check app registration exists
az ad app show --id YOUR_CLIENT_ID

# Verify credentials
az ad app credential list --id YOUR_CLIENT_ID

# Check permissions
az ad app permission list --id YOUR_CLIENT_ID
```

## Prevention

### Best Practices

```bash {linenos=inline}
# 1. Use descriptive profile names
entra-auth-cli create-profile --name prod-graph-api

# 2. Document your profiles
cat > profiles.md << 'EOF'
# Entra Token Profiles

- production: Production Graph API access
- dev: Development environment
- cicd: CI/CD pipeline authentication
EOF

# 3. Backup profile configurations
entra-auth-cli export-profile --name production > backup/production.json

# 4. Regular validation
entra-auth-cli get-token --profile production --force
```

## See Also

- [Managing Profiles](/docs/user-guide/managing-profiles/) - Profile management guide
- [Authentication Issues](/docs/troubleshooting/authentication/) - Authentication problems
- [Platform Guides](/docs/platform-guides/) - Platform-specific information
