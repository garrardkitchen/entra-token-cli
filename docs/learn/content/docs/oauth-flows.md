---
title: "OAuth2 Flows"
description: "Detailed guides for each OAuth2 authentication flow"
weight: 50
---

# OAuth2 Flows

Comprehensive guides for each supported OAuth2 authentication flow.

---

## Overview

Entra Token CLI supports four OAuth2 authentication flows, each designed for specific scenarios:

| Flow | Best For | User Interaction |
|------|----------|------------------|
| [Client Credentials](/docs/oauth-flows/client-credentials/) | Service-to-service | None |
| [Authorization Code](/docs/oauth-flows/authorization-code/) | Web applications | Required |
| [Device Code](/docs/oauth-flows/device-code/) | Limited-input devices | Required (on another device) |
| [Interactive Browser](/docs/oauth-flows/interactive-browser/) | Desktop applications | Required |

---

## Quick Overview

For a quick overview of all flows, see the [OAuth2 Flows Core Concept](/docs/core-concepts/oauth2-flows/) page.

---

## Detailed Guides

### Client Credentials Flow

**Use for:** Automated services, daemons, CI/CD pipelines

[Read the detailed guide →](/docs/oauth-flows/client-credentials/)

**Quick example:**
```bash
entratool get-token -p service-principal -f ClientCredentials
```

---

### Authorization Code Flow

**Use for:** Web applications with user sign-in

[Read the detailed guide →](/docs/oauth-flows/authorization-code/)

**Quick example:**
```bash
entratool get-token -p webapp -f AuthorizationCode
```

---

### Device Code Flow

**Use for:** Headless devices, IoT, SSH sessions

[Read the detailed guide →](/docs/oauth-flows/device-code/)

**Quick example:**
```bash
entratool get-token -p iot-device -f DeviceCode
```

---

### Interactive Browser Flow

**Use for:** Desktop applications, CLI tools with user authentication

[Read the detailed guide →](/docs/oauth-flows/interactive-browser/)

**Quick example:**
```bash
entratool get-token -p desktop-app -f InteractiveBrowser
```

---

## Flow Selection

### Automatic Inference

The tool automatically selects the appropriate flow based on your profile configuration:

- **Client Secret or Certificate** configured → Client Credentials flow
- **No client secret/certificate** → Interactive Browser flow

### Manual Override

Override the automatic selection:

```bash
entratool get-token -p myprofile -f DeviceCode
```

### Setting Default Flow in Profile

Configure a default flow when creating or editing a profile:

```bash
entratool config create
# ... other prompts ...
Set default OAuth2 flow? y
Default OAuth2 flow: ClientCredentials
```

---

## Comparing Flows

### By Security Level

1. **Client Credentials with Certificate** - Highest security
2. **Authorization Code** - High security (user context)
3. **Interactive Browser** - Medium-high security
4. **Device Code** - Medium security

### By User Experience

1. **Client Credentials** - No user interaction (best for automation)
2. **Interactive Browser** - Simple browser sign-in
3. **Authorization Code** - Browser sign-in with redirect
4. **Device Code** - Sign in on another device (slightly more complex)

### By Use Case

**Automation & CI/CD:**
- ✅ Client Credentials
- ❌ User-interactive flows

**Personal Scripts:**
- ✅ Interactive Browser
- ✅ Device Code
- ⚠️ Authorization Code (requires web server)

**Production Services:**
- ✅ Client Credentials with Certificate
- ❌ User-interactive flows

**Headless Servers:**
- ✅ Device Code
- ✅ Client Credentials
- ❌ Interactive Browser

---

## Troubleshooting

### "Flow not supported for this profile"

**Cause:** Profile configuration doesn't support the selected flow

**Solution:**
- For Client Credentials: Ensure client secret or certificate is configured
- For user flows: Ensure delegated permissions are configured

### "AADSTS50011: Redirect URI mismatch"

**Cause:** Redirect URI not configured for Authorization Code or Interactive Browser

**Solution:**
1. Azure Portal → App registrations → Your app → Authentication
2. Add redirect URI: `http://localhost:8080`

### "Device code expired"

**Cause:** User didn't authenticate within time limit (typically 15 minutes)

**Solution:**
- Request new device code
- Authenticate more quickly

---

## Next Steps

- [Core Concepts: OAuth2 Flows](/docs/core-concepts/oauth2-flows/)
- [User Guide: Generating Tokens](/docs/user-guide/generating-tokens/)
- [Troubleshooting](/docs/troubleshooting/)
