---
title: "Microsoft Graph API"
description: "Integrate with Microsoft Graph for user management, email, and more"
weight: 1
---

# Microsoft Graph API

Learn how to use Entra Auth Cli to authenticate and interact with Microsoft Graph API.

---

## Overview

Microsoft Graph is the unified API for Microsoft 365, providing access to:
- Users and groups
- Mail and calendars
- Files (OneDrive/SharePoint)
- Teams and collaboration
- Security and compliance

**Base URL:** `https://graph.microsoft.com/v1.0/`

---

## Quick Start

### Setup Profile

```bash {linenos=inline}
entra-auth-cli config create
# Name: graph-readonly
# Client ID: <your-app-id>
# Tenant ID: <your-tenant-id>
# Scope: https://graph.microsoft.com/User.Read
```

### Get Token and Call API

```bash {linenos=inline}
TOKEN=$(entra-auth-cli get-token -p graph-readonly )
curl -H "Authorization: Bearer $TOKEN" \
     https://graph.microsoft.com/v1.0/me | jq
```

---

## Read User Profile

Retrieve information about the authenticated user.

```bash {linenos=inline}
#!/bin/bash
TOKEN=$(entra-auth-cli get-token -p graph-readonly )

curl -H "Authorization: Bearer $TOKEN" \
     https://graph.microsoft.com/v1.0/me | jq
```

**Required scope:** `User.Read`

---

## List All Users

Retrieve a list of users in your organization.

```bash {linenos=inline}
#!/bin/bash
TOKEN=$(entra-auth-cli get-token -p graph-admin  \
  --scope "https://graph.microsoft.com/User.Read.All")

curl -H "Authorization: Bearer $TOKEN" \
     'https://graph.microsoft.com/v1.0/users?$select=displayName,mail,userPrincipalName' | jq
```

**Required scope:** `User.Read.All`

---

## Send Email

Send an email via Microsoft Graph.

```bash {linenos=inline}
#!/bin/bash
TOKEN=$(entra-auth-cli get-token -p graph-mail  \
  --scope "https://graph.microsoft.com/Mail.Send")

curl -X POST \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d '{
       "message": {
         "subject": "Test Email",
         "body": {
           "contentType": "Text",
           "content": "This is a test email from Entra Auth Cli"
         },
         "toRecipients": [
           {
             "emailAddress": {
               "address": "user@contoso.com"
             }
           }
         ]
       },
       "saveToSentItems": "true"
     }' \
     https://graph.microsoft.com/v1.0/me/sendMail
```

**Required scope:** `Mail.Send`

---

## List Calendar Events

Retrieve calendar events for the authenticated user.

```bash {linenos=inline}
#!/bin/bash
TOKEN=$(entra-auth-cli get-token -p graph-calendar  \
  --scope "https://graph.microsoft.com/Calendars.Read")

curl -H "Authorization: Bearer $TOKEN" \
     'https://graph.microsoft.com/v1.0/me/calendar/events?$select=subject,start,end' | jq
```

**Required scope:** `Calendars.Read`

---

## Create Calendar Event

Create a new calendar event.

```bash {linenos=inline}
#!/bin/bash
TOKEN=$(entra-auth-cli get-token -p graph-calendar  \
  --scope "https://graph.microsoft.com/Calendars.ReadWrite")

curl -X POST \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d '{
       "subject": "Team Meeting",
       "body": {
         "contentType": "HTML",
         "content": "Discuss Q1 goals"
       },
       "start": {
         "dateTime": "2024-01-15T14:00:00",
         "timeZone": "UTC"
       },
       "end": {
         "dateTime": "2024-01-15T15:00:00",
         "timeZone": "UTC"
       }
     }' \
     https://graph.microsoft.com/v1.0/me/calendar/events | jq
```

**Required scope:** `Calendars.ReadWrite`

---

## Common Scopes

| Scope | Permission | Use Case |
|-------|-----------|----------|
| `User.Read` | Read user profile | Basic user info |
| `User.Read.All` | Read all users | Directory queries |
| `Mail.Read` | Read email | Email client |
| `Mail.Send` | Send email | Email automation |
| `Calendars.Read` | Read calendars | Calendar sync |
| `Calendars.ReadWrite` | Modify calendars | Event management |
| `Files.Read` | Read files | Document access |
| `Files.ReadWrite` | Modify files | File uploads |

---

## Best Practices

### Use Specific Scopes

```bash {linenos=inline}
# Good: Specific scope
entra-auth-cli get-token -p graph --scope "https://graph.microsoft.com/User.Read"

# Avoid: .default in scripts (requests all consented permissions)
entra-auth-cli get-token -p graph --scope "https://graph.microsoft.com/.default"
```

### Cache Tokens

```bash {linenos=inline}
#!/bin/bash
TOKEN_CACHE="/tmp/graph-token.txt"

get_graph_token() {
  if [ -f "$TOKEN_CACHE" ] && entra-auth-cli discover -f "$TOKEN_CACHE" &>/dev/null; then
    cat "$TOKEN_CACHE"
  else
    entra-auth-cli get-token -p graph  | tee "$TOKEN_CACHE"
    chmod 600 "$TOKEN_CACHE"
  fi
}

TOKEN=$(get_graph_token)
```

### Handle Pagination

```bash {linenos=inline}
#!/bin/bash
TOKEN=$(entra-auth-cli get-token -p graph-admin )
URL="https://graph.microsoft.com/v1.0/users"

while [ -n "$URL" ]; do
  response=$(curl -s -H "Authorization: Bearer $TOKEN" "$URL")
  
  # Process users
  echo "$response" | jq -r '.value[].displayName'
  
  # Get next page URL
  URL=$(echo "$response" | jq -r '.["@odata.nextLink"] // empty')
done
```

---

## Next Steps

- [Azure Management API](/docs/recipes/azure-management/)
- [CI/CD Integration](/docs/recipes/cicd-integration/)
- [Security Hardening](/docs/recipes/security-hardening/)
- [Scopes & Permissions](/docs/core-concepts/scopes/)
