#!/bin/bash

# Azure Naming Tool Debug Script
# This script helps debug Docker deployment issues

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Functions
print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_section() {
    echo ""
    echo -e "${BLUE}========================================${NC}"
    echo -e "${BLUE} $1${NC}"
    echo -e "${BLUE}========================================${NC}"
}

# Configuration
CONTAINER_NAME="azure-naming-tool"
IMAGE_NAME="azure-naming-tool"

print_section "Azure Naming Tool Debug Information"

# Check if Docker is running
print_info "Checking Docker status..."
if ! docker info >/dev/null 2>&1; then
    print_error "Docker is not running or not accessible"
    exit 1
fi
print_success "Docker is running"

# Check if container exists
print_info "Checking container status..."
if docker ps -a --format 'table {{.Names}}' | grep -q "^${CONTAINER_NAME}$"; then
    CONTAINER_STATUS=$(docker inspect --format='{{.State.Status}}' ${CONTAINER_NAME})
    print_info "Container ${CONTAINER_NAME} exists with status: ${CONTAINER_STATUS}"
    
    if [ "$CONTAINER_STATUS" = "running" ]; then
        print_success "Container is running"
        
        # Check container health
        print_info "Checking container health..."
        HEALTH_STATUS=$(docker inspect --format='{{.State.Health.Status}}' ${CONTAINER_NAME} 2>/dev/null || echo "no-health-check")
        print_info "Health status: ${HEALTH_STATUS}"
        
        # Check if ports are accessible
        print_info "Checking port accessibility..."
        if curl -f http://localhost:8080/health >/dev/null 2>&1; then
            print_success "HTTP port 8080 is accessible"
        else
            print_warning "HTTP port 8080 is not accessible"
        fi
        
        # Get container logs
        print_section "Recent Container Logs"
        docker logs --tail 50 ${CONTAINER_NAME}
        
        # Check database status
        print_section "Database Status"
        print_info "Checking database connectivity..."
        if curl -f http://localhost:8080/debug/database >/dev/null 2>&1; then
            print_info "Database debug endpoint response:"
            curl -s http://localhost:8080/debug/database | jq . 2>/dev/null || curl -s http://localhost:8080/debug/database
        else
            print_warning "Database debug endpoint not accessible"
        fi
        
        # Check file system inside container
        print_section "Container File System"
        print_info "Checking data directory..."
        docker exec ${CONTAINER_NAME} ls -la /app/data/ 2>/dev/null || print_warning "Cannot access /app/data directory"
        
        print_info "Checking database file..."
        docker exec ${CONTAINER_NAME} ls -la /app/data/azurenamingtool.db 2>/dev/null || print_warning "Database file not found"
        
        print_info "Checking application files..."
        docker exec ${CONTAINER_NAME} ls -la /app/ | head -10
        
        # Check environment variables
        print_section "Environment Variables"
        print_info "Database-related environment variables:"
        docker exec ${CONTAINER_NAME} env | grep -E "(ConnectionStrings|StorageSettings|ASPNETCORE)" || print_warning "No relevant environment variables found"
        
    else
        print_warning "Container is not running"
        print_info "Getting container logs..."
        docker logs --tail 50 ${CONTAINER_NAME}
    fi
else
    print_warning "Container ${CONTAINER_NAME} does not exist"
fi

# Check if image exists
print_info "Checking image status..."
if docker images --format 'table {{.Repository}}:{{.Tag}}' | grep -q "^${IMAGE_NAME}:"; then
    print_success "Image ${IMAGE_NAME} exists"
    docker images | grep ${IMAGE_NAME}
else
    print_warning "Image ${IMAGE_NAME} does not exist"
fi

# Check volumes
print_section "Volume Information"
print_info "Checking Docker volumes..."
docker volume ls | grep azure-naming-tool || print_warning "No Azure Naming Tool volumes found"

if docker volume ls | grep -q azure-naming-tool-data; then
    print_info "Data volume contents:"
    docker run --rm -v azure-naming-tool-data:/data alpine ls -la /data/ 2>/dev/null || print_warning "Cannot inspect data volume"
fi

# Check network connectivity
print_section "Network Connectivity"
print_info "Checking port bindings..."
docker port ${CONTAINER_NAME} 2>/dev/null || print_warning "Cannot get port information"

print_info "Checking if ports are in use..."
netstat -tuln | grep -E ":808[01]" || print_info "Ports 8080/8081 are not in use by other processes"

# Provide troubleshooting suggestions
print_section "Troubleshooting Suggestions"

if [ "$CONTAINER_STATUS" != "running" ]; then
    print_info "Container is not running. Try:"
    echo "  docker-compose up -d"
    echo "  or"
    echo "  ./build-docker.sh"
fi

print_info "To view real-time logs:"
echo "  docker logs -f ${CONTAINER_NAME}"

print_info "To access container shell:"
echo "  docker exec -it ${CONTAINER_NAME} bash"

print_info "To restart the container:"
echo "  docker restart ${CONTAINER_NAME}"

print_info "To rebuild completely:"
echo "  ./build-docker.sh -c"

print_info "To check application URLs:"
echo "  HTTP:  http://localhost:8080"
echo "  HTTPS: https://localhost:8081"
echo "  Health: http://localhost:8080/health"
echo "  Debug:  http://localhost:8080/debug/database"

print_section "Debug Complete"
print_info "If issues persist, check the container logs and database connectivity"
