# Terraform Azure Naming Tool Integration

This directory contains Terraform configurations for integrating with the Azure Naming Tool to ensure consistent and compliant resource naming across your infrastructure.

## 🏗️ **Architecture**

```
terraform/
├── modules/
│   └── azure-naming/          # Reusable naming module
│       └── main.tf
├── examples/
│   └── using-naming-module/   # Example usage
│       ├── main.tf
│       └── terraform.tfvars.example
└── naming-integration/        # Direct integration example
    └── main.tf
```

## 🚀 **Quick Start**

### **1. Prerequisites**

- Terraform >= 1.0
- Azure CLI configured
- Azure Naming Tool running and accessible
- API key from Azure Naming Tool

### **2. Get Your API Key**

1. Visit your Azure Naming Tool admin interface: `http://localhost:8080/admin`
2. Login with admin password
3. Generate a "Name Generation API Key"
4. Copy the generated key

### **3. Use the Naming Module**

```hcl
module "vm_name" {
  source = "./modules/azure-naming"

  naming_tool_url = "http://localhost:8080"
  naming_api_key  = "your-api-key"
  resource_type   = "vm"
  environment     = "dev"
  location        = "use"
  organization    = "so"
  project         = "spa"
  instance        = "01"
}

resource "azurerm_linux_virtual_machine" "example" {
  name = module.vm_name.name
  # ... other configuration
}
```

## 📋 **Available Resource Types**

Use the CLI to discover available resource types:

```bash
# List all categories
./scripts/generate-name.sh list-categories

# List resource types in a category
./scripts/generate-name.sh list-types Compute

# Search for specific types
./scripts/generate-name.sh search-types storage
```

**Common resource types:**
- `rg` - Resource Groups
- `vm` - Virtual Machines
- `vmss` - Virtual Machine Scale Sets
- `st` - Storage Accounts
- `vnet` - Virtual Networks
- `snet` - Subnets
- `nic` - Network Interfaces
- `nsg` - Network Security Groups
- `pip` - Public IP Addresses

## 🔧 **Module Parameters**

### **Required Parameters**

| Parameter | Description | Example |
|-----------|-------------|---------|
| `naming_tool_url` | Azure Naming Tool URL | `"http://localhost:8080"` |
| `naming_api_key` | API key for authentication | `"your-api-key"` |
| `resource_type` | Resource type short name | `"vm"` |
| `environment` | Environment short name | `"dev"` |
| `location` | Location short name | `"use"` |
| `organization` | Organization short name | `"so"` |
| `project` | Project short name | `"spa"` |

### **Optional Parameters**

| Parameter | Description | Example |
|-----------|-------------|---------|
| `function` | Function short name | `"web"` |
| `unit` | Unit/Department short name | `"it"` |
| `instance` | Instance number | `"01"` |
| `resource_property` | Resource property (Windows/Linux) | `"Windows"` |

## 📝 **Examples**

### **Example 1: Basic Usage**

```hcl
module "storage_name" {
  source = "./modules/azure-naming"

  naming_tool_url = var.naming_tool_url
  naming_api_key  = var.naming_api_key
  resource_type   = "st"
  environment     = "prd"
  location        = "use"
  organization    = "contoso"
  project         = "webapp"
  instance        = "01"
}

resource "azurerm_storage_account" "main" {
  name = module.storage_name.name
  # Result: stwebappprduse01 (or similar based on your configuration)
}
```

### **Example 2: Windows vs Linux VMs**

```hcl
# Linux VM
module "vm_linux_name" {
  source = "./modules/azure-naming"

  naming_tool_url   = var.naming_tool_url
  naming_api_key    = var.naming_api_key
  resource_type     = "vm"
  resource_property = "Linux"
  environment       = "dev"
  location          = "use"
  organization      = "so"
  project           = "spa"
  instance          = "01"
}

# Windows VM (15 character limit)
module "vm_windows_name" {
  source = "./modules/azure-naming"

  naming_tool_url   = var.naming_tool_url
  naming_api_key    = var.naming_api_key
  resource_type     = "vm"
  resource_property = "Windows"
  environment       = "dev"
  location          = "use"
  organization      = "so"
  project           = "spa"
  instance          = "01"
}
```

### **Example 3: Multiple Resources**

```hcl
locals {
  naming_config = {
    naming_tool_url = var.naming_tool_url
    naming_api_key  = var.naming_api_key
    environment     = var.environment
    location        = var.location
    organization    = var.organization
    project         = var.project
  }
}

module "rg_name" {
  source = "./modules/azure-naming"

  naming_tool_url = local.naming_config.naming_tool_url
  naming_api_key  = local.naming_config.naming_api_key
  resource_type   = "rg"
  environment     = local.naming_config.environment
  location        = local.naming_config.location
  organization    = local.naming_config.organization
  project         = local.naming_config.project
}

module "vnet_name" {
  source = "./modules/azure-naming"

  naming_tool_url = local.naming_config.naming_tool_url
  naming_api_key  = local.naming_config.naming_api_key
  resource_type   = "vnet"
  environment     = local.naming_config.environment
  location        = local.naming_config.location
  organization    = local.naming_config.organization
  project         = local.naming_config.project
}
```

## 🔍 **Testing the Integration**

### **1. Plan and Apply**

```bash
cd terraform/examples/using-naming-module

# Copy and edit variables
cp terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars with your values

# Initialize Terraform
terraform init

# Plan to see generated names
terraform plan

# Apply to create resources
terraform apply
```

### **2. Expected Output**

```bash
terraform plan

# You should see output like:
# module.vm_name.data.external.resource_name: Reading...
# module.vm_name.data.external.resource_name: Read complete after 2s

Plan: 4 to add, 0 to change, 0 to destroy.

Changes to Outputs:
  + generated_names = {
      + resource_group = {
          + message = "Generated name follows standard naming convention"
          + name    = "rg-spa-dev-use"
        }
      + storage_account = {
          + message = "Generated name follows standard naming convention"
          + name    = "stspadevuse01"
        }
    }
```

## ⚠️ **Important Notes**

### **Component Validation**

The Azure Naming Tool validates that component values match configured options:

- **Environment**: Must be one of: `dev`, `prd`, `sbx`, `shd`, `stg`, `tst`, `uat`
- **Location**: Must be valid location short name: `use`, `use2`, `usc`, etc.
- **Organization**: Must match configured organization short names
- **Project**: Must match configured project short names

### **Error Handling**

The module includes error handling for common issues:

```bash
# If resource type not found:
Error: Resource type 'invalid-type' not found

# If component validation fails:
Error: ResourceEnvironment value is invalid

# If API key is wrong:
Error: Api Key was not provided!
```

### **Windows VM Naming**

Windows VMs have a 15-character limit. The naming tool automatically:
- Removes delimiters when needed
- Provides clear messages about changes
- Ensures compliance with Windows naming rules

## 🔧 **Troubleshooting**

### **Common Issues**

1. **API Key Issues**
   ```bash
   Error: Api Key was not provided!
   ```
   - Check your API key is correct
   - Ensure you're using the right header name (`APIKey`)

2. **Component Validation Errors**
   ```bash
   Error: ResourceEnvironment value is invalid
   ```
   - Use the CLI to check valid values: `./scripts/generate-name.sh list-categories`
   - Ensure you're using short names, not display names

3. **Resource Type Not Found**
   ```bash
   Error: Resource type 'xyz' not found
   ```
   - Check available types: `./scripts/generate-name.sh list-types`
   - Ensure the resource type is enabled in your naming tool

### **Debug Mode**

Enable debug output to see full API responses:

```hcl
output "debug" {
  value = module.vm_name.full_result
}
```

## 🚀 **Benefits**

- ✅ **Consistent Naming**: All resources follow organizational standards
- ✅ **Automatic Validation**: Names validated against Azure requirements
- ✅ **Centralized Control**: Changes to naming rules apply everywhere
- ✅ **Audit Trail**: Complete history of name generation
- ✅ **Error Prevention**: Catches naming issues before deployment
- ✅ **Team Collaboration**: Shared naming standards across teams
