---
title: "Linux"
description: "Linux-specific configuration and secure storage"
weight: 30
---

# Linux Platform Guide

Complete guide for using Entra Auth Cli on Linux, including secure token storage, distribution-specific installation, and Linux-specific features.

## Overview

Entra Auth Cli provides secure token storage and management on Linux:

- **Encrypted Storage**: XOR-based encryption with user-specific keys
- **Distribution Support**: Works on Ubuntu, Debian, RHEL, Fedora, Arch, and more
- **Container Ready**: Optimized for Docker and Kubernetes
- **Shell Integration**: Compatible with bash, zsh, fish, and more

## Installation

### Debian/Ubuntu (APT)

```bash {linenos=inline}
# Download .deb package
wget https://github.com/garrardkitchen/entra-auth-cli/releases/latest/download/entra-auth-cli_amd64.deb

# Install
sudo dpkg -i entra-auth-cli_amd64.deb

# Or install directly
curl -L https://github.com/garrardkitchen/entra-auth-cli/releases/latest/download/entra-auth-cli_amd64.deb \
  -o /tmp/entra-auth-cli.deb && \
  sudo dpkg -i /tmp/entra-auth-cli.deb

# Verify
entra-auth-cli --version
```

### RHEL/CentOS/Fedora (RPM)

```bash {linenos=inline}
# Download .rpm package
wget https://github.com/garrardkitchen/entra-auth-cli/releases/latest/download/entra-auth-cli-1.0.0-1.x86_64.rpm

# Install
sudo rpm -ivh entra-auth-cli-1.0.0-1.x86_64.rpm

# Or use dnf
sudo dnf install https://github.com/garrardkitchen/entra-auth-cli/releases/latest/download/entra-auth-cli-1.0.0-1.x86_64.rpm

# Verify
entra-auth-cli --version
```

### Arch Linux (AUR)

```bash {linenos=inline}
# Using yay
yay -S entra-auth-cli

# Using paru
paru -S entra-auth-cli

# Manual from AUR
git clone https://aur.archlinux.org/entra-auth-cli.git
cd entra-auth-cli
makepkg -si
```

### Universal Binary

```bash {linenos=inline}
# Download binary
curl -L https://github.com/garrardkitchen/entra-auth-cli/releases/latest/download/entra-auth-cli-linux-amd64 \
  -o /usr/local/bin/entra-auth-cli

# Make executable
chmod +x /usr/local/bin/entra-auth-cli

# Verify
entra-auth-cli --version
```

### From Source

```bash {linenos=inline}
# Prerequisites
sudo apt-get install golang-go  # Debian/Ubuntu
sudo dnf install golang          # Fedora/RHEL
sudo pacman -S go                # Arch

# Clone and build
git clone https://github.com/garrardkitchen/entra-auth-cli.git
cd entra-auth-cli
go build -o entra-auth-cli ./cmd/entra-auth-cli

# Install
sudo mv entra-auth-cli /usr/local/bin/
```

## Token Storage

### Storage Location

```bash {linenos=inline}
# Tokens stored in user's home directory
~/.entra-auth-cli/
├── profiles/
│   ├── default.json        # Profile configuration
│   ├── default.token       # Encrypted token
│   ├── production.json
│   └── production.token
└── config.json             # Global configuration

# Check storage
ls -la ~/.entra-auth-cli/profiles/
```

### Encryption

Tokens are encrypted using XOR with a user-specific key:

```bash {linenos=inline}
# Key derived from:
# - User ID (UID)
# - Machine ID (/etc/machine-id)
# - Home directory path

# View machine ID
cat /etc/machine-id

# View user ID
id -u

# Tokens cannot be decrypted:
# - On different machine
# - By different user
# - If machine-id changes
```

### File Permissions

```bash {linenos=inline}
# Verify secure permissions
ls -la ~/.entra-auth-cli/profiles/

# Should show:
# -rw------- (600) - Only owner can read/write
# drwx------ (700) - Only owner can access directory

# Fix permissions if needed
chmod 700 ~/.entra-auth-cli/profiles/
chmod 600 ~/.entra-auth-cli/profiles/*
```

### SELinux Context

```bash {linenos=inline}
# For SELinux-enabled systems (RHEL/CentOS/Fedora)

# Check context
ls -Z ~/.entra-auth-cli/

# Set correct context
chcon -R -t user_home_t ~/.entra-auth-cli/

# Make permanent
semanage fcontext -a -t user_home_t "~/.entra-auth-cli(/.*)?"
restorecon -R ~/.entra-auth-cli/
```

## Shell Integration

### Bash

```bash {linenos=inline}
# Add to ~/.bashrc

# Completion
complete -C entra-auth-cli entra-auth-cli

# Aliases
alias et='entra-auth-cli'
alias etg='entra-auth-cli get-token'
alias etp='entra-auth-cli config list'

# Function to get token
get_token() {
    local profile="${1:-default}"
    entra-auth-cli get-token --profile "$profile" --output json | jq -r .access_token
}

# Function to export token
export_token() {
    local profile="${1:-default}"
    export ENTRA_TOKEN=$(get_token "$profile")
    echo "Token exported to \$ENTRA_TOKEN"
}

# Graph API helper
graph() {
    local endpoint="$1"
    local token=$(get_token)
    curl -s -H "Authorization: Bearer $token" \
      "https://graph.microsoft.com/v1.0/$endpoint" | jq .
}
```

### Zsh

```zsh
# Add to ~/.zshrc

# Completion
autoload -U compinit && compinit
complete -o nospace -C entra-auth-cli entra-auth-cli

# Aliases
alias et='entra-auth-cli'
alias etg='entra-auth-cli get-token'

# Functions
get_token() {
    entra-auth-cli get-token --profile "${1:-default}" --output json | jq -r .access_token
}

# Prompt integration (show if token is valid)
precmd() {
    if entra-auth-cli inspect &>/dev/null; then
        RPROMPT="%F{green}🔑%f"
    else
        RPROMPT="%F{red}🔑%f"
    fi
}
```

### Fish

```fish
# Add to ~/.config/fish/config.fish

# Completion
complete -c entra-auth-cli -f

# Aliases
alias et='entra-auth-cli'
alias etg='entra-auth-cli get-token'

# Function
function get_token
    set -l profile (test -n "$argv[1]"; and echo $argv[1]; or echo "default")
    entra-auth-cli get-token --profile $profile --output json | jq -r .access_token
end

# Export function
function export_token
    set -l profile (test -n "$argv[1]"; and echo $argv[1]; or echo "default")
    set -gx ENTRA_TOKEN (get_token $profile)
    echo "Token exported to \$ENTRA_TOKEN"
end
```

## Linux-Specific Features

### Systemd Service

```ini
# /etc/systemd/system/entra-auth-cli-refresh.service
[Unit]
Description=Entra Auth Cli Token Refresh
After=network.target

[Service]
Type=oneshot
User=%i
ExecStart=/usr/local/bin/entra-auth-cli refresh --profile production
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target
```

```ini
# /etc/systemd/system/entra-auth-cli-refresh.timer
[Unit]
Description=Refresh Entra tokens hourly
Requires=entra-auth-cli-refresh.service

[Timer]
OnBootSec=5min
OnUnitActiveSec=1h
Unit=entra-auth-cli-refresh.service

[Install]
WantedBy=timers.target
```

```bash {linenos=inline}
# Enable and start
sudo systemctl daemon-reload
sudo systemctl enable entra-auth-cli-refresh@$USER.timer
sudo systemctl start entra-auth-cli-refresh@$USER.timer

# Check status
systemctl status entra-auth-cli-refresh@$USER.timer
systemctl list-timers --all | grep entra-auth-cli

# View logs
journalctl -u entra-auth-cli-refresh@$USER.service
```

### Cron Jobs

```bash {linenos=inline}
# Add to crontab
crontab -e

# Refresh token every hour
0 * * * * /usr/local/bin/entra-auth-cli refresh --profile production >> /var/log/entra-auth-cli-refresh.log 2>&1

# Refresh at specific times
0 8,12,17 * * * /usr/local/bin/entra-auth-cli refresh --profile work

# With environment
0 * * * * . $HOME/.profile; /usr/local/bin/entra-auth-cli refresh --profile production
```

### Environment Modules

```bash {linenos=inline}
# For HPC/cluster environments with environment modules

# /opt/modulefiles/entra-auth-cli/1.0
#%Module1.0
proc ModulesHelp { } {
    puts stderr "Entra Auth Cli - Microsoft Entra ID token management"
}

module-whatis "Microsoft Entra ID token management tool"

prepend-path PATH /opt/entra-auth-cli/bin
setenv ENTRA_TOKEN_HOME /opt/entra-auth-cli

# Load module
module load entra-auth-cli
```

### Container Integration

#### Docker

```dockerfile
FROM ubuntu:22.04

# Install dependencies
RUN apt-get update && \
    apt-get install -y curl jq && \
    rm -rf /var/lib/apt/lists/*

# Install Entra Auth Cli
RUN curl -L https://github.com/garrardkitchen/entra-auth-cli/releases/latest/download/entra-auth-cli-linux-amd64 \
    -o /usr/local/bin/entra-auth-cli && \
    chmod +x /usr/local/bin/entra-auth-cli

# Create non-root user
RUN useradd -m -s /bin/bash appuser
USER appuser

# Set up profiles directory
RUN mkdir -p /home/appuser/.entra-auth-cli/profiles

# Copy entrypoint
COPY entrypoint.sh /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]
```

```bash {linenos=inline}
#!/bin/bash
# entrypoint.sh

# Create profile from environment variables
if [ -n "$TENANT_ID" ] && [ -n "$CLIENT_ID" ] && [ -n "$CLIENT_SECRET" ]; then
    entra-auth-cli config create \
        --name container \
        --tenant-id "$TENANT_ID" \
        --client-id "$CLIENT_ID" \
        --client-secret "$CLIENT_SECRET" \
        --scope "${SCOPE:-https://graph.microsoft.com/.default}"
fi

# Get token
export TOKEN=$(entra-auth-cli get-token --profile container --output json | jq -r .access_token)

# Execute main command
exec "$@"
```

#### Kubernetes

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: entra-auth-cli-config
data:
  config create.sh: |
    #!/bin/bash
    entra-auth-cli config create \
      --name k8s \
      --tenant-id "$TENANT_ID" \
      --client-id "$CLIENT_ID" \
      --client-secret "$CLIENT_SECRET"
---
apiVersion: v1
kind: Secret
metadata:
  name: azure-credentials
type: Opaque
stringData:
  tenant-id: "your-tenant-id"
  client-id: "your-client-id"
  client-secret: "your-client-secret"
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: app-with-entra-auth-cli
spec:
  replicas: 1
  selector:
    matchLabels:
      app: myapp
  template:
    metadata:
      labels:
        app: myapp
    spec:
      containers:
      - name: app
        image: myapp:latest
        env:
        - name: TENANT_ID
          valueFrom:
            secretKeyRef:
              name: azure-credentials
              key: tenant-id
        - name: CLIENT_ID
          valueFrom:
            secretKeyRef:
              name: azure-credentials
              key: client-id
        - name: CLIENT_SECRET
          valueFrom:
            secretKeyRef:
              name: azure-credentials
              key: client-secret
        volumeMounts:
        - name: entra-auth-cli-config
          mountPath: /config
        command:
        - /bin/bash
        - -c
        - |
          /config/config create.sh
          TOKEN=$(entra-auth-cli get-token --output json | jq -r .access_token)
          export AUTH_TOKEN=$TOKEN
          exec /app/start.sh
      volumes:
      - name: entra-auth-cli-config
        configMap:
          name: entra-auth-cli-config
          defaultMode: 0755
```

## Common Use Cases

### CI/CD Integration

#### GitLab CI

```yaml
# .gitlab-ci.yml
stages:
  - deploy

deploy:
  stage: deploy
  image: ubuntu:22.04
  before_script:
    - apt-get update && apt-get install -y curl jq
    - curl -L https://github.com/garrardkitchen/entra-auth-cli/releases/latest/download/entra-auth-cli-linux-amd64 
        -o /usr/local/bin/entra-auth-cli
    - chmod +x /usr/local/bin/entra-auth-cli
    - |
      entra-auth-cli config create \
        --name ci \
        --tenant-id "$AZURE_TENANT_ID" \
        --client-id "$AZURE_CLIENT_ID" \
        --client-secret "$AZURE_CLIENT_SECRET"
  script:
    - TOKEN=$(entra-auth-cli get-token --profile ci --output json | jq -r .access_token)
    - echo "Deploying with token..."
    - curl -H "Authorization: Bearer $TOKEN" https://api.example.com/deploy
  only:
    - main
```

#### Jenkins

```groovy
// Jenkinsfile
pipeline {
    agent any
    
    environment {
        TENANT_ID = credentials('azure-tenant-id')
        CLIENT_ID = credentials('azure-client-id')
        CLIENT_SECRET = credentials('azure-client-secret')
    }
    
    stages {
        stage('Setup') {
            steps {
                sh '''
                    curl -L https://github.com/garrardkitchen/entra-auth-cli/releases/latest/download/entra-auth-cli-linux-amd64 \
                      -o /tmp/entra-auth-cli
                    chmod +x /tmp/entra-auth-cli
                    sudo mv /tmp/entra-auth-cli /usr/local/bin/
                '''
            }
        }
        
        stage('Authenticate') {
            steps {
                sh '''
                    entra-auth-cli config create \
                      --name jenkins \
                      --tenant-id "$TENANT_ID" \
                      --client-id "$CLIENT_ID" \
                      --client-secret "$CLIENT_SECRET"
                '''
            }
        }
        
        stage('Deploy') {
            steps {
                sh '''
                    TOKEN=$(entra-auth-cli get-token --profile jenkins --output json | jq -r .access_token)
                    curl -H "Authorization: Bearer $TOKEN" https://api.example.com/deploy
                '''
            }
        }
    }
}
```

### SSH Remote Execution

```bash {linenos=inline}
# Execute on remote server with token
ssh user@server "$(cat <<'EOF'
TOKEN=$(entra-auth-cli get-token --profile production --output json | jq -r .access_token)
curl -H "Authorization: Bearer $TOKEN" https://graph.microsoft.com/v1.0/me
EOF
)"

# Deploy profile to remote server
scp ~/.entra-auth-cli/profiles/production.* user@server:~/.entra-auth-cli/profiles/

# Execute script remotely
ssh user@server 'bash -s' < local-script.sh
```

### Ansible Playbook

```yaml
# playbook.yml
---
- name: Deploy application with Entra Token
  hosts: webservers
  tasks:
    - name: Install Entra Auth Cli
      get_url:
        url: https://github.com/garrardkitchen/entra-auth-cli/releases/latest/download/entra-auth-cli-linux-amd64
        dest: /usr/local/bin/entra-auth-cli
        mode: '0755'
      become: yes

    - name: Create profile
      shell: |
        entra-auth-cli config create \
          --name ansible \
          --tenant-id "{{ azure_tenant_id }}" \
          --client-id "{{ azure_client_id }}" \
          --client-secret "{{ azure_client_secret }}"
      no_log: true

    - name: Get token
      shell: entra-auth-cli get-token --profile ansible --output json
      register: token_output

    - name: Deploy application
      uri:
        url: https://api.example.com/deploy
        method: POST
        headers:
          Authorization: "Bearer {{ (token_output.stdout | from_json).access_token }}"
        body_format: json
```

## Security Best Practices

### File System Permissions

```bash {linenos=inline}
# Secure home directory
chmod 700 ~

# Secure entra-auth-cli directory
chmod 700 ~/.entra-auth-cli
chmod 700 ~/.entra-auth-cli/profiles
chmod 600 ~/.entra-auth-cli/profiles/*

# Verify
ls -la ~/.entra-auth-cli/profiles/
```

### AppArmor Profile

```bash {linenos=inline}
# /etc/apparmor.d/usr.local.bin.entra-auth-cli
#include <tunables/global>

/usr/local/bin/entra-auth-cli {
  #include <abstractions/base>
  #include <abstractions/nameservice>
  #include <abstractions/ssl_certs>

  /usr/local/bin/entra-auth-cli mr,
  
  owner @{HOME}/.entra-auth-cli/ rw,
  owner @{HOME}/.entra-auth-cli/** rw,
  
  /etc/machine-id r,
  /proc/sys/kernel/random/uuid r,
  
  network inet stream,
  network inet6 stream,
}

# Load profile
sudo apparmor_parser -r /etc/apparmor.d/usr.local.bin.entra-auth-cli
```

### Firewall Rules

```bash {linenos=inline}
# Allow outbound HTTPS to Microsoft Entra ID
sudo ufw allow out 443/tcp

# Restrict to Microsoft IPs (optional)
sudo iptables -A OUTPUT -p tcp -d login.microsoftonline.com --dport 443 -j ACCEPT
sudo iptables -A OUTPUT -p tcp --dport 443 -j DROP
```

## Troubleshooting

### Permission Denied

**Problem:** Cannot read/write token files

**Solutions:**

```bash {linenos=inline}
# Check ownership
ls -la ~/.entra-auth-cli/profiles/

# Fix ownership
chown -R $USER:$USER ~/.entra-auth-cli/

# Fix permissions
chmod 700 ~/.entra-auth-cli/profiles/
chmod 600 ~/.entra-auth-cli/profiles/*
```

### Machine ID Changed

**Problem:** "Cannot decrypt token" after system reinstall

**Solution:**

```bash {linenos=inline}
# Machine ID changed, tokens are invalid
# Delete old tokens and re-authenticate
rm ~/.entra-auth-cli/profiles/*.token

# Get new token
entra-auth-cli get-token --flow interactive
```

### SELinux Denial

**Problem:** SELinux blocks token access

**Solutions:**

```bash {linenos=inline}
# Check denials
sudo ausearch -m avc -ts recent | grep entra-auth-cli

# Allow access
sudo audit2allow -a -M entra-auth-cli
sudo semodule -i entra-auth-cli.pp

# Or set correct context
sudo chcon -R -t user_home_t ~/.entra-auth-cli/
```

### Missing Dependencies

**Problem:** "jq: command not found"

**Solutions:**

```bash {linenos=inline}
# Ubuntu/Debian
sudo apt-get install jq

# RHEL/CentOS/Fedora
sudo dnf install jq

# Arch
sudo pacman -S jq
```

## Performance Optimization

### Token Caching

```bash {linenos=inline}
# Cache token in tmpfs (RAM disk)
CACHE_DIR="/dev/shm/entra-auth-cli-$$"
mkdir -p "$CACHE_DIR"
chmod 700 "$CACHE_DIR"

# Get and cache token
entra-auth-cli get-token --output json > "$CACHE_DIR/token.json"

# Use cached token
TOKEN=$(jq -r .access_token "$CACHE_DIR/token.json")

# Cleanup on exit
trap "rm -rf $CACHE_DIR" EXIT
```

### Parallel Processing

```bash {linenos=inline}
# Parallel API calls with GNU parallel
TOKEN=$(entra-auth-cli get-token --output json | jq -r .access_token)

parallel -j 10 "curl -s -H 'Authorization: Bearer $TOKEN' {}" ::: \
  https://graph.microsoft.com/v1.0/users \
  https://graph.microsoft.com/v1.0/groups \
  https://graph.microsoft.com/v1.0/applications \
  | jq -s .
```

## Next Steps

- [Windows Platform Guide](/docs/platform-guides/windows/) - Windows-specific features
- [macOS Platform Guide](/docs/platform-guides/macos/) - macOS-specific features
- [Bash Scripts](/docs/recipes/bash-scripts/) - Advanced shell scripting
- [CI/CD Integration](/docs/recipes/cicd-integration/) - Complete CI/CD examples
