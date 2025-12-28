---
title: "Recipes & Examples"
description: "Practical examples and common integration patterns"
weight: 40
---

# Recipes & Examples

Real-world examples and patterns for integrating Entra Token CLI into your workflows.

---

## API Integration

Integrate with Microsoft APIs and your own services.

[**Microsoft Graph API →**](/docs/recipes/microsoft-graph/)

**Learn how to:**
- Read user profiles and directory data
- Send emails via Graph API
- Manage calendars and events
- Work with OneDrive and SharePoint
- List users and groups

[**Azure Management API →**](/docs/recipes/azure-management/)

**Learn how to:**
- List subscriptions and resource groups
- Create and manage virtual machines
- Deploy infrastructure resources
- Monitor Azure services
- Automate resource provisioning

### Automation

Build automated workflows and CI/CD pipelines.

[**CI/CD Integration →**](/docs/recipes/cicd-integration/)

**Learn how to:**
- Integrate with GitHub Actions
- Configure Azure Pipelines
- Automate token generation
- Secure secrets in CI/CD
- Deploy with authentication

[**Bash Scripting →**](/docs/recipes/bash-scripts/)

**Learn how to:**
- Cache tokens for performance
- Implement error handling with retries
- Create multi-API scripts
- Handle token expiration
- Build robust automation

[**PowerShell Integration →**](/docs/recipes/powershell-scripts/)

**Learn how to:**
- Retrieve tokens in PowerShell
- Cache tokens in scripts
- Handle errors gracefully
- Integrate with Windows automation
- Use with Azure cmdlets

### Security

Implement security best practices and patterns.

[**Security Hardening →**](/docs/recipes/security-hardening/)

**Learn how to:**
- Secure token storage
- Separate development and production
- Implement secret rotation
- Use environment-specific profiles
- Follow least privilege principles

### Advanced Patterns

Advanced techniques for complex scenarios.

[**Common Patterns →**](/docs/recipes/common-patterns/)

**Learn how to:**
- Handle API rate limiting
- Make parallel API calls
- Implement conditional token refresh
- Build resilient integrations
- Optimize performance

---

## Quick Examples

### Microsoft Graph API

```bash
# Read user profile
TOKEN=$(entratool get-token -p graph-readonly --silent)
curl -H "Authorization: Bearer $TOKEN" \
     https://graph.microsoft.com/v1.0/me | jq
```

### Azure Management API

```bash
# List subscriptions
TOKEN=$(entratool get-token -p azure-mgmt --silent)
curl -H "Authorization: Bearer $TOKEN" \
     'https://management.azure.com/subscriptions?api-version=2020-01-01' | jq
```

### Shell Scripting

```bash
# Token caching
get_token() {
  local cache="/tmp/token-cache.txt"
  if [ -f "$cache" ] && entratool discover -f "$cache" &>/dev/null; then
    cat "$cache"
  else
    entratool get-token -p my-profile --silent | tee "$cache"
  fi
}
```

---

## Next Steps

- [Core Concepts](/docs/core-concepts/) - Understand OAuth2 flows and scopes
- [User Guide](/docs/user-guide/) - Day-to-day usage patterns
- [Command Reference](/docs/reference/) - Complete CLI documentation
