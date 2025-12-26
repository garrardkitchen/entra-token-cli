---
title: "OAuth2 Flows"
description: "Understanding the four OAuth2 authentication flows"
weight: 20
---

# OAuth2 Flows

Entra Token CLI supports four OAuth2 authentication flows. Each flow is designed for specific scenarios and security requirements.

---

## Flow Overview

| Flow | Use Case | User Interaction | Token Type |
|------|----------|------------------|------------|
| **Client Credentials** | Service-to-service | None | Application |
| **Authorization Code** | Web apps | Required | User + Application |
| **Device Code** | Limited-input devices | Required (on another device) | User |
| **Interactive Browser** | Desktop apps | Required | User |

---

## Client Credentials Flow

### When to Use

- **Automated services** and daemons
- **CI/CD pipelines** 
- **Background jobs** without user context
- **Service-to-service** authentication

### How It Works

1. Application authenticates with client ID + secret (or certificate)
2. Entra ID validates credentials
3. Returns application-only access token

### Requirements

- App registration with **Application permissions** (not Delegated)
- Admin consent granted
- Client secret or certificate

### Example

```bash
entratool get-token -p service-principal -f ClientCredentials
```

[Full guide →](/docs/oauth-flows/client-credentials/)

---

## Authorization Code Flow

### When to Use

- **Web applications** with user sign-in
- **Apps requiring user context**
- **Multi-user scenarios**
- **Secure applications** with confidential client

### How It Works

1. User is redirected to Entra ID sign-in page
2. User authenticates and consents to permissions
3. Entra ID returns authorization code
4. Application exchanges code for access token

### Requirements

- Redirect URI configured in app registration
- **Delegated permissions**
- User account credentials

### Example

```bash
entratool get-token -p webapp -f AuthorizationCode
```

[Full guide →](/docs/oauth-flows/authorization-code/)

---

## Device Code Flow

### When to Use

- **Headless devices** (IoT, servers)
- **Limited-input devices** (smart TVs, printers)
- **SSH sessions**
- **Scenarios without browser access**

### How It Works

1. Application requests device code
2. User visits URL on another device and enters code
3. Application polls Entra ID for token
4. Token issued after user completes authentication

### Requirements

- Device code flow enabled in app registration
- User account credentials
- Access to another device with browser

### Example

```bash
entratool get-token -p iot-device -f DeviceCode
```

**Output:**
```
Device Code Authentication
To sign in, use a web browser to open https://microsoft.com/devicelogin
and enter the code: ABCD-1234

Code: ABCD-1234
URL: https://microsoft.com/devicelogin
```

[Full guide →](/docs/oauth-flows/device-code/)

---

## Interactive Browser Flow

### When to Use

- **Desktop applications**
- **Command-line tools** with user authentication
- **Personal productivity apps**
- **Interactive sessions**

### How It Works

1. Application launches system browser
2. User authenticates in browser
3. Browser redirects to localhost with authorization code
4. Application exchanges code for token

### Requirements

- Redirect URI: `http://localhost:{port}` configured
- **Delegated permissions**
- User account credentials
- Browser availability

### Example

```bash
entratool get-token -p desktop-app -f InteractiveBrowser
```

[Full guide →](/docs/oauth-flows/interactive-browser/)

---

## Flow Selection

### Automatic Inference

If you don't specify a flow with `-f`, the tool automatically infers based on your profile's authentication method:

- **Client Secret** or **Certificate** → **Client Credentials**
- **Other methods** → **Interactive Browser**

You can override this by setting a default flow in your profile or using the `-f` flag.

### Setting Default Flow

When creating or editing a profile:

```bash
entratool config create
# ... other prompts ...
Set default OAuth2 flow? y
Default OAuth2 flow: ClientCredentials
```

Or specify at runtime:

```bash
entratool get-token -p myprofile -f DeviceCode
```

---

## Comparison Matrix

### User Experience

| Flow | User Action | Complexity |
|------|-------------|-----------|
| Client Credentials | None | Simple |
| Authorization Code | Sign in + consent | Moderate |
| Device Code | Sign in on another device | Moderate |
| Interactive Browser | Sign in in browser | Simple |

### Security

| Flow | Security Level | Best For |
|------|---------------|----------|
| Client Credentials | High (with certificate) | Automation |
| Authorization Code | High | Web apps |
| Device Code | Medium | Constrained devices |
| Interactive Browser | Medium-High | User apps |

### Token Properties

| Flow | Token Scope | User Context |
|------|-------------|--------------|
| Client Credentials | Application | No |
| Authorization Code | User + Application | Yes |
| Device Code | User | Yes |
| Interactive Browser | User | Yes |

---

## Common Scenarios

### Scenario: Automated Azure Resource Management

**Flow**: Client Credentials  
**Auth**: Certificate (recommended)  
**Scopes**: `https://management.azure.com/.default`

```bash
entratool get-token -p azure-automation -f ClientCredentials
```

### Scenario: Personal Microsoft Graph Access

**Flow**: Interactive Browser or Device Code  
**Auth**: No client secret needed (public client)  
**Scopes**: `https://graph.microsoft.com/User.Read`

```bash
entratool get-token -p personal-graph -f InteractiveBrowser
```

### Scenario: CI/CD Pipeline

**Flow**: Client Credentials  
**Auth**: Client Secret (stored in CI/CD secrets)  
**Scopes**: API-specific scope

```bash
entratool get-token -p cicd-deployer -f ClientCredentials
```

---

## Troubleshooting

### "This application requires user consent"

- **Flow**: Use Authorization Code or Interactive Browser
- **Permissions**: Configure Delegated permissions, not Application permissions

### "Client credentials flow not supported"

- **Fix**: Enable Application permissions and grant admin consent
- **Alternative**: Use user-interactive flow instead

### "Redirect URI mismatch"

- **Fix**: Add exact redirect URI to app registration
- **Format**: `http://localhost:8080` (include port)

---

## Next Steps

- [Detailed Client Credentials Guide](/docs/oauth-flows/client-credentials/)
- [Detailed Authorization Code Guide](/docs/oauth-flows/authorization-code/)
- [Detailed Device Code Guide](/docs/oauth-flows/device-code/)
- [Detailed Interactive Browser Guide](/docs/oauth-flows/interactive-browser/)
- [Understand Scopes](/docs/core-concepts/scopes/)
