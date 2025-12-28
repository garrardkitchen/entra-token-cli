---
title: "Quick Start"
description: "Generate your first token in 5 minutes"
weight: 20
---

# Quick Start

Generate your first Entra ID access token in just a few minutes!

---

## Step 1: Install the Tool

```bash {linenos=inline}
dotnet tool install -g EntraTokenCli
```

[See all installation options →](/docs/getting-started/installation/)

---

## Step 2: Create Your First Profile

Run the interactive profile creation:

```bash {linenos=inline}
entra-auth-cli config create
```

You'll be prompted for:

- **Profile name**: `myprofile` (or any name you choose)
- **Tenant ID**: Your Azure tenant ID (e.g., `contoso.onmicrosoft.com` or GUID)
- **Client ID**: Your app registration client ID
- **Scopes**: `https://graph.microsoft.com/.default` (default for Microsoft Graph)
- **Auth method**: Choose `ClientSecret` for this quick start
- **Client secret**: Your app's client secret (securely stored)

**Example:**

```
Profile name: myprofile
Tenant ID: contoso.onmicrosoft.com
Client ID: 12345678-1234-1234-1234-123456789abc
Scopes (comma-separated): https://graph.microsoft.com/.default
Authentication method: ClientSecret
Client secret: ****
```

> **💡 Tip**: Don't have an app registration? See the [Complete Tutorial](/docs/getting-started/first-token/) for step-by-step setup.

---

## Step 3: Generate a Token

```bash {linenos=inline}
entra-auth-cli get-token -p myprofile
```

The tool will:

1. Authenticate using your profile credentials
2. Request a token from Entra ID
3. Display the token details
4. Copy the token to your clipboard (if available)

**Expected output:**

```
✓ Token retrieved successfully
  Expires: 2025-12-26 15:30:00 UTC (59 minutes)
  Scopes: https://graph.microsoft.com/.default
  Token copied to clipboard!
```

---

## Step 4: Use Your Token

The token is now in your clipboard. Use it to call APIs:

```bash {linenos=inline}
# Example: Get current user info from Microsoft Graph
curl -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  https://graph.microsoft.com/v1.0/me
```

Or read from the token file in headless environments:

```bash {linenos=inline}
TOKEN=$(cat ~/.config/entra-auth-cli/last-token.txt)
curl -H "Authorization: Bearer $TOKEN" \
  https://graph.microsoft.com/v1.0/me
```

---

## Step 5: Inspect the Token (Optional)

Want to see what's inside your token?

```bash {linenos=inline}
entra-auth-cli inspect $(cat ~/.config/entra-auth-cli/last-token.txt)
```

This decodes the JWT and displays claims like:

- **Audience** (aud)
- **Issuer** (iss)
- **Subject** (sub)
- **Expiration** (exp)
- **Scopes** (scp/roles)

---

## What's Next?

**Learn the basics:**
- [Authentication Profiles](/docs/core-concepts/profiles/) - Understand profiles
- [OAuth2 Flows](/docs/core-concepts/oauth2-flows/) - Learn about different flows
- [Scopes](/docs/core-concepts/scopes/) - Manage API permissions

**Explore features:**
- [Managing Profiles](/docs/user-guide/managing-profiles/) - Create, edit, export profiles
- [Certificate Authentication](/docs/certificates/) - Use certificates instead of secrets
- [Scope Overrides](/docs/user-guide/generating-tokens/scope-overrides/) - Get tokens for different APIs

**Try recipes:**
- [Service Principal Authentication](/docs/recipes/service-principals/)
- [Calling Microsoft Graph](/docs/recipes/microsoft-graph/)
- [Custom APIs](/docs/recipes/custom-apis/)

---

## Troubleshooting

**Token generation fails?**
- Verify your client ID and secret are correct
- Check app registration permissions in Azure
- Ensure tenant ID is correct

**Token not copied to clipboard?**
- In headless environments, use the file output
- Use `--no-clipboard` flag to suppress clipboard operations

[See full troubleshooting guide →](/docs/troubleshooting/)
