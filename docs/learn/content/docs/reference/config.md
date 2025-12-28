---
title: "config"
description: "Manage authentication profiles"
weight: 50
---

# config

Manage authentication profiles for storing connection settings and credentials.

## Synopsis

```bash
entratool config <command> [flags]
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

```bash
entratool config create [flags]
```

**Flags:**
- `--name`, `-n` - Profile name
- `--tenant-id` - Azure tenant ID
- `--client-id` - Application client ID
- `--client-secret` - Client secret (interactive prompt if not provided)
- `--certificate` - Path to certificate file (PFX/PEM)
- `--certificate-password` - Certificate password
- `--scope` - Default scopes (space-separated)
- `--interactive` - Use interactive prompts for all fields

**Examples:**

```bash
# Interactive creation (recommended)
entratool config create

# With flags
entratool config create \
  --name production \
  --tenant-id "12345678-1234-1234-1234-123456789012" \
  --client-id "87654321-4321-4321-4321-210987654321" \
  --client-secret "your-secret"

# With certificate
entratool config create \
  --name secure-app \
  --tenant-id "$TENANT_ID" \
  --client-id "$CLIENT_ID" \
  --certificate "./cert.pfx" \
  --certificate-password "password"

# With custom scopes
entratool config create \
  --name graph-api \
  --scope "User.Read Mail.Read Calendars.Read"
```

### list

List all configured profiles.

```bash
entratool config list [flags]
```

**Flags:**
- `--output`, `-o` - Output format (text, json, yaml)
- `--verbose`, `-v` - Show additional details

**Examples:**

```bash
# Simple list
entratool config list

# With details
entratool config list --verbose

# JSON output
entratool config list --output json
```

### show

Display detailed information about a profile.

```bash
entratool config show [flags]
```

**Flags:**
- `--name`, `-n` - Profile name
- `--output`, `-o` - Output format (text, json, yaml)
- `--show-secrets` - Show sensitive values (use with caution)

**Examples:**

```bash
# Show profile details
entratool config show --name production

# JSON format
entratool config show --name production --output json

# Show with secrets (sensitive)
entratool config show --name production --show-secrets
```

### edit

Edit an existing profile.

```bash
entratool config edit [flags]
```

**Flags:**
- `--name`, `-n` - Profile name
- `--tenant-id` - Update tenant ID
- `--client-id` - Update client ID
- `--client-secret` - Update client secret
- `--certificate` - Update certificate
- `--scope` - Update default scopes
- `--interactive` - Use interactive editor

**Examples:**

```bash
# Interactive edit
entratool config edit --name production

# Update specific fields
entratool config edit --name production --scope "User.Read Mail.Send"

# Update credentials
entratool config edit --name production --client-secret "new-secret"

# Switch to certificate auth
entratool config edit --name production \
  --certificate "./new-cert.pfx" \
  --certificate-password "password"
```

### delete

Delete a profile.

```bash
entratool config delete [flags]
```

**Flags:**
- `--name`, `-n` - Profile name
- `--force`, `-f` - Skip confirmation prompt

**Examples:**

```bash
# Delete with confirmation
entratool config delete --name old-profile

# Force delete (no confirmation)
entratool config delete --name old-profile --force

# Delete multiple profiles
for profile in old-dev old-test old-staging; do
    entratool config delete --name "$profile" --force
done
```

### export

Export profile configuration to a file.

```bash
entratool config export [flags]
```

**Flags:**
- `--name`, `-n` - Profile name (or all profiles if omitted)
- `--output`, `-o` - Output file path
- `--include-secrets` - Include sensitive data (use with caution)

**Examples:**

```bash
# Export single profile
entratool config export --name production > production-profile.json

# Export all profiles
entratool config export > all-profiles.json

# Export with secrets (for backup/migration)
entratool config export --name production --include-secrets > backup.json
```

### import

Import profile configuration from a file.

```bash
entratool config import [flags]
```

**Flags:**
- `--file`, `-f` - Input file path
- `--overwrite` - Overwrite existing profiles
- `--dry-run` - Show what would be imported without making changes

**Examples:**

```bash
# Import from file
entratool config import --file production-profile.json

# Import from stdin
cat production-profile.json | entratool config import

# Import with overwrite
entratool config import --file all-profiles.json --overwrite

# Dry run to preview
entratool config import --file backup.json --dry-run
```

## Complete Examples

### Creating Profiles

#### Client Secret Authentication

```bash
# Interactive (recommended for first-time users)
entratool config create

# You'll be prompted for:
# - Profile name: production
# - Tenant ID: 12345678-1234-1234-1234-123456789012
# - Client ID: 87654321-4321-4321-4321-210987654321
# - Auth method: client-secret
# - Client secret: (hidden input)
# - Scopes: https://graph.microsoft.com/.default
```

#### Certificate Authentication

```bash
# Create with certificate
entratool config create \
  --name secure-prod \
  --tenant-id "$TENANT_ID" \
  --client-id "$CLIENT_ID" \
  --certificate "./certs/production.pfx" \
  --certificate-password "$CERT_PASSWORD" \
  --scope "https://graph.microsoft.com/.default"
```

#### Multiple Environments

```bash
# Development environment
entratool config create --name dev \
  --tenant-id "$DEV_TENANT_ID" \
  --client-id "$DEV_CLIENT_ID" \
  --client-secret "$DEV_SECRET"

# Staging environment
entratool config create --name staging \
  --tenant-id "$STAGING_TENANT_ID" \
  --client-id "$STAGING_CLIENT_ID" \
  --client-secret "$STAGING_SECRET"

# Production environment
entratool config create --name production \
  --tenant-id "$PROD_TENANT_ID" \
  --client-id "$PROD_CLIENT_ID" \
  --certificate "./certs/prod.pfx"
```

### Managing Profiles

#### List and View

```bash
# List all profiles
entratool config list

# Output:
# default
# production
# staging
# dev
# graph-api

# Show details of specific profile
entratool config show --name production

# Output:
# Profile: production
# Tenant ID: 12345678-1234-1234-1234-123456789012
# Client ID: 87654321-4321-4321-4321-210987654321
# Auth Method: certificate
# Certificate: /path/to/cert.pfx
# Default Scopes: https://graph.microsoft.com/.default
```

#### Update Configuration

```bash
# Change default scopes
entratool config edit --name production \
  --scope "User.Read Mail.Read Calendars.Read"

# Rotate client secret
entratool config edit --name production \
  --client-secret "$NEW_SECRET"

# Switch from secret to certificate
entratool config edit --name production \
  --certificate "./new-cert.pfx" \
  --certificate-password "$CERT_PASS"
```

### Backup and Migration

#### Backup Profiles

```bash
#!/bin/bash

# Backup all profiles
BACKUP_DIR="./profile-backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

mkdir -p "$BACKUP_DIR"

# Export each profile
for profile in $(entratool config list); do
    echo "Backing up profile: $profile"
    entratool config export --name "$profile" --include-secrets \
      > "$BACKUP_DIR/${profile}_${TIMESTAMP}.json"
done

echo "Backup complete: $BACKUP_DIR"
```

#### Migrate to New Machine

```bash
# On old machine
entratool config export --include-secrets > profiles-backup.json

# Transfer file to new machine
scp profiles-backup.json user@newmachine:/tmp/

# On new machine
entratool config import --file /tmp/profiles-backup.json

# Verify
entratool config list
```

### Environment-Based Configuration

```bash
#!/bin/bash

# Script to create profiles based on environment
create_env_profile() {
    local env="$1"
    local tenant_var="${env}_TENANT_ID"
    local client_var="${env}_CLIENT_ID"
    local secret_var="${env}_CLIENT_SECRET"
    
    echo "Creating profile for $env environment..."
    
    entratool config create \
        --name "$env" \
        --tenant-id "${!tenant_var}" \
        --client-id "${!client_var}" \
        --client-secret "${!secret_var}" \
        --scope "https://graph.microsoft.com/.default"
}

# Create profiles for each environment
for env in DEV STAGING PRODUCTION; do
    create_env_profile "$env"
done

echo "All profiles created:"
entratool config list
```

### Validation Script

```bash
#!/bin/bash

# Validate all profiles can generate tokens
echo "Validating profiles..."

failed=0
for profile in $(entratool config list); do
    echo -n "Testing $profile... "
    
    if entratool get-token --profile "$profile" --output json > /dev/null 2>&1; then
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
%LOCALAPPDATA%\EntraTokenCLI\profiles\
```

### macOS
```
~/Library/Application Support/entratool/profiles/
```

### Linux
```
~/.entratool/profiles/
```

Each profile consists of:
- `profile-name.json` - Configuration
- `profile-name.token` - Encrypted tokens (if cached)

## Security Best Practices

### Secrets Management

```bash
# ✅ Good - use environment variables
export CLIENT_SECRET=$(vault read -field=secret secret/azure/client)
entratool config create --client-secret "$CLIENT_SECRET"

# ✅ Good - interactive prompt (hides input)
entratool config create  # Will prompt for secret

# ❌ Bad - hardcoded in script
entratool config create --client-secret "my-secret-123"

# ❌ Bad - visible in command history
entratool config create --client-secret "$SECRET"  # If $SECRET expands
```

### Profile Naming

```bash
# ✅ Good - descriptive names
entratool config create --name prod-graph-api
entratool config create --name staging-azure-mgmt
entratool config create --name dev-user-app

# ❌ Avoid - generic names
entratool config create --name app1
entratool config create --name test
```

### Regular Rotation

```bash
#!/bin/bash
# Rotate secrets for all profiles

for profile in $(entratool config list); do
    echo "Rotating secret for $profile"
    
    # Get new secret from vault
    NEW_SECRET=$(vault read -field=secret "secret/azure/$profile")
    
    # Update profile
    entratool config edit --name "$profile" --client-secret "$NEW_SECRET"
    
    # Verify
    if entratool get-token --profile "$profile" > /dev/null; then
        echo "✓ $profile updated successfully"
    else
        echo "✗ $profile update failed"
    fi
done
```

## Troubleshooting

### Profile Not Found

```bash
# List available profiles
entratool config list

# Check exact name (case-sensitive)
entratool config show --name Production  # Won't match "production"
```

### Cannot Create Profile

```bash
# Check storage directory permissions
# Linux/macOS
ls -la ~/.entratool/profiles/
chmod 700 ~/.entratool/profiles/

# Windows (PowerShell)
Test-Path "$env:LOCALAPPDATA\EntraTokenCLI\profiles"
```

### Profile Corruption

```bash
# View raw profile file
# Linux/macOS
cat ~/.entratool/profiles/production.json

# If corrupted, delete and recreate
entratool config delete --name production --force
entratool config create --name production
```

## See Also

- [Managing Profiles](/docs/user-guide/managing-profiles/) - Detailed profile management guide
- [get-token](/docs/reference/get-token/) - Generate tokens using profiles
- [Security Best Practices](/docs/recipes/security-hardening/) - Secure configuration practices
