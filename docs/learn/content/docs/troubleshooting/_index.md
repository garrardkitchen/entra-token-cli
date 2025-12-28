---
title: "Troubleshooting"
description: "Common issues and solutions"
weight: 80
---

# Troubleshooting

Solutions to common problems and error messages.

---

## Common Issues

### Profile Problems

[**Profile Issues →**](/docs/troubleshooting/profiles/)

**Common errors:**
- "Profile not found"
- "Profile already exists"
- "Invalid profile configuration"
- "Cannot access secure storage"

### Authentication Failures

[**Authentication Issues →**](/docs/troubleshooting/authentication/)

**Common errors:**
- "Authentication failed"
- "Invalid client secret"
- "AADSTS errors"
- "Token generation failed"

### Certificate Problems

[**Certificate Issues →**](/docs/troubleshooting/certificates/)

**Common errors:**
- "Certificate not found"
- "Invalid certificate password"
- "Client assertion contains an invalid signature"
- "Certificate has expired"

### Token Issues

[**Token Issues →**](/docs/troubleshooting/tokens/)

**Common errors:**
- "Token expired"
- "Invalid token format"
- "Insufficient permissions"
- "Token validation failed"

---

## Quick Fixes

### Profile Not Found

```bash {linenos=inline}
# List available profiles
entra-auth-cli config list

# Use correct profile name
entra-auth-cli get-token -p correct-profile-name
```

### Authentication Failed

```bash {linenos=inline}
# Verify credentials are correct
entra-auth-cli config show -p my-profile

# Recreate profile if needed
entra-auth-cli config delete -p my-profile
entra-auth-cli config create
```

### Certificate Issues

```bash {linenos=inline}
# Verify certificate file exists
ls -l /path/to/certificate.pfx

# Check certificate is uploaded to Azure
# Azure Portal → App registrations → Your app → Certificates & secrets
```

### Token Expired

```bash {linenos=inline}
# Simply request a new token
entra-auth-cli get-token -p my-profile

# Or use refresh if available
entra-auth-cli refresh -p my-profile
```

---

## Diagnostic Commands

### Check Configuration

```bash {linenos=inline}
# List profiles
entra-auth-cli config list

# Show profile details
entra-auth-cli config show -p my-profile

# Verify token generation
entra-auth-cli get-token -p my-profile
```

### Inspect Tokens

```bash {linenos=inline}
# Get token and inspect
TOKEN=$(entra-auth-cli get-token -p my-profile --silent)
entra-auth-cli inspect -t "$TOKEN"

# Check expiration
entra-auth-cli discover -t "$TOKEN"
```

### Validate Certificate

```bash {linenos=inline}
# Check certificate file
openssl pkcs12 -info -in certificate.pfx -noout

# Verify thumbprint matches Azure
openssl pkcs12 -in certificate.pfx -nokeys | \
  openssl x509 -noout -fingerprint -sha1
```

---

## Error Code Reference

### AADSTS Error Codes

| Code | Meaning | Solution |
|------|---------|----------|
| AADSTS50011 | Redirect URI mismatch | Add redirect URI in Azure Portal |
| AADSTS700016 | Application not found | Verify Client ID |
| AADSTS700027 | Invalid certificate signature | Check certificate registration |
| AADSTS70002 | Invalid client secret | Update client secret |
| AADSTS50076 | MFA required | Use interactive flow |

### Common Exit Codes

| Code | Meaning | Action |
|------|---------|--------|
| 1 | General error | Check error message |
| 2 | Authentication failed | Verify credentials |
| 3 | Profile not found | Check profile name |
| 4 | Invalid arguments | Check command syntax |

---

## Getting Help

### Check Logs

```bash {linenos=inline}
# Enable verbose logging (if supported)
entra-auth-cli get-token -p my-profile --verbose

# Check system logs
# Windows: Event Viewer
# macOS: Console.app
# Linux: journalctl
```

### Community Support

- [GitHub Issues](https://github.com/garrardkitchen/entra-auth-cli/issues)
- [Documentation](/docs/)
- [Recipes](/docs/recipes/)

---

## Next Steps

- [Profile Issues](/docs/troubleshooting/profiles/)
- [Authentication Issues](/docs/troubleshooting/authentication/)
- [Certificate Issues](/docs/troubleshooting/certificates/)
- [User Guide](/docs/user-guide/)
