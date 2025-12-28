---
title: "Entra Auth CLI"
description: "Cross-platform CLI for generating Microsoft Entra ID access tokens"
---

# Entra Auth CLI

A powerful .NET global tool for generating Microsoft Entra ID access tokens with multiple OAuth2 flows, secure storage, and certificate authentication.

[Get Started](/docs/getting-started/installation/) [Explore Features](/docs/recipes/) [GitHub](https://github.com/garrardkitchen/entra-token-cli)

## Key Features

🔑
### Multiple OAuth2 Flows

Support for Client Credentials, Authorization Code, Device Code, and Interactive Browser flows. Automatically selects the right flow for your scenario.

[Learn more →](/docs/oauth-flows/)

🔒
### Secure Storage

Platform-native encryption using Windows DPAPI and macOS Keychain. Your secrets are stored securely using OS-level protection mechanisms.

[Learn more →](/docs/core-concepts/secure-storage/)

📜
### Certificate Authentication

Support for .pfx certificates with flexible password handling, caching, and Windows Certificate Store integration for enterprise scenarios.

[Learn more →](/docs/certificates/)

👤
### Profile Management

Save and manage multiple authentication profiles. Import/export configurations for team collaboration and multi-environment setups.

[Learn more →](/docs/core-concepts/profiles/)

🎯
### Flexible Scopes

Configure API scopes in profiles or override at runtime. Support for Microsoft Graph, Azure Management, and custom APIs with scope validation.

[Learn more →](/docs/core-concepts/scopes/)

🔄
### Token Management

Automatic token caching with refresh support. Inspect, validate, and manage token lifecycle efficiently with built-in JWT decoding.

[Learn more →](/docs/user-guide/working-with-tokens/)

---

## Why Entra Auth Cli?

🚀
### Fast & Efficient

Built with .NET 10 for optimal performance with minimal memory footprint. Generate tokens in milliseconds.

🔧
### Developer-First

Simple commands, rich terminal output powered by Spectre.Console, and comprehensive error messages for quick debugging.

🌐
### Cross-Platform

Full support for Windows (10+) and macOS (10.15+) with production-ready security. Linux support for development scenarios.

---

## Quick Start

1
### Install .NET Global Tool

Download and install the [.NET Runtime 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) or later, then install the tool globally.

```bash
dotnet tool install -g EntraTokenCli
```

2
### Create Your First Profile

Create an authentication profile interactively with guided prompts for all required settings.

```bash
entra-auth-cli config create
```

3
### Generate a Token

Generate an access token using your saved profile with automatic flow selection and secure storage.

```bash
entra-auth-cli get-token -p myprofile
```

4
### Inspect & Use

Decode the JWT token to view claims, expiration, and permissions, or use it in your API calls.

```bash
entra-auth-cli inspect -t "your-token-here"
```

---

## Top 5 Features

1
### Automatic Flow Selection

Smart detection of the best OAuth2 flow based on your profile configuration—no manual flow selection needed for most scenarios.

2
### Certificate Support

Enterprise-grade certificate authentication with password caching, Windows Certificate Store integration, and automatic thumbprint lookup.

3
### Secure Secrets

Built-in platform-native encryption for client secrets, passwords, and tokens using Windows DPAPI or macOS Keychain with automatic key rotation.

4
### Profile Import/Export

Share authentication configurations across teams with JSON export/import supporting multiple profiles and encrypted secrets.

5
### Token Inspection

Decode JWT tokens to view claims, expiration, audience, issuer, and permissions with formatted JSON output and expiration warnings.

---

## Ready to Generate Tokens?

Get started with Entra Auth Cli or explore specific features and recipes.

[Getting Started Guide](/docs/getting-started/installation/) [Browse Recipes](/docs/recipes/)
