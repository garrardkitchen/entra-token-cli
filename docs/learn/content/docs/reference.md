---
title: "Command Reference"
description: "Complete reference for all Entra Token CLI commands"
weight: 60
---

# Command Reference

Complete documentation for all Entra Token CLI commands, options, and arguments.

---

## Command Overview

| Command | Description |
|---------|-------------|
| [`get-token`](/docs/reference/commands/get-token/) | Generate access tokens |
| [`refresh`](/docs/reference/commands/refresh/) | Refresh expired tokens |
| [`inspect`](/docs/reference/commands/inspect/) | Decode and inspect JWT tokens |
| [`discover`](/docs/reference/commands/discover/) | Quick token information |
| [`config`](/docs/reference/commands/config/) | Manage authentication profiles |
| [`--help`](/docs/reference/commands/help/) | Display help information |
| [`--version`](/docs/reference/commands/version/) | Show version information |

---

## `get-token`

Generate an access token using a configured profile.

### Synopsis

```bash
entratool get-token [OPTIONS]
```

### Options

| Option | Alias | Required | Description |
|--------|-------|----------|-------------|
| `--profile <NAME>` | `-p` | Yes | Profile name to use |
| `--scope <SCOPE>` | `-s` | No | Override profile scope |
| `--flow <FLOW>` | `-f` | No | OAuth2 flow to use |
| `--silent` | | No | Suppress output except token |
| `--output <FILE>` | `-o` | No | Save token to file |
| `--json` | | No | Output as JSON |

### OAuth2 Flows

- `ClientCredentials` - Service-to-service
- `AuthorizationCode` - Web applications
- `DeviceCode` - Limited-input devices
- `InteractiveBrowser` - Desktop applications

### Examples

**Basic usage:**
```bash
entratool get-token -p my-profile
```

**Override scope:**
```bash
entratool get-token -p my-profile \
  --scope "https://graph.microsoft.com/User.Read Mail.Read"
```

**Specify flow:**
```bash
entratool get-token -p my-profile -f ClientCredentials
```

**Silent output (for scripts):**
```bash
TOKEN=$(entratool get-token -p my-profile --silent)
```

**Save to file:**
```bash
entratool get-token -p my-profile -o token.txt
```

**JSON output:**
```bash
entratool get-token -p my-profile --json
```

**Multiple scopes:**
```bash
entratool get-token -p my-profile \
  --scope "https://graph.microsoft.com/User.Read,Mail.Read,Calendars.Read"
```

**Device code flow:**
```bash
entratool get-token -p headless-server -f DeviceCode
```

**Interactive browser:**
```bash
entratool get-token -p user-app -f InteractiveBrowser
```

**Certificate authentication:**
```bash
entratool get-token -p cert-profile -f ClientCredentials
```

[Full documentation →](/docs/reference/commands/get-token/)

---

## `refresh`

Refresh an expired access token using a refresh token.

### Synopsis

```bash
entratool refresh [OPTIONS]
```

### Options

| Option | Alias | Required | Description |
|--------|-------|----------|-------------|
| `--profile <NAME>` | `-p` | Yes | Profile name to use |
| `--silent` | | No | Suppress output except token |
| `--output <FILE>` | `-o` | No | Save token to file |

### Examples

**Refresh token:**
```bash
entratool refresh -p my-profile
```

**Silent output:**
```bash
TOKEN=$(entratool refresh -p my-profile --silent)
```

**Save to file:**
```bash
entratool refresh -p my-profile -o token.txt
```

### Notes

- Requires `offline_access` scope in original token request
- Not available for Client Credentials flow
- Refresh tokens expire after 90 days of inactivity

[Full documentation →](/docs/reference/commands/refresh/)

---

## `inspect`

Decode and display JWT token claims.

### Synopsis

```bash
entratool inspect [OPTIONS]
```

### Options

| Option | Alias | Required | Description |
|--------|-------|----------|-------------|
| `--token <TOKEN>` | `-t` | Conditional | Token string to inspect |
| `--file <FILE>` | `-f` | Conditional | File containing token |

**Note:** Provide either `--token` or `--file`, or pipe token via stdin.

### Examples

**Inspect token string:**
```bash
entratool inspect -t "eyJ0eXAiOiJKV1Qi..."
```

**Inspect from file:**
```bash
entratool inspect -f token.txt
```

**Inspect from pipeline:**
```bash
entratool get-token -p my-profile --silent | entratool inspect
```

**Extract specific claim:**
```bash
entratool inspect -t "$TOKEN" | jq -r .payload.scp
```

**Check expiration:**
```bash
entratool inspect -t "$TOKEN" | jq -r .payload.exp
```

**View all claims:**
```bash
entratool inspect -f token.txt | jq
```

[Full documentation →](/docs/reference/commands/inspect/)

---

## `discover`

Quick token information and validation.

### Synopsis

```bash
entratool discover [OPTIONS]
```

### Options

| Option | Alias | Required | Description |
|--------|-------|----------|-------------|
| `--token <TOKEN>` | `-t` | Conditional | Token string to discover |
| `--file <FILE>` | `-f` | Conditional | File containing token |

### Exit Codes

- `0` - Token is valid
- `1` - Token is expired or invalid

### Examples

**Discover token info:**
```bash
entratool discover -t "eyJ0eXAiOiJKV1Qi..."
```

**Check if token is valid:**
```bash
if entratool discover -t "$TOKEN" &>/dev/null; then
  echo "Token is valid"
else
  echo "Token is expired"
fi
```

**From file:**
```bash
entratool discover -f token.txt
```

**In script:**
```bash
if ! entratool discover -f token.txt; then
  # Token expired, get new one
  entratool get-token -p my-profile --silent > token.txt
fi
```

[Full documentation →](/docs/reference/commands/discover/)

---

## `config`

Manage authentication profiles.

### Synopsis

```bash
entratool config <SUBCOMMAND> [OPTIONS]
```

### Subcommands

| Subcommand | Description |
|------------|-------------|
| `create` | Create new profile |
| `list` | List all profiles |
| `edit` | Edit existing profile |
| `delete` | Delete profile |
| `export` | Export profile(s) |
| `import` | Import profile(s) |

---

### `config create`

Create a new authentication profile interactively.

**Synopsis:**
```bash
entratool config create
```

**Example:**
```bash
entratool config create

# Interactive prompts:
# - Profile name
# - Client ID
# - Tenant ID
# - Authentication method
# - Scope
# - OAuth2 flow (optional)
```

---

### `config list`

List all configured profiles.

**Synopsis:**
```bash
entratool config list [OPTIONS]
```

**Options:**
| Option | Description |
|--------|-------------|
| `--json` | Output as JSON |

**Examples:**
```bash
# List profiles
entratool config list

# JSON output
entratool config list --json
```

---

### `config edit`

Edit an existing profile interactively.

**Synopsis:**
```bash
entratool config edit [OPTIONS]
```

**Options:**
| Option | Alias | Required | Description |
|--------|-------|----------|-------------|
| `--profile <NAME>` | `-p` | Yes | Profile name to edit |

**Example:**
```bash
entratool config edit -p my-profile

# Select what to edit:
# - Client ID
# - Tenant ID
# - Client Secret
# - Scope
# - OAuth2 Flow
# - Authority URL
```

---

### `config delete`

Delete a profile and its associated secrets.

**Synopsis:**
```bash
entratool config delete [OPTIONS]
```

**Options:**
| Option | Alias | Required | Description |
|--------|-------|----------|-------------|
| `--profile <NAME>` | `-p` | Yes | Profile name to delete |
| `--force` | | No | Skip confirmation |

**Examples:**
```bash
# Delete with confirmation
entratool config delete -p my-profile

# Delete without confirmation
entratool config delete -p my-profile --force
```

---

### `config export`

Export profile configuration (without secrets).

**Synopsis:**
```bash
entratool config export [OPTIONS]
```

**Options:**
| Option | Alias | Required | Description |
|--------|-------|----------|-------------|
| `--profile <NAME>` | `-p` | No | Profile name (omit for all) |
| `--output <FILE>` | `-o` | Yes | Output file path |

**Examples:**
```bash
# Export single profile
entratool config export -p my-profile -o profile.json

# Export all profiles
entratool config export -o all-profiles.json
```

---

### `config import`

Import profile configuration from file.

**Synopsis:**
```bash
entratool config import [OPTIONS]
```

**Options:**
| Option | Alias | Required | Description |
|--------|-------|----------|-------------|
| `--file <FILE>` | `-f` | Yes | File to import |

**Example:**
```bash
entratool config import -f profile.json
```

[Full config documentation →](/docs/reference/commands/config/)

---

## `--help`

Display help information.

### Synopsis

```bash
entratool --help
entratool <COMMAND> --help
```

### Examples

**General help:**
```bash
entratool --help
```

**Command-specific help:**
```bash
entratool get-token --help
entratool config --help
entratool config create --help
```

---

## `--version`

Display version information.

### Synopsis

```bash
entratool --version
```

**Output:**
```
                 _             _              _
  ___ _ __  _ __| |_ _ __ __ _| |_ ___   ___ | |
 / _ \ '_ \| '__| __| '__/ _` | __/ _ \ / _ \| |
|  __/ | | | |  | |_| | | (_| | || (_) | (_) | |
 \___|_| |_|_|   \__|_|  \__,_|\__\___/ \___/|_|

version 1.0.0
```

---

## Global Options

Available for all commands:

| Option | Description |
|--------|-------------|
| `--help` | Display help |
| `--version` | Display version |
| `--verbose` | Enable verbose output |
| `--no-color` | Disable colored output |

---

## Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Success |
| `1` | General error |
| `2` | Invalid arguments |
| `3` | Authentication failed |
| `4` | Profile not found |
| `5` | Token expired |
| `6` | Network error |

---

## Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `AZURE_CLIENT_ID` | Default client ID | `12345678-...` |
| `AZURE_TENANT_ID` | Default tenant ID | `87654321-...` |
| `AZURE_CLIENT_SECRET` | Client secret | `abc123...` |
| `ENTRATOOL_CONFIG_PATH` | Config directory | `~/.entratool` |

---

## Configuration Files

### Profile Configuration

**Location:** `~/.entratool/profiles.json`

**Format:**
```json
{
  "profiles": [
    {
      "name": "my-profile",
      "clientId": "12345678-1234-1234-1234-123456789abc",
      "tenantId": "87654321-4321-4321-4321-cba987654321",
      "scope": "https://graph.microsoft.com/.default",
      "flow": "ClientCredentials",
      "useClientSecret": true
    }
  ]
}
```

### Secure Storage

**Secrets location:**
- **Windows:** DPAPI-encrypted store
- **macOS:** Keychain (`~/Library/Keychains/login.keychain-db`)
- **Linux:** `~/.entratool/secrets.dat` (⚠️ XOR obfuscated)

---

## Common Command Patterns

### Script Integration

```bash
#!/bin/bash
set -euo pipefail

# Get token
TOKEN=$(entratool get-token -p automation --silent)

# Use token
curl -H "Authorization: Bearer $TOKEN" \
     https://graph.microsoft.com/v1.0/me
```

### Token Caching

```bash
TOKEN_FILE="/tmp/cached-token.txt"

# Check if token is valid
if ! entratool discover -f "$TOKEN_FILE" &>/dev/null; then
  # Get fresh token
  entratool get-token -p my-profile --silent > "$TOKEN_FILE"
  chmod 600 "$TOKEN_FILE"
fi

TOKEN=$(cat "$TOKEN_FILE")
```

### Multi-Environment

```bash
# Development
entratool get-token -p dev-profile -f ClientCredentials

# Staging
entratool get-token -p staging-profile -f ClientCredentials

# Production
entratool get-token -p prod-profile -f ClientCredentials
```

### Error Handling

```bash
if ! TOKEN=$(entratool get-token -p my-profile --silent 2>&1); then
  echo "Error: Failed to get token"
  echo "$TOKEN"
  exit 1
fi

# Use token
curl -H "Authorization: Bearer $TOKEN" ...
```

---

## Next Steps

- [Detailed Command Documentation](/docs/reference/commands/)
- [Configuration Reference](/docs/reference/configuration/)
- [User Guide](/docs/user-guide/)
- [Recipes](/docs/recipes/)
