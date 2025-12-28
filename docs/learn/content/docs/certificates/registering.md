---
title: "Registering in Azure"
description: "Upload certificates to Azure app registrations"
weight: 3
---

# Registering Certificates in Azure

Learn how to upload and manage certificates in Azure app registrations.

---

## Prerequisites

Before registering a certificate:
- ✅ Certificate created (PFX format)
- ✅ Azure app registration exists
- ✅ Appropriate permissions in Azure AD

---

## Upload Certificate

### Via Azure Portal

**1. Navigate to App Registration**
1. Sign in to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory**
3. Select **App registrations**
4. Find and select your application

**2. Access Certificates & Secrets**
1. In the left menu, select **Certificates & secrets**
2. Click the **Certificates** tab

**3. Upload Certificate**
1. Click **Upload certificate**
2. Click **Select a file**
3. Select your `.cer` or `.pem` file (public key only)
4. Add a description (e.g., "Production Certificate 2024")
5. Click **Add**

**4. Verify Upload**
- Certificate appears in the list
- Note the thumbprint
- Check expiration date

### Extract Public Key from PFX

Azure requires only the public key (.cer or .pem), not the private key.

**macOS / Linux:**
```bash
# Extract public key from PFX
openssl pkcs12 -in certificate.pfx \
  -clcerts -nokeys \
  -out certificate.cer \
  -passin pass:YourPassword

# Verify extraction
openssl x509 -in certificate.cer -text -noout
```

**Windows (PowerShell):**
```powershell
# Extract public key
$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2("certificate.pfx", "YourPassword")
$bytes = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert)
[System.IO.File]::WriteAllBytes("certificate.cer", $bytes)

# Verify
certutil -dump certificate.cer
```

---

## Via Azure CLI

### Upload Certificate

```bash
# Login to Azure
az login

# Upload certificate (using .cer file)
az ad app credential reset \
  --id <application-id> \
  --cert @certificate.cer \
  --append

# Or create from PFX (extracts public key automatically)
az ad app credential reset \
  --id <application-id> \
  --cert @certificate.pfx \
  --append
```

### List Certificates

```bash
# List all credentials for app
az ad app credential list \
  --id <application-id>

# Filter for certificates only
az ad app credential list \
  --id <application-id> \
  --query "[?type=='AsymmetricX509Cert']"
```

### Delete Certificate

```bash
# Delete by key ID
az ad app credential delete \
  --id <application-id> \
  --key-id <credential-key-id>
```

---

## Via PowerShell (AzureAD Module)

### Upload Certificate

```powershell
# Install module if needed
Install-Module AzureAD

# Connect
Connect-AzureAD

# Load certificate
$certPath = "C:\path\to\certificate.cer"
$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($certPath)

# Upload to app
$appObjectId = "<application-object-id>"
New-AzureADApplicationKeyCredential `
  -ObjectId $appObjectId `
  -CustomKeyIdentifier "Production Cert 2024" `
  -Type AsymmetricX509Cert `
  -Usage Verify `
  -Value $cert.GetRawCertData()
```

### List Certificates

```powershell
# Get app
$app = Get-AzureADApplication -ObjectId "<application-object-id>"

# List key credentials (certificates)
$app.KeyCredentials | Format-Table CustomKeyIdentifier, KeyId, EndDate
```

---

## Verify Certificate Registration

### Check Thumbprint

**Azure Portal:**
1. App registrations → Your app → Certificates & secrets
2. Find your certificate
3. Note the thumbprint value

**Compare with local certificate:**
```bash
# macOS/Linux
openssl pkcs12 -in certificate.pfx \
  -nokeys -passin pass:YourPassword | \
  openssl x509 -noout -fingerprint -sha1

# Windows (PowerShell)
$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2("certificate.pfx", "YourPassword")
$cert.Thumbprint
```

### Test Authentication

```bash
# Create test profile with certificate
entratool config create
# Name: cert-test
# Client ID: <your-app-id>
# Tenant ID: <your-tenant-id>
# Auth method: Certificate
# Path: /path/to/certificate.pfx
# Password: ****

# Test token generation
entratool get-token -p cert-test

# If successful, certificate is properly registered
```

---

## Multiple Certificates

### Why Use Multiple Certificates?

- **Rotation**: Add new certificate before removing old one
- **Environments**: Separate certificates for dev/staging/prod
- **Teams**: Different certificates for different teams/services
- **Compliance**: Meet audit requirements for key rotation

### Managing Multiple Certificates

**Add second certificate without removing first:**
```bash
# Azure CLI with --append flag
az ad app credential reset \
  --id <application-id> \
  --cert @new-certificate.cer \
  --append

# Note: Without --append, existing credentials are replaced
```

**Best practice naming:**
```
Production-2024-Q1
Production-2024-Q2-Rotation
Development-Team-A
Development-Team-B
Staging-Environment
```

---

## Certificate Metadata

### What Gets Stored in Azure?

Azure stores these certificate properties:

| Property | Description | Example |
|----------|-------------|---------|
| **CustomKeyIdentifier** | Your description | "Prod Cert 2024" |
| **KeyId** | Unique identifier (GUID) | "abc123..." |
| **Type** | Credential type | AsymmetricX509Cert |
| **Usage** | Purpose | Verify |
| **EndDate** | Expiration date | 2025-12-31 |
| **StartDate** | Valid from date | 2024-01-01 |
| **Value** | Public key bytes | (binary data) |

### Private Key Security

⚠️ **Important:** 
- Only the **public key** is uploaded to Azure
- The **private key** never leaves your system
- Azure cannot access your private key
- This is a key security feature

---

## Permissions Required

### To Upload Certificates

**Application Administrator** role or:
- `Application.ReadWrite.All`
- `Application.ReadWrite.OwnedBy`

**As app owner:**
- Automatic permission if you created the app
- No additional role needed

### Grant Permissions

```bash
# Grant user Application Administrator role
az ad directory-role member add \
  --role "Application Administrator" \
  --member-id <user-object-id>
```

---

## Troubleshooting

### "Insufficient privileges"

**Cause:** Missing permissions to modify app registration

**Solution:**
```bash
# Check your roles
az ad signed-in-user list-owned-applications

# Or request Application Administrator role
```

### "Certificate validation failed"

**Cause:** Incorrect certificate format or corrupted file

**Solution:**
```bash
# Verify certificate is valid
openssl x509 -in certificate.cer -text -noout

# Re-extract from PFX
openssl pkcs12 -in certificate.pfx -clcerts -nokeys -out certificate.cer
```

### "Certificate already exists"

**Cause:** Duplicate certificate (same public key)

**Solution:**
- Remove old certificate first, or
- Use a different certificate

### Wrong certificate uploaded

**Solution:**
```bash
# Remove incorrect certificate
az ad app credential delete \
  --id <application-id> \
  --key-id <credential-key-id>

# Upload correct certificate
az ad app credential reset \
  --id <application-id> \
  --cert @correct-certificate.cer \
  --append
```

---

## Automation

### Automated Certificate Upload

```bash
#!/bin/bash
set -euo pipefail

APP_ID="<your-application-id>"
CERT_FILE="certificate.pfx"
CERT_PASSWORD="YourPassword"
DESCRIPTION="Auto-uploaded $(date +%Y-%m-%d)"

# Extract public key
openssl pkcs12 -in "$CERT_FILE" \
  -clcerts -nokeys \
  -out temp-cert.cer \
  -passin pass:$CERT_PASSWORD

# Upload to Azure
az ad app credential reset \
  --id "$APP_ID" \
  --cert @temp-cert.cer \
  --append

# Clean up
rm temp-cert.cer

echo "✓ Certificate uploaded successfully"
echo "  App ID: $APP_ID"
echo "  Description: $DESCRIPTION"

# Get thumbprint for verification
THUMBPRINT=$(openssl pkcs12 -in "$CERT_FILE" \
  -nokeys -passin pass:$CERT_PASSWORD | \
  openssl x509 -noout -fingerprint -sha1 | \
  cut -d= -f2)

echo "  Thumbprint: $THUMBPRINT"
```

---

## Next Steps

- [Configuring Profiles](/docs/certificates/configuring/) - Set up certificate authentication
- [Certificate Storage](/docs/certificates/storage/) - Secure storage options
- [Using Certificates](/docs/certificates/using/) - Generate tokens
- [Certificate Rotation](/docs/certificates/rotation/) - Rotate certificates safely
