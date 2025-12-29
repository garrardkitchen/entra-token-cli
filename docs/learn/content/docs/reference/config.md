---
title: "config"
description: "Manage authentication profiles"
weight: 50
---

# config

Manage authentication profiles for storing connection settings and credentials.

## Synopsis

```bash {linenos=inline}
entra-auth-cli config <command> [flags]
```

## Description

The `config` command manages authentication profiles, which store tenant information, client credentials, and authentication settings. Profiles allow you to easily switch between different applications, environments, or authentication methods.

Each profile contains:
- Tenant ID
- Client ID
- Authentication method (client secret or certificate)
- Default scopes
- Other configuration options

## Subcommands

### create
Create a new authentication profile.

```bash {linenos=inline}
entra-auth-cli config create
```

**Flags:**
- None - This command is fully interactive

**Examples:**

```bash {linenos=inline}
# Create a profile (fully interactive)
entra-auth-cli config create

# You will be prompted for:
# - Profile name
# - Tenant ID
# - Client ID
# - Authentication method (ClientSecret, Certificate, or PasswordlessCertificate)
# - Credentials (client secret or certificate path)
# - Default OAuth2 flow (optional)
# - Redirect URI (optional)
# - Default scopes (optional)
```

### list
List all configured profiles.

```bash {linenos=inline}
entra-auth-cli config list
```

**Flags:**
- None

**Examples:**

```bash {linenos=inline}
# List all profiles
entra-auth-cli config list
```

### edit
Edit an existing profile.

```bash {linenos=inline}
entra-auth-cli config show [flags]
```

**Flags:**
- `--name`, `-n` - Profile name
- `--output`, `-o` - Output format (text, json, yaml)
- `--show-secrets` - Show sensitive values (use with caution)

**Examples:**

```bash {linenos=inline}
# Show profile details
entra-auth-cli config list | grep production

# JSON format
entra-auth-cli config list | grep production --output json

### edit
Edit an existing profile.

```bash {linenos=inline}
entra-auth-cli config edit -p <profile>
```

**Flags:**
- `-p`, `--profile` - Profile name to edit (required)

**Examples:**

```bash {linenos=inline}
# Interactive edit (will prompt for all fields)
entra-auth-cli config edit -p production

# The command will interactively ask which fields to update:
# - Tenant ID
# - Client ID
# - Authentication method
# - Credentials
# - Default OAuth2 flow
# - Redirect URI
# - Default scopes
```

### delete
Delete a profile.

```bash {linenos=inline}
entra-auth-cli config delete -p <profile>
```

**Flags:**
- `-p`, `--profile` - Profile name to delete (required)

**Examples:**

```bash {linenos=inline}
# Delete with confirmation prompt
entra-auth-cli config delete -p old-profile

# Delete multiple profiles
for profile in old-dev old-test old-staging; do
    entra-auth-cli config delete -p "$profile"
done
```

### export
Export profile configuration to a file.

```bash {linenos=inline}
entra-auth-cli config export -p <profile> -o <file> [--include-secrets]
```

**Flags:**
- `-p`, `--profile` - Profile name to export (required)
- `-o`, `--output` - Output file path (required)
- `--include-secrets` - Include secrets in export (optional)

**Important:** Export requires entering a passphrase to encrypt the exported data.

**Examples:**

```bash {linenos=inline}
# Export a profile (will prompt for encryption passphrase)
entra-auth-cli config export -p production -o production-profile.enc

# Export with secrets (will prompt for encryption passphrase)
entra-auth-cli config export -p production --include-secrets -o backup.enc
```

### import
Import profile configuration from a file.

```bash {linenos=inline}
entra-auth-cli config import -i <file> [-n <new-name>]
```

**Flags:**
- `-i`, `--input` - Input file path (required)
- `-n`, `--name` - New profile name (optional, renames the profile)

**Important:** Import requires entering the passphrase used during export.

**Examples:**

```bash {linenos=inline}
# Import from file (will prompt for decryption passphrase)
entra-auth-cli config import -i production-profile.enc

# Import with a new name
entra-auth-cli config import -i production-profile.enc -n production-v2
```

## Complete Examples

### Creating Profiles

```bash {linenos=inline}
# Create a profile (fully interactive)
entra-auth-cli config create

# You'll be prompted for all required fields:
# - Profile name: production
# - Tenant ID: 12345678-1234-1234-1234-123456789012
# - Client ID: 87654321-4321-4321-4321-210987654321
# - Auth method: ClientSecret (or Certificate/PasswordlessCertificate)
# - Client secret: (hidden input)
# - Default OAuth2 flow: (optional)
# - Redirect URI: (optional)
# - Default scopes: https://graph.microsoft.com/.default
```

### Managing Profiles

#### List and View

```bash {linenos=inline}
# List all profiles
entra-auth-cli config list

# Output:
# production
# staging
# dev
# graph-api
```

#### Update Configuration

```bash {linenos=inline}
# Edit profile interactively
entra-auth-cli config edit -p production

# Will prompt for:
# - What to update (tenant, client ID, auth method, credentials, etc.)
# - New values for selected fields
```

### Backup and Migration

#### Backup Profiles

```bash {linenos=inline}
#!/bin/bash

# Backup all profiles
BACKUP_DIR="./profile-backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

mkdir -p "$BACKUP_DIR"

# Export each profile
for profile in $(entra-auth-cli config list | grep -v "^$"); do
    echo "Backing up profile: $profile"
    # Will prompt for encryption passphrase for each profile
    entra-auth-cli config export -p "$profile" --include-secrets \
      -o "$BACKUP_DIR/${profile}_${TIMESTAMP}.enc"
done

echo "Backup complete: $BACKUP_DIR"
```

#### Migrate to New Machine

```bash {linenos=inline}
# On old machine (will prompt for encryption passphrase)
entra-auth-cli config export -p production --include-secrets -o profiles-backup.enc

# Transfer file to new machine
scp profiles-backup.enc user@newmachine:/tmp/

# On new machine (will prompt for decryption passphrase)
entra-auth-cli config import -i /tmp/profiles-backup.enc

# Verify
entra-auth-cli config list
```

### Validation Script

```bash {linenos=inline}
#!/bin/bash

# Validate all profiles can generate tokens
echo "Validating profiles..."

failed=0
for profile in $(entra-auth-cli config list | grep -v "^$"); do
    echo -n "Testing $profile... "
    
    if entra-auth-cli get-token -p "$profile" > /dev/null 2>&1; then
        echo "✓ OK"
    else
        echo "✗ FAILED"
        failed=$((failed + 1))
    fi
done

if [ $failed -eq 0 ]; then
    echo "All profiles valid!"
    exit 0
else
    echo "$failed profile(s) failed validation"
    exit 1
fi
```

## Profile Storage

Profiles are stored in platform-specific locations:

### Windows
```
%LOCALAPPDATA%\EntraAuthCli\profiles\
```

### macOS
```
~/Library/Application Support/entra-auth-cli/profiles/
```

### Linux
```
~/.entra-auth-cli/profiles/
```

Each profile consists of:
- `profile-name.json` - Configuration
- `profile-name.token` - Encrypted tokens (if cached)

## Security Best Practices

### Secrets Management

```bash {linenos=inline}
# ✅ Good - use interactive prompt (hides input)
entra-auth-cli config create  # Will prompt for secret securely

# ❌ Bad - don't expose secrets in command history or scripts
CLIENT_SECRET="my-secret-123"
entra-auth-cli config create  # Even in scripts, use interactive mode
```

### Profile Naming

```bash {linenos=inline}
# ✅ Good - descriptive names
# Create profiles with clear names (done interactively)
entra-auth-cli config create
# Then name them: prod-graph-api, staging-azure-mgmt, dev-user-app

# ❌ Avoid - generic names
# app1, test, etc.
```

### Regular Rotation

```bash {linenos=inline}
#!/bin/bash
# Rotate secrets for all profiles

for profile in $(entra-auth-cli config list | grep -v "^$"); do
    echo "Rotating secret for $profile"
    
    # Get new secret from vault
    NEW_SECRET=$(vault read -field=secret "secret/azure/$profile")
    
    # Update profile (will prompt interactively)
    entra-auth-cli config edit -p "$profile"
    
    # Verify
    if entra-auth-cli get-token -p "$profile" > /dev/null; then
        echo "✓ $profile updated successfully"
    else
        echo "✗ $profile update failed"
    fi
done
```

## Troubleshooting

### Profile Not Found

```bash {linenos=inline}
# List available profiles
entra-auth-cli config list

# Profile names are case-sensitive
# "Production" won't match "production"
```

### Cannot Create Profile

```bash {linenos=inline}
# Check storage directory permissions
# Linux/macOS
ls -la ~/.entra-auth-cli/profiles/
chmod 700 ~/.entra-auth-cli/profiles/

# Windows (PowerShell)
Test-Path "$env:LOCALAPPDATA\EntraAuthCli\profiles"
```

### Profile Corruption

```bash {linenos=inline}
# View raw profile file
# Linux/macOS
cat ~/.entra-auth-cli/profiles/production.json

# If corrupted, delete and recreate
entra-auth-cli config delete -p production
entra-auth-cli config create
```

## See Also

- [Managing Profiles](/docs/user-guide/managing-profiles/) - Detailed profile management guide
- [get-token](/docs/reference/get-token/) - Generate tokens using profiles
- [Security Best Practices](/docs/recipes/security-hardening/) - Secure configuration practices
