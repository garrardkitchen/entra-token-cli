---
title: "CI/CD Integration"
description: "Integrate with GitHub Actions, Azure Pipelines, and other CI/CD platforms"
weight: 3
---

# CI/CD Integration

Learn how to integrate Entra Auth Cli into your CI/CD pipelines for automated authentication.

> **⚠️ Important Note on CI/CD Usage:**  
> The `entra-auth-cli config create` command is **fully interactive** and cannot accept credentials via command-line flags. For CI/CD scenarios, you have two options:
> 
> 1. **Export profiles locally** and import them in CI/CD (using encrypted files with passphrases stored as secrets)
> 2. **Use direct get-token** with profiles created ahead of time and secrets managed via secure storage
>
> The examples below show both approaches.

---

## GitHub Actions

### Basic Example

```yaml
name: Deploy

on: [push]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Install Entra Auth Cli
        run: |
          dotnet tool install --global EntraAuthCli
      
      - name: Setup Profile
        env:
          PROFILE_DATA: ${{ secrets.ENTRA_PROFILE_ENCRYPTED }}
          PASSPHRASE: ${{ secrets.PROFILE_PASSPHRASE }}
        run: |
          # Import pre-exported profile (created locally with config export)
          echo "$PROFILE_DATA" > profile.enc
          echo "$PASSPHRASE" | entra-auth-cli config import -i profile.enc
      
      - name: Deploy to Azure
        run: |
          TOKEN=$(entra-auth-cli get-token -p cicd)
          
          # Use token for deployment
          curl -X POST \
               -H "Authorization: Bearer $TOKEN" \
               -H "Content-Type: application/json" \
               -d @deployment.json \
               "https://management.azure.com/..."
```

### With Pre-Created Profiles

```yaml
name: Multi-Environment Deploy

on: [push]

jobs:
  deploy:
    runs-on: ubuntu-latest
    strategy:
      matrix:
        environment: [dev, staging, prod]
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup Entra Auth Cli
        run: dotnet tool install --global EntraAuthCli
      
      - name: Import ${{ matrix.environment }} Profile
        env:
          PROFILE_DATA: ${{ secrets[format('{0}_PROFILE_ENC', matrix.environment)] }}
          PASSPHRASE: ${{ secrets[format('{0}_PASSPHRASE', matrix.environment)] }}
        run: |
          echo "$PROFILE_DATA" > profile.enc
          echo "$PASSPHRASE" | entra-auth-cli config import -i profile.enc -n ${{ matrix.environment }}
          
      - name: Deploy
        run: |
          TOKEN=$(entra-auth-cli get-token -p ${{ matrix.environment }})
          ./deploy.sh "$TOKEN" "${{ matrix.environment }}"
```

---

## Azure Pipelines

### Basic Example

```yaml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
- script: |
    dotnet tool install --global EntraAuthCli
  displayName: 'Install Entra Auth Cli'

- task: AzureCLI@2
  inputs:
    azureSubscription: 'MyServiceConnection'
    scriptType: 'bash'
    scriptLocation: 'inlineScript'
    inlineScript: |
      # Create profile
      entra-auth-cli config create --non-interactive \
        --name cicd \
        --client-id $(ClientId) \
        --tenant-id $(TenantId) \
        --client-secret $(ClientSecret) \
        --scope "https://management.azure.com/.default"
      
      # Get token
      TOKEN=$(entra-auth-cli get-token -p cicd )
      
      # Deploy
      ./deploy.sh "$TOKEN"
```

### With Deployment Job

```yaml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

stages:
- stage: Build
  jobs:
  - job: BuildJob
    steps:
    - script: dotnet build
      displayName: 'Build Application'

- stage: Deploy
  dependsOn: Build
  jobs:
  - deployment: DeployJob
    environment: 'production'
    strategy:
      runOnce:
        deploy:
          steps:
          - script: dotnet tool install --global EntraAuthCli
            displayName: 'Install CLI'
          
          - script: |
              entra-auth-cli config create # Note: fully interactive prod \
                --client-id $(ClientId) \
                --tenant-id $(TenantId)
              
              TOKEN=$(entra-auth-cli get-token -p prod )
              ./deploy-to-production.sh "$TOKEN"
            displayName: 'Deploy'
```

---

## GitLab CI

```yaml
stages:
  - deploy

deploy:
  stage: deploy
  image: mcr.microsoft.com/dotnet/sdk:8.0
  script:
    - dotnet tool install --global EntraAuthCli
    - export PATH="$PATH:/root/.dotnet/tools"
    
    # Create profile
    - |
      cat > profile.json <<EOF
      {
        "name": "gitlab-ci",
        "clientId": "$AZURE_CLIENT_ID",
        "tenantId": "$AZURE_TENANT_ID",
        "scope": "https://management.azure.com/.default"
      }
      EOF
    - entra-auth-cli config import -i profile.json
    
    # Deploy
    - TOKEN=$(entra-auth-cli get-token -p gitlab-ci )
    - ./deploy.sh "$TOKEN"
  only:
    - main
```

---

## Jenkins

```groovy
pipeline {
    agent any
    
    environment {
        CLIENT_ID = credentials('azure-client-id')
        TENANT_ID = credentials('azure-tenant-id')
        CLIENT_SECRET = credentials('azure-client-secret')
    }
    
    stages {
        stage('Setup') {
            steps {
                sh 'dotnet tool install --global EntraAuthCli'
            }
        }
        
        stage('Deploy') {
            steps {
                sh '''
                    # Create profile
                    entra-auth-cli config create # Note: fully interactive jenkins \
                      --client-id "$CLIENT_ID" \
                      --tenant-id "$TENANT_ID"
                    
                    # Get token
                    TOKEN=$(entra-auth-cli get-token -p jenkins )
                    
                    # Deploy
                    ./deploy.sh "$TOKEN"
                '''
            }
        }
    }
}
```

---

## Best Practices

### Use Service Principals

Create dedicated service principals for CI/CD:

```bash {linenos=inline}
az ad sp create-for-rbac --name "cicd-deployment" \
  --role "Contributor" \
  --scopes "/subscriptions/YOUR_SUBSCRIPTION_ID"
```

### Store Secrets Securely

- **GitHub Actions**: Use GitHub Secrets
- **Azure Pipelines**: Use Variable Groups
- **GitLab CI**: Use CI/CD Variables
- **Jenkins**: Use Credentials Plugin

### Separate Profiles per Environment

```yaml
# Create environment-specific profiles
- name: Configure Dev Profile
  run: entra-auth-cli config create # Note: fully interactive dev ...

- name: Configure Prod Profile
  run: entra-auth-cli config create # Note: fully interactive prod ...
```

### Use Silent Mode

Always use `` flag in CI/CD to get clean token output:

```bash {linenos=inline}
TOKEN=$(entra-auth-cli get-token -p cicd )
```

### Validate Tokens

Test token validity before use:

```bash {linenos=inline}
TOKEN=$(entra-auth-cli get-token -p cicd )
if entra-auth-cli inspect <<< "$TOKEN" &>/dev/null; then
  echo "Token valid"
  # Proceed with deployment
else
  echo "Token invalid"
  exit 1
fi
```

---

## Troubleshooting

### "Tool not found"

Ensure .NET tools are in PATH:

```bash {linenos=inline}
export PATH="$PATH:$HOME/.dotnet/tools"
```

### "Profile not found"

Profile creation may have failed. Check logs and recreate:

```bash {linenos=inline}
entra-auth-cli config list
entra-auth-cli config create ...
```

### "Authentication failed"

Verify secrets are correctly configured:

```bash {linenos=inline}
echo "Client ID: $CLIENT_ID"
echo "Tenant ID: $TENANT_ID"
# Don't echo secrets!
```

---

## Next Steps

- [Bash Scripting](/docs/recipes/bash-scripts/)
- [Security Hardening](/docs/recipes/security-hardening/)
- [OAuth2 Flows](/docs/core-concepts/oauth2-flows/)
