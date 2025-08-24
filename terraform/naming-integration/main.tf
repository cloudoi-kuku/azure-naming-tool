# Terraform Azure Naming Tool Integration
# This example shows how to integrate with the Azure Naming Tool for consistent resource naming

terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~>3.0"
    }
    external = {
      source  = "hashicorp/external"
      version = "~>2.0"
    }
  }
}

provider "azurerm" {
  features {}
}

# Variables for naming components
variable "environment" {
  description = "Environment (dev, prd, sbx, etc.)"
  type        = string
  default     = "dev"
}

variable "location" {
  description = "Azure location short name (use, use2, usc, etc.)"
  type        = string
  default     = "use"
}

variable "organization" {
  description = "Organization short name"
  type        = string
  default     = "so"
}

variable "project" {
  description = "Project/Application/Service short name"
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

# External data source to generate VM name
data "external" "vm_name" {
  program = ["bash", "-c", <<-EOF
    curl -s -X POST "${var.naming_tool_url}/api/ResourceNamingRequests/RequestName" \
      -H "Content-Type: application/json" \
      -H "APIKey: ${var.naming_api_key}" \
      -d '{
        "ResourceType": "vm",
        "ResourceId": 85,
        "ResourceEnvironment": "${var.environment}",
        "ResourceLocation": "${var.location}",
        "ResourceOrg": "${var.organization}",
        "ResourceProjAppSvc": "${var.project}",
        "ResourceInstance": "01",
        "CreatedBy": "terraform"
      }' | jq '{name: .resourceName}'
  EOF
  ]
}

# External data source to generate storage account name
data "external" "storage_name" {
  program = ["bash", "-c", <<-EOF
    # Get storage account resource type ID
    STORAGE_ID=$(curl -s -H "APIKey: ${var.naming_api_key}" \
      "${var.naming_tool_url}/api/ResourceTypes" | \
      jq -r '.[] | select(.resource == "Storage/storageAccounts" and .enabled == true) | .id' | head -n1)
    
    curl -s -X POST "${var.naming_tool_url}/api/ResourceNamingRequests/RequestName" \
      -H "Content-Type: application/json" \
      -H "APIKey: ${var.naming_api_key}" \
      -d '{
        "ResourceType": "st",
        "ResourceId": '$STORAGE_ID',
        "ResourceEnvironment": "${var.environment}",
        "ResourceLocation": "${var.location}",
        "ResourceOrg": "${var.organization}",
        "ResourceProjAppSvc": "${var.project}",
        "ResourceInstance": "01",
        "CreatedBy": "terraform"
      }' | jq '{name: .resourceName}'
  EOF
  ]
}

# External data source to generate resource group name
data "external" "rg_name" {
  program = ["bash", "-c", <<-EOF
    # Get resource group resource type ID
    RG_ID=$(curl -s -H "APIKey: ${var.naming_api_key}" \
      "${var.naming_tool_url}/api/ResourceTypes" | \
      jq -r '.[] | select(.resource == "Resources/resourceGroups" and .enabled == true) | .id' | head -n1)
    
    curl -s -X POST "${var.naming_tool_url}/api/ResourceNamingRequests/RequestName" \
      -H "Content-Type: application/json" \
      -H "APIKey: ${var.naming_api_key}" \
      -d '{
        "ResourceType": "rg",
        "ResourceId": '$RG_ID',
        "ResourceEnvironment": "${var.environment}",
        "ResourceLocation": "${var.location}",
        "ResourceOrg": "${var.organization}",
        "ResourceProjAppSvc": "${var.project}",
        "CreatedBy": "terraform"
      }' | jq '{name: .resourceName}'
  EOF
  ]
}

# Create resource group with generated name
resource "azurerm_resource_group" "main" {
  name     = data.external.rg_name.result.name
  location = "East US"

  tags = {
    Environment = var.environment
    Project     = var.project
    ManagedBy   = "terraform"
    NamingTool  = "azure-naming-tool"
  }
}

# Create storage account with generated name
resource "azurerm_storage_account" "main" {
  name                     = data.external.storage_name.result.name
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = "LRS"

  tags = {
    Environment = var.environment
    Project     = var.project
    ManagedBy   = "terraform"
    NamingTool  = "azure-naming-tool"
  }
}

# Create virtual network with generated name
data "external" "vnet_name" {
  program = ["bash", "-c", <<-EOF
    # Get virtual network resource type ID
    VNET_ID=$(curl -s -H "APIKey: ${var.naming_api_key}" \
      "${var.naming_tool_url}/api/ResourceTypes" | \
      jq -r '.[] | select(.resource == "Network/virtualNetworks" and .enabled == true) | .id' | head -n1)
    
    curl -s -X POST "${var.naming_tool_url}/api/ResourceNamingRequests/RequestName" \
      -H "Content-Type: application/json" \
      -H "APIKey: ${var.naming_api_key}" \
      -d '{
        "ResourceType": "vnet",
        "ResourceId": '$VNET_ID',
        "ResourceEnvironment": "${var.environment}",
        "ResourceLocation": "${var.location}",
        "ResourceOrg": "${var.organization}",
        "ResourceProjAppSvc": "${var.project}",
        "CreatedBy": "terraform"
      }' | jq '{name: .resourceName}'
  EOF
  ]
}

resource "azurerm_virtual_network" "main" {
  name                = data.external.vnet_name.result.name
  address_space       = ["10.0.0.0/16"]
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  tags = {
    Environment = var.environment
    Project     = var.project
    ManagedBy   = "terraform"
    NamingTool  = "azure-naming-tool"
  }
}

# Create subnet with generated name
data "external" "subnet_name" {
  program = ["bash", "-c", <<-EOF
    # Get subnet resource type ID
    SUBNET_ID=$(curl -s -H "APIKey: ${var.naming_api_key}" \
      "${var.naming_tool_url}/api/ResourceTypes" | \
      jq -r '.[] | select(.resource == "Network/virtualNetworks/subnets" and .enabled == true) | .id' | head -n1)
    
    curl -s -X POST "${var.naming_tool_url}/api/ResourceNamingRequests/RequestName" \
      -H "Content-Type: application/json" \
      -H "APIKey: ${var.naming_api_key}" \
      -d '{
        "ResourceType": "snet",
        "ResourceId": '$SUBNET_ID',
        "ResourceEnvironment": "${var.environment}",
        "ResourceLocation": "${var.location}",
        "ResourceOrg": "${var.organization}",
        "ResourceProjAppSvc": "${var.project}",
        "ResourceFunction": "web",
        "CreatedBy": "terraform"
      }' | jq '{name: .resourceName}'
  EOF
  ]
}

resource "azurerm_subnet" "main" {
  name                 = data.external.subnet_name.result.name
  resource_group_name  = azurerm_resource_group.main.name
  virtual_network_name = azurerm_virtual_network.main.name
  address_prefixes     = ["10.0.1.0/24"]
}

# Create virtual machine with generated name
resource "azurerm_linux_virtual_machine" "main" {
  name                = data.external.vm_name.result.name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  size                = "Standard_B1s"
  admin_username      = "adminuser"

  disable_password_authentication = true

  network_interface_ids = [
    azurerm_network_interface.main.id,
  ]

  admin_ssh_key {
    username   = "adminuser"
    public_key = file("~/.ssh/id_rsa.pub") # Update path as needed
  }

  os_disk {
    caching              = "ReadWrite"
    storage_account_type = "Standard_LRS"
  }

  source_image_reference {
    publisher = "Canonical"
    offer     = "0001-com-ubuntu-server-focal"
    sku       = "20_04-lts-gen2"
    version   = "latest"
  }

  tags = {
    Environment = var.environment
    Project     = var.project
    ManagedBy   = "terraform"
    NamingTool  = "azure-naming-tool"
  }
}

# Create network interface with generated name
data "external" "nic_name" {
  program = ["bash", "-c", <<-EOF
    # Get network interface resource type ID
    NIC_ID=$(curl -s -H "APIKey: ${var.naming_api_key}" \
      "${var.naming_tool_url}/api/ResourceTypes" | \
      jq -r '.[] | select(.resource == "Network/networkInterfaces" and .enabled == true) | .id' | head -n1)
    
    curl -s -X POST "${var.naming_tool_url}/api/ResourceNamingRequests/RequestName" \
      -H "Content-Type: application/json" \
      -H "APIKey: ${var.naming_api_key}" \
      -d '{
        "ResourceType": "nic",
        "ResourceId": '$NIC_ID',
        "ResourceEnvironment": "${var.environment}",
        "ResourceLocation": "${var.location}",
        "ResourceOrg": "${var.organization}",
        "ResourceProjAppSvc": "${var.project}",
        "ResourceInstance": "01",
        "CreatedBy": "terraform"
      }' | jq '{name: .resourceName}'
  EOF
  ]
}

resource "azurerm_network_interface" "main" {
  name                = data.external.nic_name.result.name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  ip_configuration {
    name                          = "internal"
    subnet_id                     = azurerm_subnet.main.id
    private_ip_address_allocation = "Dynamic"
  }

  tags = {
    Environment = var.environment
    Project     = var.project
    ManagedBy   = "terraform"
    NamingTool  = "azure-naming-tool"
  }
}

# Outputs
output "resource_names" {
  description = "Generated resource names from Azure Naming Tool"
  value = {
    resource_group    = data.external.rg_name.result.name
    storage_account   = data.external.storage_name.result.name
    virtual_network   = data.external.vnet_name.result.name
    subnet           = data.external.subnet_name.result.name
    virtual_machine  = data.external.vm_name.result.name
    network_interface = data.external.nic_name.result.name
  }
}
