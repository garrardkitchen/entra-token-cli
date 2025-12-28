---
title: "Creating Certificates"
description: "Generate self-signed and CA-signed certificates"
weight: 2
---

# Creating Certificates

Learn how to generate X.509 certificates for authentication with Microsoft Entra ID.

---

## Self-Signed Certificates

### For Development/Testing Only

⚠️ **Warning:** Self-signed certificates should only be used for development and testing. Use CA-signed certificates for production.

### Windows (PowerShell)

```powershell
# Generate self-signed certificate
$cert = New-SelfSignedCertificate `
  -Subject "CN=MyApp Dev Certificate" `
  -CertStoreLocation "Cert:\CurrentUser\My" `
  -KeyExportPolicy Exportable `
  -KeySpec Signature `
  -KeyLength 4096 `
  -KeyAlgorithm RSA `
  -HashAlgorithm SHA256 `
  -NotAfter (Get-Date).AddYears(2)

# Export to PFX with password
$password = ConvertTo-SecureString -String "YourStrongPassword123!" -Force -AsPlainText
$pfxPath = "$env:USERPROFILE\Documents\certificate.pfx"
Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $password

Write-Host "Certificate created: $pfxPath"
Write-Host "Thumbprint: $($cert.Thumbprint)"
```

### macOS / Linux

```bash {linenos=inline}
# Generate private key and certificate
openssl req -x509 -newkey rsa:4096 \
  -keyout key.pem \
  -out cert.pem \
  -days 730 \
  -nodes \
  -subj "/CN=MyApp Dev Certificate/O=My Organization/C=US"

# Convert to PFX format
openssl pkcs12 -export \
  -in cert.pem \
  -inkey key.pem \
  -out certificate.pfx \
  -password pass:YourStrongPassword123!

echo "Certificate created: certificate.pfx"

# Display certificate info
openssl x509 -in cert.pem -text -noout
```

### With Subject Alternative Names (SAN)

```bash {linenos=inline}
# Create OpenSSL config file
cat > cert.conf <<EOF
[req]
default_bits = 4096
prompt = no
default_md = sha256
distinguished_name = dn
req_extensions = v3_req

[dn]
CN = MyApp
O = My Organization
C = US

[v3_req]
subjectAltName = @alt_names

[alt_names]
DNS.1 = myapp.example.com
DNS.2 = *.myapp.example.com
EOF

# Generate certificate with SAN
openssl req -new -x509 -days 730 \
  -config cert.conf \
  -keyout key.pem \
  -out cert.pem \
  -nodes

# Convert to PFX
openssl pkcs12 -export \
  -in cert.pem \
  -inkey key.pem \
  -out certificate.pfx \
  -password pass:YourStrongPassword123!
```

---

## CA-Signed Certificates

### For Production Use

Production environments should use certificates signed by a trusted Certificate Authority.

### Step 1: Generate Private Key and CSR

```bash {linenos=inline}
# Generate private key (4096-bit RSA)
openssl genrsa -out private-key.pem 4096

# Create certificate signing request (CSR)
openssl req -new \
  -key private-key.pem \
  -out certificate.csr \
  -subj "/CN=MyApp Production/O=My Organization/C=US"

# Verify CSR
openssl req -text -noout -verify -in certificate.csr
```

### Step 2: Submit CSR to CA

**Internal/Enterprise CA:**
```bash {linenos=inline}
# Submit to your organization's CA
# (Process varies by organization)

# Example for Microsoft Active Directory Certificate Services
certreq -submit -config "CA-SERVER\CA-NAME" certificate.csr
```

**Public CA (e.g., DigiCert, GlobalSign):**
1. Log in to CA portal
2. Request new certificate
3. Paste CSR content
4. Complete validation process
5. Download signed certificate

### Step 3: Combine with Private Key

```bash {linenos=inline}
# After receiving signed certificate from CA:
# Combine certificate and private key into PFX

openssl pkcs12 -export \
  -in signed-certificate.crt \
  -inkey private-key.pem \
  -out certificate.pfx \
  -password pass:YourStrongPassword123!

# Include certificate chain (if provided by CA)
openssl pkcs12 -export \
  -in signed-certificate.crt \
  -inkey private-key.pem \
  -certfile ca-chain.crt \
  -out certificate.pfx \
  -password pass:YourStrongPassword123!
```

### Step 4: Verify Certificate

```bash {linenos=inline}
# Check certificate details
openssl pkcs12 -info -in certificate.pfx -noout -passin pass:YourStrongPassword123!

# Verify certificate chain
openssl verify -CAfile ca-chain.crt signed-certificate.crt

# Check expiration date
openssl pkcs12 -in certificate.pfx -nokeys -passin pass:YourStrongPassword123! | \
  openssl x509 -noout -enddate
```

---

## Best Practices

### Key Length

```bash {linenos=inline}
# Minimum: 2048-bit (acceptable)
openssl genrsa -out key.pem 2048

# Recommended: 4096-bit (better security)
openssl genrsa -out key.pem 4096
```

### Hash Algorithm

```bash {linenos=inline}
# Use SHA-256 or better
-sha256  # Good
-sha384  # Better
-sha512  # Best

# Avoid deprecated algorithms
-md5     # ❌ Never use
-sha1    # ❌ Deprecated
```

### Validity Period

```bash {linenos=inline}
# Development: 1-2 years
-days 365   # 1 year
-days 730   # 2 years

# Production: Follow your organization's policy
# Typically 1 year, with rotation before expiry
```

### Strong Passwords

```bash {linenos=inline}
# Generate strong password
PASSWORD=$(openssl rand -base64 32)

# Use in certificate creation
openssl pkcs12 -export \
  -in cert.pem \
  -inkey key.pem \
  -out certificate.pfx \
  -password pass:$PASSWORD

echo "Certificate password: $PASSWORD"
# Store password in secure location (e.g., Key Vault)
```

---

## Certificate Conversion

### PEM to PFX

```bash {linenos=inline}
# If you have separate PEM files
openssl pkcs12 -export \
  -in certificate.pem \
  -inkey private-key.pem \
  -out certificate.pfx \
  -password pass:YourPassword
```

### PFX to PEM

```bash {linenos=inline}
# Extract certificate (public key)
openssl pkcs12 -in certificate.pfx \
  -clcerts -nokeys \
  -out certificate.pem \
  -passin pass:YourPassword

# Extract private key
openssl pkcs12 -in certificate.pfx \
  -nocerts -nodes \
  -out private-key.pem \
  -passin pass:YourPassword
```

### DER to PFX

```bash {linenos=inline}
# Convert DER to PEM first
openssl x509 -inform DER -in certificate.der -out certificate.pem

# Then PEM to PFX
openssl pkcs12 -export \
  -in certificate.pem \
  -inkey private-key.pem \
  -out certificate.pfx
```

---

## Automation Scripts

### Automated Certificate Generation

```bash {linenos=inline}
#!/bin/bash
set -euo pipefail

# Configuration
APP_NAME="MyApp"
VALIDITY_DAYS=730
KEY_LENGTH=4096
OUTPUT_DIR="./certs"

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Generate password
PASSWORD=$(openssl rand -base64 32)

# Generate certificate
openssl req -x509 -newkey rsa:$KEY_LENGTH \
  -keyout "$OUTPUT_DIR/key.pem" \
  -out "$OUTPUT_DIR/cert.pem" \
  -days $VALIDITY_DAYS \
  -nodes \
  -subj "/CN=$APP_NAME/O=MyOrg/C=US"

# Convert to PFX
openssl pkcs12 -export \
  -in "$OUTPUT_DIR/cert.pem" \
  -inkey "$OUTPUT_DIR/key.pem" \
  -out "$OUTPUT_DIR/certificate.pfx" \
  -password pass:$PASSWORD

# Set restrictive permissions
chmod 600 "$OUTPUT_DIR"/*

# Display info
echo "Certificate created successfully!"
echo "Location: $OUTPUT_DIR/certificate.pfx"
echo "Password: $PASSWORD"
echo ""
echo "⚠️  Store password securely!"

# Extract thumbprint
THUMBPRINT=$(openssl x509 -in "$OUTPUT_DIR/cert.pem" -noout -fingerprint -sha1 | cut -d= -f2)
echo "Thumbprint: $THUMBPRINT"

# Clean up temporary files
rm "$OUTPUT_DIR/key.pem" "$OUTPUT_DIR/cert.pem"
```

---

## Troubleshooting

### "unable to write 'random state'"

**Solution:**
```bash {linenos=inline}
# Remove old random state file
rm ~/.rnd

# Or specify different location
RANDFILE=/tmp/.rnd openssl ...
```

### "unable to load Private Key"

**Solution:**
```bash {linenos=inline}
# Verify private key format
openssl rsa -in private-key.pem -check

# Convert encrypted key to unencrypted
openssl rsa -in encrypted-key.pem -out private-key.pem
```

### "Verification failure"

**Solution:**
```bash {linenos=inline}
# Check if certificate and key match
CERT_MODULUS=$(openssl x509 -noout -modulus -in cert.pem | openssl md5)
KEY_MODULUS=$(openssl rsa -noout -modulus -in key.pem | openssl md5)

if [ "$CERT_MODULUS" = "$KEY_MODULUS" ]; then
  echo "Certificate and key match"
else
  echo "Certificate and key DO NOT match"
fi
```

---

## Next Steps

- [Registering in Azure](/docs/certificates/registering/) - Upload certificate to Azure
- [Configuring Profiles](/docs/certificates/configuring/) - Set up authentication
- [Certificate Storage](/docs/certificates/storage/) - Secure storage options
- [Security Best Practices](/docs/recipes/security-hardening/) - Production guidelines
