---
title: "OAuth2 Flows"
description: "Detailed guides for each OAuth2 authentication flow"
weight: 50
---

# OAuth2 Flows

Comprehensive guides for each supported OAuth2 authentication flow.

---

## Supported Flows

Entra Token CLI supports four OAuth2 authentication flows, each designed for specific scenarios.

### Service-to-Service

[**Client Credentials Flow →**](/docs/oauth-flows/client-credentials/)

**Best for:** Automated services, daemons, CI/CD pipelines

**Characteristics:**
- No user interaction required
- Uses client secret or certificate
- Application permissions only
- Highest security with certificates

### User Authentication

[**Interactive Browser Flow →**](/docs/oauth-flows/interactive-browser/)

**Best for:** Desktop applications, CLI tools with user authentication

**Characteristics:**
- Opens browser for user sign-in
- Delegated permissions
- Uses system default browser
- Cached credentials

[**Device Code Flow →**](/docs/oauth-flows/device-code/)

**Best for:** Headless devices, IoT, SSH sessions

**Characteristics:**
- Sign in on another device
- Works without local browser
- User-friendly for limited-input devices
- 15-minute time window

[**Authorization Code Flow →**](/docs/oauth-flows/authorization-code/)

**Best for:** Web applications with user sign-in

**Characteristics:**
- Redirect-based authentication
- Requires web server callback
- Most secure user flow
- Supports refresh tokens

---

## Flow Comparison

| Flow | User Interaction | Best For | Security Level |
|------|------------------|----------|----------------|
| Client Credentials | None | Services | Highest (with cert) |
| Interactive Browser | Required | Desktop apps | High |
| Device Code | Required (other device) | Headless | Medium-High |
| Authorization Code | Required | Web apps | High |

---

## Quick Examples

### Client Credentials

```bash
entratool get-token -p service-principal -f ClientCredentials
```

### Interactive Browser

```bash
entratool get-token -p desktop-app -f InteractiveBrowser
```

### Device Code

```bash
entratool get-token -p iot-device -f DeviceCode
```

### Authorization Code

```bash
entratool get-token -p webapp -f AuthorizationCode
```

---

## Automatic Flow Selection

The tool automatically selects the appropriate flow based on your profile configuration:

- **Client Secret or Certificate** configured → Client Credentials flow
- **No client secret/certificate** → Interactive Browser flow

### Manual Override

```bash
# Override automatic selection
entratool get-token -p myprofile -f DeviceCode
```

---

## Next Steps

- [Client Credentials Flow](/docs/oauth-flows/client-credentials/)
- [Interactive Browser Flow](/docs/oauth-flows/interactive-browser/)
- [Device Code Flow](/docs/oauth-flows/device-code/)
- [Core Concepts: OAuth2 Flows](/docs/core-concepts/oauth2-flows/)
