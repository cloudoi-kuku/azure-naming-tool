# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Build and Run
```bash
# Build the project
dotnet build

# Run the application
dotnet run

# Run in development mode with hot reload
dotnet watch run

# Build for release
dotnet build -c Release

# Publish for deployment
dotnet publish -c Release
```

### Docker
```bash
# Build Docker image
docker build -t azurenamingtool .

# Run Docker container
docker run -p 8080:80 -p 8081:443 azurenamingtool
```

### Testing
```bash
# Run tests (if test project exists)
dotnet test
```

## Architecture Overview

### Technology Stack
- **Framework**: ASP.NET Core 8.0 with Blazor Server-Side Rendering
- **UI Components**: Blazor Components with Bootstrap CSS
- **API**: RESTful API with Swagger documentation
- **Storage**: JSON file-based storage in `/repository` directory
- **Authentication**: API key-based authentication for API endpoints

### Project Structure

The application follows a standard ASP.NET Core Blazor architecture:

- **Components/** - Blazor UI components organized by feature
  - `Pages/` - Routable page components (Generate, Configuration, Admin, etc.)
  - `Modals/` - Reusable modal dialog components
  - `General/` - Shared UI components
  - `Instructions/` - Help/documentation components
  - `Layout/` - Application layout components

- **Controllers/** - API controllers for programmatic access to naming functionality
  - Each controller corresponds to a resource component type
  - All require API key authentication via `[ApiKey]` attribute

- **Services/** - Business logic layer
  - Service classes handle CRUD operations for each component type
  - Services interact with JSON storage via FileSystemHelper

- **Models/** - Data models and DTOs
  - Resource component models (ResourceType, ResourceLocation, etc.)
  - Request/Response models for API operations
  - Configuration and state management models

- **Helpers/** - Utility classes
  - `FileSystemHelper` - JSON file persistence
  - `ConfigurationHelper` - App configuration management
  - `GeneralHelper` - Common utilities
  - `ValidationHelper` - Name validation logic

### Key Features

1. **Name Generation**: Core functionality in `/generate` page and `ResourceNamingRequestService`
2. **Configuration Management**: Component configuration via `/configuration` page
3. **API Access**: Full API with Swagger UI at `/swagger`
4. **Admin Functions**: Protected admin area for tool configuration
5. **Audit Logging**: Generated names tracked in log with metadata

### Data Flow

1. UI components interact with Services via dependency injection
2. Services use FileSystemHelper to persist/retrieve JSON data from `/repository`
3. API controllers expose service functionality with API key authentication
4. State management via StateContainer singleton for UI session data

### Configuration Storage

All configuration data is stored as JSON files in the `/repository` directory:
- Component definitions (resourcetypes.json, resourcelocations.json, etc.)
- Generated names log (generatednames.json)
- Admin configuration (adminusers.json)
- Policy definitions (namePolicyDefinition.json)