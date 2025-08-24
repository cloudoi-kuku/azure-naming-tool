namespace AzureNamingTool.Models
{
    /// <summary>
    /// Represents the result of a data migration operation.
    /// </summary>
    public class MigrationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the migration was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets a descriptive message about the migration result.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of records successfully migrated.
        /// </summary>
        public int MigratedCount { get; set; }

        /// <summary>
        /// Gets or sets the number of records that failed to migrate.
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of records processed.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Gets or sets the list of error messages encountered during migration.
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Gets or sets the duration of the migration operation.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the migration started.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the migration completed.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets the path to the backup file created during migration.
        /// </summary>
        public string? BackupFilePath { get; set; }

        /// <summary>
        /// Gets the success rate as a percentage.
        /// </summary>
        public double SuccessRate => TotalCount > 0 ? (double)MigratedCount / TotalCount * 100 : 0;

        /// <summary>
        /// Gets a value indicating whether the migration completed without any errors.
        /// </summary>
        public bool IsCompleteSuccess => Success && ErrorCount == 0;
    }
}
