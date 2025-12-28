---
title: "Scopes & Permissions"
description: "Understanding API scopes and permission requests"
weight: 30
---

# Scopes & Permissions

Scopes define what your application can access using an access token. Understanding scopes is critical for requesting the right permissions and troubleshooting authorization issues.

---

## What Are Scopes?

A **scope** is a permission that grants access to specific resources or operations in an API.

### Format

```
https://api.example.com/.default
https://graph.microsoft.com/User.Read
https://management.azure.com/user_impersonation
```

**Parts:**
- **Resource URI**: The API you're accessing (`https://graph.microsoft.com`)
- **Permission**: The specific capability (`User.Read`, `.default`)

---

## Common Scope Patterns

### Microsoft Graph API

```bash {linenos=inline}
# Read user profile
https://graph.microsoft.com/User.Read

# Read user profile and email
https://graph.microsoft.com/User.Read
https://graph.microsoft.com/Mail.Read

# All configured permissions
https://graph.microsoft.com/.default
```

### Azure Management API

```bash {linenos=inline}
# Azure Resource Manager access
https://management.azure.com/.default

# With user impersonation
https://management.azure.com/user_impersonation
```

### Custom APIs

```bash {linenos=inline}
# Your custom API
https://api.contoso.com/.default

# Specific scopes
api://12345678-1234-1234-1234-123456789abc/access_as_user
```

### Azure Key Vault

```bash {linenos=inline}
https://vault.azure.net/.default
```

### Azure Storage

```bash {linenos=inline}
https://storage.azure.com/.default
```

---

## The `.default` Scope

### What It Means

The `.default` suffix requests **all permissions configured** in your app registration.

**Example:**
If your app is configured with:
- `User.Read`
- `Mail.Read`
- `Calendars.Read`

Then requesting `https://graph.microsoft.com/.default` includes all three.

### When to Use

✅ **Use `.default` when:**
- Working with service principals (Client Credentials flow)
- You want all configured permissions
- Building automation scripts

⚠️ **Avoid `.default` when:**
- Requesting minimal permissions
- Building user-facing apps (use explicit scopes)
- Different operations need different scopes

### Example

```bash {linenos=inline}
# Request all configured Graph permissions
entra-auth-cli get-token -p service-principal \
  --scope "https://graph.microsoft.com/.default"
```

---

## Profile Scopes vs Runtime Scopes

### Profile Scopes

Scopes stored in your profile configuration:

```json
{
  "name": "my-profile",
  "scope": "https://graph.microsoft.com/User.Read Mail.Read"
}
```

**Used by default** when running:
```bash {linenos=inline}
entra-auth-cli get-token -p my-profile
```

### Runtime Scope Override

Override profile scopes with `--scope` or `-s`:

```bash {linenos=inline}
# Override to request different scope
entra-auth-cli get-token -p my-profile \
  --scope "https://graph.microsoft.com/Calendar.Read"
```

**Use cases:**
- Different operations need different permissions
- Testing with minimal scopes
- Temporary scope changes without editing profile

---

## Multiple Scopes

### Space-Separated Format

```bash {linenos=inline}
entra-auth-cli get-token -p myprofile \
  --scope "https://graph.microsoft.com/User.Read Mail.Read Calendars.Read"
```

### Comma-Separated Format

```bash {linenos=inline}
entra-auth-cli get-token -p myprofile \
  --scope "https://graph.microsoft.com/User.Read,Mail.Read,Calendars.Read"
```

The tool normalizes both formats automatically.

---

## Scope Requirements by Flow

### Client Credentials Flow

**Requires:** Application permissions configured in Azure Portal

```bash {linenos=inline}
# App registration → API permissions → Add permission
# Select: Application permissions (not Delegated)
# Grant admin consent ✓
```

**Example scopes:**
```bash {linenos=inline}
https://graph.microsoft.com/.default
https://management.azure.com/.default
```

### User-Interactive Flows

**Requires:** Delegated permissions configured in Azure Portal

```bash {linenos=inline}
# App registration → API permissions → Add permission
# Select: Delegated permissions
# User consent required (or admin consent)
```

**Example scopes:**
```bash {linenos=inline}
https://graph.microsoft.com/User.Read
https://graph.microsoft.com/Mail.Read
```

---

## Inspecting Token Scopes

Use `inspect` to see what scopes are included in your token:

```bash {linenos=inline}
entra-auth-cli inspect -t "eyJ0eXAiOiJKV1QiLCJhbGci..."
```

**Output:**
```json
{
  "scp": "User.Read Mail.Read",
  "roles": ["User.Read.All"]
}
```

**Fields:**
- `scp`: Delegated permissions (user context)
- `roles`: Application permissions (app context)

---

## Common Scope Patterns

### Reading User Data

```bash {linenos=inline}
# Microsoft Graph
https://graph.microsoft.com/User.Read          # Basic profile
https://graph.microsoft.com/User.Read.All      # All users (admin)
https://graph.microsoft.com/User.ReadWrite     # Modify profile
```

### Email Access

```bash {linenos=inline}
https://graph.microsoft.com/Mail.Read          # Read email
https://graph.microsoft.com/Mail.Send          # Send email
https://graph.microsoft.com/Mail.ReadWrite     # Full email access
```

### Calendar Access

```bash {linenos=inline}
https://graph.microsoft.com/Calendars.Read
https://graph.microsoft.com/Calendars.ReadWrite
```

### Azure Resource Management

```bash {linenos=inline}
https://management.azure.com/.default
https://management.azure.com/user_impersonation
```

### SharePoint

```bash {linenos=inline}
https://graph.microsoft.com/Sites.Read.All
https://graph.microsoft.com/Files.ReadWrite.All
```

---

## Scope Configuration

### Setting Scopes in Profile

#### During Creation

```bash {linenos=inline}
entra-auth-cli config create
# ... prompts ...
Scope: https://graph.microsoft.com/.default
```

#### During Edit

```bash {linenos=inline}
entra-auth-cli config edit -p myprofile
# Select: Scope
# Enter new: https://management.azure.com/.default
```

#### Manual JSON Edit

Edit `~/.entra-auth-cli/profiles.json`:

```json
{
  "profiles": [
    {
      "name": "myprofile",
      "scope": "https://graph.microsoft.com/User.Read Mail.Read"
    }
  ]
}
```

---

## Scope Troubleshooting

### "Invalid scope"

**Cause:** Scope format is incorrect

**Fix:**
```bash {linenos=inline}
# ❌ Wrong
--scope "User.Read"

# ✓ Correct
--scope "https://graph.microsoft.com/User.Read"
```

### "AADSTS65001: User consent required"

**Cause:** User hasn't consented to delegated permissions

**Fix:**
1. Use interactive flow (Authorization Code or Interactive Browser)
2. User will be prompted to consent
3. Or: Admin grants consent in Azure Portal

### "AADSTS70011: Invalid scopes"

**Cause:** Scope not configured in app registration

**Fix:**
1. Go to Azure Portal → App registrations
2. Select your app → API permissions
3. Add the required permission
4. Grant admin consent (if Application permission)

### "Insufficient privileges"

**Cause:** Token has scope but user/app lacks underlying permission

**Fix:**
1. **For users:** Assign appropriate role (e.g., Global Reader)
2. **For apps:** Grant admin consent for Application permissions
3. Verify permission configuration in Azure Portal

---

## Best Practices

### ✅ Principle of Least Privilege

Request only the scopes you need:

```bash {linenos=inline}
# ❌ Over-privileged
--scope "https://graph.microsoft.com/.default"

# ✓ Minimal scopes
--scope "https://graph.microsoft.com/User.Read Mail.Read"
```

### ✅ Use Explicit Scopes for User Apps

For user-facing applications, request specific scopes:

```bash {linenos=inline}
entra-auth-cli get-token -p user-app \
  --scope "https://graph.microsoft.com/User.Read"
```

### ✅ Use `.default` for Service Principals

For automation and service accounts:

```bash {linenos=inline}
entra-auth-cli get-token -p automation \
  --scope "https://graph.microsoft.com/.default"
```

### ✅ Separate Profiles for Different Scopes

Create profiles for different scenarios:

```bash {linenos=inline}
# Profile: graph-readonly
scope: https://graph.microsoft.com/User.Read

# Profile: graph-admin
scope: https://graph.microsoft.com/.default
```

### ❌ Don't Hard-Code Tokens

Always request fresh tokens with appropriate scopes:

```bash {linenos=inline}
# ✓ Request token per operation
TOKEN=$(entra-auth-cli get-token -p myprofile --scope "...")
curl -H "Authorization: Bearer $TOKEN" ...
```

---

## Scope Discovery

### Finding Available Scopes

1. **Azure Portal:**
   - App registrations → API permissions → Add permission
   - Browse Microsoft APIs or your custom APIs
   - View available Delegated and Application permissions

2. **Microsoft Graph Explorer:**
   - Visit: https://developer.microsoft.com/graph/graph-explorer
   - Explore API endpoints and required scopes

3. **API Documentation:**
   - Microsoft Graph: https://docs.microsoft.com/graph/permissions-reference
   - Azure Management: https://docs.microsoft.com/rest/api/azure/

### Using `discover` Command

```bash {linenos=inline}
entra-auth-cli discover -t "eyJ0eXAiOiJKV1Qi..."
```

**Output shows:**
- Token audience
- Issued scopes
- Roles
- Expiration time

---

## Scope Combinations

### Common Combinations

#### Graph API Read-Only Access
```bash {linenos=inline}
--scope "https://graph.microsoft.com/User.Read Mail.Read Calendars.Read"
```

#### Graph API Admin Access
```bash {linenos=inline}
--scope "https://graph.microsoft.com/.default"
```

#### Azure Management + Graph
```bash {linenos=inline}
# Request separately (different audiences)
TOKEN_GRAPH=$(entra-auth-cli get-token -p graph --scope "https://graph.microsoft.com/.default")
TOKEN_AZURE=$(entra-auth-cli get-token -p azure --scope "https://management.azure.com/.default")
```

---

## Next Steps

- [Learn about OAuth2 Flows](/docs/core-concepts/oauth2-flows/)
- [Understand Authentication Profiles](/docs/core-concepts/profiles/)
- [Inspect Tokens](/docs/user-guide/working-with-tokens/inspecting/)
- [Scope Troubleshooting Guide](/docs/troubleshooting/)
