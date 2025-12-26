---
title: "Secure Storage"
description: "How secrets and certificates are stored securely"
weight: 40
---

# Secure Storage

Entra Token CLI uses platform-specific secure storage mechanisms to protect sensitive authentication data. Understanding how secrets are stored helps you make informed security decisions.

---

## Platform Security Overview

| Platform | Mechanism | Security Level | Notes |
|----------|-----------|----------------|-------|
| **Windows** | DPAPI | 🔒 **Strong** | User-account encryption |
| **macOS** | Keychain | 🔒 **Strong** | System-level encryption |
| **Linux** | XOR Obfuscation | ⚠️ **Weak** | Not secure for production |

---

## What Gets Stored

### Profile Configuration (Plaintext)

**Location:** `~/.entratool/profiles.json`

**Stored in plaintext:**
- Profile name
- Client ID
- Tenant ID
- Scope
- Authority URL
- OAuth2 flow preference

**Example:**
```json
{
  "profiles": [
    {
      "name": "my-service-principal",
      "clientId": "12345678-1234-1234-1234-123456789abc",
      "tenantId": "87654321-4321-4321-4321-cba987654321",
      "scope": "https://graph.microsoft.com/.default",
      "flow": "ClientCredentials"
    }
  ]
}
```

### Secrets (Encrypted)

**What's encrypted:**
- Client secrets
- Certificate passwords
- Private key data

**Storage location:**
- **Windows:** DPAPI-encrypted data
- **macOS:** Keychain access
- **Linux:** XOR-obfuscated file

---

## Windows Security (DPAPI)

### How It Works

**Data Protection API (DPAPI):**
- Built into Windows
- Uses user account credentials to encrypt data
- Data is automatically tied to user profile

**Security Properties:**
- ✅ Strong encryption (AES-256)
- ✅ Automatic key management
- ✅ Per-user encryption
- ✅ OS-level protection

### Storage Location

Encrypted data is stored in user-specific Windows data stores. The exact location is managed by Windows.

### Access Control

- Only the **same user account** on the **same machine** can decrypt
- Administrator accounts **cannot** decrypt another user's DPAPI data
- Backing up and restoring requires user profile migration

### Security Considerations

✅ **Safe for:**
- Development workstations
- Single-user systems
- Corporate managed devices

⚠️ **Be aware:**
- Data is tied to user profile
- Lost password = lost data (unless backed up properly)
- Malware running under your account can access

---

## macOS Security (Keychain)

### How It Works

**macOS Keychain:**
- System-level secure storage
- Encrypted with user's login password
- Managed by macOS Security framework

**Security Properties:**
- ✅ Strong encryption (AES-256)
- ✅ Integration with Touch ID / Face ID
- ✅ Per-item access control
- ✅ Audit logging

### Storage Location

```bash
~/Library/Keychains/login.keychain-db
```

**Note:** Keychain is encrypted; you cannot read it directly.

### Viewing Secrets

Open **Keychain Access** app:

1. Launch: `/Applications/Utilities/Keychain Access.app`
2. Search for: `entratool`
3. Double-click item → "Show password"
4. Authenticate with your login password

**Screenshot:**
<!-- Screenshot: keychain-access-entratool.png - Keychain Access showing entratool entries -->

### Access Control

- Requires user authentication to access
- Can be protected by Touch ID
- Can set per-item access policies

### Exporting Keychain

You can export secrets for backup:

```bash
security export -k login.keychain-db -t identities -o backup.p12
```

⚠️ **Warning:** Exported files contain unencrypted secrets. Protect them carefully.

### Security Considerations

✅ **Safe for:**
- Development workstations
- Personal Macs
- Production automation (with proper access controls)

⚠️ **Be aware:**
- Malware with keychain access can extract secrets
- User password compromise = keychain compromise
- Backup keychain securely

---

## Linux Security (XOR Obfuscation)

### ⚠️ Security Warning

{{% alert context="danger" %}}
**Linux storage is NOT secure for production use.**

Secrets are obfuscated using XOR encoding, which provides **no real security**. Anyone with access to the file can easily reverse the obfuscation.
{{% /alert %}}

### How It Works

**XOR Obfuscation:**
- Simple XOR cipher with fixed key
- **Not encryption** — just encoding
- Prevents casual viewing in text editors

**Security Properties:**
- ❌ No real protection
- ❌ Easily reversible
- ❌ Fixed obfuscation key in source code

### Storage Location

```bash
~/.entratool/secrets.dat
```

**Warning:** This file contains your secrets in an easily decodable format.

### Why XOR?

Linux lacks a universal, secure secret storage mechanism:
- **Keyring services** vary by distribution (GNOME Keyring, KWallet, etc.)
- **Not always available** on servers or minimal installs
- **Inconsistent APIs** across distributions

XOR obfuscation is a **lowest common denominator** approach that works everywhere but provides minimal security.

### Linux Alternatives

#### 1. Environment Variables

Store secrets in environment variables:

```bash
export AZURE_CLIENT_SECRET="your-secret"
entratool get-token -p myprofile
```

**Profile without stored secret:**
```json
{
  "name": "myprofile",
  "clientId": "...",
  "useClientSecret": true
  // No "clientSecret" field
}
```

The tool will read from `AZURE_CLIENT_SECRET` if available.

#### 2. Azure Key Vault

Store secrets in Azure Key Vault and retrieve at runtime:

```bash
SECRET=$(az keyvault secret show --vault-name MyVault --name ClientSecret --query value -o tsv)
entratool get-token -p myprofile --client-secret "$SECRET"
```

#### 3. HashiCorp Vault

```bash
SECRET=$(vault kv get -field=client_secret secret/entratool)
entratool get-token -p myprofile --client-secret "$SECRET"
```

#### 4. Certificate Authentication

Use certificates instead of secrets (more secure):

```bash
entratool config create
# Select: Certificate
# Provide: /path/to/cert.pfx
```

Certificates can be stored with restricted file permissions:
```bash
chmod 600 /path/to/cert.pfx
chown myuser:myuser /path/to/cert.pfx
```

#### 5. Managed Identity (Azure VMs)

On Azure VMs, use Managed Identity (no secrets needed):

```bash
# Configure VM with Managed Identity
# No secrets stored locally
entratool get-token -p managed-identity-profile
```

### Security Recommendations

{{% alert context="warning" %}}
**For production Linux workloads:**

1. **Use certificate authentication** instead of client secrets
2. **Store certificates** with strict file permissions (600)
3. **Use external secret managers** (Azure Key Vault, Vault)
4. **Use Managed Identity** on Azure VMs
5. **Rotate secrets frequently**
6. **Monitor secret access**

**DO NOT** rely on `~/.entratool/secrets.dat` for production security.
{{% /alert %}}

---

## Certificate Storage

### How Certificates Are Stored

Certificates are **not stored by the tool**. You provide the certificate path:

```json
{
  "name": "cert-profile",
  "certificatePath": "/path/to/certificate.pfx"
}
```

### Certificate Security

**Security depends on:**
1. **File permissions** on certificate file
2. **Password protection** of PFX/PKCS12 file
3. **Storage location** security

### Best Practices

#### 1. Restrict File Permissions

```bash
# Linux/macOS
chmod 600 /path/to/cert.pfx
chown myuser:myuser /path/to/cert.pfx

# Windows
icacls cert.pfx /inheritance:r /grant:r "%USERNAME%:F"
```

#### 2. Password-Protect Certificates

Always use password-protected PFX files:

```bash
# Convert to password-protected PFX
openssl pkcs12 -export -in cert.pem -inkey key.pem \
  -out cert.pfx -password pass:YourStrongPassword
```

#### 3. Store in Secure Locations

**macOS:**
```bash
# Store in user-protected directory
~/Library/Application Support/entratool/certs/
```

**Windows:**
```bash
# Store in user profile
%USERPROFILE%\.entratool\certs\
```

**Linux:**
```bash
# Store with restricted permissions
~/.entratool/certs/
chmod 700 ~/.entratool/certs/
```

#### 4. Use Certificate Stores

**Windows Certificate Store:**
```bash
# Import certificate to Windows store
certutil -user -p YourPassword -importPFX cert.pfx

# Reference by thumbprint in profile
"certificateThumbprint": "ABC123..."
```

**macOS Keychain:**
```bash
# Import to Keychain
security import cert.pfx -k login.keychain -P YourPassword

# Reference by name
"certificateName": "My Certificate"
```

---

## Secret Lifecycle

### 1. Secret Creation

**Client Secret:**
```bash
entratool config create
# Select: Client Secret
# Enter: your-secret-here
# ✓ Encrypted and stored securely
```

**Certificate:**
```bash
entratool config create
# Select: Certificate
# Enter: /path/to/cert.pfx
# Enter password: ****
# ✓ Password encrypted and stored
# ✓ Certificate path stored (plaintext)
```

### 2. Secret Usage

When generating tokens:
1. Profile is read from `profiles.json`
2. Secrets are decrypted from secure storage
3. Used for authentication
4. Never written to logs or disk unencrypted

### 3. Secret Rotation

Update secrets regularly:

```bash
# Edit profile and update secret
entratool config edit -p myprofile
# Select: Client Secret or Certificate
# Enter new secret
# ✓ Old secret overwritten
```

### 4. Secret Deletion

When deleting a profile:

```bash
entratool config delete -p myprofile
# ✓ Profile removed from profiles.json
# ✓ Associated secrets removed from secure storage
```

---

## Security Audit

### Checking What's Stored

#### List Profiles
```bash
entratool config list
```

#### View Profile Details
```bash
# View non-sensitive profile data
cat ~/.entratool/profiles.json | jq
```

#### Check Keychain (macOS)
```bash
security find-generic-password -s entratool -g
```

#### DPAPI Inventory (Windows)
Use specialized tools like `dpapick` to audit DPAPI-protected data.

---

## Best Practices

### ✅ Development

- Use platform secure storage (DPAPI on Windows, Keychain on macOS)
- Separate profiles for dev/test/prod
- Short-lived secrets
- Regular rotation

### ✅ Production

**Windows/macOS:**
- Platform secure storage is acceptable
- Certificates preferred over secrets
- Regular audits

**Linux:**
- ⚠️ **Do not use built-in storage**
- Use external secret manager (Key Vault, Vault)
- Or use certificate authentication with restricted file permissions
- Or use Managed Identity

### ✅ CI/CD

- Store secrets in CI/CD secret manager (GitHub Secrets, Azure Pipelines)
- Inject at runtime
- Never commit secrets to git
- Use short-lived tokens

### ❌ Never

- ❌ Commit `profiles.json` to git
- ❌ Share secrets via email/Slack
- ❌ Store secrets in plaintext files
- ❌ Use the same secret across environments
- ❌ Trust Linux XOR storage for production

---

## Troubleshooting

### "Access to secure storage denied"

**Windows:**
- User profile may be corrupted
- Try running as the correct user

**macOS:**
- Keychain may be locked
- Open Keychain Access and unlock

### "Cannot decrypt secret"

**Cause:** Secret was encrypted on different machine or user account

**Fix:**
1. Delete profile: `entratool config delete -p myprofile`
2. Recreate profile: `entratool config create`
3. Re-enter secret

### Migrating Secrets

Secrets are tied to user accounts and machines. To migrate:

1. Export profile configuration (plaintext):
   ```bash
   cat ~/.entratool/profiles.json
   ```

2. On new machine, recreate profile:
   ```bash
   entratool config create
   # Re-enter all secrets
   ```

**Note:** Secrets cannot be exported/imported directly due to encryption.

---

## Next Steps

- [Creating Profiles](/docs/user-guide/managing-profiles/creating/)
- [Certificate Authentication Guide](/docs/certificates/)
- [Production Deployment](/docs/platform-guides/production/)
- [Security Best Practices](/docs/recipes/security-hardening/)
