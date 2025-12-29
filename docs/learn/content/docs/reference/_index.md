---
title: "Command Reference"
description: "Complete reference for all Entra Auth Cli commands"
weight: 60
---

# Command Reference

Complete documentation for all Entra Auth Cli commands, options, and arguments.

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

```bash {linenos=inline}
# General help
entra-auth-cli --help

# Command-specific help
entra-auth-cli get-token --help
entra-auth-cli config --help
```

### --version

Show version information.

```bash {linenos=inline}
entra-auth-cli --version
```

---

## Quick Reference

### Common Commands

```bash {linenos=inline}
# Generate token
entra-auth-cli get-token -p my-profile

# Generate token (silent mode for scripts)
TOKEN=$(entra-auth-cli get-token -p my-profile)

# Override scope
entra-auth-cli get-token -p my-profile --scope "https://graph.microsoft.com/User.Read"

# Use specific flow
entra-auth-cli get-token -p my-profile -f ClientCredentials

# Inspect token
entra-auth-cli inspect -t "eyJ0eXAiOiJKV1Q..."

# List profiles
entra-auth-cli config list

# Create profile
entra-auth-cli config create

# Delete profile
entra-auth-cli config delete -p my-profile
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
