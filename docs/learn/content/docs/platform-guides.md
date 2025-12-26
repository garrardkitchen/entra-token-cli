---
title: "Platform Guides"
description: "Platform-specific considerations and best practices"
weight: 70
---

# Platform Guides

Platform-specific configuration, security considerations, and best practices for Windows, macOS, and Linux.

---

## Overview

Entra Token CLI works across all major platforms, but each has unique characteristics for secure storage and certificate management.

| Platform | Secure Storage | Security Level | Production Ready |
|----------|---------------|----------------|------------------|
| **Windows** | DPAPI | 🔒 Strong | ✅ Yes |
| **macOS** | Keychain | 🔒 Strong | ✅ Yes |
| **Linux** | XOR Obfuscation | ⚠️ Weak | ⚠️ Use alternatives |

---

## Windows

### Installation

**Global Tool:**
```powershell
dotnet tool install --global EntraTokenCli
```

**Self-Contained Executable:**
```powershell
# Download from releases
curl -L -o entratool.exe https://github.com/.../entratool-win-x64.exe

# Add to PATH
$env:PATH += ";C:\path\to\entratool"
```

### Secure Storage

**Technology:** Data Protection API (DPAPI)

**Characteristics:**
- ✅ Strong encryption (AES-256)
- ✅ User-account bound
- ✅ No external dependencies
- ✅ Automatic key management

**Security Properties:**
- Secrets encrypted with user's Windows credentials
- Only same user on same machine can decrypt
- Administrators cannot decrypt other users' data
- Survives system restarts

**Configuration Location:**
```powershell
# Profiles
$env:USERPROFILE\.entratool\profiles.json

# Secure storage managed by Windows
```

### Certificate Management

**Windows Certificate Store:**
```powershell
# Import certificate
certutil -user -p YourPassword -importPFX cert.pfx

# List certificates
certutil -user -store My

# Reference in profile by thumbprint
```

**File-Based Certificates:**
```powershell
# Store with restricted permissions
$cert = "C:\Users\$env:USERNAME\.entratool\certs\cert.pfx"
icacls $cert /inheritance:r /grant:r "$env:USERNAME:F"
```

### PowerShell Integration

```powershell
function Get-EntraToken {
    param(
        [string]$Profile,
        [switch]$Silent
    )
    
    $args = @("get-token", "-p", $Profile)
    if ($Silent) { $args += "--silent" }
    
    $token = & entratool $args
    return $token
}

# Usage
$token = Get-EntraToken -Profile "my-profile" -Silent
$headers = @{"Authorization" = "Bearer $token"}

Invoke-RestMethod -Uri "https://graph.microsoft.com/v1.0/me" `
                  -Headers $headers
```

### Best Practices

✅ **Do:**
- Use DPAPI secure storage (default)
- Store certificates in user profile or Windows Certificate Store
- Use Group Policy for enterprise deployment
- Implement credential rotation policies

⚠️ **Avoid:**
- Storing secrets in environment variables
- Using plaintext configuration files
- Sharing user profiles between environments

[Detailed Windows guide →](/docs/platform-guides/windows/)

---

## macOS

### Installation

**Global Tool:**
```bash
dotnet tool install --global EntraTokenCli
```

**Self-Contained Executable:**

**Apple Silicon (M1/M2/M3):**
```bash
curl -L -o entratool https://github.com/.../entratool-osx-arm64
chmod +x entratool
sudo mv entratool /usr/local/bin/
```

**Intel:**
```bash
curl -L -o entratool https://github.com/.../entratool-osx-x64
chmod +x entratool
sudo mv entratool /usr/local/bin/
```

### Secure Storage

**Technology:** macOS Keychain

**Characteristics:**
- ✅ System-level encryption (AES-256)
- ✅ Integration with Touch ID/Face ID
- ✅ Per-item access control
- ✅ Audit logging

**Keychain Location:**
```bash
~/Library/Keychains/login.keychain-db
```

**Viewing Secrets:**
1. Open Keychain Access app: `/Applications/Utilities/Keychain Access.app`
2. Search for "entratool"
3. Double-click item
4. Check "Show password"
5. Authenticate with your password or biometrics

**Keychain Management:**
```bash
# List entratool entries
security find-generic-password -s entratool

# Export keychain (for backup)
security export -k login.keychain-db -t identities -o backup.p12

# Import keychain
security import backup.p12 -k login.keychain-db
```

### Certificate Management

**Import to Keychain:**
```bash
security import cert.pfx -k login.keychain -P YourPassword
```

**File-Based Certificates:**
```bash
# Store with restricted permissions
chmod 600 ~/Library/Application\ Support/entratool/certs/cert.pfx
```

### Shell Integration

```bash
# Add to ~/.zshrc or ~/.bash_profile
get_entra_token() {
  local profile=$1
  entratool get-token -p "$profile" --silent
}

# Usage
TOKEN=$(get_entra_token "my-profile")
curl -H "Authorization: Bearer $TOKEN" https://graph.microsoft.com/v1.0/me
```

### Best Practices

✅ **Do:**
- Use Keychain secure storage (default)
- Enable Touch ID for keychain access
- Use FileVault for full-disk encryption
- Back up keychain securely

⚠️ **Avoid:**
- Exporting keychain to unencrypted files
- Storing secrets in plaintext
- Sharing login keychain

[Detailed macOS guide →](/docs/platform-guides/macos/)

---

## Linux

### Installation

**Global Tool:**
```bash
dotnet tool install --global EntraTokenCli
```

**Self-Contained Executable:**
```bash
# Download (replace with appropriate architecture)
curl -L -o entratool https://github.com/.../entratool-linux-x64
chmod +x entratool
sudo mv entratool /usr/local/bin/
```

### ⚠️ Security Warning

{{% alert context="danger" %}}
**Linux storage uses XOR obfuscation - NOT secure for production!**

Secrets are stored in `~/.entratool/secrets.dat` using reversible XOR encoding. Anyone with file access can decode secrets easily.

**For production workloads, use:**
- Certificate authentication with restricted file permissions
- External secret managers (Azure Key Vault, HashiCorp Vault)
- Managed Identity on Azure VMs
- Environment variables (CI/CD)
{{% /alert %}}

### Storage Location

```bash
# Profiles (plaintext)
~/.entratool/profiles.json

# Secrets (XOR obfuscated - NOT secure)
~/.entratool/secrets.dat
```

**File Permissions:**
```bash
chmod 700 ~/.entratool
chmod 600 ~/.entratool/*
```

### Production Alternatives

#### 1. Certificate Authentication

```bash
# Store certificate with restricted permissions
chmod 600 ~/.entratool/certs/cert.pfx
chown $USER:$USER ~/.entratool/certs/cert.pfx

# Use certificate in profile
entratool config create
# Select: Certificate
# Path: /home/user/.entratool/certs/cert.pfx
```

#### 2. Azure Key Vault

```bash
#!/bin/bash
# Retrieve secret from Key Vault
SECRET=$(az keyvault secret show \
  --vault-name MyVault \
  --name EntraClientSecret \
  --query value -o tsv)

# Create profile without stored secret
cat > /tmp/profile.json <<EOF
{
  "name": "prod-profile",
  "clientId": "...",
  "tenantId": "...",
  "scope": "https://management.azure.com/.default",
  "useClientSecret": true
}
EOF

entratool config import -f /tmp/profile.json
rm /tmp/profile.json

# Use with runtime secret
entratool get-token -p prod-profile --client-secret "$SECRET"
```

#### 3. HashiCorp Vault

```bash
# Retrieve from Vault
SECRET=$(vault kv get -field=client_secret secret/entratool/prod)

# Use at runtime
entratool get-token -p prod-profile --client-secret "$SECRET"
```

#### 4. Environment Variables

```bash
# Set in environment
export AZURE_CLIENT_ID="..."
export AZURE_TENANT_ID="..."
export AZURE_CLIENT_SECRET="..."

# Create profile that reads from environment
entratool config create
# ... configure without storing secret
```

#### 5. Managed Identity (Azure VMs)

```bash
# On Azure VM with Managed Identity enabled
# No secrets needed!
entratool get-token -p managed-identity-profile -f ManagedIdentity
```

### Distribution-Specific Notes

**Ubuntu/Debian:**
```bash
# Install prerequisites
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0

# Install tool
dotnet tool install --global EntraTokenCli
```

**RHEL/CentOS/Fedora:**
```bash
# Install .NET SDK
sudo dnf install dotnet-sdk-8.0

# Install tool
dotnet tool install --global EntraTokenCli
```

**Alpine Linux:**
```bash
# Use self-contained executable (musl-based)
curl -L -o entratool https://github.com/.../entratool-linux-musl-x64
chmod +x entratool
mv entratool /usr/local/bin/
```

### Container Usage

**Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Install entratool
RUN dotnet tool install --global EntraTokenCli

# Add to PATH
ENV PATH="$PATH:/root/.dotnet/tools"

# Use in scripts
ENTRYPOINT ["/bin/bash"]
```

**Docker Compose with Secrets:**
```yaml
version: '3.8'
services:
  app:
    image: myapp
    environment:
      AZURE_CLIENT_ID: ${AZURE_CLIENT_ID}
      AZURE_TENANT_ID: ${AZURE_TENANT_ID}
    secrets:
      - azure_client_secret

secrets:
  azure_client_secret:
    external: true
```

### Best Practices

✅ **Do:**
- Use certificate authentication
- Store secrets in external secret managers
- Use Managed Identity on Azure VMs
- Set restrictive file permissions (600, 700)
- Use encrypted volumes
- Rotate credentials frequently
- Monitor secret access

⚠️ **Avoid:**
- Relying on built-in XOR storage for production
- Storing secrets in environment variables (visible in `ps`)
- Using root account unnecessarily
- Committing configuration files to git

[Detailed Linux guide →](/docs/platform-guides/linux/)

---

## Cross-Platform Considerations

### Profile Portability

**What's portable:**
- Profile configuration (plaintext)
- Client ID, Tenant ID, Scope
- Certificate paths (if relative)

**What's NOT portable:**
- Secrets (platform-specific storage)
- Absolute certificate paths
- Platform-specific configurations

### Sharing Profiles Across Platforms

**1. Export profile (without secrets):**
```bash
entratool config export -p my-profile -o profile.json
```

**2. Share `profile.json` via secure channel**

**3. Import on target platform:**
```bash
entratool config import -f profile.json
```

**4. Add secrets manually:**
```bash
entratool config edit -p my-profile
# Add client secret or certificate
```

### Platform Detection

The tool automatically detects the platform and uses appropriate secure storage.

---

## Cloud Platform Integration

### Azure VMs

**Use Managed Identity:**
```bash
# Enable Managed Identity on VM
az vm identity assign --name MyVM --resource-group MyRG

# No secrets needed
entratool get-token -p managed-identity -f ManagedIdentity
```

### AWS EC2

**Use secrets in AWS Secrets Manager:**
```bash
SECRET=$(aws secretsmanager get-secret-value \
  --secret-id prod/entratool/client-secret \
  --query SecretString -o text)

entratool get-token -p my-profile --client-secret "$SECRET"
```

### Google Cloud VMs

**Use Secret Manager:**
```bash
SECRET=$(gcloud secrets versions access latest --secret="entratool-secret")
entratool get-token -p my-profile --client-secret "$SECRET"
```

---

## Next Steps

- [Windows Detailed Guide](/docs/platform-guides/windows/)
- [macOS Detailed Guide](/docs/platform-guides/macos/)
- [Linux Detailed Guide](/docs/platform-guides/linux/)
- [Security Best Practices](/docs/recipes/security-hardening/)
- [Production Deployment](/docs/platform-guides/production/)
