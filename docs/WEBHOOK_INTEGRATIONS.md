# 🔗 Azure Naming Tool Webhook Integrations

This guide provides practical webhook integration examples to extend the Azure Naming Tool's capabilities across your DevOps and cloud infrastructure workflows.

## 🎯 **Why Webhooks?**

Webhooks enable:
- **Automated name generation** in CI/CD pipelines
- **Real-time validation** of resource names
- **Integration** with existing tools and workflows
- **Centralized governance** across teams and environments

## 🚀 **Quick Start Webhook Examples**

### **1. Infrastructure as Code (IaC) Integration**

#### **Terraform Integration**
```bash
# First, get the resource type ID (one-time lookup)
RESOURCE_TYPE_ID=$(curl -s -H "X-API-KEY: your-api-key" \
  "http://naming-tool/api/ResourceTypes" | \
  jq -r '.[] | select(.shortName == "vm" and .enabled == true) | .id' | head -n1)

# Generate names for Terraform resources
curl -X POST "http://naming-tool/api/ResourceNamingRequests/RequestName" \
  -H "Content-Type: application/json" \
  -H "X-API-KEY: your-api-key" \
  -d '{
    "ResourceType": "vm",
    "ResourceId": '$RESOURCE_TYPE_ID',
    "ResourceEnvironment": "prod",
    "ResourceLocation": "eastus",
    "ResourceOrg": "contoso",
    "ResourceProjAppSvc": "web",
    "CreatedBy": "terraform"
  }'
```

**Terraform Integration Script:**
```hcl
# terraform/naming.tf
data "external" "vm_name" {
  program = ["bash", "-c", <<EOF
curl -s -X POST "${var.naming_tool_url}/api/ResourceNamingRequests/RequestName" \
  -H "Content-Type: application/json" \
  -H "X-API-KEY: ${var.naming_api_key}" \
  -d '{
    "ResourceEnvironment": "${var.environment}",
    "ResourceLocation": "${var.location}",
    "ResourceOrg": "${var.organization}",
    "ResourceProjAppSvc": "${var.project}",
    "ResourceType": "vm",
    "CreatedBy": "terraform"
  }' | jq '{name: .resourceName}'
EOF
  ]
}

resource "azurerm_virtual_machine" "main" {
  name = data.external.vm_name.result.name
  # ... other configuration
}
```

#### **ARM Template Integration**
```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "namingToolUrl": {"type": "string"},
    "namingApiKey": {"type": "string"},
    "environment": {"type": "string"},
    "location": {"type": "string"}
  },
  "variables": {
    "vmName": "[reference(concat('generateVMName-', uniqueString(resourceGroup().id))).outputs.resourceName.value]"
  },
  "resources": [
    {
      "type": "Microsoft.Resources/deploymentScripts",
      "apiVersion": "2020-10-01",
      "name": "[concat('generateVMName-', uniqueString(resourceGroup().id))]",
      "location": "[parameters('location')]",
      "kind": "AzureCLI",
      "properties": {
        "azCliVersion": "2.40.0",
        "scriptContent": "[concat('curl -X POST \"', parameters('namingToolUrl'), '/api/ResourceNamingRequests/RequestName\" -H \"Content-Type: application/json\" -H \"X-API-KEY: ', parameters('namingApiKey'), '\" -d ''{\"ResourceEnvironment\": \"', parameters('environment'), '\", \"ResourceLocation\": \"', parameters('location'), '\", \"ResourceType\": \"vm\", \"CreatedBy\": \"arm\"}'' | jq -r ''.resourceName''')]",
        "retentionInterval": "P1D"
      }
    }
  ]
}
```

### **2. CI/CD Pipeline Integration**

#### **Azure DevOps Pipeline**
```yaml
# azure-pipelines.yml
trigger:
- main

variables:
  namingToolUrl: 'http://naming-tool:8080'
  namingApiKey: $(NAMING_API_KEY) # Set in pipeline variables

stages:
- stage: GenerateNames
  jobs:
  - job: GenerateResourceNames
    steps:
    - task: PowerShell@2
      displayName: 'Generate VM Name'
      inputs:
        targetType: 'inline'
        script: |
          $headers = @{
            "Content-Type" = "application/json"
            "X-API-KEY" = "$(namingApiKey)"
          }
          $body = @{
            ResourceEnvironment = "$(environment)"
            ResourceLocation = "$(location)"
            ResourceOrg = "$(organization)"
            ResourceProjAppSvc = "$(project)"
            ResourceType = "vm"
            CreatedBy = "azdevops-$(Build.BuildId)"
          } | ConvertTo-Json
          
          $response = Invoke-RestMethod -Uri "$(namingToolUrl)/api/ResourceNamingRequests/RequestName" -Method POST -Headers $headers -Body $body
          Write-Host "##vso[task.setvariable variable=vmName;isOutput=true]$($response.resourceName)"
      name: 'generateNames'
    
    - task: AzureResourceManagerTemplateDeployment@3
      displayName: 'Deploy Resources'
      inputs:
        deploymentScope: 'Resource Group'
        azureResourceManagerConnection: '$(serviceConnection)'
        resourceGroupName: '$(resourceGroup)'
        location: '$(location)'
        templateLocation: 'Linked artifact'
        csmFile: 'templates/vm.json'
        overrideParameters: '-vmName "$(generateNames.vmName)"'
```

#### **GitHub Actions Workflow**
```yaml
# .github/workflows/deploy.yml
name: Deploy Infrastructure

on:
  push:
    branches: [main]

env:
  NAMING_TOOL_URL: ${{ secrets.NAMING_TOOL_URL }}
  NAMING_API_KEY: ${{ secrets.NAMING_API_KEY }}

jobs:
  generate-names:
    runs-on: ubuntu-latest
    outputs:
      vm-name: ${{ steps.generate.outputs.vm-name }}
      storage-name: ${{ steps.generate.outputs.storage-name }}
    
    steps:
    - name: Generate Resource Names
      id: generate
      run: |
        # Generate VM name
        VM_NAME=$(curl -s -X POST "$NAMING_TOOL_URL/api/ResourceNamingRequests/RequestName" \
          -H "Content-Type: application/json" \
          -H "X-API-KEY: $NAMING_API_KEY" \
          -d '{
            "ResourceEnvironment": "${{ inputs.environment }}",
            "ResourceLocation": "${{ inputs.location }}",
            "ResourceOrg": "${{ github.repository_owner }}",
            "ResourceProjAppSvc": "${{ github.event.repository.name }}",
            "ResourceType": "vm",
            "CreatedBy": "github-${{ github.run_id }}"
          }' | jq -r '.resourceName')
        
        # Generate Storage name
        STORAGE_NAME=$(curl -s -X POST "$NAMING_TOOL_URL/api/ResourceNamingRequests/RequestName" \
          -H "Content-Type: application/json" \
          -H "X-API-KEY: $NAMING_API_KEY" \
          -d '{
            "ResourceEnvironment": "${{ inputs.environment }}",
            "ResourceLocation": "${{ inputs.location }}",
            "ResourceOrg": "${{ github.repository_owner }}",
            "ResourceProjAppSvc": "${{ github.event.repository.name }}",
            "ResourceType": "storage",
            "CreatedBy": "github-${{ github.run_id }}"
          }' | jq -r '.resourceName')
        
        echo "vm-name=$VM_NAME" >> $GITHUB_OUTPUT
        echo "storage-name=$STORAGE_NAME" >> $GITHUB_OUTPUT

  deploy:
    needs: generate-names
    runs-on: ubuntu-latest
    steps:
    - name: Deploy Resources
      run: |
        echo "Deploying VM: ${{ needs.generate-names.outputs.vm-name }}"
        echo "Deploying Storage: ${{ needs.generate-names.outputs.storage-name }}"
        # Add your deployment logic here
```

### **3. Slack/Teams Integration**

#### **Slack Bot Integration**
```javascript
// slack-bot.js
const { App } = require('@slack/bolt');

const app = new App({
  token: process.env.SLACK_BOT_TOKEN,
  signingSecret: process.env.SLACK_SIGNING_SECRET
});

app.command('/generate-name', async ({ command, ack, respond }) => {
  await ack();
  
  const [resourceType, environment, location, org, project] = command.text.split(' ');
  
  try {
    const response = await fetch(`${process.env.NAMING_TOOL_URL}/api/ResourceNamingRequests/RequestName`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-KEY': process.env.NAMING_API_KEY
      },
      body: JSON.stringify({
        ResourceEnvironment: environment,
        ResourceLocation: location,
        ResourceOrg: org,
        ResourceProjAppSvc: project,
        ResourceType: resourceType,
        CreatedBy: `slack-${command.user_id}`
      })
    });
    
    const data = await response.json();
    
    await respond({
      text: `Generated name: \`${data.resourceName}\``,
      response_type: 'in_channel'
    });
  } catch (error) {
    await respond({
      text: `Error generating name: ${error.message}`,
      response_type: 'ephemeral'
    });
  }
});

(async () => {
  await app.start(process.env.PORT || 3000);
  console.log('⚡️ Slack bot is running!');
})();
```

**Usage in Slack:**
```
/generate-name vm prod eastus contoso web
# Returns: Generated name: vm-eus-contoso-web-prod-01
```

### **4. Azure Monitor Integration**

#### **Logic App for Resource Monitoring**
```json
{
  "definition": {
    "$schema": "https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#",
    "triggers": {
      "When_a_resource_is_created": {
        "type": "ApiConnection",
        "inputs": {
          "host": {
            "connection": {
              "name": "@parameters('$connections')['azuremonitorlogs']['connectionId']"
            }
          },
          "method": "post",
          "path": "/queryData",
          "queries": {
            "resourceTypes": "Microsoft.Compute/virtualMachines",
            "subscriptions": "@parameters('subscriptionId')",
            "timespan": "PT5M"
          }
        }
      }
    },
    "actions": {
      "Validate_naming_compliance": {
        "type": "Http",
        "inputs": {
          "method": "POST",
          "uri": "@concat(parameters('namingToolUrl'), '/api/ResourceNamingRequests/ValidateName')",
          "headers": {
            "Content-Type": "application/json",
            "X-API-KEY": "@parameters('namingApiKey')"
          },
          "body": {
            "resourceName": "@triggerBody()['resourceName']",
            "resourceType": "@triggerBody()['resourceType']"
          }
        }
      }
    }
  }
}
```

## 🔧 **Implementation Tips**

### **Error Handling**
```bash
# Robust error handling in scripts
generate_name() {
    local response=$(curl -s -w "%{http_code}" -X POST "$API_URL/api/ResourceNamingRequests/RequestName" \
        -H "Content-Type: application/json" \
        -H "X-API-KEY: $API_KEY" \
        -d "$json_payload")
    
    local http_code="${response: -3}"
    local body="${response%???}"
    
    if [ "$http_code" -eq 200 ]; then
        echo "$body" | jq -r '.resourceName'
    else
        echo "Error: HTTP $http_code - $body" >&2
        exit 1
    fi
}
```

### **Caching for Performance**
```bash
# Cache generated names to avoid duplicate API calls
CACHE_FILE="/tmp/naming_cache_$(date +%Y%m%d).json"

get_cached_name() {
    local key="$1"
    if [ -f "$CACHE_FILE" ]; then
        jq -r --arg key "$key" '.[$key] // empty' "$CACHE_FILE"
    fi
}

cache_name() {
    local key="$1"
    local name="$2"
    local cache_data="{}"
    
    if [ -f "$CACHE_FILE" ]; then
        cache_data=$(cat "$CACHE_FILE")
    fi
    
    echo "$cache_data" | jq --arg key "$key" --arg name "$name" '. + {($key): $name}' > "$CACHE_FILE"
}
```

## 📊 **Monitoring and Analytics**

### **Track Usage with Custom Headers**
```bash
# Add tracking headers to API calls
curl -X POST "$API_URL/api/ResourceNamingRequests/RequestName" \
  -H "Content-Type: application/json" \
  -H "X-API-KEY: $API_KEY" \
  -H "X-Source: terraform" \
  -H "X-Pipeline-ID: $BUILD_ID" \
  -H "X-Team: platform-engineering" \
  -d "$json_payload"
```

### **Webhook for Usage Analytics**
```javascript
// analytics-webhook.js
app.post('/webhook/usage-analytics', (req, res) => {
  const { resourceName, resourceType, createdBy, timestamp } = req.body;
  
  // Send to analytics platform
  analytics.track({
    event: 'resource_name_generated',
    properties: {
      resourceType,
      environment: req.body.environment,
      team: extractTeam(createdBy),
      timestamp
    }
  });
  
  res.status(200).send('OK');
});
```

## 🎯 **Next Steps**

1. **Choose Integration Points**: Identify where in your workflow naming happens
2. **Set Up API Keys**: Configure authentication for your tools
3. **Test Integrations**: Start with simple curl commands
4. **Add Error Handling**: Implement robust error handling and retries
5. **Monitor Usage**: Track naming patterns and compliance
6. **Scale Gradually**: Expand to more teams and use cases

## 📚 **Additional Resources**

- [API Documentation](../api/README.md)
- [CLI Tools](../scripts/README.md)
- [Configuration Guide](../docs/CONFIGURATION.md)
- [Troubleshooting](../docs/TROUBLESHOOTING.md)
