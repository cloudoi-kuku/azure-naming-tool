# Azure Naming Tool Terraform Module
# This module provides a reusable way to generate Azure resource names using the Azure Naming Tool

terraform {
  required_providers {
    external = {
      source  = "hashicorp/external"
      version = "~>2.0"
    }
  }
}

# Variables
variable "naming_tool_url" {
  description = "Azure Naming Tool URL"
  type        = string
}

variable "naming_api_key" {
  description = "Azure Naming Tool API Key"
  type        = string
  sensitive   = true
}

variable "resource_type" {
  description = "Resource type short name (vm, st, rg, etc.)"
  type        = string
}

variable "environment" {
  description = "Environment short name"
  type        = string
}

variable "location" {
  description = "Location short name"
  type        = string
}

variable "organization" {
  description = "Organization short name"
  type        = string
}

variable "project" {
  description = "Project/Application/Service short name"
  type        = string
}

variable "function" {
  description = "Function short name (optional)"
  type        = string
  default     = ""
}

variable "unit" {
  description = "Unit/Department short name (optional)"
  type        = string
  default     = ""
}

variable "instance" {
  description = "Instance number (optional)"
  type        = string
  default     = ""
}

variable "resource_property" {
  description = "Resource property (Windows, Linux, etc.) - optional"
  type        = string
  default     = ""
}

# Local values for building the API request
locals {
  # Build the JSON payload for the API request
  api_payload = jsonencode({
    ResourceType        = var.resource_type
    ResourceEnvironment = var.environment
    ResourceLocation    = var.location
    ResourceOrg         = var.organization
    ResourceProjAppSvc  = var.project
    ResourceFunction    = var.function
    ResourceUnitDept    = var.unit
    ResourceInstance    = var.instance
    CreatedBy          = "terraform"
  })
}

# External data source to get resource type ID and generate name
data "external" "resource_name" {
  program = ["bash", "-c", <<-EOF
    set -e
    
    # Get resource type information
    if [ -n "${var.resource_property}" ]; then
      # Find resource type with specific property
      RESOURCE_INFO=$(curl -s -H "APIKey: ${var.naming_api_key}" \
        "${var.naming_tool_url}/api/ResourceTypes" | \
        jq -r '.[] | select(.ShortName == "${var.resource_type}" and .property == "${var.resource_property}" and .enabled == true) | "\(.id)|\(.resource)|\(.property)"' | head -n1)
    else
      # Find resource type without property filter
      RESOURCE_INFO=$(curl -s -H "APIKey: ${var.naming_api_key}" \
        "${var.naming_tool_url}/api/ResourceTypes" | \
        jq -r '.[] | select(.ShortName == "${var.resource_type}" and .enabled == true) | "\(.id)|\(.resource)|\(.property)"' | head -n1)
    fi
    
    if [ -z "$RESOURCE_INFO" ]; then
      echo '{"error": "Resource type not found"}' >&2
      exit 1
    fi
    
    # Extract resource ID
    RESOURCE_ID=$(echo "$RESOURCE_INFO" | cut -d'|' -f1)
    
    # Generate name with resource ID
    RESPONSE=$(curl -s -X POST "${var.naming_tool_url}/api/ResourceNamingRequests/RequestName" \
      -H "Content-Type: application/json" \
      -H "APIKey: ${var.naming_api_key}" \
      -d '{
        "ResourceType": "${var.resource_type}",
        "ResourceId": '$RESOURCE_ID',
        "ResourceEnvironment": "${var.environment}",
        "ResourceLocation": "${var.location}",
        "ResourceOrg": "${var.organization}",
        "ResourceProjAppSvc": "${var.project}",
        "ResourceFunction": "${var.function}",
        "ResourceUnitDept": "${var.unit}",
        "ResourceInstance": "${var.instance}",
        "CreatedBy": "terraform"
      }')
    
    # Check if the response contains an error
    if echo "$RESPONSE" | jq -e '.success == false' > /dev/null; then
      ERROR_MSG=$(echo "$RESPONSE" | jq -r '.message // "Unknown error"')
      echo "{\"error\": \"$ERROR_MSG\"}" >&2
      exit 1
    fi
    
    # Extract the generated name and return as JSON
    RESOURCE_NAME=$(echo "$RESPONSE" | jq -r '.resourceName // ""')
    MESSAGE=$(echo "$RESPONSE" | jq -r '.message // ""')
    
    if [ -z "$RESOURCE_NAME" ]; then
      echo '{"error": "No resource name returned"}' >&2
      exit 1
    fi
    
    # Return the result
    jq -n --arg name "$RESOURCE_NAME" --arg message "$MESSAGE" --arg id "$RESOURCE_ID" \
      '{name: $name, message: $message, resource_id: $id}'
  EOF
  ]
}

# Outputs
output "name" {
  description = "Generated resource name"
  value       = data.external.resource_name.result.name
}

output "message" {
  description = "Message from naming tool (if any)"
  value       = data.external.resource_name.result.message
}

output "resource_id" {
  description = "Resource type ID used for generation"
  value       = data.external.resource_name.result.resource_id
}

output "full_result" {
  description = "Full result from naming tool"
  value       = data.external.resource_name.result
}
