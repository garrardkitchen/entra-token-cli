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
entra-auth-cli config create [flags]
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

```bash {linenos=inline}
# Interactive creation (recommended)
entra-auth-cli config create

# With flags
entra-auth-cli config create \
  --name production \
  --tenant-id "12345678-1234-1234-1234-123456789012" \
  --client-id "87654321-4321-4321-4321-210987654321" \
  --client-secret "your-secret"

# With certificate
entra-auth-cli config create \
  --name secure-app \
  --tenant-id "$TENANT_ID" \
  --client-id "$CLIENT_ID" \
  --certificate "./cert.pfx" \
  --certificate-password "password"

# With custom scopes
entra-auth-cli config create \
  --name graph-api \
  --scope "User.Read Mail.Read Calendars.Read"
```

### list
List all configured profiles.

```bash {linenos=inline}
entra-auth-cli config list [flags]
```

**Flags:**
- `--output`, `-o` - Output format (text, json, yaml)
- `--verbose`, `-v` - Show additional details

**Examples:**

```bash {linenos=inline}
# Simple list
entra-auth-cli config list

# With details
entra-auth-cli config list --verbose

# JSON output
entra-auth-cli config list --output json
```

### show
Display detailed information about a profile.

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
entra-auth-cli config show --name production

# JSON format
entra-auth-cli config show --name production --output json

# Show with secrets (sensitive)
entra-auth-cli config show --name production --show-secrets
```

### edit
Edit an existing profile.

```bash {linenos=inline}
entra-auth-cli config edit [flags]
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

```bash {linenos=inline}
# Interactive edit
entra-auth-cli config edit --name production

# Update specific fields
entra-auth-cli config edit --name production --scope "User.Read Mail.Send"

# Update credentials
entra-auth-cli config edit --name production --client-secret "new-secret"

# Switch to certificate auth
entra-auth-cli config edit --name production \
  --certificate "./new-cert.pfx" \
  --certificate-password "password"
```

### delete
Delete a profile.

```bash {linenos=inline}
entra-auth-cli config delete [flags]
```

**Flags:**
- `--name`, `-n` - Profile name
- `--force`, `-f` - Skip confirmation prompt

**Examples:**

```bash {linenos=inline}
# Delete with confirmation
entra-auth-cli config delete --name old-profile

# Force delete (no confirmation)
entra-auth-cli config delete --name old-profile --force

# Delete multiple profiles
for profile in old-dev old-test old-staging; do
    entra-auth-cli config delete --name "$profile" --force
done
```

### export
Export profile configuration to a file.

```bash {linenos=inline}
entra-auth-cli config export [flags]
```

**Flags:**
- `--name`, `-n` - Profile name (or all profiles if omitted)
- `--output`, `-o` - Output file path
- `--include-secrets` - Include sensitive data (use with caution)

**Examples:**

```bash {linenos=inline}
# Export single profile
entra-auth-cli config export --name production > production-profile.json

# Export all profiles
entra-auth-cli config export > all-profiles.json

# Export with secrets (for backup/migration)
entra-auth-cli config export --name production --include-secrets > backup.json
```

### import
Import profile configuration from a file.

```bash {linenos=inline}
entra-auth-cli config import [flags]
```

**Flags:**
- `--file`, `-f` - Input file path
- `--overwrite` - Overwrite existing profiles
- `--dry-run` - Show what would be imported without making changes

**Examples:**

```bash {linenos=inline}
# Import from file
entra-auth-cli config import --file production-profile.json

# Import from stdin
cat production-profile.json | entra-auth-cli config import

# Import with overwrite
entra-auth-cli config import --file all-profiles.json --overwrite

# Dry run to preview
entra-auth-cli config import --file backup.json --dry-run
```

## Complete Examples

### Creating Profiles

#### Client Secret Authentication

```bash {linenos=inline}
# Interactive (recommended for first-time users)
entra-auth-cli config create

# You'll be prompted for:
# - Profile name: production
# - Tenant ID: 12345678-1234-1234-1234-123456789012
# - Client ID: 87654321-4321-4321-4321-210987654321
# - Auth method: client-secret
# - Client secret: (hidden input)
# - Scopes: https://graph.microsoft.com/.default
```

#### Certificate Authentication

```bash {linenos=inline}
# Create with certificate
entra-auth-cli config create \
  --name secure-prod \
  --tenant-id "$TENANT_ID" \
  --client-id "$CLIENT_ID" \
  --certificate "./certs/production.pfx" \
  --certificate-password "$CERT_PASSWORD" \
  --scope "https://graph.microsoft.com/.default"
```

#### Multiple Environments

```bash {linenos=inline}
# Development environment
entra-auth-cli config create --name dev \
  --tenant-id "$DEV_TENANT_ID" \
  --client-id "$DEV_CLIENT_ID" \
  --client-secret "$DEV_SECRET"

# Staging environment
entra-auth-cli config create --name staging \
  --tenant-id "$STAGING_TENANT_ID" \
  --client-id "$STAGING_CLIENT_ID" \
  --client-secret "$STAGING_SECRET"

# Production environment
entra-auth-cli config create --name production \
  --tenant-id "$PROD_TENANT_ID" \
  --client-id "$PROD_CLIENT_ID" \
  --certificate "./certs/prod.pfx"
```

### Managing Profiles

#### List and View

```bash {linenos=inline}
# List all profiles
entra-auth-cli config list

# Output:
# default
# production
# staging
# dev
# graph-api

# Show details of specific profile
entra-auth-cli config show --name production

# Output:
# Profile: production
# Tenant ID: 12345678-1234-1234-1234-123456789012
# Client ID: 87654321-4321-4321-4321-210987654321
# Auth Method: certificate
# Certificate: /path/to/cert.pfx
# Default Scopes: https://graph.microsoft.com/.default
```

#### Update Configuration

```bash {linenos=inline}
# Change default scopes
entra-auth-cli config edit --name production \
  --scope "User.Read Mail.Read Calendars.Read"

# Rotate client secret
entra-auth-cli config edit --name production \
  --client-secret "$NEW_SECRET"

# Switch from secret to certificate
entra-auth-cli config edit --name production \
  --certificate "./new-cert.pfx" \
  --certificate-password "$CERT_PASS"
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
for profile in $(entra-auth-cli config list); do
    echo "Backing up profile: $profile"
    entra-auth-cli config export --name "$profile" --include-secrets \
      > "$BACKUP_DIR/${profile}_${TIMESTAMP}.json"
done

echo "Backup complete: $BACKUP_DIR"
```

#### Migrate to New Machine

```bash {linenos=inline}
# On old machine
entra-auth-cli config export --include-secrets > profiles-backup.json

# Transfer file to new machine
scp profiles-backup.json user@newmachine:/tmp/

# On new machine
entra-auth-cli config import --file /tmp/profiles-backup.json

# Verify
entra-auth-cli config list
```

### Environment-Based Configuration

```bash {linenos=inline}
#!/bin/bash

# Script to create profiles based on environment
create_env_profile() {
    local env="$1"
    local tenant_var="${env}_TENANT_ID"
    local client_var="${env}_CLIENT_ID"
    local secret_var="${env}_CLIENT_SECRET"
    
    echo "Creating profile for $env environment..."
    
    entra-auth-cli config create \
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
entra-auth-cli config list
```

### Validation Script

```bash {linenos=inline}
#!/bin/bash

# Validate all profiles can generate tokens
echo "Validating profiles..."

failed=0
for profile in $(entra-auth-cli config list); do
    echo -n "Testing $profile... "
    
    if entra-auth-cli get-token --profile "$profile" --output json > /dev/null 2>&1; then
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
# ✅ Good - use environment variables
export CLIENT_SECRET=$(vault read -field=secret secret/azure/client)
entra-auth-cli config create --client-secret "$CLIENT_SECRET"

# ✅ Good - interactive prompt (hides input)
entra-auth-cli config create  # Will prompt for secret

# ❌ Bad - hardcoded in script
entra-auth-cli config create --client-secret "my-secret-123"

# ❌ Bad - visible in command history
entra-auth-cli config create --client-secret "$SECRET"  # If $SECRET expands
```

### Profile Naming

```bash {linenos=inline}
# ✅ Good - descriptive names
entra-auth-cli config create --name prod-graph-api
entra-auth-cli config create --name staging-azure-mgmt
entra-auth-cli config create --name dev-user-app

# ❌ Avoid - generic names
entra-auth-cli config create --name app1
entra-auth-cli config create --name test
```

### Regular Rotation

```bash {linenos=inline}
#!/bin/bash
# Rotate secrets for all profiles

for profile in $(entra-auth-cli config list); do
    echo "Rotating secret for $profile"
    
    # Get new secret from vault
    NEW_SECRET=$(vault read -field=secret "secret/azure/$profile")
    
    # Update profile
    entra-auth-cli config edit --name "$profile" --client-secret "$NEW_SECRET"
    
    # Verify
    if entra-auth-cli get-token --profile "$profile" > /dev/null; then
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

# Check exact name (case-sensitive)
entra-auth-cli config show --name Production  # Won't match "production"
```

### Cannot Create Profile

```bash {linenos=inline}
# Check storage directory permissions
# Linux/macOS
ls -la ~/.entra-auth-cli/profiles/
chmod 700 ~/.entra-auth-cli/profiles/

# Windows (PowerShell)
Test-Path "$env:LOCALAPPDATA\EntraTokenCLI\profiles"
```

### Profile Corruption

```bash {linenos=inline}
# View raw profile file
# Linux/macOS
cat ~/.entra-auth-cli/profiles/production.json

# If corrupted, delete and recreate
entra-auth-cli config delete --name production --force
entra-auth-cli config create --name production
```

## See Also

- [Managing Profiles](/docs/user-guide/managing-profiles/) - Detailed profile management guide
- [get-token](/docs/reference/get-token/) - Generate tokens using profiles
- [Security Best Practices](/docs/recipes/security-hardening/) - Secure configuration practices
