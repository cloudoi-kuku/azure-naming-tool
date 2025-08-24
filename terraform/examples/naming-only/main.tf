# Naming-Only Example: Generate Azure resource names without creating resources
# This example demonstrates the Azure Naming Tool integration without requiring Azure CLI

terraform {
  required_providers {
    external = {
      source  = "hashicorp/external"
      version = "~>2.0"
    }
  }
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
  default     = "f855c41b-2d4c-4f6f-95c9-58e38775c6ce"
  sensitive   = true
}

# Generate names for various resource types
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

module "vm_linux_name" {
  source = "../../modules/azure-naming"

  naming_tool_url   = var.naming_tool_url
  naming_api_key    = var.naming_api_key
  resource_type     = "vm"
  resource_property = "Linux"
  environment       = var.environment
  location          = var.location
  organization      = var.organization
  project           = var.project
  function          = "web"
  instance          = "01"
}

module "vm_windows_name" {
  source = "../../modules/azure-naming"

  naming_tool_url   = var.naming_tool_url
  naming_api_key    = var.naming_api_key
  resource_type     = "vm"
  resource_property = "Windows"
  environment       = var.environment
  location          = var.location
  organization      = var.organization
  project           = var.project
  function          = "app"
  instance          = "01"
}

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

module "vmss_linux_name" {
  source = "../../modules/azure-naming"

  naming_tool_url   = var.naming_tool_url
  naming_api_key    = var.naming_api_key
  resource_type     = "vmss"
  resource_property = "Linux"
  environment       = var.environment
  location          = var.location
  organization      = var.organization
  project           = var.project
  function          = "api"
  instance          = "01"
}

module "vmss_windows_name" {
  source = "../../modules/azure-naming"

  naming_tool_url   = var.naming_tool_url
  naming_api_key    = var.naming_api_key
  resource_type     = "vmss"
  resource_property = "Windows"
  environment       = var.environment
  location          = var.location
  organization      = var.organization
  project           = var.project
  function          = "app"
  instance          = "01"
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
    vmss_linux = {
      name    = module.vmss_linux_name.name
      message = module.vmss_linux_name.message
    }
    vmss_windows = {
      name    = module.vmss_windows_name.name
      message = module.vmss_windows_name.message
    }
  }
}

# Output showing the naming pattern
output "naming_summary" {
  description = "Summary of naming patterns"
  value = {
    environment  = var.environment
    location     = var.location
    organization = var.organization
    project      = var.project
    pattern_examples = {
      "Resource Group"     = module.rg_name.name
      "Storage Account"    = module.storage_name.name
      "Linux VM"          = module.vm_linux_name.name
      "Windows VM"        = module.vm_windows_name.name
      "Linux VMSS"        = module.vmss_linux_name.name
      "Windows VMSS"      = module.vmss_windows_name.name
      "Virtual Network"   = module.vnet_name.name
      "Subnet"            = module.subnet_name.name
    }
  }
}
