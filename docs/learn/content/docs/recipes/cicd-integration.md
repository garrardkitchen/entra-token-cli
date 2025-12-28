---
title: "CI/CD Integration"
description: "Integrate with GitHub Actions, Azure Pipelines, and other CI/CD platforms"
weight: 3
---

# CI/CD Integration

Learn how to integrate Entra Token CLI into your CI/CD pipelines for automated authentication.

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
      
      - name: Install Entra Token CLI
        run: |
          dotnet tool install --global EntraTokenCli
      
      - name: Create Profile
        env:
          CLIENT_ID: ${{ secrets.AZURE_CLIENT_ID }}
          TENANT_ID: ${{ secrets.AZURE_TENANT_ID }}
          CLIENT_SECRET: ${{ secrets.AZURE_CLIENT_SECRET }}
        run: |
          # Create profile non-interactively
          cat > profile.json <<EOF
          {
            "name": "cicd",
            "clientId": "$CLIENT_ID",
            "tenantId": "$TENANT_ID",
            "scope": "https://management.azure.com/.default",
            "useClientSecret": true
          }
          EOF
          entratool config import -f profile.json
          
          # Store secret (implementation depends on tool capabilities)
      
      - name: Deploy to Azure
        run: |
          TOKEN=$(entratool get-token -p cicd --silent)
          
          # Use token for deployment
          curl -X POST \
               -H "Authorization: Bearer $TOKEN" \
               -H "Content-Type: application/json" \
               -d @deployment.json \
               "https://management.azure.com/..."
```

### With Matrix Strategy

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
      
      - name: Setup Entra Token CLI
        run: dotnet tool install --global EntraTokenCli
      
      - name: Deploy to ${{ matrix.environment }}
        env:
          CLIENT_ID: ${{ secrets[format('{0}_CLIENT_ID', matrix.environment)] }}
          TENANT_ID: ${{ secrets[format('{0}_TENANT_ID', matrix.environment)] }}
          CLIENT_SECRET: ${{ secrets[format('{0}_CLIENT_SECRET', matrix.environment)] }}
        run: |
          # Create environment-specific profile
          entratool config create --name ${{ matrix.environment }} \
            --client-id "$CLIENT_ID" \
            --tenant-id "$TENANT_ID"
          
          TOKEN=$(entratool get-token -p ${{ matrix.environment }} --silent)
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
    dotnet tool install --global EntraTokenCli
  displayName: 'Install Entra Token CLI'

- task: AzureCLI@2
  inputs:
    azureSubscription: 'MyServiceConnection'
    scriptType: 'bash'
    scriptLocation: 'inlineScript'
    inlineScript: |
      # Create profile
      entratool config create --non-interactive \
        --name cicd \
        --client-id $(ClientId) \
        --tenant-id $(TenantId) \
        --client-secret $(ClientSecret) \
        --scope "https://management.azure.com/.default"
      
      # Get token
      TOKEN=$(entratool get-token -p cicd --silent)
      
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
          - script: dotnet tool install --global EntraTokenCli
            displayName: 'Install CLI'
          
          - script: |
              entratool config create --name prod \
                --client-id $(ClientId) \
                --tenant-id $(TenantId)
              
              TOKEN=$(entratool get-token -p prod --silent)
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
    - dotnet tool install --global EntraTokenCli
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
    - entratool config import -f profile.json
    
    # Deploy
    - TOKEN=$(entratool get-token -p gitlab-ci --silent)
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
                sh 'dotnet tool install --global EntraTokenCli'
            }
        }
        
        stage('Deploy') {
            steps {
                sh '''
                    # Create profile
                    entratool config create --name jenkins \
                      --client-id "$CLIENT_ID" \
                      --tenant-id "$TENANT_ID"
                    
                    # Get token
                    TOKEN=$(entratool get-token -p jenkins --silent)
                    
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

```bash
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
  run: entratool config create --name dev ...

- name: Configure Prod Profile
  run: entratool config create --name prod ...
```

### Use Silent Mode

Always use `--silent` flag in CI/CD to get clean token output:

```bash
TOKEN=$(entratool get-token -p cicd --silent)
```

### Validate Tokens

Test token validity before use:

```bash
TOKEN=$(entratool get-token -p cicd --silent)
if entratool inspect <<< "$TOKEN" &>/dev/null; then
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

```bash
export PATH="$PATH:$HOME/.dotnet/tools"
```

### "Profile not found"

Profile creation may have failed. Check logs and recreate:

```bash
entratool config list
entratool config create ...
```

### "Authentication failed"

Verify secrets are correctly configured:

```bash
echo "Client ID: $CLIENT_ID"
echo "Tenant ID: $TENANT_ID"
# Don't echo secrets!
```

---

## Next Steps

- [Bash Scripting](/docs/recipes/bash-scripts/)
- [Security Hardening](/docs/recipes/security-hardening/)
- [OAuth2 Flows](/docs/core-concepts/oauth2-flows/)
