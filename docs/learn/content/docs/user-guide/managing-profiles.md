---
title: "Managing Profiles"
description: "Create, edit, and manage authentication profiles"
weight: 10
---

# Managing Profiles

Authentication profiles store your configuration for connecting to Microsoft Entra ID. Learn how to create, view, edit, and delete profiles.

---

## Quick Reference

| Task | Command |
|------|---------|
| Create new profile | `entra-auth-cli config create` |
| List all profiles | `entra-auth-cli config list` |
| Edit existing profile | `entra-auth-cli config edit -p NAME` |
| Delete profile | `entra-auth-cli config delete -p NAME` |
| Export profile | `entra-auth-cli config export -p NAME` |
| Import profile | `entra-auth-cli config import -f FILE` |

---

## Creating Profiles

### Interactive Creation

The easiest way to create a profile:

```bash {linenos=inline}
entra-auth-cli config create
```

**Prompts:**
1. **Profile name**: Unique identifier
2. **Client ID**: From app registration
3. **Tenant ID**: Your Azure AD tenant
4. **Authentication method**: Secret or Certificate
5. **Scope**: API permissions
6. **OAuth2 flow**: (Optional) Default flow

**Example session:**
```
Profile name: my-graph-app
Client ID: 12345678-1234-1234-1234-123456789abc
Tenant ID: 87654321-4321-4321-4321-cba987654321
Authentication method:
  1. Client Secret
  2. Certificate
Select: 1
Client secret: ********************************
Scope: https://graph.microsoft.com/.default
Set default OAuth2 flow? (y/n): y
OAuth2 flow:
  1. ClientCredentials
  2. AuthorizationCode
  3. DeviceCode
  4. InteractiveBrowser
Select: 1

✓ Profile 'my-graph-app' created successfully
```

[Detailed guide →](/docs/user-guide/managing-profiles/creating/)

---

## Listing Profiles

### View All Profiles

```bash {linenos=inline}
entra-auth-cli config list
```

**Output:**
```
Available profiles:

1. my-graph-app
   Client ID: 12345678-1234-1234-1234-123456789abc
   Tenant ID: 87654321-4321-4321-4321-cba987654321
   Scope: https://graph.microsoft.com/.default
   Flow: ClientCredentials

2. personal-graph
   Client ID: abcdef12-3456-7890-abcd-ef1234567890
   Tenant ID: common
   Scope: https://graph.microsoft.com/User.Read
   Flow: InteractiveBrowser
```

### JSON Output

```bash {linenos=inline}
entra-auth-cli config list --json
```

**Output:**
```json
{
  "profiles": [
    {
      "name": "my-graph-app",
      "clientId": "12345678-1234-1234-1234-123456789abc",
      "tenantId": "87654321-4321-4321-4321-cba987654321",
      "scope": "https://graph.microsoft.com/.default",
      "flow": "ClientCredentials",
      "useClientSecret": true
    }
  ]
}
```

[Detailed guide →](/docs/user-guide/managing-profiles/listing/)

---

## Editing Profiles

### Interactive Edit

```bash {linenos=inline}
entra-auth-cli config edit -p my-graph-app
```

**Select what to edit:**
```
What would you like to edit?
  1. Client ID
  2. Tenant ID
  3. Client Secret
  4. Scope
  5. OAuth2 Flow
  6. Authority URL
  q. Done

Select: 4
Current scope: https://graph.microsoft.com/.default
New scope: https://graph.microsoft.com/User.Read Mail.Read

✓ Profile 'my-graph-app' updated
```

### Common Edits

**Rotate client secret:**
```bash {linenos=inline}
entra-auth-cli config edit -p my-graph-app
# Select: Client Secret
# Enter new secret
```

**Change scope:**
```bash {linenos=inline}
entra-auth-cli config edit -p my-graph-app
# Select: Scope
# Enter: new-scope
```

**Switch authentication method:**
```bash {linenos=inline}
entra-auth-cli config edit -p my-graph-app
# Select: Authentication Method
# Choose: Certificate
# Enter certificate path and password
```

[Detailed guide →](/docs/user-guide/managing-profiles/editing/)

---

## Deleting Profiles

### Delete Single Profile

```bash {linenos=inline}
entra-auth-cli config delete -p my-graph-app
```

**Confirmation prompt:**
```
Are you sure you want to delete profile 'my-graph-app'? (y/n): y
✓ Profile deleted successfully
```

**What gets deleted:**
- Profile configuration from `profiles.json`
- Associated secrets from secure storage

### Delete with Force

Skip confirmation:

```bash {linenos=inline}
entra-auth-cli config delete -p my-graph-app --force
```

⚠️ **Warning:** This is permanent. Secrets cannot be recovered.

[Detailed guide →](/docs/user-guide/managing-profiles/deleting/)

---

## Exporting Profiles

### Export for Backup

Export profile configuration (without secrets):

```bash {linenos=inline}
entra-auth-cli config export -p my-graph-app -o my-profile.json
```

**Output file:**
```json
{
  "name": "my-graph-app",
  "clientId": "12345678-1234-1234-1234-123456789abc",
  "tenantId": "87654321-4321-4321-4321-cba987654321",
  "scope": "https://graph.microsoft.com/.default",
  "flow": "ClientCredentials",
  "useClientSecret": true
  // Note: clientSecret is NOT included
}
```

### Export All Profiles

```bash {linenos=inline}
entra-auth-cli config export -o all-profiles.json
```

### Security Note

{{% alert context="warning" %}}
**Exported files do NOT include secrets or certificates.**

You must manually transfer secrets to the new location using secure methods.
{{% /alert %}}

[Detailed guide →](/docs/user-guide/managing-profiles/exporting/)

---

## Importing Profiles

### Import Single Profile

```bash {linenos=inline}
entra-auth-cli config import -f my-profile.json
```

**Post-import:**
```
✓ Profile 'my-graph-app' imported successfully
⚠ You must set the client secret:
  entra-auth-cli config edit -p my-graph-app
```

### Import with Merge

If a profile with the same name exists:

```
Profile 'my-graph-app' already exists.
  1. Skip
  2. Overwrite
  3. Rename (import as 'my-graph-app-2')
Select:
```

### Batch Import

Import multiple profiles:

```bash {linenos=inline}
entra-auth-cli config import -f team-profiles.json
```

**team-profiles.json:**
```json
{
  "profiles": [
    { "name": "profile1", "clientId": "...", ... },
    { "name": "profile2", "clientId": "...", ... }
  ]
}
```

[Detailed guide →](/docs/user-guide/managing-profiles/importing/)

---

## Profile Storage

### Storage Location

**File:** `~/.entra-auth-cli/profiles.json`

**Platforms:**
- **Windows:** `%USERPROFILE%\.entra-auth-cli\profiles.json`
- **macOS/Linux:** `~/.entra-auth-cli/profiles.json`

### What's Stored

**In profiles.json (plaintext):**
- Profile name
- Client ID
- Tenant ID
- Scope
- OAuth2 flow preference
- Authority URL
- Certificate path

**In secure storage (encrypted):**
- Client secrets
- Certificate passwords

### Manual Editing

You can manually edit `profiles.json`:

```bash {linenos=inline}
# macOS/Linux
nano ~/.entra-auth-cli/profiles.json

# Windows
notepad %USERPROFILE%\.entra-auth-cli\profiles.json
```

**Example:**
```json
{
  "profiles": [
    {
      "name": "my-profile",
      "clientId": "12345678-1234-1234-1234-123456789abc",
      "tenantId": "87654321-4321-4321-4321-cba987654321",
      "scope": "https://graph.microsoft.com/.default",
      "flow": "ClientCredentials",
      "useClientSecret": true
    }
  ]
}
```

⚠️ **Be careful:** Invalid JSON will break profile loading.

---

## Common Workflows

### Scenario: Team Onboarding

**Share profiles with new team members:**

1. Export profiles (without secrets):
   ```bash
   entra-auth-cli config export -o team-profiles.json
   ```

2. Share `team-profiles.json` via secure channel

3. Team member imports:
   ```bash
   entra-auth-cli config import -f team-profiles.json
   ```

4. Securely share secrets (use Azure Key Vault or password manager)

5. Team member adds secrets:
   ```bash
   entra-auth-cli config edit -p profile1
   # Add client secret
   ```

### Scenario: Environment Migration

**Move profiles from dev to prod machine:**

1. Export from dev:
   ```bash
   entra-auth-cli config export -p prod-profile -o prod.json
   ```

2. Transfer file securely (SCP, encrypted USB, etc.)

3. Import on prod:
   ```bash
   entra-auth-cli config import -f prod.json
   ```

4. Add production secret:
   ```bash
   entra-auth-cli config edit -p prod-profile
   ```

### Scenario: Secret Rotation

**Rotate client secret after compromise:**

1. Generate new secret in Azure Portal
2. Update profile:
   ```bash
   entra-auth-cli config edit -p compromised-profile
   # Select: Client Secret
   # Enter: new-secret
   ```
3. Test:
   ```bash
   entra-auth-cli get-token -p compromised-profile
   ```
4. Delete old secret from Azure Portal

---

## Best Practices

### ✅ Naming Conventions

Use descriptive, hierarchical names:

```bash {linenos=inline}
# Good
company-graph-prod
company-graph-dev
project-api-staging

# Avoid
profile1
test
myprofile
```

### ✅ Organize by Environment

```bash {linenos=inline}
# Production
prod-graph-api
prod-azure-management

# Staging
staging-graph-api
staging-azure-management

# Development
dev-graph-api
dev-azure-management
```

### ✅ Separate Concerns

Create profiles for different purposes:

```bash {linenos=inline}
# Read-only operations
readonly-graph-profile

# Write operations
admin-graph-profile

# Different APIs
profile-graph
profile-azure
profile-custom-api
```

### ✅ Regular Maintenance

- Review profiles monthly
- Delete unused profiles
- Rotate secrets quarterly
- Update scopes as needed

### ❌ Avoid

- ❌ Sharing profiles via email
- ❌ Committing profiles to git
- ❌ Using the same profile for dev and prod
- ❌ Storing secrets in plaintext
- ❌ Reusing secrets across profiles

---

## Troubleshooting

### "Profile not found"

**Cause:** Profile name doesn't exist

**Fix:**
```bash {linenos=inline}
# List available profiles
entra-auth-cli config list

# Use correct name
entra-auth-cli get-token -p correct-profile-name
```

### "Profile already exists"

**Cause:** Creating profile with duplicate name

**Fix:**
```bash {linenos=inline}
# Option 1: Delete existing
entra-auth-cli config delete -p duplicate-name

# Option 2: Use different name
entra-auth-cli config create
# Enter: duplicate-name-2
```

### "Invalid profile configuration"

**Cause:** Corrupted `profiles.json`

**Fix:**
```bash {linenos=inline}
# Backup current file
cp ~/.entra-auth-cli/profiles.json ~/.entra-auth-cli/profiles.json.bak

# Validate JSON
cat ~/.entra-auth-cli/profiles.json | jq

# Fix syntax errors or restore from backup
```

### "Cannot access secure storage"

**Cause:** Platform secure storage unavailable

**Fix:**
- **Windows:** Ensure user profile is not corrupted
- **macOS:** Unlock Keychain
- **Linux:** Check file permissions on `~/.entra-auth-cli/`

---

## Next Steps

- [Creating Profiles (Detailed)](/docs/user-guide/managing-profiles/creating/)
- [Generating Tokens](/docs/user-guide/generating-tokens/)
- [Certificate Authentication](/docs/certificates/)
- [Security Best Practices](/docs/recipes/security-hardening/)
