---
title: "Overview"
description: "Introduction to certificate authentication"
weight: 1
---

# Certificate Authentication Overview

X.509 certificates provide a more secure alternative to client secrets for authenticating service principals with Microsoft Entra ID.

---

## Advantages Over Client Secrets

| Feature | Client Secret | Certificate |
|---------|--------------|-------------|
| **Security** | Medium | High |
| **Validity Period** | Up to 2 years | Up to 3 years |
| **Rotation** | Manual | Automated possible |
| **Compromise Risk** | Higher | Lower |
| **Audit Trail** | Limited | Better |
| **Storage** | Text-based | PKI infrastructure |
| **Revocation** | Immediate | Requires CRL/OCSP |

---

## When to Use Certificates

### ✅ Use Certificates For:

**Production Service Principals**
- Long-running automated services
- High-security requirements
- Compliance requirements (SOC 2, HIPAA)
- Certificate-based infrastructure

**Enterprise Deployments**
- Organizations with PKI infrastructure
- Centralized certificate management
- Hardware security module (HSM) integration
- Certificate lifecycle management

**Regulated Industries**
- Financial services
- Healthcare (HIPAA)
- Government
- Critical infrastructure

### ⚠️ Use Client Secrets For:

**Development and Testing**
- Quick prototyping
- Local development
- Short-lived projects
- POC implementations

**Simple Automation**
- Personal scripts
- Internal tools
- Non-production workloads
- Limited scope applications

---

## Certificate Types

### PFX/PKCS12 Format

**Most common format for certificates with private keys**

```bash
# File extensions
certificate.pfx
certificate.p12
```

**Contains:**
- X.509 certificate (public key)
- Private key (encrypted)
- Optional: Certificate chain
- Password protected

**Supported by:**
- Windows Certificate Store
- macOS Keychain
- File-based storage
- Most PKI systems

### PEM Format

**Text-based format (not directly supported)**

```bash
# Separate files
certificate.pem  # Public key
private-key.pem  # Private key
```

**To use with Entra Token CLI:**
Convert to PFX format:

```bash
openssl pkcs12 -export \
  -in certificate.pem \
  -inkey private-key.pem \
  -out certificate.pfx \
  -password pass:YourPassword
```

### Certificate vs Thumbprint

**File-Based (Recommended)**
- Store PFX file on disk
- Portable across systems
- Easier backup and restore
- Better for CI/CD

**Thumbprint-Based (Windows/macOS)**
- Reference certificate in system store
- Requires certificate import
- Better integration with OS
- Automatic key protection

---

## Security Benefits

### 1. Stronger Cryptography

Certificates use asymmetric encryption (RSA 2048/4096-bit):
- Private key never leaves your system
- Only public key uploaded to Azure
- Cannot be guessed or brute-forced

### 2. Better Audit Trail

Certificate operations are more traceable:
- Certificate serial numbers
- Issuer information
- Validity periods
- Usage timestamps

### 3. Reduced Compromise Risk

If certificate is compromised:
- Can be revoked via CRL/OCSP
- Clear expiration dates
- Hardware protection possible (HSM)
- Less likely to be accidentally exposed

### 4. Compliance-Friendly

Certificates align with security standards:
- SOC 2 requirements
- HIPAA compliance
- PCI-DSS standards
- ISO 27001 controls

---

## How Certificate Authentication Works

### Flow Diagram

```
1. Client → Generates JWT assertion signed with private key
2. Client → Sends assertion to Azure AD
3. Azure AD → Verifies signature using public key from app registration
4. Azure AD → Issues access token
5. Client → Uses access token to call API
```

### Detailed Steps

**1. Certificate Configuration**
- Private key stored securely on client
- Public key uploaded to Azure app registration
- Certificate thumbprint recorded

**2. Token Request**
- Client creates JWT assertion
- Signs assertion with private key
- Includes certificate thumbprint in header

**3. Azure Verification**
- Azure retrieves public key by thumbprint
- Verifies JWT signature
- Checks certificate validity
- Validates app permissions

**4. Token Issuance**
- Azure issues access token
- Token contains claims and scopes
- Valid for 1 hour (default)

---

## Certificate Lifecycle

### 1. Creation
- Generate key pair (public + private)
- Create certificate signing request (CSR)
- Sign certificate (self-signed or CA)

### 2. Registration
- Upload public key to Azure app registration
- Record certificate thumbprint
- Configure permissions

### 3. Usage
- Store private key securely
- Configure application profiles
- Generate access tokens

### 4. Rotation
- Generate new certificate
- Upload to Azure (keep old active)
- Update applications
- Remove old certificate

### 5. Revocation
- Revoke if compromised
- Update CRL/OCSP
- Replace with new certificate

---

## Comparison with Other Auth Methods

### Certificate vs Client Secret

| Aspect | Certificate | Client Secret |
|--------|-------------|---------------|
| Setup Complexity | Higher | Lower |
| Security | Stronger | Weaker |
| Rotation | More complex | Simpler |
| Best For | Production | Development |

### Certificate vs Managed Identity

| Aspect | Certificate | Managed Identity |
|--------|-------------|------------------|
| Azure-Only | No | Yes |
| Setup | Manual | Automatic |
| Management | Self-managed | Azure-managed |
| Best For | Hybrid/On-prem | Azure resources |

### Certificate vs Interactive Auth

| Aspect | Certificate | Interactive Auth |
|--------|-------------|------------------|
| User Interaction | None | Required |
| Automation | Yes | No |
| Delegated Permissions | No | Yes |
| Best For | Services | User applications |

---

## Next Steps

- [Creating Certificates](/docs/certificates/creating/) - Generate self-signed or CA certificates
- [Registering in Azure](/docs/certificates/registering/) - Upload to app registrations
- [Configuring Profiles](/docs/certificates/configuring/) - Set up certificate authentication
- [Certificate Storage](/docs/certificates/storage/) - Secure storage options
