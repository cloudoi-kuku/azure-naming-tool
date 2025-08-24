#!/bin/bash

# Azure Naming Tool Docker Build Script
# This script builds and optionally runs the Azure Naming Tool Docker container

set -e

# Configuration
IMAGE_NAME="azure-naming-tool"
CONTAINER_NAME="azure-naming-tool"
VERSION="latest"

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

# Help function
show_help() {
    echo "Azure Naming Tool Docker Build Script"
    echo ""
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -b, --build-only    Build the Docker image only (don't run)"
    echo "  -r, --run-only      Run existing Docker image (don't build)"
    echo "  -c, --clean         Clean up existing containers and images"
    echo "  -t, --tag TAG       Specify image tag (default: latest)"
    echo "  -p, --port PORT     Specify HTTP port (default: 8080)"
    echo "  -s, --https-port    Specify HTTPS port (default: 8081)"
    echo "  -h, --help          Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0                  Build and run the container"
    echo "  $0 -b               Build image only"
    echo "  $0 -r               Run existing image"
    echo "  $0 -c               Clean up and rebuild"
    echo "  $0 -t v1.0.0        Build with specific tag"
    echo "  $0 -p 9080          Use port 9080 for HTTP"
}

# Parse command line arguments
BUILD_ONLY=false
RUN_ONLY=false
CLEAN=false
HTTP_PORT=8080
HTTPS_PORT=8081

while [[ $# -gt 0 ]]; do
    case $1 in
        -b|--build-only)
            BUILD_ONLY=true
            shift
            ;;
        -r|--run-only)
            RUN_ONLY=true
            shift
            ;;
        -c|--clean)
            CLEAN=true
            shift
            ;;
        -t|--tag)
            VERSION="$2"
            shift 2
            ;;
        -p|--port)
            HTTP_PORT="$2"
            shift 2
            ;;
        -s|--https-port)
            HTTPS_PORT="$2"
            shift 2
            ;;
        -h|--help)
            show_help
            exit 0
            ;;
        *)
            print_error "Unknown option: $1"
            show_help
            exit 1
            ;;
    esac
done

# Clean up function
cleanup() {
    print_info "Cleaning up existing containers and images..."
    
    # Stop and remove container if it exists
    if docker ps -a --format 'table {{.Names}}' | grep -q "^${CONTAINER_NAME}$"; then
        print_info "Stopping and removing existing container: ${CONTAINER_NAME}"
        docker stop ${CONTAINER_NAME} >/dev/null 2>&1 || true
        docker rm ${CONTAINER_NAME} >/dev/null 2>&1 || true
    fi
    
    # Remove image if it exists
    if docker images --format 'table {{.Repository}}:{{.Tag}}' | grep -q "^${IMAGE_NAME}:${VERSION}$"; then
        print_info "Removing existing image: ${IMAGE_NAME}:${VERSION}"
        docker rmi ${IMAGE_NAME}:${VERSION} >/dev/null 2>&1 || true
    fi
    
    print_success "Cleanup completed"
}

# Build function
build_image() {
    print_info "Building Docker image: ${IMAGE_NAME}:${VERSION}"
    
    # Check if Dockerfile exists
    if [ ! -f "Dockerfile" ]; then
        print_error "Dockerfile not found in current directory"
        exit 1
    fi
    
    # Build the image
    docker build -t ${IMAGE_NAME}:${VERSION} .
    
    if [ $? -eq 0 ]; then
        print_success "Docker image built successfully: ${IMAGE_NAME}:${VERSION}"
    else
        print_error "Failed to build Docker image"
        exit 1
    fi
}

# Run function
run_container() {
    print_info "Running Docker container: ${CONTAINER_NAME}"
    print_info "HTTP Port: ${HTTP_PORT}"
    print_info "HTTPS Port: ${HTTPS_PORT}"
    
    # Check if image exists
    if ! docker images --format 'table {{.Repository}}:{{.Tag}}' | grep -q "^${IMAGE_NAME}:${VERSION}$"; then
        print_error "Docker image ${IMAGE_NAME}:${VERSION} not found. Build it first."
        exit 1
    fi
    
    # Stop existing container if running
    if docker ps --format 'table {{.Names}}' | grep -q "^${CONTAINER_NAME}$"; then
        print_warning "Container ${CONTAINER_NAME} is already running. Stopping it first."
        docker stop ${CONTAINER_NAME}
    fi
    
    # Remove existing container if it exists
    if docker ps -a --format 'table {{.Names}}' | grep -q "^${CONTAINER_NAME}$"; then
        print_info "Removing existing container: ${CONTAINER_NAME}"
        docker rm ${CONTAINER_NAME}
    fi
    
    # Run the container
    docker run -d \
        --name ${CONTAINER_NAME} \
        -p ${HTTP_PORT}:8080 \
        -p ${HTTPS_PORT}:8081 \
        -v azure-naming-tool-data:/app/data \
        -v azure-naming-tool-settings:/app/settings \
        -v azure-naming-tool-repository:/app/repository \
        ${IMAGE_NAME}:${VERSION}
    
    if [ $? -eq 0 ]; then
        print_success "Container started successfully: ${CONTAINER_NAME}"
        print_info "Application URLs:"
        print_info "  HTTP:  http://localhost:${HTTP_PORT}"
        print_info "  HTTPS: https://localhost:${HTTPS_PORT}"
        print_info ""
        print_info "To view logs: docker logs -f ${CONTAINER_NAME}"
        print_info "To stop: docker stop ${CONTAINER_NAME}"
    else
        print_error "Failed to start container"
        exit 1
    fi
}

# Main execution
print_info "Azure Naming Tool Docker Build Script"
print_info "======================================"

# Clean up if requested
if [ "$CLEAN" = true ]; then
    cleanup
fi

# Build image if not run-only
if [ "$RUN_ONLY" = false ]; then
    build_image
fi

# Run container if not build-only
if [ "$BUILD_ONLY" = false ]; then
    run_container
fi

print_success "Script completed successfully!"
