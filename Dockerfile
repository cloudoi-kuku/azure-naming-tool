#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Create directories for data persistence
RUN mkdir -p /app/data /app/settings /app/repository

# Set permissions for data directories
RUN chmod 755 /app/data /app/settings /app/repository

EXPOSE 8080
EXPOSE 8081

ENV ASPNETCORE_HTTP_PORTS=80

# Environment variables for SQLite database
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/azurenamingtool.db"
ENV StorageSettings__UseDatabase=true
ENV StorageSettings__EnableMigration=true

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["AzureNamingTool.csproj", "."]
RUN dotnet restore "./AzureNamingTool.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "AzureNamingTool.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AzureNamingTool.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

# Create startup script for database initialization
RUN echo '#!/bin/bash\n\
set -e\n\
\n\
echo "Starting Azure Naming Tool initialization..."\n\
\n\
# Ensure data directory exists and has correct permissions\n\
mkdir -p /app/data /app/settings /app/repository\n\
chmod 755 /app/data /app/settings /app/repository\n\
\n\
# Set environment variables for debugging\n\
export ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT:-Production}\n\
export DOTNET_ENVIRONMENT=${DOTNET_ENVIRONMENT:-Production}\n\
\n\
echo "Environment: $ASPNETCORE_ENVIRONMENT"\n\
echo "Data directory: /app/data"\n\
echo "Database path: /app/data/azurenamingtool.db"\n\
\n\
echo "Database will be created automatically by the application using Entity Framework"\n\
\n\
echo "Starting Azure Naming Tool application..."\n\
# Start the application\n\
exec dotnet AzureNamingTool.dll\n\
' > /app/startup.sh && chmod +x /app/startup.sh

# Create volume mount points for persistent data
VOLUME ["/app/data", "/app/settings", "/app/repository"]

ENTRYPOINT ["/app/startup.sh"]
