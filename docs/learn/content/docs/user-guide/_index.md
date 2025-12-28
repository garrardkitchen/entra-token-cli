---
title: "User Guide"
description: "Complete guide to using Entra Auth Cli"
weight: 20
---

# User Guide

Comprehensive guides for day-to-day usage of Entra Auth Cli.

---

## Getting Started

If you're new to Entra Auth Cli, start here:

- [Installation](/docs/getting-started/installation/) - Install the tool on your platform
- [Quick Start](/docs/getting-started/quickstart/) - Generate your first token in 5 minutes
- [First Token Tutorial](/docs/getting-started/first-token/) - Step-by-step from Azure to token

---

## Core Tasks

### Managing Profiles

Authentication profiles store your configuration for connecting to Microsoft Entra ID.

[**Managing Profiles →**](/docs/user-guide/managing-profiles/)

**Learn how to:**
- Create new profiles with interactive prompts
- List and view all configured profiles
- Edit existing profile settings
- Delete profiles and associated secrets
- Export profiles for backup
- Import profiles from files
- Organize profiles by environment

### Generating Tokens

Request access tokens using different OAuth2 flows and configurations.

[**Generating Tokens →**](/docs/user-guide/generating-tokens/)

**Learn how to:**
- Generate tokens with client credentials flow
- Authenticate users with interactive browser
- Use device code for limited-input devices
- Override scopes at runtime
- Work with certificate authentication
- Handle different OAuth2 flows
- Automate token generation

### Working with Tokens

Inspect, validate, refresh, and use tokens effectively.

[**Working with Tokens →**](/docs/user-guide/working-with-tokens/)

**Learn how to:**
- Inspect JWT token claims
- Discover token information quickly
- Validate token expiration
- Refresh expired tokens
- Use tokens with Microsoft Graph
- Use tokens with Azure Management API
- Handle token expiration in scripts
- Store tokens securely

---

## By Scenario

### For Developers

**Building applications:**
- [Interactive user authentication](/docs/user-guide/generating-tokens/interactive-browser/)
- [Inspecting token claims](/docs/user-guide/working-with-tokens/inspecting/)
- [Testing different scopes](/docs/core-concepts/scopes/)

**API integration:**
- [Microsoft Graph API access](/docs/recipes/microsoft-graph/)
- [Azure Management API](/docs/recipes/azure-management/)
- [Custom API integration](/docs/recipes/custom-apis/)

### For DevOps Engineers

**Automation:**
- [Service principal setup](/docs/user-guide/managing-profiles/creating/)
- [Client credentials flow](/docs/user-guide/generating-tokens/client-credentials/)
- [CI/CD integration](/docs/recipes/cicd-integration/)

**Certificates:**
- [Certificate authentication](/docs/certificates/)
- [Certificate rotation](/docs/certificates/rotation/)
- [Secure certificate storage](/docs/certificates/storage/)

### For System Administrators

**Multi-environment management:**
- [Organizing profiles](/docs/user-guide/managing-profiles/#organizing-by-environment)
- [Profile export/import](/docs/user-guide/managing-profiles/exporting/)
- [Secret rotation](/docs/user-guide/managing-profiles/editing/)

**Security:**
- [Secure storage](/docs/core-concepts/secure-storage/)
- [Security hardening](/docs/recipes/security-hardening/)
- [Production deployment](/docs/platform-guides/production/)

---

## Common Workflows

### Daily Development

```bash {linenos=inline}
# Morning: Get fresh token
entra-auth-cli get-token -p dev-graph --silent > ~/.cache/token.txt

# Use throughout the day
TOKEN=$(cat ~/.cache/token.txt)
curl -H "Authorization: Bearer $TOKEN" https://graph.microsoft.com/v1.0/me
```

### Multi-Environment Testing

```bash {linenos=inline}
# Test in dev
entra-auth-cli get-token -p dev-api -f ClientCredentials

# Test in staging
entra-auth-cli get-token -p staging-api -f ClientCredentials

# Deploy to prod
entra-auth-cli get-token -p prod-api -f ClientCredentials
```

### Secret Rotation

```bash {linenos=inline}
# 1. Generate new secret in Azure Portal
# 2. Update profile
entra-auth-cli config edit -p my-service-principal
# Select: Client Secret
# Enter: new-secret-from-portal

# 3. Test
entra-auth-cli get-token -p my-service-principal

# 4. Delete old secret in Azure Portal
```

### Team Onboarding

```bash {linenos=inline}
# Team lead: Export profiles (without secrets)
entra-auth-cli config export -o team-profiles.json

# Share team-profiles.json via secure channel
# Share secrets via Azure Key Vault or password manager

# New team member: Import
entra-auth-cli config import -f team-profiles.json

# Add secrets
entra-auth-cli config edit -p profile1
# Add client secret from Key Vault
```

---

## Quick Command Reference

| Task | Command |
|------|---------|
| **Profiles** | |
| Create profile | `entra-auth-cli config create` |
| List profiles | `entra-auth-cli config list` |
| Edit profile | `entra-auth-cli config edit -p NAME` |
| Delete profile | `entra-auth-cli config delete -p NAME` |
| Export profile | `entra-auth-cli config export -p NAME -o FILE` |
| Import profile | `entra-auth-cli config import -f FILE` |
| **Tokens** | |
| Get token | `entra-auth-cli get-token -p PROFILE` |
| Override scope | `entra-auth-cli get-token -p PROFILE --scope "SCOPE"` |
| Specify flow | `entra-auth-cli get-token -p PROFILE -f FLOW` |
| Silent output | `entra-auth-cli get-token -p PROFILE --silent` |
| Save to file | `entra-auth-cli get-token -p PROFILE -o token.txt` |
| Refresh token | `entra-auth-cli refresh -p PROFILE` |
| **Token Info** | |
| Inspect token | `entra-auth-cli inspect -t TOKEN` |
| Discover info | `entra-auth-cli discover -t TOKEN` |
| Check expiration | `entra-auth-cli discover -t TOKEN` |

---

## Advanced Topics

### OAuth2 Flows

Understanding when to use each flow:

- [Client Credentials](/docs/oauth-flows/client-credentials/) - Service-to-service
- [Authorization Code](/docs/oauth-flows/authorization-code/) - Web applications
- [Device Code](/docs/oauth-flows/device-code/) - Limited-input devices
- [Interactive Browser](/docs/oauth-flows/interactive-browser/) - Desktop apps

### Certificate Authentication

Using certificates for enhanced security:

- [Getting Started with Certificates](/docs/certificates/getting-started/)
- [Creating Certificates](/docs/certificates/creation/)
- [Certificate Storage](/docs/certificates/storage/)
- [Certificate Rotation](/docs/certificates/rotation/)

### Platform-Specific Guides

Platform considerations and best practices:

- [Windows Guide](/docs/platform-guides/windows/)
- [macOS Guide](/docs/platform-guides/macos/)
- [Linux Guide](/docs/platform-guides/linux/) - ⚠️ Security warnings

---

## Troubleshooting

Common issues and solutions:

- [Profile Issues](/docs/troubleshooting/#profile-issues)
- [Authentication Failures](/docs/troubleshooting/#authentication-failures)
- [Token Problems](/docs/troubleshooting/#token-problems)
- [Permission Errors](/docs/troubleshooting/#permission-errors)
- [Certificate Issues](/docs/troubleshooting/#certificate-issues)

---

## Next Steps

### Learn Core Concepts

- [Authentication Profiles](/docs/core-concepts/profiles/)
- [OAuth2 Flows](/docs/core-concepts/oauth2-flows/)
- [Scopes & Permissions](/docs/core-concepts/scopes/)
- [Secure Storage](/docs/core-concepts/secure-storage/)

### Explore Recipes

- [Microsoft Graph Integration](/docs/recipes/microsoft-graph/)
- [Azure Management](/docs/recipes/azure-management/)
- [CI/CD Integration](/docs/recipes/cicd-integration/)
- [Security Hardening](/docs/recipes/security-hardening/)

### Reference

- [Command Reference](/docs/reference/)
- [Configuration Reference](/docs/reference/configuration/)
- [API Reference](/docs/reference/api/)
