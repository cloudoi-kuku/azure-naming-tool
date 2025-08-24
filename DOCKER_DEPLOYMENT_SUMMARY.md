# Azure Naming Tool - Docker Deployment with SQLite Database

## Overview

The Azure Naming Tool has been enhanced with SQLite database support and optimized Docker deployment. This implementation provides persistent audit logging, automatic data migration, and production-ready containerization.

## 🚀 Quick Start

### Option 1: Using Docker Compose (Recommended)

```bash
# Build and run with Docker Compose
docker-compose up -d

# Access the application
# HTTP:  http://localhost:8080
# HTTPS: https://localhost:8081
```

### Option 2: Using the Build Script

```bash
# Make the script executable (if not already)
chmod +x build-docker.sh

# Build and run
./build-docker.sh

# Or with custom options
./build-docker.sh -p 9080 -s 9081  # Custom ports
./build-docker.sh -t v1.0.0        # Custom tag
./build-docker.sh -c               # Clean rebuild
```

### Option 3: Manual Docker Commands

```bash
# Build the image
docker build -t azure-naming-tool .

# Run the container
docker run -d \
  --name azure-naming-tool \
  -p 8080:8080 \
  -p 8081:8081 \
  -v azure-naming-tool-data:/app/data \
  -v azure-naming-tool-settings:/app/settings \
  -v azure-naming-tool-repository:/app/repository \
  azure-naming-tool
```

## 📁 Files Added/Modified for Docker Support

### New Files Created:
- **`docker-compose.yml`** - Complete Docker Compose configuration
- **`.env.example`** - Environment variables template
- **`DOCKER.md`** - Comprehensive Docker deployment guide
- **`build-docker.sh`** - Automated build and deployment script
- **`DOCKER_DEPLOYMENT_SUMMARY.md`** - This summary document

### Modified Files:
- **`Dockerfile`** - Enhanced with SQLite support, Entity Framework tools, and automatic migrations
- **`.dockerignore`** - Updated to exclude database files and include necessary components

## 🔧 Key Docker Enhancements

### 1. SQLite Database Integration
- **Persistent Storage**: Database files stored in `/app/data` volume
- **Automatic Migrations**: Database schema created automatically on first run
- **Data Migration**: Existing JSON data automatically migrated to SQLite
- **Fallback Support**: Maintains JSON storage compatibility

### 2. Enhanced Dockerfile Features
- **Multi-stage Build**: Optimized build process with separate build and runtime stages
- **Entity Framework Tools**: Includes EF Core tools for database operations
- **Startup Script**: Intelligent initialization with database setup
- **Volume Mounts**: Proper persistent storage configuration
- **Environment Variables**: Comprehensive configuration options

### 3. Production-Ready Features
- **Health Checks**: Built-in application health monitoring
- **Proper Permissions**: Secure file system permissions
- **Resource Management**: Configurable CPU and memory limits
- **Logging**: Structured logging with configurable levels
- **Security**: Non-root user execution and secure defaults

## 🗄️ Database Configuration

### Default Settings
```bash
# Database location
ConnectionStrings__DefaultConnection=Data Source=/app/data/azurenamingtool.db

# Enable database storage
StorageSettings__UseDatabase=true

# Enable automatic migration
StorageSettings__EnableMigration=true
```

### Volume Persistence
The application uses three persistent volumes:
- **`/app/data`**: SQLite database files
- **`/app/settings`**: Application configuration files
- **`/app/repository`**: JSON repository files (fallback storage)

## 🔄 Data Migration Process

The application automatically handles data migration:

1. **First Run**: Creates SQLite database and applies initial schema
2. **Existing Data**: Migrates JSON data to SQLite database
3. **Subsequent Runs**: Applies any pending database migrations
4. **Fallback**: Maintains JSON storage for compatibility

## 📊 Monitoring and Management

### View Logs
```bash
# Real-time logs
docker-compose logs -f

# Specific service logs
docker logs -f azure-naming-tool
```

### Database Management
```bash
# Access container shell
docker exec -it azure-naming-tool bash

# Run manual migrations
dotnet ef database update

# Check database status
ls -la /app/data/
```

### Health Monitoring
```bash
# Check container health
docker ps

# Manual health check
curl http://localhost:8080/health
```

## 🔧 Configuration Options

### Environment Variables
Copy `.env.example` to `.env` and customize:

```bash
# Core database settings
ConnectionStrings__DefaultConnection=Data Source=/app/data/azurenamingtool.db
StorageSettings__UseDatabase=true
StorageSettings__EnableMigration=true

# Feature toggles
GeneratedNamesLogEnabled=true
DuplicateNamesAllowed=false
ConnectivityCheckEnabled=true

# Security settings (optional - auto-generated if not provided)
AdminPassword=your-secure-password
APIKey=your-api-key
```

### Port Configuration
Modify `docker-compose.yml` to change ports:
```yaml
ports:
  - "9080:8080"  # HTTP
  - "9081:8081"  # HTTPS
```

## 🔒 Security Considerations

### Production Deployment
1. **Use HTTPS**: Configure proper SSL certificates
2. **Secure Secrets**: Use Docker secrets or external secret management
3. **Network Security**: Implement proper firewall rules
4. **Regular Updates**: Keep base images and dependencies updated
5. **Backup Strategy**: Implement automated database backups

### Backup and Restore
```bash
# Backup
docker cp azure-naming-tool:/app/data ./backup-$(date +%Y%m%d)

# Restore
docker cp ./backup-20240101/data azure-naming-tool:/app/
docker restart azure-naming-tool
```

## 🚨 Troubleshooting

### Common Issues

1. **Port Conflicts**
   ```bash
   # Change ports in docker-compose.yml
   ports:
     - "8090:8080"
     - "8091:8081"
   ```

2. **Database Permission Issues**
   ```bash
   docker exec -it azure-naming-tool chmod 755 /app/data
   ```

3. **Migration Failures**
   ```bash
   # Check logs
   docker logs azure-naming-tool
   
   # Reset database
   docker-compose down
   docker volume rm azure-naming-tool-data
   docker-compose up -d
   ```

### Reset Everything
```bash
# Complete reset (WARNING: Deletes all data)
docker-compose down
docker volume rm azure-naming-tool-data azure-naming-tool-settings azure-naming-tool-repository
docker-compose up -d
```

## 📈 Performance Optimization

### Resource Limits
Add to `docker-compose.yml`:
```yaml
deploy:
  resources:
    limits:
      memory: 512M
      cpus: '0.5'
    reservations:
      memory: 256M
      cpus: '0.25'
```

### Database Optimization
- SQLite performs well for typical workloads
- Consider PostgreSQL for high-volume environments
- Regular database maintenance and backups recommended

## 🎯 Next Steps

1. **Deploy**: Use the provided Docker configuration for deployment
2. **Configure**: Customize environment variables for your environment
3. **Monitor**: Set up logging and monitoring for production use
4. **Backup**: Implement regular backup procedures
5. **Scale**: Consider load balancing for high-availability deployments

## 📞 Support

For issues and questions:
- Check the application logs: `docker logs azure-naming-tool`
- Review the troubleshooting section above
- Consult the main project documentation
- Check Docker and database configuration

The enhanced Docker deployment provides a robust, scalable, and production-ready solution for the Azure Naming Tool with comprehensive audit logging capabilities.
