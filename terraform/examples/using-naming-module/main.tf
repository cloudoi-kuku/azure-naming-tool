# Example: Using the Azure Naming Tool Module
# This example shows how to use the azure-naming module for consistent resource naming

terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~>3.0"
    }
  }
}

provider "azurerm" {
  features {}
}

# Variables
variable "environment" {
  description = "Environment (dev, prd, sbx, etc.)"
  type        = string
  default     = "dev"
}

variable "location" {
  description = "Azure location short name"
  type        = string
  default     = "use"
}

variable "organization" {
  description = "Organization short name"
  type        = string
  default     = "so"
}

variable "project" {
  description = "Project short name"
  type        = string
  default     = "spa"
}

variable "naming_tool_url" {
  description = "Azure Naming Tool URL"
  type        = string
  default     = "http://localhost:8080"
}

variable "naming_api_key" {
  description = "Azure Naming Tool API Key"
  type        = string
  sensitive   = true
}

# Generate resource group name
module "rg_name" {
  source = "../../modules/azure-naming"

  naming_tool_url = var.naming_tool_url
  naming_api_key  = var.naming_api_key
  resource_type   = "rg"
  environment     = var.environment
  location        = var.location
  organization    = var.organization
  project         = var.project
  instance        = "01"
}

# Generate storage account name
module "storage_name" {
  source = "../../modules/azure-naming"

  naming_tool_url = var.naming_tool_url
  naming_api_key  = var.naming_api_key
  resource_type   = "st"
  environment     = var.environment
  location        = var.location
  organization    = var.organization
  project         = var.project
  instance        = "01"
}

# Generate Linux VM name
module "vm_linux_name" {
  source = "../../modules/azure-naming"

  naming_tool_url     = var.naming_tool_url
  naming_api_key      = var.naming_api_key
  resource_type       = "vm"
  resource_property   = "Linux"
  environment         = var.environment
  location            = var.location
  organization        = var.organization
  project             = var.project
  function            = "web"
  instance            = "01"
}

# Generate Windows VM name
module "vm_windows_name" {
  source = "../../modules/azure-naming"

  naming_tool_url     = var.naming_tool_url
  naming_api_key      = var.naming_api_key
  resource_type       = "vm"
  resource_property   = "Windows"
  environment         = var.environment
  location            = var.location
  organization        = var.organization
  project             = var.project
  function            = "app"
  instance            = "01"
}

# Generate virtual network name
module "vnet_name" {
  source = "../../modules/azure-naming"

  naming_tool_url = var.naming_tool_url
  naming_api_key  = var.naming_api_key
  resource_type   = "vnet"
  environment     = var.environment
  location        = var.location
  organization    = var.organization
  project         = var.project
  instance        = "01"
}

# Generate subnet name
module "subnet_name" {
  source = "../../modules/azure-naming"

  naming_tool_url = var.naming_tool_url
  naming_api_key  = var.naming_api_key
  resource_type   = "snet"
  environment     = var.environment
  location        = var.location
  organization    = var.organization
  project         = var.project
  function        = "web"
  instance        = "01"
}

# Create the actual Azure resources using the generated names
resource "azurerm_resource_group" "main" {
  name     = module.rg_name.name
  location = "East US"

  tags = {
    Environment    = var.environment
    Project        = var.project
    ManagedBy      = "terraform"
    NamingTool     = "azure-naming-tool"
    NamingMessage  = module.rg_name.message
  }
}

resource "azurerm_storage_account" "main" {
  name                     = module.storage_name.name
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = "LRS"

  tags = {
    Environment    = var.environment
    Project        = var.project
    ManagedBy      = "terraform"
    NamingTool     = "azure-naming-tool"
    NamingMessage  = module.storage_name.message
  }
}

resource "azurerm_virtual_network" "main" {
  name                = module.vnet_name.name
  address_space       = ["10.0.0.0/16"]
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  tags = {
    Environment    = var.environment
    Project        = var.project
    ManagedBy      = "terraform"
    NamingTool     = "azure-naming-tool"
    NamingMessage  = module.vnet_name.message
  }
}

resource "azurerm_subnet" "web" {
  name                 = module.subnet_name.name
  resource_group_name  = azurerm_resource_group.main.name
  virtual_network_name = azurerm_virtual_network.main.name
  address_prefixes     = ["10.0.1.0/24"]
}

# Outputs showing all generated names
output "generated_names" {
  description = "All generated resource names from Azure Naming Tool"
  value = {
    resource_group = {
      name    = module.rg_name.name
      message = module.rg_name.message
    }
    storage_account = {
      name    = module.storage_name.name
      message = module.storage_name.message
    }
    virtual_network = {
      name    = module.vnet_name.name
      message = module.vnet_name.message
    }
    subnet = {
      name    = module.subnet_name.name
      message = module.subnet_name.message
    }
    vm_linux = {
      name    = module.vm_linux_name.name
      message = module.vm_linux_name.message
    }
    vm_windows = {
      name    = module.vm_windows_name.name
      message = module.vm_windows_name.message
    }
  }
}

# Output for debugging
output "naming_debug" {
  description = "Debug information from naming tool"
  value = {
    rg_full_result      = module.rg_name.full_result
    storage_full_result = module.storage_name.full_result
  }
}
