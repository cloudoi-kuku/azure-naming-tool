using AzureNamingTool.Models;

namespace AzureNamingTool.Interfaces
{
    /// <summary>
    /// Service interface for migrating data from JSON files to the database.
    /// </summary>
    public interface IDataMigrationService
    {
        /// <summary>
        /// Migrates data from JSON files to the database if migration is needed and enabled.
        /// </summary>
        /// <returns>True if migration was successful or not needed, otherwise false.</returns>
        Task<bool> MigrateFromJsonIfNeeded();

        /// <summary>
        /// Migrates generated names data from JSON file to the database.
        /// </summary>
        /// <param name="backupOriginal">Whether to create a backup of the original JSON file.</param>
        /// <returns>The migration result.</returns>
        Task<MigrationResult> MigrateFromJson(bool backupOriginal = true);

        /// <summary>
        /// Checks if JSON migration is needed.
        /// </summary>
        /// <returns>True if migration is needed, otherwise false.</returns>
        Task<bool> IsJsonMigrationNeeded();

        /// <summary>
        /// Validates the database schema and creates tables if needed.
        /// </summary>
        /// <returns>True if validation was successful, otherwise false.</returns>
        Task<bool> ValidateDatabaseSchema();

        /// <summary>
        /// Gets migration status information.
        /// </summary>
        /// <returns>Migration status information.</returns>
        Task<MigrationStatus> GetMigrationStatus();
    }

    /// <summary>
    /// Represents the status of data migration.
    /// </summary>
    public class MigrationStatus
    {
        /// <summary>
        /// Gets or sets a value indicating whether the database is initialized.
        /// </summary>
        public bool DatabaseInitialized { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether JSON migration is needed.
        /// </summary>
        public bool JsonMigrationNeeded { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether migration is enabled in configuration.
        /// </summary>
        public bool MigrationEnabled { get; set; }

        /// <summary>
        /// Gets or sets the number of records in the JSON file.
        /// </summary>
        public int JsonRecordCount { get; set; }

        /// <summary>
        /// Gets or sets the number of records in the database.
        /// </summary>
        public int DatabaseRecordCount { get; set; }

        /// <summary>
        /// Gets or sets the last migration date.
        /// </summary>
        public DateTime? LastMigrationDate { get; set; }

        /// <summary>
        /// Gets or sets any error messages.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
