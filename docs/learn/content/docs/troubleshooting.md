---
title: "Troubleshooting"
description: "Common issues and solutions"
weight: 80
---

# Troubleshooting

Solutions to common problems and error messages.

---

## Profile Issues

### "Profile not found"

**Error:**
```
Error: Profile 'my-profile' not found
```

**Cause:** Profile name is incorrect or doesn't exist.

**Solution:**
```bash
# List available profiles
entratool config list

# Use correct name
entratool get-token -p correct-profile-name
```

---

### "Profile already exists"

**Error:**
```
Error: Profile 'my-profile' already exists
```

**Cause:** Attempting to create a profile with a duplicate name.

**Solution:**
```bash
# Option 1: Delete existing profile
entratool config delete -p my-profile

# Option 2: Use a different name
entratool config create
# Enter: my-profile-2
```

---

### "Invalid profile configuration"

**Error:**
```
Error: Failed to load profiles.json
Invalid JSON format
```

**Cause:** Corrupted `profiles.json` file.

**Solution:**
```bash
# Backup current file
cp ~/.entratool/profiles.json ~/.entratool/profiles.json.bak

# Validate JSON syntax
cat ~/.entratool/profiles.json | jq

# If invalid, manually fix JSON syntax or restore from backup
# Or delete and recreate profiles
rm ~/.entratool/profiles.json
entratool config create
```

---

### "Cannot access secure storage"

**Error:**
```
Error: Failed to access secure storage
```

**Platform-specific causes and solutions:**

**Windows:**
- **Cause:** User profile corrupted
- **Solution:** Run `sfc /scannow` as administrator

**macOS:**
- **Cause:** Keychain is locked
- **Solution:** 
  1. Open Keychain Access app
  2. Select "login" keychain
  3. Unlock with your password

**Linux:**
- **Cause:** File permissions on `~/.entratool/` directory
- **Solution:**
  ```bash
  chmod 700 ~/.entratool
  chmod 600 ~/.entratool/*
  ```

---

## Authentication Failures

### "AADSTS70011: Invalid scope"

**Error:**
```
AADSTS70011: The provided value for the input parameter 'scope' is not valid.
```

**Cause:** Requested scope is not configured in app registration or has incorrect format.

**Solutions:**

**1. Check scope format:**
```bash
# ❌ Wrong (missing base URL)
--scope "User.Read"

# ✓ Correct
--scope "https://graph.microsoft.com/User.Read"
```

**2. Add permission in Azure Portal:**
1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to Azure Active Directory → App registrations
3. Select your application
4. Click "API permissions"
5. Click "Add a permission"
6. Select the API (e.g., Microsoft Graph)
7. Select the required permission
8. Click "Grant admin consent" (if needed)

---

### "AADSTS7000215: Invalid client secret"

**Error:**
```
AADSTS7000215: Invalid client secret provided.
```

**Cause:** Client secret is expired, incorrect, or not properly stored.

**Solutions:**

**1. Check secret expiration in Azure Portal:**
1. App registrations → Your app → Certificates & secrets
2. Check "Client secrets" section for expiration date

**2. Rotate secret:**
```bash
# Generate new secret in Azure Portal
# Then update profile:
entratool config edit -p my-profile
# Select: Client Secret
# Enter: new-secret-from-portal
```

**3. Verify secret was stored correctly:**
```bash
# Recreate profile
entratool config delete -p my-profile
entratool config create
# Carefully re-enter all information
```

---

### "AADSTS50057: Application is disabled"

**Error:**
```
AADSTS50057: The application is currently disabled.
```

**Cause:** App registration is disabled in Azure Portal.

**Solution:**
1. Go to Azure Portal → App registrations
2. Select your application
3. If disabled, contact your administrator to re-enable it

---

### "AADSTS65001: User consent required"

**Error:**
```
AADSTS65001: The user or administrator has not consented to use the application.
```

**Cause:** User hasn't consented to delegated permissions.

**Solution:**

**Option 1: Use interactive flow to consent:**
```bash
entratool get-token -p my-profile -f InteractiveBrowser
# User will be prompted to consent
```

**Option 2: Admin grants consent in Azure Portal:**
1. App registrations → Your app → API permissions
2. Click "Grant admin consent for [Tenant]"
3. Click "Yes"

---

### "AADSTS65002: Consent required for required scopes"

**Error:**
```
AADSTS65002: Consent between first party application and first party resource must be configured via preauthorization.
```

**Cause:** Requesting scopes that require admin consent but using user-interactive flow.

**Solution:**
- Have admin grant consent in Azure Portal
- Or use Client Credentials flow with Application permissions

---

### "AADSTS700016: Application not found"

**Error:**
```
AADSTS700016: Application with identifier '...' was not found in the directory.
```

**Cause:** Client ID is incorrect or app registration doesn't exist in the specified tenant.

**Solutions:**

**1. Verify Client ID:**
```bash
# Check profile configuration
entratool config list

# Correct Client ID format: 12345678-1234-1234-1234-123456789abc
```

**2. Verify Tenant ID:**
- Ensure you're using the correct tenant ID
- For multi-tenant apps, try `common` instead of specific tenant ID

**3. Check app registration exists:**
1. Azure Portal → Azure Active Directory → App registrations
2. Verify app exists and note the correct Application (client) ID

---

## Token Problems

### "Token expired"

**Error:**
```
Error: Token has expired
HTTP 401 Unauthorized
```

**Cause:** Access token lifetime exceeded (typically 1 hour).

**Solutions:**

**1. Get fresh token:**
```bash
TOKEN=$(entratool get-token -p my-profile --silent)
```

**2. Use refresh token (if available):**
```bash
TOKEN=$(entratool refresh -p my-profile --silent)
```

**3. Implement automatic refresh:**
```bash
#!/bin/bash
get_valid_token() {
  if ! entratool discover -f token.txt &>/dev/null; then
    entratool get-token -p my-profile --silent > token.txt
  fi
  cat token.txt
}

TOKEN=$(get_valid_token)
```

---

### "Invalid token format"

**Error:**
```
Error: Token is not a valid JWT
```

**Cause:** Token string is malformed or corrupted.

**Solutions:**

**1. Check for whitespace or newlines:**
```bash
# ❌ Token with newline
TOKEN="eyJ0eXAi...
extra text"

# ✓ Trim whitespace
TOKEN=$(entratool get-token -p my-profile --silent | tr -d '\n')
```

**2. Verify token structure:**
```bash
# Should have 3 parts separated by dots
echo "$TOKEN" | awk -F. '{print NF-1}'
# Should output: 2
```

**3. Regenerate token:**
```bash
TOKEN=$(entratool get-token -p my-profile --silent)
```

---

### "Invalid audience"

**Error:**
```
Error: Token audience does not match
HTTP 401 Unauthorized
```

**Cause:** Token was issued for a different API than you're calling.

**Solution:**

**1. Check token audience:**
```bash
entratool inspect -t "$TOKEN" | jq -r .payload.aud
```

**2. Request token with correct scope:**
```bash
# For Microsoft Graph
entratool get-token -p my-profile \
  --scope "https://graph.microsoft.com/.default"

# For Azure Management
entratool get-token -p my-profile \
  --scope "https://management.azure.com/.default"

# For custom API
entratool get-token -p my-profile \
  --scope "https://api.contoso.com/.default"
```

---

## Permission Errors

### "Insufficient privileges"

**Error:**
```
Error: Insufficient privileges to complete the operation
HTTP 403 Forbidden
```

**Cause:** Token has the scope but user/app lacks the underlying permission.

**Solutions:**

**For users:**
1. Verify user has appropriate Azure AD role:
   - Azure Portal → Azure Active Directory → Users
   - Select user → Assigned roles
   - Assign required role (e.g., Global Reader, User Administrator)

**For service principals:**
1. Grant admin consent for Application permissions:
   - Azure Portal → App registrations → Your app → API permissions
   - Check "Status" column shows "Granted"
   - If not, click "Grant admin consent"

**2. Check token claims:**
```bash
entratool inspect -t "$TOKEN" | jq '.payload | {scp, roles}'
```

---

### "AADSTS50105: Not assigned to a role"

**Error:**
```
AADSTS50105: The signed in user is not assigned to a role for the application.
```

**Cause:** User is not assigned to the application (when assignment is required).

**Solution:**
1. Azure Portal → Enterprise applications → Your app
2. Click "Users and groups"
3. Click "Add user/group"
4. Select user and assign role
5. Click "Assign"

---

### "AADSTS50076: MFA required"

**Error:**
```
AADSTS50076: Due to a configuration change made by your administrator, or because you moved to a new location, you must use multi-factor authentication to access.
```

**Cause:** Multi-factor authentication is required by conditional access policy.

**Solution:**
- Use interactive flow (Interactive Browser or Device Code)
- User will be prompted for MFA during authentication
- Client Credentials flow cannot satisfy MFA requirements (use certificate or Managed Identity)

---

## Certificate Issues

### "Certificate not found"

**Error:**
```
Error: Certificate file not found: /path/to/cert.pfx
```

**Cause:** Certificate path is incorrect or file doesn't exist.

**Solution:**

**1. Verify path:**
```bash
ls -l /path/to/cert.pfx
```

**2. Update profile with correct path:**
```bash
entratool config edit -p my-profile
# Select: Certificate Path
# Enter: /correct/path/to/cert.pfx
```

---

### "Invalid certificate password"

**Error:**
```
Error: The specified password is incorrect
```

**Cause:** Certificate password is incorrect.

**Solution:**

**1. Update password:**
```bash
entratool config edit -p my-profile
# Select: Certificate Password
# Enter correct password
```

**2. Verify certificate manually:**
```bash
# macOS/Linux
openssl pkcs12 -in cert.pfx -noout -passin pass:YourPassword

# If this fails, password is incorrect
```

---

### "Certificate expired"

**Error:**
```
Error: Certificate has expired
AADSTS700027: Client assertion contains an invalid signature.
```

**Cause:** Certificate has passed its expiration date.

**Solution:**

**1. Check certificate expiration:**
```bash
openssl pkcs12 -in cert.pfx -nodes -passin pass:YourPassword | \
  openssl x509 -noout -enddate
```

**2. Generate new certificate:**
```bash
# See: /docs/certificates/creation/
```

**3. Update app registration:**
1. Azure Portal → App registrations → Your app
2. Certificates & secrets → Certificates → Upload
3. Upload new certificate

**4. Update profile:**
```bash
entratool config edit -p my-profile
# Update certificate path to new certificate
```

---

## Network Issues

### "Connection timeout"

**Error:**
```
Error: Connection timed out
Failed to connect to login.microsoftonline.com
```

**Cause:** Network connectivity issue or firewall blocking.

**Solutions:**

**1. Check internet connectivity:**
```bash
ping login.microsoftonline.com
```

**2. Check firewall rules:**
- Ensure outbound HTTPS (443) is allowed
- Check corporate proxy settings

**3. Configure proxy (if needed):**
```bash
export HTTPS_PROXY=http://proxy.company.com:8080
entratool get-token -p my-profile
```

---

### "SSL/TLS error"

**Error:**
```
Error: SSL certificate problem: unable to get local issuer certificate
```

**Cause:** SSL/TLS certificate validation failure.

**Solutions:**

**1. Update CA certificates:**
```bash
# Ubuntu/Debian
sudo apt-get update && sudo apt-get install ca-certificates

# macOS
# Certificates managed by Keychain, usually no action needed

# Windows
# Certificates managed by Windows, usually no action needed
```

**2. Corporate proxy with SSL inspection:**
```bash
# If behind corporate proxy doing SSL inspection:
# Import corporate root CA certificate to system trust store
```

---

## Flow-Specific Issues

### Device Code: "Code expired"

**Error:**
```
Error: Device code expired
```

**Cause:** User didn't authenticate within the time limit (typically 15 minutes).

**Solution:**
```bash
# Request new device code
entratool get-token -p my-profile -f DeviceCode
# Authenticate faster this time
```

---

### Interactive Browser: "Redirect URI mismatch"

**Error:**
```
AADSTS50011: The redirect URI specified in the request does not match the redirect URIs configured for the application.
```

**Cause:** Localhost redirect URI not configured in app registration.

**Solution:**
1. Azure Portal → App registrations → Your app
2. Authentication → Add a platform → Web
3. Add redirect URI: `http://localhost:8080`
4. Save

**Try:**
```bash
entratool get-token -p my-profile -f InteractiveBrowser
```

---

### "Browser did not respond"

**Error:**
```
Error: Browser authentication timed out
```

**Cause:** Browser didn't complete authentication or localhost port blocked.

**Solutions:**

**1. Try Device Code flow instead:**
```bash
entratool get-token -p my-profile -f DeviceCode
```

**2. Check localhost is not blocked:**
```bash
# Verify localhost resolves
ping localhost

# Check port is available
nc -zv localhost 8080
```

**3. Try different port (if configurable):**
```bash
# Configure alternate redirect URI in Azure Portal
# e.g., http://localhost:8081
```

---

## Platform-Specific Issues

### Linux: "Secrets not secure"

**Warning:**
```
Warning: Linux storage uses XOR obfuscation (not secure)
```

**Cause:** Linux lacks universal secure storage.

**Recommendations:**

**1. Use certificate authentication:**
```bash
entratool config create
# Select: Certificate
# Store certificate with restricted permissions
chmod 600 /path/to/cert.pfx
```

**2. Use environment variables:**
```bash
export AZURE_CLIENT_SECRET="your-secret"
# Create profile without stored secret
```

**3. Use external secret manager:**
```bash
# Azure Key Vault
SECRET=$(az keyvault secret show --vault-name MyVault --name MySecret -query value -o tsv)
entratool get-token -p my-profile --client-secret "$SECRET"
```

---

### macOS: "Operation not permitted"

**Error:**
```
Error: Keychain operation not permitted
```

**Cause:** Keychain is locked or application needs permission.

**Solution:**
1. Open Keychain Access app
2. Unlock "login" keychain
3. Try again

---

### Windows: "DPAPI error"

**Error:**
```
Error: Failed to protect data using DPAPI
```

**Cause:** User profile issue.

**Solution:**
1. Check user profile integrity
2. Run `sfc /scannow` as administrator
3. If problem persists, create new Windows user profile

---

## Diagnostic Commands

### Check Tool Version

```bash
entratool --version
```

### Verify Profile Configuration

```bash
entratool config list
cat ~/.entratool/profiles.json | jq
```

### Test Token Generation

```bash
entratool get-token -p my-profile --silent
```

### Inspect Token

```bash
TOKEN=$(entratool get-token -p my-profile --silent)
entratool inspect -t "$TOKEN"
```

### Check Token Validity

```bash
entratool discover -t "$TOKEN"
echo "Exit code: $?"  # 0 = valid, 1 = expired
```

---

## Getting Help

### Enable Verbose Output

```bash
entratool get-token -p my-profile --verbose
```

### Check Logs

**Location:**
- Windows: `%TEMP%\entratool\`
- macOS/Linux: `/tmp/entratool/`

### Report Issues

Include:
1. Tool version: `entratool --version`
2. Platform: Windows/macOS/Linux
3. Error message (redact sensitive info)
4. Steps to reproduce

---

## Next Steps

- [Core Concepts](/docs/core-concepts/)
- [User Guide](/docs/user-guide/)
- [Platform Guides](/docs/platform-guides/)
- [Security Best Practices](/docs/recipes/security-hardening/)
