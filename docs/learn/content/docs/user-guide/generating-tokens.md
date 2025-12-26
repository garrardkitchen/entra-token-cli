---
title: "Generating Tokens"
description: "Request and obtain access tokens from Microsoft Entra ID"
weight: 20
---

# Generating Tokens

Learn how to generate access tokens using different OAuth2 flows and configurations.

---

## Quick Reference

| Task | Command |
|------|---------|
| Get token with profile | `entratool get-token -p PROFILE` |
| Override scope | `entratool get-token -p PROFILE --scope "SCOPE"` |
| Specify flow | `entratool get-token -p PROFILE -f FLOW` |
| Silent mode | `entratool get-token -p PROFILE --silent` |
| Save to file | `entratool get-token -p PROFILE -o token.txt` |
| Refresh token | `entratool refresh -p PROFILE` |

---

## Basic Token Generation

### Using a Profile

```bash
entratool get-token -p my-profile
```

**Output:**
```
Authenticating with profile 'my-profile'...
✓ Token acquired successfully

eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Ij...
```

**What happens:**
1. Profile loaded from `~/.entratool/profiles.json`
2. Secrets retrieved from secure storage
3. Authentication flow executed
4. Access token printed to stdout

### Specifying a Flow

Override the default flow:

```bash
entratool get-token -p my-profile -f ClientCredentials
```

**Available flows:**
- `ClientCredentials`
- `AuthorizationCode`
- `DeviceCode`
- `InteractiveBrowser`

[Learn about flows →](/docs/core-concepts/oauth2-flows/)

---

## Client Credentials Flow

### Non-Interactive Service Authentication

```bash
entratool get-token -p service-principal -f ClientCredentials
```

**Requirements:**
- Client secret or certificate configured
- Application permissions granted
- Admin consent completed

**Example:**
```bash
# Service principal for Graph API
entratool get-token -p graph-sp -f ClientCredentials
```

**Output:**
```
✓ Token acquired successfully
eyJ0eXAiOiJKV1QiLCJh...
```

[Detailed guide →](/docs/user-guide/generating-tokens/client-credentials/)

---

## Interactive Browser Flow

### User Authentication with Browser

```bash
entratool get-token -p user-app -f InteractiveBrowser
```

**What happens:**
1. Browser opens to Entra ID login page
2. User enters credentials
3. User consents to permissions (if required)
4. Browser redirects to localhost with code
5. Tool exchanges code for token

**Example:**
```bash
entratool get-token -p personal-graph -f InteractiveBrowser
```

**Output:**
```
Opening browser for authentication...
✓ Authentication successful
✓ Token acquired

eyJ0eXAiOiJKV1QiLCJh...
```

[Detailed guide →](/docs/user-guide/generating-tokens/interactive-browser/)

---

## Device Code Flow

### Authentication on Limited-Input Devices

```bash
entratool get-token -p iot-device -f DeviceCode
```

**What happens:**
1. Tool displays code and URL
2. User visits URL on another device
3. User enters code
4. User authenticates
5. Tool polls and receives token

**Example:**
```bash
entratool get-token -p headless-server -f DeviceCode
```

**Output:**
```
Device Code Authentication
To sign in, use a web browser to open:
  https://microsoft.com/devicelogin
and enter the code: ABCD-1234

Waiting for authentication...
✓ Token acquired successfully
```

[Detailed guide →](/docs/user-guide/generating-tokens/device-code/)

---

## Authorization Code Flow

### Web Application Authentication

```bash
entratool get-token -p webapp -f AuthorizationCode
```

**Requirements:**
- Redirect URI configured in app registration
- Delegated permissions
- User credentials

**Example:**
```bash
entratool get-token -p web-backend -f AuthorizationCode
```

**Output:**
```
Opening browser for authentication...
Redirect URI: http://localhost:8080
✓ Authorization code received
✓ Token acquired

eyJ0eXAiOiJKV1QiLCJh...
```

[Detailed guide →](/docs/user-guide/generating-tokens/authorization-code/)

---

## Scope Override

### Runtime Scope Specification

Override profile's default scope:

```bash
entratool get-token -p myprofile \
  --scope "https://graph.microsoft.com/User.Read Mail.Read"
```

**Use cases:**
- Different operations need different permissions
- Testing with minimal scopes
- Temporary scope changes

### Multiple Scopes

**Space-separated:**
```bash
entratool get-token -p myprofile \
  --scope "https://graph.microsoft.com/User.Read Mail.Read Calendars.Read"
```

**Comma-separated:**
```bash
entratool get-token -p myprofile \
  --scope "https://graph.microsoft.com/User.Read,Mail.Read,Calendars.Read"
```

### .default Scope

Request all configured permissions:

```bash
entratool get-token -p myprofile \
  --scope "https://graph.microsoft.com/.default"
```

[Learn about scopes →](/docs/core-concepts/scopes/)

---

## Certificate Authentication

### Using Certificates Instead of Secrets

```bash
entratool get-token -p cert-profile -f ClientCredentials
```

**Profile configuration:**
```json
{
  "name": "cert-profile",
  "clientId": "...",
  "tenantId": "...",
  "certificatePath": "/path/to/cert.pfx",
  "useCertificate": true
}
```

**Certificate password:**
- Stored securely in platform-specific storage
- Prompted once during profile creation
- Never stored in plaintext

### Advantages

✅ More secure than secrets  
✅ Longer validity periods  
✅ Better for automation  
✅ Certificate rotation support

[Detailed certificate guide →](/docs/certificates/)

---

## Output Options

### Silent Mode

Suppress all output except the token:

```bash
entratool get-token -p myprofile --silent
```

**Output:**
```
eyJ0eXAiOiJKV1QiLCJh...
```

**Use in scripts:**
```bash
TOKEN=$(entratool get-token -p myprofile --silent)
curl -H "Authorization: Bearer $TOKEN" https://graph.microsoft.com/v1.0/me
```

### Save to File

```bash
entratool get-token -p myprofile -o token.txt
```

**Output:**
```
✓ Token saved to token.txt
```

**Use later:**
```bash
TOKEN=$(cat token.txt)
curl -H "Authorization: Bearer $TOKEN" ...
```

### JSON Output

```bash
entratool get-token -p myprofile --json
```

**Output:**
```json
{
  "accessToken": "eyJ0eXAiOiJKV1QiLCJh...",
  "expiresOn": "2024-12-01T12:00:00Z",
  "tokenType": "Bearer",
  "scopes": ["https://graph.microsoft.com/.default"]
}
```

---

## Token Inspection

### Inspect Token Claims

```bash
# Generate and inspect in one command
entratool get-token -p myprofile --silent | entratool inspect
```

**Or separately:**
```bash
TOKEN=$(entratool get-token -p myprofile --silent)
entratool inspect -t "$TOKEN"
```

**Output:**
```json
{
  "aud": "https://graph.microsoft.com",
  "iss": "https://sts.windows.net/...",
  "iat": 1701432000,
  "exp": 1701435600,
  "appid": "12345678-1234-1234-1234-123456789abc",
  "scp": "User.Read Mail.Read",
  "roles": []
}
```

[Learn about token inspection →](/docs/user-guide/working-with-tokens/inspecting/)

---

## Token Refresh

### Refreshing Expired Tokens

If you have a refresh token:

```bash
entratool refresh -p myprofile
```

**Requirements:**
- Original token acquired with `offline_access` scope
- Refresh token stored in cache

**Output:**
```
Refreshing token for profile 'myprofile'...
✓ Token refreshed successfully

eyJ0eXAiOiJKV1QiLCJh...
```

**Note:** Client Credentials flow doesn't support refresh tokens. Just request a new token instead.

[Learn about token refreshing →](/docs/user-guide/working-with-tokens/refreshing/)

---

## Common Patterns

### Pattern 1: Script Automation

```bash
#!/bin/bash
set -e

# Get token
TOKEN=$(entratool get-token -p automation --silent)

# Use token
curl -H "Authorization: Bearer $TOKEN" \
     https://graph.microsoft.com/v1.0/users \
     | jq
```

### Pattern 2: Environment Variable

```bash
export ACCESS_TOKEN=$(entratool get-token -p myprofile --silent)

# Use in multiple commands
curl -H "Authorization: Bearer $ACCESS_TOKEN" https://api1.example.com
curl -H "Authorization: Bearer $ACCESS_TOKEN" https://api2.example.com
```

### Pattern 3: Token Caching

```bash
TOKEN_FILE=~/.cache/entratool-token.txt

# Check if token exists and is valid
if [ ! -f "$TOKEN_FILE" ] || ! entratool discover -f "$TOKEN_FILE" &>/dev/null; then
  entratool get-token -p myprofile --silent > "$TOKEN_FILE"
fi

TOKEN=$(cat "$TOKEN_FILE")
```

### Pattern 4: Multi-API Access

```bash
# Get tokens for different APIs
TOKEN_GRAPH=$(entratool get-token -p graph-profile --silent)
TOKEN_AZURE=$(entratool get-token -p azure-profile --silent)

# Use with different APIs
curl -H "Authorization: Bearer $TOKEN_GRAPH" https://graph.microsoft.com/v1.0/me
curl -H "Authorization: Bearer $TOKEN_AZURE" https://management.azure.com/subscriptions
```

---

## Troubleshooting

### "Profile not found"

**Cause:** Profile name is incorrect

**Fix:**
```bash
# List profiles
entratool config list

# Use correct name
entratool get-token -p correct-name
```

### "AADSTS70011: Invalid scope"

**Cause:** Requested scope is not configured in app registration

**Fix:**
1. Go to Azure Portal → App registrations
2. Select your app → API permissions
3. Add the required permission
4. Grant admin consent (if needed)

### "AADSTS7000215: Invalid client secret"

**Cause:** Client secret is expired or incorrect

**Fix:**
```bash
# Rotate secret in Azure Portal
# Update profile
entratool config edit -p myprofile
# Select: Client Secret
# Enter: new-secret
```

### "Insufficient privileges"

**Cause:** Token has scope but lacks underlying permission

**Fix:**
1. Verify API permissions in Azure Portal
2. Grant admin consent
3. For users: Assign appropriate Azure AD role

### "Browser did not respond"

**Cause:** Browser authentication timed out

**Fix:**
- Try Device Code flow instead:
  ```bash
  entratool get-token -p myprofile -f DeviceCode
  ```
- Check redirect URI configuration
- Ensure localhost port is not blocked

---

## Best Practices

### ✅ Use Appropriate Flows

- **Automation:** Client Credentials
- **User apps:** Interactive Browser
- **Headless/SSH:** Device Code
- **Web apps:** Authorization Code

### ✅ Request Minimal Scopes

```bash
# ❌ Over-privileged
--scope "https://graph.microsoft.com/.default"

# ✓ Minimal
--scope "https://graph.microsoft.com/User.Read"
```

### ✅ Handle Token Expiration

```bash
# Check expiration before use
entratool discover -f token.txt

# Refresh if needed
if [ $? -ne 0 ]; then
  entratool get-token -p myprofile --silent > token.txt
fi
```

### ✅ Secure Token Storage

```bash
# Set restrictive permissions
TOKEN_FILE=~/.cache/my-token.txt
entratool get-token -p myprofile --silent > "$TOKEN_FILE"
chmod 600 "$TOKEN_FILE"
```

### ❌ Avoid

- ❌ Hard-coding tokens
- ❌ Committing tokens to git
- ❌ Sharing tokens via email/Slack
- ❌ Using expired tokens
- ❌ Requesting excessive scopes

---

## Next Steps

- [Client Credentials Flow (Detailed)](/docs/user-guide/generating-tokens/client-credentials/)
- [Interactive Browser Flow (Detailed)](/docs/user-guide/generating-tokens/interactive-browser/)
- [Working with Tokens](/docs/user-guide/working-with-tokens/)
- [Token Recipes](/docs/recipes/)
