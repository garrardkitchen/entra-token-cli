---
title: "Authentication Profiles"
description: "Understanding and managing authentication profiles"
weight: 10
---

# Authentication Profiles

Profiles are saved authentication configurations that store everything needed to generate tokens for a specific application or scenario.

---

## What is a Profile?

A profile contains:

- **Profile name**: Unique identifier you choose
- **Tenant ID**: Your Azure/Entra ID tenant
- **Client ID**: Application (client) ID from app registration
- **Scopes**: API permissions to request (e.g., `https://graph.microsoft.com/.default`)
- **Authentication method**: How to authenticate (Client Secret, Certificate, or Passwordless Certificate)
- **Credentials**: Client secret or certificate path (stored securely)
- **Default OAuth2 flow** (optional): Preferred authentication flow
- **Redirect URI** (optional): Custom redirect for interactive flows

---

## Why Use Profiles?

### Convenience
Store configuration once, reuse many times. No need to type tenant IDs and secrets repeatedly.

### Security
Credentials are stored using platform-native secure storage, not in plain text files.

### Organization
Manage multiple applications, environments, or tenants easily. Examples:
- `prod-app-client`
- `dev-graph-api`
- `staging-custom-api`

### Portability
Export profiles (with encryption) to share with team members or move between machines.

---

## Profile Anatomy

### Required Fields

```yaml
name: myprofile
tenantId: contoso.onmicrosoft.com
clientId: 12345678-1234-1234-1234-123456789abc
scopes:
  - https://graph.microsoft.com/.default
authMethod: ClientSecret
```

### Optional Fields

```yaml
defaultFlow: ClientCredentials    # Auto-select flow
redirectUri: http://localhost:8080  # For interactive flows
certificatePath: /path/to/cert.pfx  # For certificate auth
cacheCertificatePassword: true     # Cache cert password
```

---

## Profile Storage

Profiles are stored in platform-specific locations:

- **Windows**: `%APPDATA%\entratool\profiles.json`
- **macOS/Linux**: `~/.config/entratool/profiles.json`

The `profiles.json` file contains metadata **only** - no secrets!

**Example** `profiles.json`:

```json
{
  "profiles": [
    {
      "name": "myprofile",
      "tenantId": "contoso.onmicrosoft.com",
      "clientId": "12345678-...",
      "scopes": ["https://graph.microsoft.com/.default"],
      "authMethod": "ClientSecret",
      "createdAt": "2025-12-26T10:00:00Z",
      "updatedAt": "2025-12-26T10:00:00Z"
    }
  ]
}
```

---

## Secret Storage

Secrets (client secrets and certificate passwords) are stored separately using **secure storage**:

- **Windows**: Encrypted with DPAPI in `%APPDATA%\entratool\secure\`
- **macOS**: Stored in Keychain (service: `entratool`, account: `entratool:{profileName}:{secretType}`)
- **Linux**: XOR-obfuscated in `~/.config/entratool/secure/` ⚠️

> **Security Note**: See [Secure Storage](/docs/core-concepts/secure-storage/) for details about platform security.

---

## Profile Lifecycle

### 1. Create
```bash
entratool config create
```

Interactive prompts guide you through setup.

### 2. Use
```bash
entratool get-token -p myprofile
```

Reference the profile by name when generating tokens.

### 3. Update
```bash
entratool config edit -p myprofile
```

Modify settings or rotate credentials.

### 4. Share
```bash
entratool config export -p myprofile --include-secrets -o myprofile.enc
```

Export with AES-256 encryption for team sharing.

### 5. Delete
```bash
entratool config delete -p myprofile
```

Remove profile and associated secrets.

---

## Common Profile Patterns

### Service Principal for Azure Resources
```
Name: azure-prod-sp
Tenant: contoso.onmicrosoft.com
Client ID: [service principal client ID]
Scopes: https://management.azure.com/.default
Auth Method: ClientSecret
```

### Microsoft Graph API Access
```
Name: graph-api-client
Tenant: contoso.onmicrosoft.com
Client ID: [app registration client ID]
Scopes: https://graph.microsoft.com/.default
Auth Method: Certificate
Certificate Path: /path/to/cert.pfx
```

### Custom API Access
```
Name: myapi-client
Tenant: [tenant ID]
Client ID: [client app ID]
Scopes: api://[api-app-id]/.default
Auth Method: ClientSecret
```

### Multi-Tenant Application
```
Name: multitenant-app
Tenant: organizations  # or 'common'
Client ID: [app client ID]
Scopes: https://graph.microsoft.com/User.Read
Auth Method: InteractiveBrowser
```

---

## Profile Management Commands

| Command | Purpose |
|---------|---------|
| `config create` | Create a new profile |
| `config list` | List all profiles |
| `config edit -p NAME` | Edit existing profile |
| `config delete -p NAME` | Delete a profile |
| `config export -p NAME` | Export profile (optionally with secrets) |
| `config import -i FILE` | Import profile from file |

[See detailed profile management guide →](/docs/user-guide/managing-profiles/)

---

## Best Practices

### ✅ Do

- Use descriptive profile names: `prod-graph-api`, not `profile1`
- Store one app per profile for clarity
- Export profiles for backup before major changes
- Rotate secrets regularly

### ❌ Don't

- Share profiles with secrets via insecure channels
- Use the same profile for multiple environments
- Store production credentials on Linux (XOR obfuscation only)
- Commit `profiles.json` to version control

---

## Next Steps

- [Learn about OAuth2 Flows](/docs/oauth-flows/) to understand authentication methods
- [Manage Scopes](/docs/core-concepts/scopes/) to control API access
- [Profile Management Guide](/docs/user-guide/managing-profiles/) for detailed commands
