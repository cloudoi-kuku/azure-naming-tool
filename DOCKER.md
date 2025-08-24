# Azure Naming Tool - Docker Deployment Guide

This guide covers deploying the Azure Naming Tool with SQLite database support using Docker.

## Features

- **SQLite Database**: Persistent audit logging with automatic migrations
- **Volume Persistence**: Data, settings, and repository files persist across container restarts
- **Automatic Migration**: Existing JSON data is automatically migrated to SQLite on first run
- **Health Checks**: Built-in health monitoring
- **Environment Configuration**: Easy configuration via environment variables

## Quick Start

### Using Docker Compose (Recommended)

1. **Clone the repository and navigate to the project directory**
   ```bash
   git clone <repository-url>
   cd AzureNamingTool
   ```

2. **Build and run with Docker Compose**
   ```bash
   docker-compose up -d
   ```

3. **Access the application**
   - HTTP: http://localhost:8080
   - HTTPS: https://localhost:8081

### Using Docker CLI

1. **Build the image**
   ```bash
   docker build -t azure-naming-tool .
   ```

2. **Run the container**
   ```bash
   docker run -d \
     --name azure-naming-tool \
     -p 8080:8080 \
     -p 8081:8081 \
     -v azure-naming-tool-data:/app/data \
     -v azure-naming-tool-settings:/app/settings \
     -v azure-naming-tool-repository:/app/repository \
     azure-naming-tool
   ```

## Configuration

### Environment Variables

The application can be configured using environment variables:

#### Database Configuration
- `ConnectionStrings__DefaultConnection`: SQLite connection string (default: `Data Source=/app/data/azurenamingtool.db`)
- `StorageSettings__UseDatabase`: Enable database storage (default: `true`)
- `StorageSettings__EnableMigration`: Enable automatic data migration (default: `true`)

#### Application Settings
- `ASPNETCORE_ENVIRONMENT`: Environment (default: `Production`)
- `ASPNETCORE_HTTP_PORTS`: HTTP port (default: `8080`)
- `ASPNETCORE_HTTPS_PORTS`: HTTPS port (default: `8081`)

#### Feature Toggles
- `GeneratedNamesLogEnabled`: Enable audit logging page (default: `true`)
- `DuplicateNamesAllowed`: Allow duplicate names (default: `false`)
- `ConnectivityCheckEnabled`: Enable connectivity checks (default: `true`)

### Using .env File

1. **Copy the example environment file**
   ```bash
   cp .env.example .env
   ```

2. **Edit the .env file with your configuration**
   ```bash
   nano .env
   ```

3. **Run with environment file**
   ```bash
   docker-compose --env-file .env up -d
   ```

## Data Persistence

The application uses three persistent volumes:

- **`/app/data`**: SQLite database files
- **`/app/settings`**: Application settings and configuration
- **`/app/repository`**: JSON repository files (fallback storage)

### Backup and Restore

#### Backup
```bash
# Create backup directory
mkdir -p ./backups/$(date +%Y%m%d_%H%M%S)

# Backup database
docker cp azure-naming-tool:/app/data ./backups/$(date +%Y%m%d_%H%M%S)/data

# Backup settings
docker cp azure-naming-tool:/app/settings ./backups/$(date +%Y%m%d_%H%M%S)/settings

# Backup repository
docker cp azure-naming-tool:/app/repository ./backups/$(date +%Y%m%d_%H%M%S)/repository
```

#### Restore
```bash
# Stop the container
docker-compose down

# Restore data (replace BACKUP_DATE with your backup date)
docker cp ./backups/BACKUP_DATE/data azure-naming-tool:/app/
docker cp ./backups/BACKUP_DATE/settings azure-naming-tool:/app/
docker cp ./backups/BACKUP_DATE/repository azure-naming-tool:/app/

# Start the container
docker-compose up -d
```

## Database Migration

The application automatically handles database migration on startup:

1. **First Run**: Creates SQLite database and runs initial migrations
2. **Existing JSON Data**: Automatically migrates JSON data to SQLite database
3. **Subsequent Runs**: Applies any pending database migrations

### Manual Migration

If you need to run migrations manually:

```bash
# Access the container
docker exec -it azure-naming-tool bash

# Run migrations
dotnet ef database update

# Exit container
exit
```

## Monitoring and Logs

### View Logs
```bash
# View real-time logs
docker-compose logs -f

# View logs for specific service
docker-compose logs -f azurenamingtool
```

### Health Check
```bash
# Check container health
docker ps

# Manual health check
curl http://localhost:8080/health
```

## Troubleshooting

### Common Issues

1. **Database Permission Issues**
   ```bash
   # Fix permissions
   docker exec -it azure-naming-tool chmod 755 /app/data
   ```

2. **Migration Failures**
   ```bash
   # Check logs
   docker-compose logs azurenamingtool
   
   # Restart with fresh database
   docker-compose down
   docker volume rm azure-naming-tool-data
   docker-compose up -d
   ```

3. **Port Conflicts**
   ```bash
   # Change ports in docker-compose.yml
   ports:
     - "8090:8080"  # Change host port
     - "8091:8081"
   ```

### Reset Application

To completely reset the application:

```bash
# Stop and remove containers
docker-compose down

# Remove volumes (WARNING: This deletes all data)
docker volume rm azure-naming-tool-data
docker volume rm azure-naming-tool-settings
docker volume rm azure-naming-tool-repository

# Restart
docker-compose up -d
```

## Production Deployment

For production deployment, consider:

1. **Use HTTPS**: Configure proper SSL certificates
2. **Environment Variables**: Use secure methods to manage secrets
3. **Backup Strategy**: Implement regular automated backups
4. **Monitoring**: Set up proper logging and monitoring
5. **Resource Limits**: Configure appropriate CPU and memory limits

### Example Production docker-compose.yml

```yaml
version: '3.8'
services:
  azurenamingtool:
    build: .
    restart: always
    ports:
      - "80:8080"
      - "443:8081"
    volumes:
      - /opt/azure-naming-tool/data:/app/data
      - /opt/azure-naming-tool/settings:/app/settings
      - /opt/azure-naming-tool/repository:/app/repository
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    deploy:
      resources:
        limits:
          memory: 512M
        reservations:
          memory: 256M
```

## Support

For issues and questions:
- Check the application logs: `docker-compose logs`
- Review the troubleshooting section above
- Check the main project documentation
