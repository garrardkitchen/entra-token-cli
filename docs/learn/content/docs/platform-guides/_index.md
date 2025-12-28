---
title: "Platform Guides"
description: "Platform-specific considerations and best practices"
weight: 70
---

# Platform Guides

Platform-specific configuration, security considerations, and best practices for Windows, macOS, and Linux.

---

## Platform Overview

Entra Token CLI works across all major platforms, but each has unique characteristics for secure storage and certificate management.

| Platform | Secure Storage | Security Level | Production Ready |
|----------|---------------|----------------|------------------|
| **Windows** | DPAPI | 🔒 Strong | ✅ Yes |
| **macOS** | Keychain | 🔒 Strong | ✅ Yes |
| **Linux** | XOR Obfuscation | ⚠️ Weak | ⚠️ Use alternatives |

---

## Platform-Specific Guides

### Windows

[**Windows Guide →**](/docs/platform-guides/windows/)

**Learn about:**
- DPAPI secure storage
- Windows Certificate Store integration
- PowerShell automation
- Installation methods

### macOS

[**macOS Guide →**](/docs/platform-guides/macos/)

**Learn about:**
- Keychain integration
- Certificate management
- Bash scripting patterns
- Homebrew installation

### Linux

[**Linux Guide →**](/docs/platform-guides/linux/)

**Learn about:**
- Secure storage limitations
- Alternative security approaches
- Package manager installation
- Container deployment

---

## Quick Start by Platform

### Windows

```powershell
# Install
dotnet tool install --global EntraTokenCli

# Create profile (uses DPAPI for secrets)
entratool config create

# Generate token
entratool get-token -p my-profile
```

### macOS

```bash {linenos=inline}
# Install
dotnet tool install --global EntraTokenCli

# Create profile (uses Keychain for secrets)
entratool config create

# Generate token
entratool get-token -p my-profile
```

### Linux

```bash {linenos=inline}
# Install
dotnet tool install --global EntraTokenCli

# Create profile (uses XOR obfuscation - consider alternatives)
entratool config create

# Generate token
entratool get-token -p my-profile

# For production, use environment variables or external secrets manager
```

---

## Security Recommendations

### Windows ✅
- Use built-in DPAPI secure storage
- Leverage Windows Certificate Store
- Production-ready out of the box

### macOS ✅
- Use built-in Keychain integration
- Leverage macOS Keychain for certificates
- Production-ready out of the box

### Linux ⚠️
- XOR obfuscation provides minimal security
- **Recommended alternatives:**
  - Environment variables
  - Azure Key Vault
  - HashiCorp Vault
  - Docker secrets
  - Kubernetes secrets

---

## Next Steps

- [Windows Guide](/docs/platform-guides/windows/)
- [macOS Guide](/docs/platform-guides/macos/)
- [Linux Guide](/docs/platform-guides/linux/)
- [Security Hardening](/docs/recipes/security-hardening/)
