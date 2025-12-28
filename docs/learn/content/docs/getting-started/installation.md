---
title: "Installation"
description: "How to install Entra Auth Cli on your platform"
weight: 10
---

# Installation

Entra Auth Cli can be installed as a .NET global tool or downloaded as a self-contained executable for your platform.

---

## Option 1: .NET Global Tool (Recommended)

**Prerequisites**: .NET Runtime 10.0 or later

### Install

```bash {linenos=inline}
dotnet tool install -g EntraAuthCli
```

### Update

```bash {linenos=inline}
dotnet tool update -g EntraAuthCli
```

### Uninstall

```bash {linenos=inline}
dotnet tool uninstall -g EntraAuthCli
```

### Verify Installation

```bash {linenos=inline}
entra-auth-cli --version
```

---

## Option 2: Self-Contained Executables

Download the latest release for your platform from the [Releases page](https://github.com/garrardkitchen/entra-token-cli/releases).

### Windows

1. Download `entra-auth-cli-win-x64.exe`
2. Place in a directory in your PATH (e.g., `C:\Tools\`)
3. Run from command prompt:

```cmd
entra-auth-cli --version
```

### macOS (Apple Silicon)

1. Download `entra-auth-cli-osx-arm64`
2. Make executable and move to PATH:

```bash {linenos=inline}
chmod +x entra-auth-cli-osx-arm64
sudo mv entra-auth-cli-osx-arm64 /usr/local/bin/entra-auth-cli
```

3. Verify:

```bash {linenos=inline}
entra-auth-cli --version
```

### macOS (Intel)

1. Download `entra-auth-cli-osx-x64`
2. Make executable and move to PATH:

```bash {linenos=inline}
chmod +x entra-auth-cli-osx-x64
sudo mv entra-auth-cli-osx-x64 /usr/local/bin/entra-auth-cli
```

3. Verify:

```bash {linenos=inline}
entra-auth-cli --version
```

### Linux

1. Download `entra-auth-cli-linux-x64`
2. Make executable and move to PATH:

```bash {linenos=inline}
chmod +x entra-auth-cli-linux-x64
sudo mv entra-auth-cli-linux-x64 /usr/local/bin/entra-auth-cli
```

3. Verify:

```bash {linenos=inline}
entra-auth-cli --version
```

> **⚠️ Linux Security Note**: Linux uses XOR obfuscation for secret storage, not cryptographic encryption. Suitable for development only. See [Platform-Specific Guides](/docs/platform-guides/linux/) for production alternatives.

---

## Platform Requirements

### Windows
- Windows 10+ (build 1607+)
- Secure storage via DPAPI

### macOS
- macOS 10.15+ (Catalina or later)
- Secure storage via Keychain

### Linux
- Ubuntu 20.04+, Fedora 35+, or compatible distributions
- XOR obfuscation (not cryptographically secure)

---

## Next Steps

- **[Quick Start Guide →](/docs/getting-started/quickstart/)** - Generate your first token
- **[Complete Tutorial →](/docs/getting-started/first-token/)** - Full walkthrough
- **[Core Concepts →](/docs/core-concepts/)** - Understand how it works
