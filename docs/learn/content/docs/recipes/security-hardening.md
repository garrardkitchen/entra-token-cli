---
title: "Security Hardening"
description: "Production security best practices and hardening guidelines"
weight: 6
---

# Security Hardening

Learn how to secure your Entra Token CLI deployments for production environments.

---

## Secure Token Storage

### Use Temporary Files with Restricted Permissions

```bash
#!/bin/bash
set -euo pipefail

# Use secure temp file
TOKEN_FILE=$(mktemp)
trap "rm -f $TOKEN_FILE" EXIT

# Get token with restricted permissions
entratool get-token -p my-profile --silent > "$TOKEN_FILE"
chmod 600 "$TOKEN_FILE"

# Use token
TOKEN=$(cat "$TOKEN_FILE")
curl -H "Authorization: Bearer $TOKEN" https://api.example.com

# File automatically deleted on exit
```

### Never Log Tokens

```bash
# Bad: Token in logs
echo "Token: $TOKEN"
logger "Using token: $TOKEN"

# Good: Redact sensitive data
echo "Token acquired successfully"
logger "Authentication completed"
```

---

## Profile Separation

### Environment-Specific Profiles

Create separate profiles for each environment:

```bash
# Development
entratool config create
# Name: dev-graph
# Client ID: <dev-app-id>
# Tenant ID: <dev-tenant-id>
# Scope: https://graph.microsoft.com/User.Read

# Staging
entratool config create
# Name: staging-graph
# Client ID: <staging-app-id>
# Tenant ID: <staging-tenant-id>
# Scope: https://graph.microsoft.com/User.Read

# Production
entratool config create
# Name: prod-graph
# Client ID: <prod-app-id>
# Tenant ID: <prod-tenant-id>
# Scope: https://graph.microsoft.com/.default
```

### Never Mix Environments

```bash
# Good: Clear environment separation
entratool get-token -p dev-graph      # Development
entratool get-token -p prod-graph     # Production

# Bad: Shared profiles across environments
entratool get-token -p shared-graph   # Risk of production data exposure
```

---

## Secret Rotation

### Regular Rotation Schedule

```bash
#!/bin/bash

rotate_secret() {
  local profile=$1
  local new_secret=$2
  
  echo "Rotating secret for profile: $profile"
  
  # Update profile with new secret
  entratool config edit -p "$profile" <<EOF
3
$new_secret
q
EOF
  
  # Test new secret
  if entratool get-token -p "$profile" --silent > /dev/null; then
    echo "✓ Secret rotation successful"
    
    # Optionally revoke old secret in Azure Portal
    echo "Don't forget to revoke old secret in Azure Portal"
    return 0
  else
    echo "✗ Secret rotation failed"
    echo "Rolling back..."
    return 1
  fi
}

# Usage
NEW_SECRET=$(az keyvault secret show --name prod-secret --vault-name my-vault --query value -o tsv)
rotate_secret "prod-service-principal" "$NEW_SECRET"
```

### Automated Rotation

```bash
#!/bin/bash
set -euo pipefail

# Rotation interval: 90 days
ROTATION_INTERVAL_DAYS=90
SECRET_NAME="entratool-prod-secret"
KEY_VAULT="my-prod-vault"

# Check last rotation date
last_rotation=$(az keyvault secret show \
  --name "$SECRET_NAME-last-rotation" \
  --vault-name "$KEY_VAULT" \
  --query value -o tsv 2>/dev/null || echo "0")

current_date=$(date +%s)
days_since_rotation=$(( (current_date - last_rotation) / 86400 ))

if [ $days_since_rotation -gt $ROTATION_INTERVAL_DAYS ]; then
  echo "Secret is $days_since_rotation days old, rotating..."
  
  # Generate new secret in Azure
  new_secret=$(az ad app credential reset \
    --id <app-id> \
    --append \
    --query password -o tsv)
  
  # Update profile
  rotate_secret "prod-profile" "$new_secret"
  
  # Record rotation date
  az keyvault secret set \
    --name "$SECRET_NAME-last-rotation" \
    --vault-name "$KEY_VAULT" \
    --value "$current_date"
else
  echo "Secret is $days_since_rotation days old, no rotation needed"
fi
```

---

## Least Privilege

### Scope Restriction

Always use the minimum required scopes:

```bash
# Good: Specific scope
entratool get-token -p my-profile \
  --scope "https://graph.microsoft.com/User.Read"

# Bad: Over-permissive
entratool get-token -p my-profile \
  --scope "https://graph.microsoft.com/.default"  # Grants all consented permissions
```

### Service Principal RBAC

Assign minimum required Azure RBAC roles:

```bash
# Good: Specific role
az ad sp create-for-rbac --name "entratool-deployment" \
  --role "Contributor" \
  --scopes "/subscriptions/xxx/resourceGroups/my-rg"

# Bad: Over-permissive
az ad sp create-for-rbac --name "entratool-deployment" \
  --role "Owner" \
  --scopes "/subscriptions/xxx"  # Full subscription access
```

---

## Certificate Security

### Use Certificates for Production

Prefer certificates over client secrets for production:

```bash
# Create certificate with strong key
openssl req -x509 -newkey rsa:4096 \
  -keyout key.pem -out cert.pem \
  -days 730 -nodes \
  -subj "/CN=MyProductionApp"

# Convert to PFX
openssl pkcs12 -export \
  -in cert.pem -inkey key.pem \
  -out certificate.pfx \
  -password pass:$(openssl rand -base64 32)
```

### Secure Certificate Storage

```bash
# Create secure directory
mkdir -p ~/.entratool/certs
chmod 700 ~/.entratool/certs

# Store certificate with restricted permissions
cp certificate.pfx ~/.entratool/certs/
chmod 600 ~/.entratool/certs/certificate.pfx

# Never store in git
echo "*.pfx" >> .gitignore
echo "*.p12" >> .gitignore
```

---

## Network Security

### Use HTTPS Only

```bash
# Good: HTTPS
curl -H "Authorization: Bearer $TOKEN" \
  https://graph.microsoft.com/v1.0/me

# Bad: HTTP (never use)
curl -H "Authorization: Bearer $TOKEN" \
  http://api.example.com/data  # Token exposed in plaintext
```

### Proxy Configuration

```bash
# Use corporate proxy
export HTTPS_PROXY="https://proxy.corp.com:8080"
export HTTP_PROXY="http://proxy.corp.com:8080"

entratool get-token -p my-profile
```

---

## Audit and Monitoring

### Log Authentication Attempts

```bash
#!/bin/bash

log_auth() {
  local profile=$1
  local status=$2
  
  logger -t entratool \
    "Authentication attempt: profile=$profile status=$status user=$(whoami) host=$(hostname)"
}

# Get token with logging
if TOKEN=$(entratool get-token -p prod-profile --silent); then
  log_auth "prod-profile" "success"
else
  log_auth "prod-profile" "failure"
  exit 1
fi
```

### Monitor Token Usage

```bash
#!/bin/bash

# Track token acquisition
echo "$(date +%s),$(whoami),prod-profile,acquired" >> /var/log/token-usage.csv

# Alert on unusual patterns
token_count=$(grep -c "$(date +%Y-%m-%d)" /var/log/token-usage.csv)
if [ $token_count -gt 100 ]; then
  # Send alert
  echo "High token usage detected: $token_count requests today" | \
    mail -s "Token Usage Alert" security@example.com
fi
```

---

## Multi-Environment Setup

### Directory Structure

```
~/.entratool/
├── dev/
│   ├── profiles.json
│   └── certs/
├── staging/
│   ├── profiles.json
│   └── certs/
└── prod/
    ├── profiles.json
    └── certs/
```

### Environment-Specific Configuration

```bash
#!/bin/bash

# Set environment
ENVIRONMENT=${1:-dev}
export ENTRATOOL_CONFIG_PATH="$HOME/.entratool/$ENVIRONMENT"

# Ensure directory exists
mkdir -p "$ENTRATOOL_CONFIG_PATH"

# Get token
entratool get-token -p "$ENVIRONMENT-profile"
```

---

## Secrets Management

### Use External Secret Managers

**Azure Key Vault:**
```bash
#!/bin/bash

# Retrieve secret from Key Vault
CLIENT_SECRET=$(az keyvault secret show \
  --name prod-client-secret \
  --vault-name my-vault \
  --query value -o tsv)

# Use secret (avoid storing in profile)
entratool get-token -p prod-profile --client-secret "$CLIENT_SECRET"
```

**HashiCorp Vault:**
```bash
#!/bin/bash

# Retrieve secret from Vault
CLIENT_SECRET=$(vault kv get -field=client_secret secret/entratool/prod)

# Use secret
entratool get-token -p prod-profile --client-secret "$CLIENT_SECRET"
```

---

## Compliance

### GDPR/Privacy

- Never log personally identifiable information (PII)
- Use encrypted storage for sensitive data
- Implement data retention policies
- Document data processing activities

### SOC 2

- Enable audit logging
- Implement access controls
- Rotate secrets regularly
- Monitor for security events
- Conduct regular security reviews

### HIPAA

- Use encryption at rest and in transit
- Implement access logging
- Enforce strong authentication
- Regular security audits
- Incident response procedures

---

## Security Checklist

### Before Production

- [ ] Use certificates instead of client secrets
- [ ] Implement secret rotation
- [ ] Separate profiles per environment
- [ ] Restrict file permissions (600 for secrets)
- [ ] Enable audit logging
- [ ] Use least privilege scopes
- [ ] Test token expiration handling
- [ ] Document security procedures
- [ ] Set up monitoring and alerts
- [ ] Review Azure AD app permissions

### Regular Maintenance

- [ ] Rotate secrets every 90 days
- [ ] Review audit logs monthly
- [ ] Update to latest CLI version
- [ ] Review app registrations quarterly
- [ ] Test disaster recovery procedures
- [ ] Update documentation
- [ ] Security training for team
- [ ] Penetration testing annually

---

## Next Steps

- [Bash Scripting](/docs/recipes/bash-scripts/)
- [CI/CD Integration](/docs/recipes/cicd-integration/)
- [Certificate Authentication](/docs/certificates/)
- [Troubleshooting](/docs/troubleshooting/)
