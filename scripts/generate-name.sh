#!/bin/bash

# Azure Naming Tool CLI Script
# Usage: ./generate-name.sh [command] [options...]

# Configuration
API_URL="http://localhost:8080"
API_KEY="f855c41b-2d4c-4f6f-95c9-58e38775c6ce"  # Set your API key here if required

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Function to display usage
show_usage() {
    echo -e "${BLUE}Azure Naming Tool CLI${NC}"
    echo ""
    echo "Commands:"
    echo "  list-categories                    - List all resource categories"
    echo "  list-types [category]              - List resource types (optionally filtered by category)"
    echo "  search-types [search-term]         - Search for resource types"
    echo "  generate [options...]              - Generate a resource name"
    echo ""
    echo "Generate Usage:"
    echo "  $0 generate --type <shortname> --env <env> --location <loc> --org <org> --project <proj> [options]"
    echo ""
    echo "Generate Options:"
    echo "  --type <shortname>        Resource type short name (required)"
    echo "  --env <environment>       Environment (required)"
    echo "  --location <location>     Location (required)"
    echo "  --org <organization>      Organization (required)"
    echo "  --project <project>       Project/Application/Service (required)"
    echo "  --function <function>     Function (optional)"
    echo "  --unit <unit>             Unit/Department (optional)"
    echo "  --instance <instance>     Instance number (optional)"
    echo ""
    echo "Examples:"
    echo "  $0 list-categories"
    echo "  $0 list-types Compute"
    echo "  $0 search-types virtual"
    echo "  $0 generate --type vm --env prod --location eastus --org contoso --project web"
    echo "  $0 generate --type vmss --env dev --location westus2 --org myorg --project api --instance 01"
    echo ""
}

# Function to check API key
check_api_key() {
    if [ -z "$API_KEY" ]; then
        echo -e "${RED}❌ Error: API_KEY not set${NC}"
        echo ""
        echo "Please set your API key in this script or as an environment variable:"
        echo "  export NAMING_API_KEY=\"your-api-key\""
        echo ""
        echo "To get an API key:"
        echo "  1. Visit $API_URL/admin"
        echo "  2. Login with admin password"
        echo "  3. Generate a Name Generation API Key"
        echo ""
        exit 1
    fi
}

# Function to make API call with proper headers
api_call() {
    local method=$1
    local endpoint=$2
    local data=$3

    # The server's ApiKeyAttribute expects the header name "APIKey".
    # Keep "X-API-KEY" for backward compatibility with examples/tools that use the alternative name.
    # Use an array so header arguments are passed correctly to curl (avoids word-splitting issues).
    local headers
    headers=( -H "Content-Type: application/json" -H "APIKey: $API_KEY" -H "X-API-KEY: $API_KEY" )

    if [ -z "$data" ]; then
        curl -s -X "$method" "$API_URL$endpoint" "${headers[@]}"
    else
        curl -s -X "$method" "$API_URL$endpoint" "${headers[@]}" -d "$data"
    fi
}

# Function to list resource categories
list_categories() {
    echo -e "${BLUE}📋 Available Resource Categories:${NC}"
    echo ""

    local response=$(api_call "GET" "/api/ResourceTypes")

    if [[ "$response" == *"Api Key"* ]]; then
        echo -e "${RED}❌ API Error: $response${NC}"
        exit 1
    fi

    # Extract unique categories
    local categories=$(echo "$response" | jq -r '.[].resource' | cut -d'/' -f1 | sort -u)

    echo "$categories" | while read -r category; do
        if [ ! -z "$category" ]; then
            echo -e "  ${CYAN}$category${NC}"
        fi
    done
    echo ""
}

# Function to list resource types
list_types() {
    local filter_category=$1

    if [ -z "$filter_category" ]; then
        echo -e "${BLUE}📋 Available Resource Types:${NC}"
    else
        echo -e "${BLUE}📋 Resource Types in Category: $filter_category${NC}"
    fi
    echo ""

    local response=$(api_call "GET" "/api/ResourceTypes")

    if [[ "$response" == *"Api Key"* ]]; then
        echo -e "${RED}❌ API Error: $response${NC}"
        exit 1
    fi

    # Filter and display types
    echo "$response" | jq -r '.[] | select(.enabled == true) | "\(.resource)|\(.property)|\(.ShortName)|\(.id)"' | while IFS='|' read -r resource property shortname id; do
        if [ -z "$filter_category" ] || [[ "$resource" == "$filter_category"* ]]; then
            if [ ! -z "$property" ] && [ "$property" != "null" ]; then
                echo -e "  ${GREEN}$shortname${NC} - $resource - $property"
            else
                echo -e "  ${GREEN}$shortname${NC} - $resource"
            fi
        fi
    done
    echo ""
}

# Function to search resource types
search_types() {
    local search_term=$1

    if [ -z "$search_term" ]; then
        echo -e "${RED}❌ Error: Search term required${NC}"
        exit 1
    fi

    echo -e "${BLUE}🔍 Searching for: $search_term${NC}"
    echo ""

    local response=$(api_call "GET" "/api/ResourceTypes")

    if [[ "$response" == *"Api Key"* ]]; then
        echo -e "${RED}❌ API Error: $response${NC}"
        exit 1
    fi

    # Search and display matching types
    local found=false
    echo "$response" | jq -r '.[] | select(.enabled == true) | "\(.resource)|\(.property)|\(.ShortName)|\(.id)"' | while IFS='|' read -r resource property shortname id; do
        if [[ "$resource" == *"$search_term"* ]] || [[ "$property" == *"$search_term"* ]] || [[ "$shortname" == *"$search_term"* ]]; then
            if [ ! -z "$property" ] && [ "$property" != "null" ]; then
                echo -e "  ${GREEN}$shortname${NC} - $resource - $property"
            else
                echo -e "  ${GREEN}$shortname${NC} - $resource"
            fi
            found=true
        fi
    done

    if [ "$found" = false ]; then
        echo -e "${YELLOW}No resource types found matching: $search_term${NC}"
    fi
    echo ""
}

# Function to generate name
generate_name() {
    local type_shortname=""
    local env=""
    local location=""
    local org=""
    local project=""
    local function=""
    local unit=""
    local instance=""

    # Parse arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            --type)
                type_shortname="$2"
                shift 2
                ;;
            --env)
                env="$2"
                shift 2
                ;;
            --location)
                location="$2"
                shift 2
                ;;
            --org)
                org="$2"
                shift 2
                ;;
            --project)
                project="$2"
                shift 2
                ;;
            --function)
                function="$2"
                shift 2
                ;;
            --unit)
                unit="$2"
                shift 2
                ;;
            --instance)
                instance="$2"
                shift 2
                ;;
            *)
                echo -e "${RED}❌ Unknown option: $1${NC}"
                show_usage
                exit 1
                ;;
        esac
    done

    # Validate required parameters
    if [ -z "$type_shortname" ] || [ -z "$env" ] || [ -z "$location" ] || [ -z "$org" ] || [ -z "$project" ]; then
        echo -e "${RED}❌ Error: Missing required parameters${NC}"
        echo ""
        echo "Required: --type, --env, --location, --org, --project"
        echo ""
        show_usage
        exit 1
    fi

    echo -e "${YELLOW}🔄 Generating name for:${NC}"
    echo "  Resource Type: $type_shortname"
    echo "  Environment: $env"
    echo "  Location: $location"
    echo "  Organization: $org"
    echo "  Project: $project"
    if [ ! -z "$function" ]; then echo "  Function: $function"; fi
    if [ ! -z "$unit" ]; then echo "  Unit: $unit"; fi
    if [ ! -z "$instance" ]; then echo "  Instance: $instance"; fi
    echo ""

    # Get resource type details
    local response=$(api_call "GET" "/api/ResourceTypes")

    if [[ "$response" == *"Api Key"* ]]; then
        echo -e "${RED}❌ API Error: $response${NC}"
        exit 1
    fi

    # Find the resource type by short name
    local resource_type_info=$(echo "$response" | jq -r ".[] | select(.ShortName == \"$type_shortname\" and .enabled == true) | \"\(.id)|\(.resource)|\(.property)\"" | head -n1)

    if [ -z "$resource_type_info" ]; then
        echo -e "${RED}❌ Error: Resource type '$type_shortname' not found or not enabled${NC}"
        echo ""
        echo "Use '$0 list-types' to see available resource types"
        exit 1
    fi

    IFS='|' read -r resource_id resource_name resource_property <<< "$resource_type_info"

    # Prepare API request
    local json_payload=$(cat <<EOF
{
    "ResourceType": "$type_shortname",
    "ResourceId": $resource_id,
    "ResourceEnvironment": "$env",
    "ResourceLocation": "$location",
    "ResourceOrg": "$org",
    "ResourceProjAppSvc": "$project",
    "ResourceFunction": "$function",
    "ResourceUnitDept": "$unit",
    "ResourceInstance": "$instance",
    "CreatedBy": "cli-$(whoami)"
}
EOF
)

    # Make API call
    local response=$(api_call "POST" "/api/ResourceNamingRequests/RequestName" "$json_payload")

    # Parse response
    local resource_name=$(echo "$response" | jq -r '.resourceName // empty')
    local success=$(echo "$response" | jq -r '.success // false')
    local message=$(echo "$response" | jq -r '.message // empty')

    if [ "$success" = "true" ] && [ ! -z "$resource_name" ]; then
        echo -e "${GREEN}✅ Generated Name: $resource_name${NC}"
        if [ ! -z "$message" ] && [ "$message" != "null" ]; then
            echo -e "${YELLOW}ℹ️  Note: $message${NC}"
        fi
    else
        echo -e "${RED}❌ Failed to generate name${NC}"
        if [ ! -z "$message" ] && [ "$message" != "null" ]; then
            echo -e "${RED}Error: $message${NC}"
        else
            echo -e "${RED}Error: $response${NC}"
        fi
        exit 1
    fi
}

# Main script
if [ $# -eq 0 ]; then
    show_usage
    exit 0
fi

# Check if curl and jq are available
if ! command -v curl &> /dev/null; then
    echo -e "${RED}❌ Error: curl is required but not installed${NC}"
    exit 1
fi

if ! command -v jq &> /dev/null; then
    echo -e "${RED}❌ Error: jq is required but not installed${NC}"
    echo "Install with: sudo apt-get install jq (Ubuntu) or brew install jq (macOS)"
    exit 1
fi

# Use environment variable if API_KEY is not set in script
if [ -z "$API_KEY" ] && [ ! -z "$NAMING_API_KEY" ]; then
    API_KEY="$NAMING_API_KEY"
fi

# Parse command
case $1 in
    list-categories)
        check_api_key
        list_categories
        ;;
    list-types)
        check_api_key
        list_types "$2"
        ;;
    search-types)
        check_api_key
        search_types "$2"
        ;;
    generate)
        check_api_key
        shift
        generate_name "$@"
        ;;
    *)
        echo -e "${RED}❌ Unknown command: $1${NC}"
        echo ""
        show_usage
        exit 1
        ;;
esac
