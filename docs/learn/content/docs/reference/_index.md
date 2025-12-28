---
title: "Command Reference"
description: "Complete reference for all Entra Token CLI commands"
weight: 60
---

# Command Reference

Complete documentation for all Entra Token CLI commands, options, and arguments.

---

## Commands

Core commands for token generation and management.

[**get-token →**](/docs/reference/get-token/)

Generate access tokens using configured profiles

**Learn how to:**
- Generate tokens with different flows
- Override scopes at runtime
- Use silent mode for scripts
- Save tokens to files

[**refresh →**](/docs/reference/refresh/)

Refresh expired access tokens

**Learn how to:**
- Refresh tokens with refresh tokens
- Handle token expiration
- Use offline_access scope

[**inspect →**](/docs/reference/inspect/)

Decode and inspect JWT tokens

**Learn how to:**
- View token claims
- Check expiration dates
- Validate token structure
- Debug authentication issues

[**discover →**](/docs/reference/discover/)

Quick token information and validation

**Learn how to:**
- Validate token format
- Check token validity
- Get quick token info

[**config →**](/docs/reference/config/)

Manage authentication profiles

**Learn how to:**
- Create new profiles
- List existing profiles
- Edit profile settings
- Delete profiles
- Import/export profiles

---

## Global Options

### --help

Display help information for any command.

```bash
# General help
entratool --help

# Command-specific help
entratool get-token --help
entratool config --help
```

### --version

Show version information.

```bash
entratool --version
```

---

## Quick Reference

### Common Commands

```bash
# Generate token
entratool get-token -p my-profile

# Generate token (silent mode for scripts)
TOKEN=$(entratool get-token -p my-profile --silent)

# Override scope
entratool get-token -p my-profile --scope "https://graph.microsoft.com/User.Read"

# Use specific flow
entratool get-token -p my-profile -f ClientCredentials

# Inspect token
entratool inspect -t "eyJ0eXAiOiJKV1Q..."

# List profiles
entratool config list

# Create profile
entratool config create

# Delete profile
entratool config delete -p my-profile
```

---

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | General error |
| 2 | Authentication failed |
| 3 | Profile not found |
| 4 | Invalid arguments |
| 5 | Network error |

---

## Next Steps

- [get-token Command](/docs/reference/get-token/)
- [config Command](/docs/reference/config/)
- [User Guide](/docs/user-guide/)
- [Recipes](/docs/recipes/)
