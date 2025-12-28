---
title: "Azure Management API"
description: "Automate Azure resource management"
weight: 2
---

# Azure Management API

Learn how to use Entra Token CLI to authenticate and manage Azure resources.

---

## Overview

The Azure Management API provides programmatic access to:
- Subscriptions and resource groups
- Virtual machines and networking
- Storage accounts and databases
- Azure services configuration
- Resource provisioning and monitoring

**Base URL:** `https://management.azure.com/`

---

## Quick Start

### Setup Profile

```bash
entratool config create
# Name: azure-mgmt
# Client ID: <your-app-id>
# Tenant ID: <your-tenant-id>
# Scope: https://management.azure.com/.default
```

### Get Token and Call API

```bash
TOKEN=$(entratool get-token -p azure-mgmt --silent)
curl -H "Authorization: Bearer $TOKEN" \
     'https://management.azure.com/subscriptions?api-version=2020-01-01' | jq
```

---

## List Subscriptions

Retrieve all Azure subscriptions.

```bash
#!/bin/bash
TOKEN=$(entratool get-token -p azure-mgmt --silent \
  --scope "https://management.azure.com/.default")

curl -H "Authorization: Bearer $TOKEN" \
     'https://management.azure.com/subscriptions?api-version=2020-01-01' | jq
```

---

## List Resource Groups

List resource groups in a subscription.

```bash
#!/bin/bash
TOKEN=$(entratool get-token -p azure-mgmt --silent)
SUBSCRIPTION_ID="12345678-1234-1234-1234-123456789abc"

curl -H "Authorization: Bearer $TOKEN" \
     "https://management.azure.com/subscriptions/$SUBSCRIPTION_ID/resourcegroups?api-version=2021-04-01" | jq
```

---

## Create Resource Group

Create a new resource group.

```bash
#!/bin/bash
TOKEN=$(entratool get-token -p azure-mgmt --silent)
SUBSCRIPTION_ID="12345678-1234-1234-1234-123456789abc"
RESOURCE_GROUP="my-resource-group"
LOCATION="eastus"

curl -X PUT \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d '{
       "location": "'$LOCATION'"
     }' \
     "https://management.azure.com/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP?api-version=2021-04-01" | jq
```

---

## List Virtual Machines

List all VMs in a resource group.

```bash
#!/bin/bash
TOKEN=$(entratool get-token -p azure-mgmt --silent)
SUBSCRIPTION_ID="12345678-1234-1234-1234-123456789abc"
RESOURCE_GROUP="my-rg"

curl -H "Authorization: Bearer $TOKEN" \
     "https://management.azure.com/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Compute/virtualMachines?api-version=2021-03-01" | jq
```

---

## Create Virtual Machine

Create a new virtual machine.

```bash
#!/bin/bash
TOKEN=$(entratool get-token -p azure-admin --silent)
SUBSCRIPTION_ID="..."
RESOURCE_GROUP="my-rg"
VM_NAME="my-vm"

curl -X PUT \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d @vm-config.json \
     "https://management.azure.com/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Compute/virtualMachines/$VM_NAME?api-version=2021-03-01"
```

**vm-config.json:**
```json
{
  "location": "eastus",
  "properties": {
    "hardwareProfile": {
      "vmSize": "Standard_B1s"
    },
    "storageProfile": {
      "imageReference": {
        "publisher": "Canonical",
        "offer": "UbuntuServer",
        "sku": "18.04-LTS",
        "version": "latest"
      }
    },
    "osProfile": {
      "computerName": "my-vm",
      "adminUsername": "azureuser",
      "adminPassword": "P@ssw0rd123!"
    },
    "networkProfile": {
      "networkInterfaces": [
        {
          "id": "/subscriptions/.../networkInterfaces/my-nic"
        }
      ]
    }
  }
}
```

---

## List Storage Accounts

List storage accounts in a subscription.

```bash
#!/bin/bash
TOKEN=$(entratool get-token -p azure-mgmt --silent)
SUBSCRIPTION_ID="12345678-1234-1234-1234-123456789abc"

curl -H "Authorization: Bearer $TOKEN" \
     "https://management.azure.com/subscriptions/$SUBSCRIPTION_ID/providers/Microsoft.Storage/storageAccounts?api-version=2021-04-01" | jq
```

---

## Best Practices

### Use Service Principal

For automation, use a service principal with appropriate RBAC roles:

```bash
# Create service principal
az ad sp create-for-rbac --name "entratool-automation" \
  --role "Contributor" \
  --scopes "/subscriptions/YOUR_SUBSCRIPTION_ID"

# Configure profile
entratool config create
# Client ID: <from output>
# Client Secret: <from output>
# Tenant ID: <from output>
# Scope: https://management.azure.com/.default
```

### Limit Permissions

Assign minimum required roles:
- **Reader**: List resources only
- **Contributor**: Manage resources
- **Owner**: Full control (avoid if possible)

### Handle Long-Running Operations

```bash
#!/bin/bash
TOKEN=$(entratool get-token -p azure-mgmt --silent)

# Start operation
response=$(curl -X PUT \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @resource-config.json \
  "https://management.azure.com/...")

# Get operation status URL
status_url=$(echo "$response" | jq -r '.properties.provisioningState')

# Poll until complete
while true; do
  status=$(curl -s -H "Authorization: Bearer $TOKEN" "$status_url")
  state=$(echo "$status" | jq -r '.status')
  
  if [ "$state" = "Succeeded" ]; then
    echo "Operation completed"
    break
  elif [ "$state" = "Failed" ]; then
    echo "Operation failed"
    exit 1
  fi
  
  sleep 10
done
```

---

## Next Steps

- [Microsoft Graph API](/docs/recipes/microsoft-graph/)
- [CI/CD Integration](/docs/recipes/cicd-integration/)
- [Security Hardening](/docs/recipes/security-hardening/)
- [OAuth2 Flows](/docs/core-concepts/oauth2-flows/)
