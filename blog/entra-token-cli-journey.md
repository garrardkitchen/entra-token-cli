---
title: "Building entra-auth-cli: A Secure CLI for Microsoft Entra ID Token Management"
date: 2025-12-18T10:00:00Z
draft: false
author: "Kit Cheng"
tags: ["entra-id", "azure", "security", "oauth2", "dotnet", "cli", "devops", "ai-development"]
categories: ["Development", "Security", "DevOps"]
summary: "How I built a cross-platform CLI tool in 1 hour to eliminate the frustration and security risks of manually managing Entra ID access tokens across multiple environments—and why you should never commit another secret to your repo."
featured_image: "/images/entra-token-cli/hero.png"
---

## The Problem: Death by a Thousand Manual Token Requests

Picture this: You're building a modern cloud application with proper security architecture. You've followed the principle of least privilege, creating dedicated app registrations for each API and client application. Your service principals are perfectly scoped with granular permissions. You're doing everything right.

**Then reality hits.**

Every time you need to test an API, debug an integration, or verify permissions, you're stuck in a tedious workflow:
- Hunt down the tenant ID (again)
- Find the client ID in the Azure portal
- Copy that client secret you saved... somewhere
- Craft the OAuth2 token request manually or fiddle with Postman
- Repeat this 5-10 times a day across dev, staging, and production environments

**But the real nightmare? The security risk.**

After creating my third C# console app to generate tokens (the last one being a .NET 10 file-based utility), I realized I was one `git commit` away from disaster. How long before I accidentally pushed a client secret into a repo? How many developers have made that mistake, triggering emergency credential rotations at 2 AM?

**There had to be a better way.**

## The Solution: entra-auth-cli - Secure Token Management Made Simple

I decided to build `entra-auth-cli`, a cross-platform CLI that eliminates both the frustration and the security risk. The elevator pitch:

> **Stop manually hunting for credentials across Azure portals. Stop risking accidental secret commits. Store your profiles once with platform-native encryption, and generate tokens with a single command—no matter which OS you're on.**

The goal was simple: **secure by default, fast by design, natural to use.**

## The Journey: From Problem to Solution in 1 Hour

Here's where it gets interesting—I built the entire initial version in just **1 hour** using Claude Sonnet 4.5 and GitHub Copilot in VS Code.

### AI-Assisted Development Done Right

Rather than writing boilerplate from scratch, I focused on:
- **Architecture decisions**: OAuth2 flows, secure storage patterns, command structure
- **UX design**: Making the CLI feel natural and intuitive
- **Platform integration**: macOS Keychain, Windows DPAPI, Linux fallbacks

The AI pair programming handled:
- Complex OAuth2 flow implementations with MSAL
- Certificate authentication edge cases
- Platform-specific secure storage integrations (especially the tricky macOS Keychain CLI interactions)
- Rich console UI with Spectre.Console

**The biggest challenge?** Getting the UX right. The tool needed to feel natural—profiles should be easy to create, secrets should be transparently handled, and common workflows should "just work" without reading documentation.

## Technical Deep Dive: How It Works

### Understanding Service Principals and API Permissions

When you create an **app registration** in Microsoft Entra ID (formerly Azure AD), you're defining an application identity. But here's the key concept many developers miss:

**An app registration is just a blueprint.** When your app registration gets permissions to access a resource (like Microsoft Graph or your custom API), Azure automatically creates a **service principal** in your tenant.

```
App Registration (Blueprint)
     ↓ grants permission to
Resource API (e.g., Microsoft Graph)
     ↓ automatically creates
Service Principal (Runtime Identity)
```

The service principal is what actually authenticates and gets tokens. This is why:
- You create app registrations in one place
- But service principals exist per tenant
- Multi-tenant apps have one app registration but many service principals

**entra-auth-cli manages both sides:** It stores your client credentials securely and handles the OAuth2 flows to get tokens from those service principals.

### Four OAuth2 Flows, One Command

EntratTool supports all the major OAuth2 flows:

**1. Client Credentials** (Service-to-Service)
```bash
entra-auth-cli get-token -p myapi
```
Perfect for service principals, API access, automation. No user interaction needed.

**2. Authorization Code with PKCE** (User-Interactive)
```bash
entra-auth-cli get-token -p myapp -f AuthorizationCode
```
Opens a browser, user logs in, token is cached. Great for delegated permissions.

**3. Device Code** (Headless/SSH)
```bash
entra-auth-cli get-token -p myapp -f DeviceCode
```
Displays a code to enter on another device. Perfect for SSH sessions or containers.

**4. Interactive Browser** (Local Development)
```bash
entra-auth-cli get-token -p myapp -f InteractiveBrowser
```
Full browser flow with automatic localhost callback handling.

### Secure Storage: Platform-Native Encryption

This is where security meets usability. Secrets are **never stored in plain text**:

| Platform | Storage Method | Location |
|----------|---------------|----------|
| **Windows** | DPAPI (Data Protection API) | `%APPDATA%\entra-auth-cli\secure\` |
| **macOS** | System Keychain | Keychain Access (search "entra-auth-cli") |
| **Linux** | XOR Obfuscation* | `~/.config/entra-auth-cli/secure/` |

*Linux fallback is weak; proper libsecret integration planned for future versions.

The profile metadata (tenant IDs, client IDs, scopes) is stored separately in `profiles.json`, but **zero secrets touch that file**.

```json
// profiles.json - safe to commit (no secrets!)
{
  "name": "production-api",
  "tenantId": "f1a8cfe1-...",
  "clientId": "2d1b44f8-...",
  "scopes": ["api://myapi/.default"],
  "authMethod": "ClientSecret"
}
```

The actual client secret? Encrypted in the OS keychain, accessible only to your user account.

### Smart Flow Inference

One of my favorite UX touches: **you don't have to specify the OAuth2 flow every time.**

```csharp
// In GetTokenCommand.cs
OAuth2Flow flow;
if (profile.AuthMethod == AuthenticationMethod.ClientSecret)
    flow = OAuth2Flow.ClientCredentials;  // Service principal? Use client credentials
else if (profile.AuthMethod == AuthenticationMethod.Certificate)
    flow = OAuth2Flow.ClientCredentials;  // Certificate auth? Same deal
else
    flow = OAuth2Flow.InteractiveBrowser;  // Otherwise, interactive browser
```

**Result:** Just type `entra-auth-cli get-token -p myprofile` and the tool picks the right flow based on your authentication method. No mental overhead.

## Key Features That Save Time and Prevent Disasters

### 1. Profile Management
Create once, use everywhere:
```bash
# Interactive profile creation
entra-auth-cli config create

# List all profiles
entra-auth-cli config list

# Edit existing profiles
entra-auth-cli config edit -p production-api
```

### 2. Team Sharing (Without Exposing Secrets)
Export profiles with encryption for team distribution:
```bash
# Export with secrets (AES-256 + PBKDF2)
entra-auth-cli config export -p myprofile --include-secrets -o team-profile.enc

# Team member imports
entra-auth-cli config import -i team-profile.enc
```

### 3. Token Inspection
Decode JWTs instantly:
```bash
entra-auth-cli inspect eyJ0eXAiOiJKV1Qi...
```
Shows all claims, expiration, issuer—perfect for debugging permission issues.

### 4. Clipboard Integration
Tokens auto-copy to clipboard (with smart fallback to file in headless environments):
```bash
entra-auth-cli get-token -p myapi
# Token in clipboard, ready to paste into Postman/curl
```

### 5. Certificate Authentication
Full support for .pfx certificates with three password strategies:
- **Always prompt** (maximum security)
- **Cached password** (convenience)
- **Passwordless certificates** (when possible)

```bash
entra-auth-cli get-token -p cert-app --cache-cert-password
```

### 6. App Registration Discovery
Search your tenant for app registrations without leaving the terminal:
```bash
entra-auth-cli discover -t contoso.onmicrosoft.com -s "MyApp*"
```
Uses Microsoft Graph API with keyboard search filtering and colored output.

## Best Practices and Architecture Decisions

### 1. Separation of Concerns
- **Configuration layer** (`ConfigService`) handles profile CRUD
- **Authentication layer** (`MsalAuthService`) handles OAuth2 flows
- **Storage layer** (`ISecureStorage`) abstracts platform differences
- **UI layer** (`ConsoleUi`) handles all Spectre.Console interactions

### 2. Immutable Data Models
Using C# record types for profiles:
```csharp
public record AuthProfile
{
    public string Name { get; init; }
    public string TenantId { get; init; }
    public OAuth2Flow? DefaultFlow { get; init; }
    // ... with expressions for updates
}

var updated = profile with { TenantId = newTenantId };
```

### 3. Explicit Over Implicit
- Token refresh is a separate command (`refresh`), not automatic
- Certificate password caching requires explicit flag (`--cache-cert-password`)
- Token expiration warnings enabled by default but configurable

### 4. Platform Detection at Runtime
```csharp
public static ISecureStorage Create()
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        return new WindowsSecureStorage();
    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        return new MacOsSecureStorage();
    return new LinuxSecureStorage();  // fallback
}
```

### 5. Async All the Way Down
Every I/O operation is async, keeping the CLI responsive even during network calls or certificate loading.

## Real-World Impact: Time Savings and Security Wins

### Time Savings
**Before entra-auth-cli:**
- Hunt for credentials in Azure Portal: **2-3 minutes**
- Craft token request manually: **1-2 minutes**
- Copy/paste credentials: **30 seconds**
- **Total: 4-5 minutes per token request**

**With entra-auth-cli:**
- `entra-auth-cli get-token -p myapi`: **3 seconds**

**For a developer making 10 token requests per day:**
- Daily savings: ~40 minutes
- Monthly savings: **~13 hours** (conservative estimate) 😬
- **Plus:** Zero risk of committing secrets

### Security Wins
✅ **No secrets in code** - Everything in OS-native secure storage
✅ **No secrets in version control** - Profiles contain metadata only
✅ **Encrypted team sharing** - AES-256 profile exports
✅ **Token caching** - Reduces authentication frequency
✅ **Audit trail** - Keychain entries visible for compliance

## Who Should Use This?

**DevOps Engineers:**
- Managing multiple environments (dev/staging/prod)
- Automating deployments with service principals
- Testing infrastructure-as-code (Terraform, Bicep)

**Backend Developers:**
- Testing APIs locally with proper authentication
- Debugging permission issues with token inspection
- Working with Microsoft Graph API

**Cloud Architects:**
- Demonstrating authentication flows
- Prototyping security architectures
- Managing multiple tenant configurations

**Security Teams:**
- Auditing service principal access
- Rotating credentials without disruption
- Ensuring least-privilege implementations

## The Tech Stack

- **.NET 10.0** - Latest framework with modern C# features
- **Spectre.Console** - Rich, colored console UI
- **MSAL.NET** - Official Microsoft authentication library
- **Microsoft.Graph SDK** - For app registration discovery
- **TextCopy** - Cross-platform clipboard integration
- **System.Security.Cryptography** - DPAPI and encryption

## Get Started in 30 Seconds

```bash
# Install globally
dotnet tool install -g EntraAuthCli

# Or download executables from releases
# https://github.com/garrardkitchen/entra-token-cli/releases

# Create your first profile
entra-auth-cli config create

# Get a token
entra-auth-cli get-token
```

## What's Next?

**Planned features:**
- Proper libsecret integration for Linux (replacing XOR obfuscation). This is currently only used with Linux and as a fallback mechanism
- Managed identity support for Azure VMs
- Shell completion scripts (bash, zsh, PowerShell)
- Automated testing suite
- Token validation with signature verification

## Lessons Learned

### 1. AI Accelerates, Humans Guide
Claude and Copilot handled the heavy lifting, but the **architecture, UX decisions, and platform nuances** required human judgment. AI is incredible for implementation, but you still need to know what you want to build.

### 2. Security Should Be Invisible
The best security UX is when users don't think about security. By using platform-native storage and making secrets transparent, users get security without friction.

### 3. CLI Tools Need Love Too
Good CLI UX means:
- Colored output for scannability
- Interactive prompts with sensible defaults
- Smart inference (like automatic flow selection)
- Comprehensive `--help` text
- Consistent command structure

### 4. Cross-Platform Is Hard
What works on macOS doesn't work on Windows. Testing on all three platforms revealed issues that wouldn't surface otherwise (looking at you, Keychain CLI quirks).

## Conclusion: Stop Committing Secrets, Start Using entra-auth-cli

I built `entra-auth-cli` because I was tired of:
- **The tedium** of manually generating tokens
- **The anxiety** of potentially committing secrets
- **The friction** of context-switching to the Azure Portal
- **The inconsistency** across different operating systems

If you've ever:
- ✅ Accidentally committed a secret (or feared doing so)
- ✅ Wasted time hunting for client IDs in Azure Portal
- ✅ Struggled with OAuth2 flows in Postman
- ✅ Needed tokens across multiple environments
- ✅ Wished for secure credential storage that "just works"

**Then `entra-auth-cli` is for you.**

It took 1 hour to build with AI assistance, but it will save you **hours every month** and eliminate a major security risk from your workflow.

## Resources

- **GitHub**: [github.com/garrardkitchen/entra-token-cli](https://github.com/garrardkitchen/entra-token-cli)
- **Documentation**: See README.md for full documentation
- **Issues/Feedback**: GitHub Issues welcome!
- **License**: MIT

---

**Have you struggled with token management or accidentally committed secrets?** Share your story in the comments below. Let's build better, more secure developer experiences together.

*Built with ☕ and Claude Sonnet 4.5 in 1 hour on December 18, 2025.*
