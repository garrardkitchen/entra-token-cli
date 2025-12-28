---
title: "macOS"
description: "macOS-specific configuration and Keychain integration"
weight: 20
---

# macOS Platform Guide

Complete guide for using Entra Token CLI on macOS, including Keychain integration, Homebrew installation, and macOS-specific features.

## Overview

Entra Token CLI integrates seamlessly with macOS security features:

- **Keychain Integration**: Secure token storage in macOS Keychain
- **Native Security**: Leverages macOS security framework
- **Homebrew Support**: Easy installation and updates
- **Shell Integration**: Works with zsh, bash, and fish

## Installation

### Using Homebrew

```bash
# Add tap (if not already added)
brew tap garrardkitchen/entratool

# Install
brew install entratool-cli

# Update
brew upgrade entratool-cli
```

### Manual Installation

```bash
# Download latest release
VERSION="1.0.0"
curl -L "https://github.com/garrardkitchen/entratool-cli/releases/download/v${VERSION}/entratool-darwin-amd64" \
  -o /usr/local/bin/entratool

# Make executable
chmod +x /usr/local/bin/entratool

# Verify installation
entratool --version
```

### Intel vs Apple Silicon

```bash
# Apple Silicon (M1/M2/M3)
curl -L "https://github.com/garrardkitchen/entratool-cli/releases/latest/download/entratool-darwin-arm64" \
  -o /usr/local/bin/entratool

# Intel (x86_64)
curl -L "https://github.com/garrardkitchen/entratool-cli/releases/latest/download/entratool-darwin-amd64" \
  -o /usr/local/bin/entratool

# Universal Binary (works on both)
curl -L "https://github.com/garrardkitchen/entratool-cli/releases/latest/download/entratool-darwin-universal" \
  -o /usr/local/bin/entratool
```

## Token Storage

### macOS Keychain

Tokens are securely stored in macOS Keychain:

```bash
# Tokens stored in Keychain with:
# - Service: com.garrardkitchen.entratool-cli
# - Account: profile-name
# - Kind: Application password

# View in Keychain Access app
open -a "Keychain Access"
# Search for: entratool-cli
```

**Security characteristics:**
- Encrypted by macOS security framework
- Protected by user login password
- Synchronized via iCloud Keychain (optional)
- Requires user authorization for access

### Keychain Access Management

```bash
# List Entra Token CLI keychains
security find-generic-password -s "com.garrardkitchen.entratool-cli"

# View specific profile
security find-generic-password -s "com.garrardkitchen.entratool-cli" -a "default"

# Delete keychain entry
security delete-generic-password -s "com.garrardkitchen.entratool-cli" -a "default"

# Export keychain item (requires authorization)
security export -k ~/Library/Keychains/login.keychain-db \
  -t identities -f pkcs12 -o export.p12
```

### iCloud Keychain Sync

Enable token sync across your Apple devices:

```bash
# Check iCloud Keychain status
security show-keychain-info ~/Library/Keychains/login.keychain-db

# Tokens automatically sync if:
# 1. iCloud Keychain is enabled in System Settings
# 2. Same Apple ID on all devices
# 3. Two-factor authentication enabled

# View synced items
# System Settings > Passwords > entratool-cli
```

## Shell Integration

### Zsh (Default on macOS)

```zsh
# Add to ~/.zshrc

# Completion
autoload -U compinit && compinit
complete -o nospace -C entratool entratool

# Aliases
alias et='entratool'
alias etg='entratool get-token'
alias etp='entratool list-profiles'

# Function to get token
get_token() {
    local profile="${1:-default}"
    entratool get-token --profile "$profile" --output json | jq -r .access_token
}

# Function to call Microsoft Graph
graph_api() {
    local endpoint="$1"
    local token=$(get_token)
    curl -s -H "Authorization: Bearer $token" \
      "https://graph.microsoft.com/v1.0/$endpoint" | jq .
}
```

### Bash

```bash
# Add to ~/.bash_profile or ~/.bashrc

# Completion
complete -C entratool entratool

# Aliases
alias et='entratool'
alias etg='entratool get-token'

# Token helper
export_token() {
    local profile="${1:-default}"
    export ENTRA_TOKEN=$(entratool get-token --profile "$profile" --output json | jq -r .access_token)
    echo "Token exported to \$ENTRA_TOKEN"
}
```

### Fish Shell

```fish
# Add to ~/.config/fish/config.fish

# Completion
complete -c entratool -f

# Aliases
alias et='entratool'
alias etg='entratool get-token'

# Function
function get_token
    set -l profile (test -n "$argv[1]"; and echo $argv[1]; or echo "default")
    entratool get-token --profile $profile --output json | jq -r .access_token
end
```

## macOS-Specific Features

### Notification Center

```bash
# Show notification on token refresh
refresh_with_notification() {
    if entratool refresh --profile "$1"; then
        osascript -e 'display notification "Token refreshed successfully" with title "Entra Token CLI"'
    else
        osascript -e 'display notification "Token refresh failed" with title "Entra Token CLI" sound name "Basso"'
    fi
}

# Usage
refresh_with_notification production
```

### Launch Agent

Automate token refresh using Launch Agent:

```xml
<!-- ~/Library/LaunchAgents/com.entratool.refresh.plist -->
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.entratool.refresh</string>
    <key>ProgramArguments</key>
    <array>
        <string>/usr/local/bin/entratool</string>
        <string>refresh</string>
        <string>--profile</string>
        <string>production</string>
    </array>
    <key>StartInterval</key>
    <integer>3600</integer><!-- Run every hour -->
    <key>RunAtLoad</key>
    <true/>
    <key>StandardErrorPath</key>
    <string>/tmp/entratool-refresh.err</string>
    <key>StandardOutPath</key>
    <string>/tmp/entratool-refresh.out</string>
</dict>
</plist>
```

```bash
# Load Launch Agent
launchctl load ~/Library/LaunchAgents/com.entratool.refresh.plist

# Unload
launchctl unload ~/Library/LaunchAgents/com.entratool.refresh.plist

# Check status
launchctl list | grep entratool

# View logs
tail -f /tmp/entratool-refresh.out
```

### Spotlight Integration

```bash
# Add entratool to Spotlight search
# Create Alfred/Raycast workflow

# Example: Alfred Workflow Script
cat << 'EOF' > ~/Library/Application\ Support/Alfred/Alfred.alfredpreferences/workflows/entratool/get-token.sh
#!/bin/bash
profile="${1:-default}"
token=$(entratool get-token --profile "$profile" --output json | jq -r .access_token)
echo -n "$token" | pbcopy
echo "Token copied to clipboard!"
EOF

chmod +x ~/Library/Application\ Support/Alfred/Alfred.alfredpreferences/workflows/entratool/get-token.sh
```

### Touch Bar Support

```bash
# Add Entra Token CLI to Touch Bar using BetterTouchTool
# Quick actions:
# - Get Token (copies to clipboard)
# - Refresh Token
# - List Profiles
```

## Common Use Cases

### Clipboard Integration

```bash
# Copy token to clipboard
get_token_clipboard() {
    local profile="${1:-default}"
    entratool get-token --profile "$profile" --output json | \
      jq -r .access_token | \
      pbcopy
    echo "✓ Token copied to clipboard"
}

# Usage
get_token_clipboard production

# Use in next command
curl -H "Authorization: Bearer $(pbpaste)" \
  https://graph.microsoft.com/v1.0/me
```

### Shortcuts Integration

```applescript
-- Create macOS Shortcut for token retrieval
on run
    set token to do shell script "/usr/local/bin/entratool get-token --output json | jq -r .access_token"
    return token
end run
```

### Script Menu

```bash
# Add to ~/Library/Scripts/
# Appears in Script menu (if enabled)

#!/bin/bash
# Get Entra Token.scpt

osascript << 'EOF'
tell application "Terminal"
    activate
    do script "entratool get-token --flow interactive"
end tell
EOF
```

### Automator Workflows

```applescript
-- Automator Service: Get Entra Token
on run {input, parameters}
    set token to do shell script "/usr/local/bin/entratool get-token --output json | jq -r .access_token"
    set the clipboard to token
    
    display notification "Token copied to clipboard" with title "Entra Token CLI"
    
    return input
end run
```

## Security Best Practices

### FileVault Integration

```bash
# Ensure FileVault is enabled for disk encryption
sudo fdesetup status

# Enable if not active
sudo fdesetup enable
```

### Keychain Security

```bash
# Set Keychain to lock after inactivity
security set-keychain-settings -t 3600 ~/Library/Keychains/login.keychain-db

# Require password after sleep
sudo pmset -a destroyfvkeyonstandby 1
sudo pmset -a hibernatemode 25
sudo pmset -a powernap 0
sudo pmset -a standby 0
sudo pmset -a standbydelay 0
sudo pmset -a autopoweroff 0
```

### Secure Profile Creation

```bash
# Use environment variables from secure source
export TENANT_ID=$(security find-generic-password -s "azure-tenant-id" -w)
export CLIENT_ID=$(security find-generic-password -s "azure-client-id" -w)
export CLIENT_SECRET=$(security find-generic-password -s "azure-client-secret" -w)

entratool create-profile \
  --name secure-profile \
  --tenant-id "$TENANT_ID" \
  --client-id "$CLIENT_ID" \
  --client-secret "$CLIENT_SECRET"

# Clear variables
unset TENANT_ID CLIENT_ID CLIENT_SECRET
```

## Docker Integration

### Docker for Mac

```bash
# Use Entra Token CLI from Docker container
docker run -it --rm \
  -v ~/Library/Keychains:/root/Library/Keychains:ro \
  -e HOME=/root \
  ubuntu:22.04 bash

# Inside container (Keychain available via volume mount)
# Note: This is for illustration; prefer host authentication
```

### Docker Compose

```yaml
version: '3.8'

services:
  app:
    image: myapp:latest
    environment:
      - ENTRA_TOKEN_PROFILE=production
    volumes:
      - $HOME/.entratool:/root/.entratool:ro
    command: |
      sh -c "
        TOKEN=$$(entratool get-token --output json | jq -r .access_token) &&
        ./myapp --token $$TOKEN
      "
```

## Troubleshooting

### Keychain Access Prompt

**Problem:** Repeated authorization prompts

**Solutions:**

```bash
# Allow entratool always
# Click "Always Allow" in Keychain prompt

# Or programmatically:
security set-generic-password-partition-list \
  -s com.garrardkitchen.entratool-cli \
  -k ~/Library/Keychains/login.keychain-db

# Reset Keychain permissions
security delete-generic-password -s "com.garrardkitchen.entratool-cli"
entratool get-token  # Creates new entry
```

### Gatekeeper Warnings

**Problem:** "entratool cannot be opened because the developer cannot be verified"

**Solutions:**

```bash
# Remove quarantine attribute
xattr -d com.apple.quarantine /usr/local/bin/entratool

# Or allow in System Settings
# System Settings > Privacy & Security > Security
# Click "Allow Anyway" next to blocked message

# Verify removal
xattr -l /usr/local/bin/entratool
```

### Path Issues

**Problem:** Command not found

**Solutions:**

```bash
# Check if installed
which entratool

# Add to PATH in shell config
echo 'export PATH="/usr/local/bin:$PATH"' >> ~/.zshrc
source ~/.zshrc

# Or use Homebrew path
echo 'export PATH="/opt/homebrew/bin:$PATH"' >> ~/.zshrc  # Apple Silicon
echo 'export PATH="/usr/local/bin:$PATH"' >> ~/.zshrc     # Intel

# Verify
entratool --version
```

### Architecture Mismatch

**Problem:** "Bad CPU type in executable"

**Solutions:**

```bash
# Check system architecture
uname -m
# arm64 = Apple Silicon
# x86_64 = Intel

# Download correct version
# Apple Silicon
curl -L ".../entratool-darwin-arm64" -o /usr/local/bin/entratool

# Intel
curl -L ".../entratool-darwin-amd64" -o /usr/local/bin/entratool

# Use Rosetta 2 (temporary workaround)
arch -x86_64 /usr/local/bin/entratool
```

## Performance Optimization

### Token Caching

```bash
# Cache token in memory for session
export ENTRA_TOKEN_CACHE=$(entratool get-token --output json)

# Use cached token
echo "$ENTRA_TOKEN_CACHE" | jq -r .access_token

# Clear cache
unset ENTRA_TOKEN_CACHE
```

### Parallel Requests

```bash
# Parallel API calls with GNU parallel
TOKEN=$(entratool get-token --output json | jq -r .access_token)

parallel -j 4 "curl -s -H 'Authorization: Bearer $TOKEN' {}" ::: \
  "https://graph.microsoft.com/v1.0/users" \
  "https://graph.microsoft.com/v1.0/groups" \
  "https://graph.microsoft.com/v1.0/applications" \
  "https://graph.microsoft.com/v1.0/servicePrincipals" \
  | jq -s .
```

### Keychain Optimization

```bash
# Reduce Keychain search time
# Create separate keychain for entratool
security create-keychain -p "" entratool.keychain
security set-keychain-settings entratool.keychain
security unlock-keychain entratool.keychain

# Add to search list
security list-keychains -s entratool.keychain login.keychain

# Move tokens to dedicated keychain
# (Requires manual reconfiguration)
```

## Advanced Features

### Menu Bar App

```swift
// SwiftUI Menu Bar App wrapper
import SwiftUI
import AppKit

@main
struct EntraTokenApp: App {
    @NSApplicationDelegateAdaptor(AppDelegate.self) var appDelegate
    
    var body: some Scene {
        Settings {
            EmptyView()
        }
    }
}

class AppDelegate: NSObject, NSApplicationDelegate {
    var statusItem: NSStatusItem!
    var menu: NSMenu!
    
    func applicationDidFinishLaunching(_ notification: Notification) {
        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        statusItem.button?.title = "🔑"
        
        menu = NSMenu()
        menu.addItem(NSMenuItem(title: "Get Token", action: #selector(getToken), keyEquivalent: "t"))
        menu.addItem(NSMenuItem(title: "Refresh", action: #selector(refresh), keyEquivalent: "r"))
        menu.addItem(NSMenuItem.separator())
        menu.addItem(NSMenuItem(title: "Quit", action: #selector(quit), keyEquivalent: "q"))
        
        statusItem.menu = menu
    }
    
    @objc func getToken() {
        let task = Process()
        task.launchPath = "/usr/local/bin/entratool"
        task.arguments = ["get-token", "--output", "json"]
        
        let pipe = Pipe()
        task.standardOutput = pipe
        task.launch()
        
        let data = pipe.fileHandleForReading.readDataToEndOfFile()
        if let output = String(data: data, encoding: .utf8),
           let token = try? JSONDecoder().decode([String: String].self, from: output.data(using: .utf8)!)["access_token"] {
            NSPasteboard.general.clearContents()
            NSPasteboard.general.setString(token, forType: .string)
            
            let notification = NSUserNotification()
            notification.title = "Entra Token"
            notification.informativeText = "Token copied to clipboard"
            NSUserNotificationCenter.default.deliver(notification)
        }
    }
    
    @objc func refresh() {
        // Implement refresh logic
    }
    
    @objc func quit() {
        NSApplication.shared.terminate(nil)
    }
}
```

## Next Steps

- [Windows Platform Guide](/docs/platform-guides/windows/) - Windows-specific features
- [Linux Platform Guide](/docs/platform-guides/linux/) - Linux-specific features
- [Bash Scripts](/docs/recipes/bash-scripts/) - Shell scripting examples
- [Security Hardening](/docs/recipes/security-hardening/) - Security best practices
