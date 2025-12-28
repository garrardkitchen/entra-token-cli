---
title: "First Token Tutorial"
description: "Complete end-to-end guide from Azure setup to token generation"
weight: 30
---

# Your First Token - Complete Tutorial

This tutorial walks you through the complete process from setting up an app registration in Azure to generating your first token.

---

## Prerequisites

- Azure subscription with access to create app registrations
- Entra Token CLI installed ([Installation Guide](/docs/getting-started/installation/))
- Basic understanding of Azure and authentication concepts

---

## Step 1: Create an App Registration

### 1.1 Navigate to Azure Portal

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** (or **Microsoft Entra ID**)
3. Select **App registrations** from the left menu
4. Click **New registration**

### 1.2 Register Your Application

Fill in the registration form:

- **Name**: `entratool-demo` (or your preferred name)
- **Supported account types**: 
  - Select "Accounts in this organizational directory only" for single tenant
  - Or choose multi-tenant if needed
- **Redirect URI**: Leave blank for now (we'll add later if needed)

Click **Register**

### 1.3 Note Your IDs

After registration, save these values:

- **Application (client) ID**: Found on the Overview page
- **Directory (tenant) ID**: Also on the Overview page

---

## Step 2: Create a Client Secret

### 2.1 Generate Secret

1. In your app registration, go to **Certificates & secrets**
2. Click **New client secret**
3. Add a description: `entratool-demo-secret`
4. Choose expiration: 
   - **6 months** for testing
   - **24 months** for longer-term use
5. Click **Add**

### 2.2 Copy the Secret

**⚠️ Important**: Copy the secret **Value** immediately - it won't be shown again!

---

## Step 3: Configure API Permissions

### 3.1 Add Permissions

1. Go to **API permissions** in your app registration
2. Click **Add a permission**
3. Select **Microsoft Graph**
4. Choose **Application permissions** (for service-to-service)
5. Search for and add these permissions:
   - `User.Read.All` (to read user information)
   - Or any other permissions your application needs

### 3.2 Grant Admin Consent

1. Click **Grant admin consent for [Your Organization]**
2. Confirm by clicking **Yes**

> **💡 Note**: Admin consent is required for application permissions

---

## Step 4: Create Your Profile

Now that your Azure app is configured, create a profile in entratool:

```bash {linenos=inline}
entratool config create
```

Enter the following when prompted:

```
Profile name: demo-profile
Tenant ID: YOUR-TENANT-ID
Client ID: YOUR-CLIENT-ID
Scopes (comma-separated): https://graph.microsoft.com/.default
Authentication method: ClientSecret
Client secret: YOUR-CLIENT-SECRET
Set default OAuth2 flow? n
Configure custom redirect URI? n
```

**Success!** Your profile is created and the secret is securely stored.

---

## Step 5: Generate Your First Token

```bash {linenos=inline}
entratool get-token -p demo-profile
```

You should see output like:

```
✓ Token retrieved successfully
  Expires: 2025-12-26 15:30:00 UTC (59 minutes)
  Scopes: https://graph.microsoft.com/.default
  Token Type: Bearer
  Token copied to clipboard!
```

---

## Step 6: Verify Your Token

### 6.1 Inspect the Token

```bash {linenos=inline}
entratool inspect $(cat ~/.config/entratool/last-token.txt)
```

This shows the decoded JWT claims, including:

- **aud**: Audience (should be `https://graph.microsoft.com`)
- **iss**: Issuer (your Azure tenant)
- **app_displayname**: Your app name
- **roles**: Assigned application permissions
- **exp**: Expiration timestamp

### 6.2 Use the Token

Test your token with Microsoft Graph:

```bash {linenos=inline}
TOKEN=$(cat ~/.config/entratool/last-token.txt)
curl -H "Authorization: Bearer $TOKEN" \
  https://graph.microsoft.com/v1.0/users?$top=5
```

You should receive a JSON response with user data!

---

## Troubleshooting

### "Invalid client secret"

- Verify you copied the secret value (not the secret ID)
- Check if the secret has expired
- Regenerate the secret if needed

### "Insufficient privileges"

- Ensure you granted admin consent for the permissions
- Verify the permissions are **Application permissions**, not **Delegated permissions**
- Wait a few minutes for changes to propagate

### "Token request failed"

- Double-check tenant ID and client ID
- Ensure network connectivity to Azure
- Check if your Azure subscription is active

---

## What's Next?

**Explore different flows:**
- [Authorization Code Flow](/docs/oauth-flows/authorization-code/) - For user authentication
- [Device Code Flow](/docs/oauth-flows/device-code/) - For limited-input devices
- [Interactive Browser](/docs/oauth-flows/interactive-browser/) - For desktop applications

**Learn about features:**
- [Scope Management](/docs/core-concepts/scopes/) - Using tokens for different APIs
- [Certificate Authentication](/docs/certificates/) - Using certificates instead of secrets
- [Profile Management](/docs/user-guide/managing-profiles/) - Managing multiple profiles

**Try recipes:**
- [Calling Microsoft Graph](/docs/recipes/microsoft-graph/) - Common Graph API scenarios
- [Custom APIs](/docs/recipes/custom-apis/) - Access your own APIs
- [CI/CD Integration](/docs/recipes/cicd-integration/) - Use in automated pipelines
