---
title: "Certificate Authentication"
description: "Using certificates for secure authentication"
weight: 55
---

# Certificate Authentication

Learn how to use X.509 certificates for secure, passwordless authentication with Microsoft Entra ID.

---

## Why Use Certificates?

### Advantages Over Client Secrets

| Feature | Client Secret | Certificate |
|---------|--------------|-------------|
| **Security** | Medium | High |
| **Validity Period** | Up to 2 years | Up to 3 years |
| **Rotation** | Manual | Automated possible |
| **Compromise Risk** | Higher | Lower |
| **Audit Trail** | Limited | Better |

### When to Use Certificates

✅ **Use certificates for:**
- Production service principals
- Long-running automated services
- High-security requirements
- Certificate-based infrastructure

⚠️ **Stick with secrets for:**
- Quick development/testing
- Short-lived projects
- Simple automation scripts

---

## Getting Started

### Quick Example

```bash
# Create profile with certificate
entratool config create
# Select: Certificate
# Path: /path/to/certificate.pfx
# Password: ****

# Generate token
entratool get-token -p cert-profile
```

---

## Certificate Types

### PFX/PKCS12 Format

**Most common format for certificates with private keys**

```bash
# File extension
certificate.pfx
certificate.p12

# Contains
- X.509 certificate
- Private key
- Optional: Certificate chain
```

**Supported by:**
- Windows Certificate Store
- macOS Keychain
- File-based storage

### PEM Format

**Text-based format (not directly supported)**

To use PEM certificates, convert to PFX:

```bash
openssl pkcs12 -export \
  -in certificate.pem \
  -inkey private-key.pem \
  -out certificate.pfx \
  -password pass:YourPassword
```

---

## Creating Certificates

### Option 1: Self-Signed Certificate

**For development/testing only:**

```bash
# Windows (PowerShell)
$cert = New-SelfSignedCertificate `
  -Subject "CN=MyApp" `
  -CertStoreLocation "Cert:\CurrentUser\My" `
  -KeyExportPolicy Exportable `
  -KeySpec Signature `
  -NotAfter (Get-Date).AddYears(2)

$password = ConvertTo-SecureString -String "YourPassword" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "certificate.pfx" -Password $password
```

```bash
# macOS/Linux
openssl req -x509 -newkey rsa:4096 \
  -keyout key.pem -out cert.pem \
  -days 730 -nodes \
  -subj "/CN=MyApp"

# Convert to PFX
openssl pkcs12 -export \
  -in cert.pem -inkey key.pem \
  -out certificate.pfx \
  -password pass:YourPassword
```

### Option 2: Certificate Authority (CA)

**For production:**

1. Generate Certificate Signing Request (CSR)
2. Submit to your organization's CA
3. Receive signed certificate
4. Combine with private key into PFX

---

## Registering Certificates in Azure

### Upload Certificate to App Registration

1. **Azure Portal** → **Azure Active Directory** → **App registrations**
2. Select your application
3. **Certificates & secrets** → **Certificates** tab
4. Click **Upload certificate**
5. Select your `.cer` or `.pem` file (public key only)
6. Add description
7. Click **Add**

### Extract Public Key from PFX

```bash
# macOS/Linux
openssl pkcs12 -in certificate.pfx -nokeys -out certificate.cer

# Windows (PowerShell)
$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2("certificate.pfx", "password")
[System.IO.File]::WriteAllBytes("certificate.cer", $cert.Export("Cert"))
```

---

## Configuring Profiles with Certificates

### Interactive Configuration

```bash
entratool config create

# Prompts:
Profile name: production-service
Client ID: 12345678-1234-1234-1234-123456789abc
Tenant ID: 87654321-4321-4321-4321-cba987654321
Authentication method:
  1. Client Secret
  2. Certificate
Select: 2
Certificate path: /path/to/certificate.pfx
Certificate password: ****
Scope: https://graph.microsoft.com/.default

✓ Profile 'production-service' created successfully
```

### Profile Structure

**profiles.json:**
```json
{
  "profiles": [
    {
      "name": "production-service",
      "clientId": "12345678-1234-1234-1234-123456789abc",
      "tenantId": "87654321-4321-4321-4321-cba987654321",
      "certificatePath": "/path/to/certificate.pfx",
      "useCertificate": true,
      "scope": "https://graph.microsoft.com/.default"
    }
  ]
}
```

**Certificate password:** Stored securely in platform-specific storage

---

## Certificate Storage

### File-Based Storage

**Best practices:**

```bash
# Create secure directory
mkdir -p ~/.entratool/certs
chmod 700 ~/.entratool/certs

# Store certificate with restricted permissions
cp certificate.pfx ~/.entratool/certs/
chmod 600 ~/.entratool/certs/certificate.pfx

# Update profile with path
entratool config edit -p myprofile
# Certificate path: /Users/username/.entratool/certs/certificate.pfx
```

### Windows Certificate Store

**Import certificate:**

```powershell
# Import to Current User store
Import-PfxCertificate -FilePath certificate.pfx `
  -CertStoreLocation Cert:\CurrentUser\My `
  -Password (ConvertTo-SecureString -String "password" -AsPlainText -Force)

# List certificates
Get-ChildItem Cert:\CurrentUser\My
```

**Reference by thumbprint in profile:**
```json
{
  "certificateThumbprint": "ABC123...",
  "certificateStoreLocation": "CurrentUser"
}
```

### macOS Keychain

**Import certificate:**

```bash
security import certificate.pfx -k login.keychain -P YourPassword

# List certificates
security find-identity -v -p codesigning
```

---

## Using Certificates

### Generate Token

```bash
entratool get-token -p cert-profile
```

**What happens:**
1. Profile loaded
2. Certificate password retrieved from secure storage
3. Certificate loaded from path or store
4. Token generated using certificate authentication
5. No password prompt if cached

### Certificate Password Caching

**First use:**
```bash
entratool get-token -p cert-profile
# May prompt for certificate password
```

**Subsequent uses:**
```bash
entratool get-token -p cert-profile
# No password prompt (cached securely)
```

---

## Certificate Rotation

### Why Rotate Certificates?

- Expiring certificates (check expiration dates)
- Security policy requirements
- Compromised certificates
- Annual/periodic rotation policies

### Rotation Process

**1. Generate new certificate:**
```bash
# Generate new certificate (see "Creating Certificates" above)
```

**2. Upload to Azure Portal:**
- App registrations → Your app → Certificates & secrets
- Upload new certificate
- **Don't delete old certificate yet**

**3. Test new certificate:**
```bash
# Create test profile with new certificate
entratool config create
# Name: test-new-cert
# Certificate: /path/to/new-certificate.pfx

# Test token generation
entratool get-token -p test-new-cert
```

**4. Update production profile:**
```bash
entratool config edit -p production-service
# Select: Certificate Path
# Enter: /path/to/new-certificate.pfx
# Enter password: ****

# Test production profile
entratool get-token -p production-service
```

**5. Remove old certificate from Azure:**
- App registrations → Your app → Certificates & secrets
- Delete old certificate

### Automated Rotation Script

```bash
#!/bin/bash
set -euo pipefail

PROFILE="production-service"
NEW_CERT="/path/to/new-certificate.pfx"
CERT_PASSWORD="YourPassword"

# Backup current profile
entratool config export -p "$PROFILE" -o "backup-$PROFILE.json"

# Update certificate path (manual edit required for password)
echo "Updating profile with new certificate..."
entratool config edit -p "$PROFILE"
# Manual step: Select Certificate Path, enter new path and password

# Test
echo "Testing new certificate..."
if entratool get-token -p "$PROFILE" --silent > /dev/null; then
  echo "✓ Certificate rotation successful"
else
  echo "✗ Certificate rotation failed"
  exit 1
fi
```

---

## Certificate Validation

### Check Certificate Expiration

```bash
# macOS/Linux
openssl pkcs12 -in certificate.pfx -nodes -passin pass:YourPassword | \
  openssl x509 -noout -enddate

# Windows (PowerShell)
$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2("certificate.pfx", "password")
$cert.NotAfter
```

### Verify Certificate Thumbprint

```bash
# macOS/Linux
openssl pkcs12 -in certificate.pfx -nodes -passin pass:YourPassword | \
  openssl x509 -noout -fingerprint -sha1

# Windows (PowerShell)
$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2("certificate.pfx", "password")
$cert.Thumbprint
```

---

## Troubleshooting

### "Certificate not found"

**Cause:** Certificate path is incorrect

**Solution:**
```bash
# Verify file exists
ls -l /path/to/certificate.pfx

# Update profile
entratool config edit -p myprofile
# Select: Certificate Path
```

### "Invalid certificate password"

**Cause:** Incorrect password or corrupted certificate

**Solution:**
```bash
# Test certificate manually
openssl pkcs12 -in certificate.pfx -noout -passin pass:YourPassword

# Update password in profile
entratool config edit -p myprofile
# Select: Certificate Password
```

### "AADSTS700027: Client assertion contains an invalid signature"

**Cause:** Certificate not registered in Azure or expired

**Solution:**
1. Verify certificate is uploaded to app registration
2. Check certificate expiration
3. Ensure correct certificate (match thumbprints)

### "Certificate has expired"

**Cause:** Certificate validity period has passed

**Solution:**
1. Generate new certificate
2. Upload to Azure Portal
3. Update profile with new certificate

---

## Security Best Practices

### ✅ Do

- Use strong certificate passwords
- Store certificates with restrictive file permissions (600)
- Rotate certificates annually or per policy
- Use CA-signed certificates for production
- Monitor certificate expiration dates
- Use separate certificates for dev/staging/prod

### ❌ Don't

- Store certificates in git repositories
- Share certificates via email/Slack
- Use weak passwords
- Ignore certificate expiration warnings
- Reuse certificates across environments

---

## Next Steps

- [Creating Profiles with Certificates](/docs/user-guide/managing-profiles/creating/)
- [Platform-Specific Certificate Storage](/docs/platform-guides/)
- [Security Best Practices](/docs/recipes/security-hardening/)
- [Troubleshooting Certificate Issues](/docs/troubleshooting/)
