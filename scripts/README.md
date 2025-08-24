# Azure Naming Tool CLI Scripts

This directory contains command-line tools for generating Azure resource names using the Azure Naming Tool API.

## 🖥️ Available CLI Options

### Option 1: Bash Script (Cross-platform)

**File:** `generate-name.sh`

**Cross-platform compatibility:**
- ✅ **Linux/macOS**: Native bash support
- ✅ **Windows**: Use PowerShell 7+ or WSL
- ✅ **Windows PowerShell**: `bash ./generate-name.sh [command]`

**Commands:**
```bash
# List available resource categories
./generate-name.sh list-categories

# List all resource types or filter by category
./generate-name.sh list-types
./generate-name.sh list-types Compute

# Search for resource types
./generate-name.sh search-types virtual

# Generate a resource name
./generate-name.sh generate --type vm --env prod --location eastus --org contoso --project web
```

**Requirements:**
- `curl` (usually pre-installed)
- `jq` (install with `sudo apt-get install jq`, `brew install jq`, or `winget install jqlang.jq`)

### Option 2: Docker CLI Wrapper

**Usage:**
```bash
# Enable CLI profile and run
docker-compose --profile cli run --rm naming-cli prod eastus contoso web vm
```

### Option 3: Direct API Calls

**curl (Cross-platform):**
```bash
curl -X POST "http://localhost:8080/api/ResourceNamingRequests/RequestName" \
  -H "Content-Type: application/json" \
  -H "APIKey: your-api-key" \   # preferred header name
  -H "X-API-KEY: your-api-key" \   # kept for compatibility
  -d '{
    "ResourceType": "vm",
    "ResourceId": 123,
    "ResourceEnvironment": "prod",
    "ResourceLocation": "eastus",
    "ResourceOrg": "contoso",
    "ResourceProjAppSvc": "web",
    "CreatedBy": "cli-user"
  }'
```

**PowerShell:**
```powershell
$headers = @{
  "Content-Type" = "application/json"
  "APIKey" = "your-api-key"  # preferred header name
  "X-API-KEY" = "your-api-key"  # compatibility
}
$body = @{
    ResourceType = "vm"
    ResourceId = 123
    ResourceEnvironment = "prod"
    ResourceLocation = "eastus"
    ResourceOrg = "contoso"
    ResourceProjAppSvc = "web"
    CreatedBy = "cli-user"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:8080/api/ResourceNamingRequests/RequestName" -Method POST -Headers $headers -Body $body
```

## 🔧 Configuration

### API Key Setup (Required)
The Azure Naming Tool requires an API key for name generation. Follow these steps:

1. **Access Admin Interface:**
   - Open http://localhost:8080/admin
   - Set up Global Admin Password on first visit

2. **Generate API Key:**
   - Login with admin password
   - Go to "Configuration" → "API Keys"
   - Generate either:
     - **Full Access API Key** (can do everything)
     - **Name Generation API Key** (only for name generation)

3. **Configure CLI Script:**
   - **Option A**: Edit `API_KEY="your-generated-key"` in `generate-name.sh`
   - **Option B**: Set environment variable: `export NAMING_API_KEY="your-generated-key"`

### API URL
Default: `http://localhost:8080`

Edit `API_URL=""` in `generate-name.sh` if using a different URL

## 📋 Common Resource Types

- `vm` - Virtual Machine
- `vmss` - Virtual Machine Scale Set  
- `storage` - Storage Account
- `sql` - SQL Database
- `webapp` - Web App
- `func` - Function App
- `aks` - Azure Kubernetes Service
- `vnet` - Virtual Network
- `subnet` - Subnet
- `nsg` - Network Security Group
- `pip` - Public IP Address

## 🚀 Quick Start

1. **Start Azure Naming Tool:**
   ```bash
   docker-compose up -d
   ```

2. **Make script executable (Linux/macOS):**
   ```bash
   chmod +x scripts/generate-name.sh
   ```

3. **Generate a name:**
   ```bash
   # First, explore available resource types
   ./scripts/generate-name.sh list-categories
   ./scripts/generate-name.sh list-types Compute

   # Then generate a name
   ./scripts/generate-name.sh generate --type vm --env prod --location eastus --org contoso --project web
   ```

## 📝 Output Examples

**Success:**
```
🔄 Generating name for:
  Resource Type: vm
  Environment: prod
  Location: eastus
  Organization: contoso
  Project: web

✅ Generated Name: vm-eus-contoso-web-prod-01
ℹ️  Note: Generated name follows standard naming convention
```

**With delimiter removal (Windows VM):**
```
✅ Generated Name: vmeuscontosoweb01
ℹ️  Note: Generated name with the selected delimiter is more than the maximum length for the selected resource type. The delimiter has been removed.
```

**Exploring resource types:**
```
📋 Available Resource Categories:
  Compute
  Storage
  Network
  Database
  Web

📋 Resource Types in Category: Compute
  vm - Microsoft.Compute/virtualMachines
  vmss - Microsoft.Compute/virtualMachineScaleSets - Windows
  aks - Microsoft.ContainerService/managedClusters
```
