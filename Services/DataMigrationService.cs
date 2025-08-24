using AzureNamingTool.Data;
using AzureNamingTool.Data.Entities;
using AzureNamingTool.Helpers;
using AzureNamingTool.Interfaces;
using AzureNamingTool.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;

namespace AzureNamingTool.Services
{
    /// <summary>
    /// Service for migrating data from JSON files to the database.
    /// </summary>
    public class DataMigrationService : IDataMigrationService
    {
        private readonly AzureNamingToolDbContext _context;
        private readonly ILogger<DataMigrationService> _logger;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataMigrationService"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="configuration">The configuration.</param>
        public DataMigrationService(
            AzureNamingToolDbContext context,
            ILogger<DataMigrationService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        /// <inheritdoc/>
        public async Task<bool> MigrateFromJsonIfNeeded()
        {
            try
            {
                // Check if migration is enabled
                if (!_configuration.GetValue<bool>("StorageSettings:EnableMigration", true))
                {
                    _logger.LogInformation("Migration is disabled in configuration");
                    return true;
                }

                // Check if migration is needed
                if (!await IsJsonMigrationNeeded())
                {
                    _logger.LogInformation("JSON migration is not needed");
                    return true;
                }

                // Perform migration
                var result = await MigrateFromJson(true);
                
                if (result.Success)
                {
                    _logger.LogInformation("Automatic migration completed successfully: {Message}", result.Message);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Automatic migration failed: {Message}", result.Message);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to perform automatic migration");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<MigrationResult> MigrateFromJson(bool backupOriginal = true)
        {
            var result = new MigrationResult
            {
                StartTime = DateTime.UtcNow
            };
            
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Check if migration is enabled
                if (!_configuration.GetValue<bool>("StorageSettings:EnableMigration", true))
                {
                    result.Success = false;
                    result.Message = "Migration is disabled in configuration";
                    return result;
                }

                // Ensure database is created
                await _context.Database.EnsureCreatedAsync();

                // Read existing JSON data
                var jsonData = await FileSystemHelper.ReadFile("generatednames.json");
                if (string.IsNullOrEmpty(jsonData) || jsonData == "[]")
                {
                    result.Success = true;
                    result.Message = "No data to migrate - JSON file is empty";
                    return result;
                }

                var existingNames = JsonSerializer.Deserialize<List<GeneratedName>>(jsonData);
                if (existingNames == null || !existingNames.Any())
                {
                    result.Success = true;
                    result.Message = "No valid data found in JSON file";
                    return result;
                }

                result.TotalCount = existingNames.Count;

                // Backup original file if requested
                if (backupOriginal)
                {
                    var backupFileName = $"generatednames_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                    await FileSystemHelper.WriteFile(backupFileName, jsonData);
                    result.BackupFilePath = backupFileName;
                    _logger.LogInformation("Created backup file: {BackupFileName}", backupFileName);
                }

                // Migrate each record
                var migratedCount = 0;
                var errors = new List<string>();

                using var transaction = await _context.Database.BeginTransactionAsync();
                
                try
                {
                    foreach (var name in existingNames)
                    {
                        try
                        {
                            // Check if record already exists
                            var existingEntity = await _context.GeneratedNames
                                .FirstOrDefaultAsync(g => g.ResourceName == name.ResourceName && 
                                                         g.User == name.User && 
                                                         g.CreatedOn == name.CreatedOn);

                            if (existingEntity != null)
                            {
                                _logger.LogDebug("Skipping duplicate record: {ResourceName}", name.ResourceName);
                                continue;
                            }

                            var entity = new GeneratedNameEntity
                            {
                                CreatedOn = name.CreatedOn,
                                ResourceName = name.ResourceName,
                                ResourceTypeName = name.ResourceTypeName,
                                User = name.User,
                                Message = name.Message,
                                CreatedBy = "Migration",
                                Components = name.Components.Select((component, index) => 
                                    new GeneratedNameComponentEntity
                                    {
                                        ComponentName = component.Length > 0 ? component[0] : "Unknown",
                                        ComponentValue = component.Length > 1 ? component[1] : "",
                                        SortOrder = index
                                    }).ToList()
                            };

                            _context.GeneratedNames.Add(entity);
                            migratedCount++;

                            // Save in batches to avoid memory issues
                            if (migratedCount % 100 == 0)
                            {
                                await _context.SaveChangesAsync();
                                _logger.LogDebug("Migrated {Count} records so far", migratedCount);
                            }
                        }
                        catch (Exception ex)
                        {
                            var error = $"Failed to migrate record for resource '{name.ResourceName}': {ex.Message}";
                            errors.Add(error);
                            _logger.LogWarning(ex, "Failed to migrate record for resource {ResourceName}", name.ResourceName);
                        }
                    }

                    // Save any remaining changes
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    result.Success = errors.Count == 0;
                    result.MigratedCount = migratedCount;
                    result.ErrorCount = errors.Count;
                    result.Errors = errors;
                    result.Message = $"Migrated {migratedCount} of {existingNames.Count} records";

                    if (errors.Count > 0)
                    {
                        result.Message += $" with {errors.Count} errors";
                    }

                    _logger.LogInformation("Migration completed: {Message}", result.Message);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Migration failed with exception");
                result.Success = false;
                result.Message = $"Migration failed: {ex.Message}";
                result.Errors.Add(ex.Message);
            }
            finally
            {
                stopwatch.Stop();
                result.EndTime = DateTime.UtcNow;
                result.Duration = stopwatch.Elapsed;
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<bool> IsJsonMigrationNeeded()
        {
            try
            {
                // Check if database has any records
                var dbRecordCount = await _context.GeneratedNames.CountAsync();
                if (dbRecordCount > 0)
                {
                    _logger.LogDebug("Database already contains {Count} records, migration not needed", dbRecordCount);
                    return false;
                }

                // Check if JSON file has records
                var jsonData = await FileSystemHelper.ReadFile("generatednames.json");
                if (string.IsNullOrEmpty(jsonData) || jsonData == "[]")
                {
                    _logger.LogDebug("JSON file is empty, migration not needed");
                    return false;
                }

                var existingNames = JsonSerializer.Deserialize<List<GeneratedName>>(jsonData);
                var hasJsonRecords = existingNames != null && existingNames.Any();

                _logger.LogDebug("JSON migration needed: {Needed} (JSON records: {JsonCount}, DB records: {DbCount})", 
                    hasJsonRecords, existingNames?.Count ?? 0, dbRecordCount);

                return hasJsonRecords;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if JSON migration is needed");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ValidateDatabaseSchema()
        {
            try
            {
                // Ensure database and tables are created
                await _context.Database.EnsureCreatedAsync();
                
                // Test basic operations
                var canQuery = await _context.GeneratedNames.AnyAsync();
                
                _logger.LogInformation("Database schema validation successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database schema validation failed");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<MigrationStatus> GetMigrationStatus()
        {
            var status = new MigrationStatus();

            try
            {
                // Check if database is initialized
                status.DatabaseInitialized = await _context.Database.CanConnectAsync();
                
                if (status.DatabaseInitialized)
                {
                    status.DatabaseRecordCount = await _context.GeneratedNames.CountAsync();
                }

                // Check migration configuration
                status.MigrationEnabled = _configuration.GetValue<bool>("StorageSettings:EnableMigration", true);

                // Check JSON file
                try
                {
                    var jsonData = await FileSystemHelper.ReadFile("generatednames.json");
                    if (!string.IsNullOrEmpty(jsonData) && jsonData != "[]")
                    {
                        var existingNames = JsonSerializer.Deserialize<List<GeneratedName>>(jsonData);
                        status.JsonRecordCount = existingNames?.Count ?? 0;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read JSON file for migration status");
                    status.ErrorMessage = $"Failed to read JSON file: {ex.Message}";
                }

                // Determine if migration is needed
                status.JsonMigrationNeeded = await IsJsonMigrationNeeded();

                // Try to get last migration date from logs or database
                // This is a simplified approach - in a real scenario, you might store this in a separate table
                if (status.DatabaseRecordCount > 0)
                {
                    var oldestRecord = await _context.GeneratedNames
                        .Where(g => g.CreatedBy == "Migration")
                        .OrderBy(g => g.CreatedOn)
                        .FirstOrDefaultAsync();
                    
                    if (oldestRecord != null)
                    {
                        status.LastMigrationDate = oldestRecord.CreatedOn;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get migration status");
                status.ErrorMessage = ex.Message;
            }

            return status;
        }
    }
}
