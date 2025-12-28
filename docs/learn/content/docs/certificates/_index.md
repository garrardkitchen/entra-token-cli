---
title: "Certificate Authentication"
description: "Using certificates for secure authentication"
weight: 55
---

# Certificate Authentication

Learn how to use X.509 certificates for secure, passwordless authentication with Microsoft Entra ID.

---

## Why Use Certificates?

Certificates provide stronger security than client secrets and are recommended for production deployments.

[**Overview →**](/docs/certificates/overview/)

**Learn about:**
- Advantages over client secrets
- When to use certificates vs secrets
- Certificate types and formats
- Security benefits

### Getting Started

Step-by-step guides for certificate authentication.

[**Creating Certificates →**](/docs/certificates/creating/)

**Learn how to:**
- Generate self-signed certificates for testing
- Request CA-signed certificates for production
- Convert certificate formats (PEM to PFX)
- Create certificates on Windows, macOS, and Linux

[**Registering in Azure →**](/docs/certificates/registering/)

**Learn how to:**
- Upload certificates to app registrations
- Extract public keys from PFX files
- Verify certificate thumbprints
- Manage certificates in Azure Portal

[**Configuring Profiles →**](/docs/certificates/configuring/)

**Learn how to:**
- Create profiles with certificate authentication
- Configure certificate paths
- Store certificate passwords securely
- Update existing profiles to use certificates

### Certificate Management

Advanced topics for managing certificates in production.

[**Certificate Storage →**](/docs/certificates/storage/)

**Learn how to:**
- Store certificates securely on the filesystem
- Use Windows Certificate Store
- Use macOS Keychain
- Set proper file permissions

[**Using Certificates →**](/docs/certificates/using/)

**Learn how to:**
- Generate tokens with certificate profiles
- Understand certificate password caching
- Troubleshoot common issues
- Monitor certificate usage

[**Certificate Rotation →**](/docs/certificates/rotation/)

**Learn how to:**
- Plan certificate rotation schedules
- Rotate certificates without downtime
- Automate rotation processes
- Test new certificates safely

[**Validation & Troubleshooting →**](/docs/certificates/validation/)

**Learn how to:**
- Check certificate expiration dates
- Verify certificate thumbprints
- Diagnose certificate errors
- Fix common problems

---

## Quick Start

```bash {linenos=inline}
# 1. Create a self-signed certificate (development only)
openssl req -x509 -newkey rsa:4096 \
  -keyout key.pem -out cert.pem \
  -days 730 -nodes \
  -subj "/CN=MyApp"

# 2. Convert to PFX
openssl pkcs12 -export \
  -in cert.pem -inkey key.pem \
  -out certificate.pfx \
  -password pass:YourPassword

# 3. Upload public key to Azure Portal
# Extract public key:
openssl pkcs12 -in certificate.pfx -nokeys -out certificate.cer

# 4. Create profile with certificate
entratool config create
# Select: Certificate
# Path: /path/to/certificate.pfx
# Password: ****

# 5. Generate token
entratool get-token -p cert-profile
```

---

## Security Best Practices

### ✅ Do

- Use certificates for production service principals
- Store certificates with restrictive file permissions (600)
- Rotate certificates annually or per policy
- Use CA-signed certificates for production
- Monitor certificate expiration dates
- Use separate certificates for dev/staging/prod

### ❌ Don't

- Store certificates in git repositories
- Share certificates via email/Slack
- Use weak certificate passwords
- Ignore certificate expiration warnings
- Reuse certificates across environments

---

## Next Steps

- [Creating Certificates](/docs/certificates/creating/)
- [Configuring Profiles](/docs/certificates/configuring/)
- [Security Hardening](/docs/recipes/security-hardening/)
- [Platform Guides](/docs/platform-guides/)
