---
title: "Device Code Flow"
description: "Authentication for devices without browsers or limited input"
weight: 30
---

# Device Code Flow

The Device Code flow enables authentication on devices that lack a web browser or have limited input capabilities. Users sign in on a separate device (like their phone or computer) while the original device polls for authentication completion.

## Overview

**Use this flow when:**
- Device doesn't have a web browser
- Limited input capabilities (e.g., smart TV, IoT device)
- Working in SSH/remote terminal sessions
- Browser-based authentication isn't practical

**How it works:**
1. Device requests a user code
2. User visits URL on another device
3. User enters code and signs in
4. Original device receives tokens

## Quick Start

```bash {linenos=inline}
# Start device code authentication
entratool get-token --flow device-code --profile myapp
```

Output:
```
To sign in, use a web browser to open the page:
  https://microsoft.com/devicelogin

And enter the code: ABCD-EFGH

Waiting for authentication...
```

## Configuration

### Profile Setup

Create a profile for device code flow:

```bash {linenos=inline}
entratool create-profile --name device-app
```

Profile configuration:
- **Tenant ID**: Your Microsoft Entra tenant
- **Client ID**: Public client application
- **Default Flow**: device-code (optional)
- **Scopes**: Delegated permissions

### Azure App Registration

Configure your app registration for device code flow:

1. **Application Type**: Public client
2. **Platform Configuration**:
   - Enable "Mobile and desktop applications"
   - No redirect URI needed
3. **API Permissions**:
   - Add delegated permissions (not application permissions)
   - Example: `User.Read`, `Mail.Read`

```bash {linenos=inline}
# Create public client app
az ad app create \
  --display-name "Device Code App" \
  --sign-in-audience AzureADMyOrg \
  --enable-access-token-issuance true

# Configure as public client
az ad app update \
  --id <app-id> \
  --is-fallback-public-client true
```

## Usage Examples

### Basic Authentication

```bash {linenos=inline}
# Using default profile
entratool get-token --flow device-code

# Using specific profile
entratool get-token --flow device-code --profile production

# With custom scopes
entratool get-token --flow device-code \
  --scope https://graph.microsoft.com/User.Read
```

### In Scripts

```bash {linenos=inline}
#!/bin/bash
set -euo pipefail

echo "Authenticating with Microsoft Entra ID..."
echo "Please complete the sign-in process on another device."
echo

# Start device code flow
if ! TOKEN=$(entratool get-token --flow device-code --output json); then
    echo "Authentication failed" >&2
    exit 1
fi

# Extract access token
ACCESS_TOKEN=$(echo "$TOKEN" | jq -r .access_token)

# Use the token
echo "Authentication successful!"
echo "Fetching user profile..."

curl -s -H "Authorization: Bearer $ACCESS_TOKEN" \
  https://graph.microsoft.com/v1.0/me \
  | jq .
```

### SSH Sessions

Perfect for remote terminal sessions:

```bash {linenos=inline}
# On remote server via SSH
ssh user@server

# Authenticate using device code
entratool get-token --flow device-code

# Complete sign-in on your local computer/phone
# Remote session receives token automatically
```

## User Experience

### Authentication Flow

```
User runs command:
$ entratool get-token --flow device-code

CLI displays:
┌─────────────────────────────────────────────────────┐
│ Device Code Authentication                          │
├─────────────────────────────────────────────────────┤
│ 1. Visit: https://microsoft.com/devicelogin        │
│ 2. Enter code: ABCD-EFGH                            │
│ 3. Sign in with your credentials                    │
├─────────────────────────────────────────────────────┤
│ Waiting for authentication... (expires in 15 min)   │
│ Press Ctrl+C to cancel                              │
└─────────────────────────────────────────────────────┘

[Polling dots animation...]

✓ Authentication successful!
Token saved to profile 'default'
```

### On Separate Device

User navigates to https://microsoft.com/devicelogin:

1. **Enter Code**: ABCD-EFGH
2. **Sign In**: Microsoft credentials
3. **MFA** (if required): Complete additional verification
4. **Consent**: Approve permissions (first time only)
5. **Confirmation**: "You have signed in to the application on your device"

## Advanced Usage

### Custom Timeout

Device codes expire after 15 minutes by default:

```bash {linenos=inline}
# Check token expiry
entratool get-token --flow device-code --output json \
  | jq '.expires_at'

# The CLI handles timeout automatically
# User sees: "Device code expired. Please try again."
```

### Polling Interval

The CLI polls Microsoft Entra ID while waiting for user authentication:

- Default: 5-second intervals
- Respects Microsoft's rate limits
- Shows progress indicators

### Cancellation

User can cancel at any time:

```bash {linenos=inline}
# Press Ctrl+C during polling
^C
Authentication cancelled by user
```

## Integration Examples

### Docker Containers

```dockerfile
FROM ubuntu:22.04

RUN apt-get update && apt-get install -y curl jq

# Install Entra Token CLI
RUN curl -L https://github.com/garrardkitchen/entratool-cli/releases/latest/download/entratool-linux-amd64 \
    -o /usr/local/bin/entratool && \
    chmod +x /usr/local/bin/entratool

COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]
```

```bash {linenos=inline}
#!/bin/bash
# entrypoint.sh

echo "Authenticating container..."
entratool get-token --flow device-code --profile container

# Run application with token
exec "$@"
```

### IoT Devices

```python
import subprocess
import json

def authenticate_device():
    """Authenticate IoT device using device code flow."""
    print("Starting device authentication...")
    
    # Start device code flow
    result = subprocess.run(
        ["entratool", "get-token", "--flow", "device-code", "--output", "json"],
        capture_output=True,
        text=True
    )
    
    if result.returncode != 0:
        raise Exception("Authentication failed")
    
    token_data = json.loads(result.stdout)
    return token_data['access_token']

def send_telemetry(token, data):
    """Send telemetry data to Azure."""
    import requests
    
    response = requests.post(
        "https://api.example.com/telemetry",
        headers={"Authorization": f"Bearer {token}"},
        json=data
    )
    return response.status_code == 200

# Main IoT application
if __name__ == "__main__":
    # Authenticate once
    access_token = authenticate_device()
    
    # Send data
    while True:
        data = collect_sensor_data()
        send_telemetry(access_token, data)
        time.sleep(60)
```

### Raspberry Pi

```bash {linenos=inline}
#!/bin/bash
# raspberry-pi-auth.sh

PROFILE="raspberry-pi"
TOKEN_FILE="/home/pi/.entratool-cache"

# Create profile if it doesn't exist
if ! entratool list-profiles | grep -q "$PROFILE"; then
    echo "Creating profile for Raspberry Pi..."
    entratool create-profile \
        --name "$PROFILE" \
        --tenant-id "$TENANT_ID" \
        --client-id "$CLIENT_ID" \
        --scope "https://graph.microsoft.com/User.Read"
fi

# Authenticate using device code
echo "Please sign in using another device..."
entratool get-token --flow device-code --profile "$PROFILE"

# Cache token for application
entratool get-token --profile "$PROFILE" --output json > "$TOKEN_FILE"

echo "Authentication complete! Starting application..."
python3 /home/pi/app/main.py
```

## Security Considerations

### Public Client

Device code flow uses public client apps:

- No client secret required
- Cannot securely store secrets
- Uses only client ID for authentication

### User Consent

Users must consent to permissions:

```bash {linenos=inline}
# First-time authentication requires consent
entratool get-token --flow device-code

# Subsequent authentications (same user):
# - No consent required if permissions unchanged
# - Cached credentials may be reused
```

### Token Security

Protect access tokens after authentication:

```bash {linenos=inline}
# ❌ Bad - token exposed in command
curl -H "Authorization: Bearer $(entratool get-token --output json | jq -r .access_token)" ...

# ✅ Good - token in variable
TOKEN=$(entratool get-token --output json | jq -r .access_token)
# Use $TOKEN in scripts, not exposed in process list
```

### Timeout Security

- Codes expire after 15 minutes
- Prevents old codes from being used
- Forces fresh authentication for security

## Troubleshooting

### Code Expired

**Problem:** "Device code has expired"

**Solution:** Run the command again to get a new code:

```bash {linenos=inline}
# Code expires after 15 minutes
entratool get-token --flow device-code
```

### Wrong Code Entered

**Problem:** "Invalid device code"

**Solution:** 
- Double-check the code (case-sensitive)
- Ensure no extra spaces
- Request new code if needed

### Cancelled Authentication

**Problem:** Polling stopped but didn't complete sign-in

**Solution:**
```bash {linenos=inline}
# Start over with fresh code
entratool get-token --flow device-code
```

### Permissions Denied

**Problem:** "User does not have permission"

**Solution:**
1. Check Azure app registration permissions
2. Ensure delegated permissions (not application)
3. User must have required role/permissions in Entra ID

```bash {linenos=inline}
# Verify required permissions in Azure
az ad app permission list --id <app-id>
```

## Comparison with Other Flows

| Feature | Device Code | Interactive Browser | Client Credentials |
|---------|-------------|---------------------|-------------------|
| **Browser Required** | On separate device | Yes, local | No |
| **User Interaction** | Sign in on another device | Sign in locally | None |
| **Input Limitations** | Works with limited input | Requires keyboard | N/A |
| **Auth Type** | Delegated | Delegated | Application |
| **Refresh Tokens** | Yes | Yes | No (always valid) |
| **Best For** | Headless devices, SSH | Desktop apps | Automation, services |

## Best Practices

### Clear Instructions

Provide clear guidance to users:

```bash {linenos=inline}
cat << 'EOF'
╔════════════════════════════════════════════════════╗
║             DEVICE AUTHENTICATION                  ║
╠════════════════════════════════════════════════════╣
║ This application needs permission to access your   ║
║ Microsoft account.                                 ║
║                                                    ║
║ Steps:                                            ║
║ 1. Grab your phone or another computer            ║
║ 2. Open a web browser                             ║
║ 3. Follow the instructions below                  ║
╚════════════════════════════════════════════════════╝
EOF

entratool get-token --flow device-code
```

### Error Handling

```bash {linenos=inline}
authenticate_with_retry() {
    local max_attempts=3
    local attempt=1
    
    while [ $attempt -le $max_attempts ]; do
        echo "Authentication attempt $attempt of $max_attempts..."
        
        if entratool get-token --flow device-code --profile "$1"; then
            echo "✓ Authentication successful"
            return 0
        fi
        
        if [ $attempt -lt $max_attempts ]; then
            echo "Authentication failed. Retrying..."
            sleep 5
        fi
        
        attempt=$((attempt + 1))
    done
    
    echo "✗ Authentication failed after $max_attempts attempts" >&2
    return 1
}

authenticate_with_retry "myapp"
```

### Progress Feedback

Keep users informed during polling:

```bash {linenos=inline}
# CLI provides automatic progress indicators
# Example output:
Waiting for authentication... ⠋
# Updates every second with spinner animation
```

## Next Steps

- [Interactive Browser Flow](/docs/oauth-flows/interactive-browser/) - Alternative user authentication
- [Client Credentials Flow](/docs/oauth-flows/client-credentials/) - Service authentication
- [Platform Guides](/docs/platform-guides/) - Platform-specific authentication
- [Troubleshooting](/docs/troubleshooting/authentication/) - Common authentication issues
