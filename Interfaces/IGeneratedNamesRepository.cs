using AzureNamingTool.Data.Entities;
using AzureNamingTool.Models;

namespace AzureNamingTool.Interfaces
{
    /// <summary>
    /// Repository interface for managing generated names in the database.
    /// </summary>
    public interface IGeneratedNamesRepository
    {
        /// <summary>
        /// Creates a new generated name record in the database.
        /// </summary>
        /// <param name="entity">The generated name entity to create.</param>
        /// <returns>The created entity with assigned ID.</returns>
        Task<GeneratedNameEntity> CreateAsync(GeneratedNameEntity entity);

        /// <summary>
        /// Gets a generated name by its ID.
        /// </summary>
        /// <param name="id">The ID of the generated name.</param>
        /// <returns>The generated name entity if found, otherwise null.</returns>
        Task<GeneratedNameEntity?> GetByIdAsync(long id);

        /// <summary>
        /// Updates an existing generated name record.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <returns>True if the update was successful, otherwise false.</returns>
        Task<bool> UpdateAsync(GeneratedNameEntity entity);

        /// <summary>
        /// Permanently deletes a generated name record.
        /// </summary>
        /// <param name="id">The ID of the record to delete.</param>
        /// <returns>True if the deletion was successful, otherwise false.</returns>
        Task<bool> DeleteAsync(long id);

        /// <summary>
        /// Soft deletes a generated name record by marking it as deleted.
        /// </summary>
        /// <param name="id">The ID of the record to soft delete.</param>
        /// <returns>True if the soft deletion was successful, otherwise false.</returns>
        Task<bool> SoftDeleteAsync(long id);

        /// <summary>
        /// Gets a paginated list of generated names with optional filtering.
        /// </summary>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="filter">Optional filter criteria.</param>
        /// <returns>A paginated result containing the generated names.</returns>
        Task<PagedResult<GeneratedNameEntity>> GetPagedAsync(int page, int pageSize, GeneratedNameFilter? filter = null);

        /// <summary>
        /// Gets generated names for a specific user.
        /// </summary>
        /// <param name="user">The user name.</param>
        /// <param name="limit">The maximum number of records to return.</param>
        /// <returns>A collection of generated names for the user.</returns>
        Task<IEnumerable<GeneratedNameEntity>> GetByUserAsync(string user, int limit = 100);

        /// <summary>
        /// Gets the most recently generated names.
        /// </summary>
        /// <param name="count">The number of recent records to return.</param>
        /// <returns>A collection of the most recent generated names.</returns>
        Task<IEnumerable<GeneratedNameEntity>> GetRecentAsync(int count = 50);

        /// <summary>
        /// Searches generated names using a search term.
        /// </summary>
        /// <param name="searchTerm">The term to search for.</param>
        /// <returns>A collection of matching generated names.</returns>
        Task<IEnumerable<GeneratedNameEntity>> SearchAsync(string searchTerm);

        /// <summary>
        /// Gets the total count of generated names.
        /// </summary>
        /// <returns>The total number of generated names.</returns>
        Task<int> GetCountAsync();

        /// <summary>
        /// Gets the count of generated names for a specific user.
        /// </summary>
        /// <param name="user">The user name.</param>
        /// <returns>The number of generated names for the user.</returns>
        Task<int> GetCountByUserAsync(string user);

        /// <summary>
        /// Gets the count of generated names for a specific resource type.
        /// </summary>
        /// <param name="resourceType">The resource type.</param>
        /// <returns>The number of generated names for the resource type.</returns>
        Task<int> GetCountByResourceTypeAsync(string resourceType);

        /// <summary>
        /// Gets usage statistics for a date range.
        /// </summary>
        /// <param name="fromDate">The start date.</param>
        /// <param name="toDate">The end date.</param>
        /// <returns>A dictionary containing resource type usage statistics.</returns>
        Task<Dictionary<string, int>> GetUsageStatisticsAsync(DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Checks if a resource name already exists.
        /// </summary>
        /// <param name="resourceName">The resource name to check.</param>
        /// <returns>True if the resource name exists, otherwise false.</returns>
        Task<bool> ExistsAsync(string resourceName);

        /// <summary>
        /// Checks if a resource name is a duplicate for a specific user.
        /// </summary>
        /// <param name="resourceName">The resource name to check.</param>
        /// <param name="user">The user name.</param>
        /// <returns>True if the resource name is a duplicate for the user, otherwise false.</returns>
        Task<bool> IsDuplicateAsync(string resourceName, string user);

        /// <summary>
        /// Bulk deletes multiple generated name records.
        /// </summary>
        /// <param name="ids">The IDs of the records to delete.</param>
        /// <returns>The number of records deleted.</returns>
        Task<int> BulkDeleteAsync(IEnumerable<long> ids);

        /// <summary>
        /// Cleans up old records based on a cutoff date.
        /// </summary>
        /// <param name="cutoffDate">Records older than this date will be deleted.</param>
        /// <returns>The number of records deleted.</returns>
        Task<int> CleanupOldRecordsAsync(DateTime cutoffDate);
    }
}
