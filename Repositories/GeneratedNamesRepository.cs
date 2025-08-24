using AzureNamingTool.Data;
using AzureNamingTool.Data.Entities;
using AzureNamingTool.Interfaces;
using AzureNamingTool.Models;
using Microsoft.EntityFrameworkCore;

namespace AzureNamingTool.Repositories
{
    /// <summary>
    /// Repository implementation for managing generated names in the database.
    /// </summary>
    public class GeneratedNamesRepository : IGeneratedNamesRepository
    {
        private readonly AzureNamingToolDbContext _context;
        private readonly ILogger<GeneratedNamesRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneratedNamesRepository"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="logger">The logger.</param>
        public GeneratedNamesRepository(
            AzureNamingToolDbContext context,
            ILogger<GeneratedNamesRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<GeneratedNameEntity> CreateAsync(GeneratedNameEntity entity)
        {
            try
            {
                _context.GeneratedNames.Add(entity);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Created generated name record with ID {Id} for resource {ResourceName}", 
                    entity.Id, entity.ResourceName);
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create generated name record for resource {ResourceName}", 
                    entity.ResourceName);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<GeneratedNameEntity?> GetByIdAsync(long id)
        {
            try
            {
                return await _context.GeneratedNames
                    .Include(g => g.Components.OrderBy(c => c.SortOrder))
                    .FirstOrDefaultAsync(g => g.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get generated name by ID {Id}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateAsync(GeneratedNameEntity entity)
        {
            try
            {
                entity.UpdatedOn = DateTime.UtcNow;
                _context.GeneratedNames.Update(entity);
                var result = await _context.SaveChangesAsync();
                
                _logger.LogInformation("Updated generated name record with ID {Id}", entity.Id);
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update generated name record with ID {Id}", entity.Id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(long id)
        {
            try
            {
                var entity = await _context.GeneratedNames.FindAsync(id);
                if (entity == null)
                    return false;

                _context.GeneratedNames.Remove(entity);
                var result = await _context.SaveChangesAsync();
                
                _logger.LogInformation("Deleted generated name record with ID {Id}", id);
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete generated name record with ID {Id}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> SoftDeleteAsync(long id)
        {
            try
            {
                var entity = await _context.GeneratedNames.FindAsync(id);
                if (entity == null)
                    return false;

                entity.IsDeleted = true;
                entity.UpdatedOn = DateTime.UtcNow;
                var result = await _context.SaveChangesAsync();
                
                _logger.LogInformation("Soft deleted generated name record with ID {Id}", id);
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to soft delete generated name record with ID {Id}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<PagedResult<GeneratedNameEntity>> GetPagedAsync(
            int page, 
            int pageSize, 
            GeneratedNameFilter? filter = null)
        {
            try
            {
                var query = _context.GeneratedNames
                    .Include(g => g.Components.OrderBy(c => c.SortOrder))
                    .AsQueryable();

                // Apply filters
                if (filter != null)
                {
                    query = ApplyFilters(query, filter);
                }

                var totalCount = await query.CountAsync();
                
                var items = await query
                    .OrderByDescending(g => g.CreatedOn)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return new PagedResult<GeneratedNameEntity>
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get paged generated names");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<GeneratedNameEntity>> GetByUserAsync(string user, int limit = 100)
        {
            try
            {
                return await _context.GeneratedNames
                    .Include(g => g.Components.OrderBy(c => c.SortOrder))
                    .Where(g => g.User == user)
                    .OrderByDescending(g => g.CreatedOn)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get generated names for user {User}", user);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<GeneratedNameEntity>> GetRecentAsync(int count = 50)
        {
            try
            {
                return await _context.GeneratedNames
                    .Include(g => g.Components.OrderBy(c => c.SortOrder))
                    .OrderByDescending(g => g.CreatedOn)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get recent generated names");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<GeneratedNameEntity>> SearchAsync(string searchTerm)
        {
            try
            {
                return await _context.GeneratedNames
                    .Include(g => g.Components.OrderBy(c => c.SortOrder))
                    .Where(g => 
                        g.ResourceName.Contains(searchTerm) ||
                        g.ResourceTypeName.Contains(searchTerm) ||
                        g.User.Contains(searchTerm) ||
                        g.Components.Any(c => 
                            c.ComponentName.Contains(searchTerm) ||
                            c.ComponentValue.Contains(searchTerm)))
                    .OrderByDescending(g => g.CreatedOn)
                    .Take(100)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search generated names with term {SearchTerm}", searchTerm);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<int> GetCountAsync()
        {
            try
            {
                return await _context.GeneratedNames.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get total count of generated names");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<int> GetCountByUserAsync(string user)
        {
            try
            {
                return await _context.GeneratedNames
                    .Where(g => g.User == user)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get count for user {User}", user);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<int> GetCountByResourceTypeAsync(string resourceType)
        {
            try
            {
                return await _context.GeneratedNames
                    .Where(g => g.ResourceTypeName == resourceType)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get count for resource type {ResourceType}", resourceType);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, int>> GetUsageStatisticsAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var stats = await _context.GeneratedNames
                    .Where(g => g.CreatedOn >= fromDate && g.CreatedOn <= toDate)
                    .GroupBy(g => g.ResourceTypeName)
                    .Select(g => new { ResourceType = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToDictionaryAsync(x => x.ResourceType ?? "Unknown", x => x.Count);

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get usage statistics from {FromDate} to {ToDate}", fromDate, toDate);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(string resourceName)
        {
            try
            {
                return await _context.GeneratedNames
                    .AnyAsync(g => g.ResourceName == resourceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if resource name {ResourceName} exists", resourceName);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> IsDuplicateAsync(string resourceName, string user)
        {
            try
            {
                return await _context.GeneratedNames
                    .AnyAsync(g => g.ResourceName == resourceName && g.User == user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check duplicate for resource name {ResourceName} and user {User}", 
                    resourceName, user);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<int> BulkDeleteAsync(IEnumerable<long> ids)
        {
            try
            {
                var entities = await _context.GeneratedNames
                    .Where(g => ids.Contains(g.Id))
                    .ToListAsync();

                _context.GeneratedNames.RemoveRange(entities);
                var result = await _context.SaveChangesAsync();
                
                _logger.LogInformation("Bulk deleted {Count} generated name records", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to bulk delete generated name records");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<int> CleanupOldRecordsAsync(DateTime cutoffDate)
        {
            try
            {
                var oldRecords = await _context.GeneratedNames
                    .Where(g => g.CreatedOn < cutoffDate)
                    .ToListAsync();

                _context.GeneratedNames.RemoveRange(oldRecords);
                var result = await _context.SaveChangesAsync();
                
                _logger.LogInformation("Cleaned up {Count} old generated name records before {CutoffDate}", 
                    result, cutoffDate);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old records before {CutoffDate}", cutoffDate);
                throw;
            }
        }

        /// <summary>
        /// Applies filters to the query.
        /// </summary>
        /// <param name="query">The query to filter.</param>
        /// <param name="filter">The filter criteria.</param>
        /// <returns>The filtered query.</returns>
        private IQueryable<GeneratedNameEntity> ApplyFilters(
            IQueryable<GeneratedNameEntity> query, 
            GeneratedNameFilter filter)
        {
            if (!string.IsNullOrEmpty(filter.User))
                query = query.Where(g => g.User.Contains(filter.User));
                
            if (!string.IsNullOrEmpty(filter.ResourceType))
                query = query.Where(g => g.ResourceTypeName.Contains(filter.ResourceType));
                
            if (!string.IsNullOrEmpty(filter.ResourceName))
                query = query.Where(g => g.ResourceName.Contains(filter.ResourceName));
                
            if (filter.FromDate.HasValue)
                query = query.Where(g => g.CreatedOn >= filter.FromDate.Value);
                
            if (filter.ToDate.HasValue)
                query = query.Where(g => g.CreatedOn <= filter.ToDate.Value);
                
            if (!string.IsNullOrEmpty(filter.IPAddress))
                query = query.Where(g => g.IPAddress == filter.IPAddress);
                
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(g => 
                    g.ResourceName.Contains(filter.SearchTerm) ||
                    g.ResourceTypeName.Contains(filter.SearchTerm) ||
                    g.User.Contains(filter.SearchTerm) ||
                    g.Components.Any(c => 
                        c.ComponentName.Contains(filter.SearchTerm) ||
                        c.ComponentValue.Contains(filter.SearchTerm)));
            }

            if (!string.IsNullOrEmpty(filter.ComponentName))
                query = query.Where(g => g.Components.Any(c => c.ComponentName.Contains(filter.ComponentName)));

            if (!string.IsNullOrEmpty(filter.ComponentValue))
                query = query.Where(g => g.Components.Any(c => c.ComponentValue.Contains(filter.ComponentValue)));
            
            if (filter.IncludeDeleted)
                query = query.IgnoreQueryFilters();

            return query;
        }
    }
}
