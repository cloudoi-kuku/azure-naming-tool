using AzureNamingTool.Data.Entities;
using AzureNamingTool.Helpers;
using AzureNamingTool.Interfaces;
using AzureNamingTool.Models;
using System.Text.Json;

namespace AzureNamingTool.Services
{
    /// <summary>
    /// Service for managing generated names with database support.
    /// </summary>
    public class GeneratedNamesService
    {
        private readonly IGeneratedNamesRepository? _repository;
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private readonly IConfiguration? _configuration;
        private readonly ILogger<GeneratedNamesService>? _logger;
        private readonly bool _useDatabaseStorage;

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneratedNamesService"/> class for dependency injection.
        /// </summary>
        /// <param name="repository">The generated names repository.</param>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="logger">The logger.</param>
        public GeneratedNamesService(
            IGeneratedNamesRepository repository,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            ILogger<GeneratedNamesService> logger)
        {
            _repository = repository;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _logger = logger;
            _useDatabaseStorage = configuration.GetValue<bool>("StorageSettings:UseDatabase", true);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneratedNamesService"/> class for static usage (backward compatibility).
        /// </summary>
        public GeneratedNamesService()
        {
            _useDatabaseStorage = false; // Fall back to JSON storage for static calls
        }
        /// <summary>
        /// Retrieves a list of items.
        /// </summary>
        /// <returns>Task&lt;ServiceResponse&gt; - The response containing the list of items or an error message.</returns>
        public static async Task<ServiceResponse> GetItems()
        {
            ServiceResponse serviceResponse = new();
            List<GeneratedName> lstGeneratedNames = [];
            try
            {
                // Get list of items
                var items = await ConfigurationHelper.GetList<GeneratedName>();
                if (GeneralHelper.IsNotNull(items))
                {
                    serviceResponse.ResponseObject = items.OrderByDescending(x => x.CreatedOn).ToList();
                    serviceResponse.Success = true;
                }
            }
            catch (Exception ex)
            {
                AdminLogService.PostItem(new AdminLogMessage { Title = "ERROR", Message = ex.Message });
                serviceResponse.Success = false;
            }
            return serviceResponse;
        }

        /// <summary>
        /// Retrieves an item with the specified ID.
        /// </summary>
        /// <param name="id">int - The ID of the item to retrieve.</param>
        /// <returns>Task&lt;ServiceResponse&gt; - The response containing the retrieved item or an error message.</returns>
        public static async Task<ServiceResponse> GetItem(int id)
        {
            ServiceResponse serviceResponse = new();
            try
            {
                // Get list of items
                var items = await ConfigurationHelper.GetList<GeneratedName>();
                if (GeneralHelper.IsNotNull(items))
                {
                    var item = items.Find(x => x.Id == id);
                    if (GeneralHelper.IsNotNull(item))
                    {
                        serviceResponse.ResponseObject = item;
                        serviceResponse.Success = true;
                    }
                    else
                    {
                        serviceResponse.ResponseObject = "Generated Name not found!";
                    }
                }
                else
                {
                    serviceResponse.ResponseObject = "Generated Names not found!";
                }
            }
            catch (Exception ex)
            {
                AdminLogService.PostItem(new AdminLogMessage() { Title = "ERROR", Message = ex.Message });
                serviceResponse.Success = false;
                serviceResponse.ResponseObject = ex;
            }
            return serviceResponse;
        }

        /// <summary>
        /// Posts an item to the list of generated names using the repository pattern.
        /// </summary>
        /// <param name="generatedName">GeneratedName - The generated name to be added.</param>
        /// <returns>Task&lt;ServiceResponse&gt; - The response indicating the success or failure of the operation.</returns>
        public async Task<ServiceResponse> PostItemAsync(GeneratedName generatedName)
        {
            ServiceResponse serviceResponse = new();
            try
            {
                if (_useDatabaseStorage && _repository != null)
                {
                    // Use database storage
                    var entity = new GeneratedNameEntity
                    {
                        CreatedOn = generatedName.CreatedOn,
                        ResourceName = generatedName.ResourceName,
                        ResourceTypeName = generatedName.ResourceTypeName,
                        User = generatedName.User,
                        Message = generatedName.Message,
                        IPAddress = GetClientIPAddress(),
                        UserAgent = GetUserAgent(),
                        SessionId = GetSessionId(),
                        RequestId = GetRequestId(),
                        CreatedBy = generatedName.User,
                        Components = generatedName.Components.Select((component, index) =>
                            new GeneratedNameComponentEntity
                            {
                                ComponentName = component.Length > 0 ? component[0] : "Unknown",
                                ComponentValue = component.Length > 1 ? component[1] : "",
                                SortOrder = index
                            }).ToList()
                    };

                    var createdEntity = await _repository.CreateAsync(entity);

                    // Update the original object with the generated ID
                    generatedName.Id = createdEntity.Id;

                    serviceResponse.Success = true;
                    serviceResponse.ResponseObject = generatedName;

                    _logger?.LogInformation("Successfully logged generated name: {ResourceName} for user: {User}",
                        generatedName.ResourceName, generatedName.User);
                }
                else
                {
                    // Fall back to JSON storage for backward compatibility
                    return await PostItemLegacy(generatedName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to log generated name: {ResourceName}", generatedName.ResourceName);
                AdminLogService.PostItem(new AdminLogMessage { Title = "ERROR", Message = ex.Message });
                serviceResponse.Success = false;
                serviceResponse.ResponseObject = ex;
            }
            return serviceResponse;
        }

        /// <summary>
        /// Static method for backward compatibility.
        /// </summary>
        /// <param name="generatedName">The generated name to be added.</param>
        /// <returns>The service response.</returns>
        public static async Task<ServiceResponse> PostItem(GeneratedName generatedName)
        {
            // For static calls, use legacy JSON storage
            return await PostItemLegacy(generatedName);
        }

        /// <summary>
        /// Legacy implementation using JSON storage.
        /// </summary>
        /// <param name="generatedName">The generated name to be added.</param>
        /// <returns>The service response.</returns>
        private static async Task<ServiceResponse> PostItemLegacy(GeneratedName generatedName)
        {
            ServiceResponse serviceResponse = new();
            try
            {
                // Get the previously generated names
                var items = await ConfigurationHelper.GetList<GeneratedName>();
                if (GeneralHelper.IsNotNull(items))
                {
                    if (items.Count > 0)
                    {
                        generatedName.Id = items.Max(x => x.Id) + 1;
                    }
                    else
                    {
                        generatedName.Id = 1;
                    }

                    items.Add(generatedName);

                    // Write items to file
                    await ConfigurationHelper.WriteList<GeneratedName>(items);

                    CacheHelper.InvalidateCacheObject("generatednames.json");

                    serviceResponse.Success = true;
                    serviceResponse.ResponseObject = generatedName;
                }
            }
            catch (Exception ex)
            {
                AdminLogService.PostItem(new AdminLogMessage { Title = "ERROR", Message = ex.Message });
                serviceResponse.Success = false;
                serviceResponse.ResponseObject = ex;
            }
            return serviceResponse;
        }

        /// <summary>
        /// Deletes an item with the specified ID.
        /// </summary>
        /// <param name="id">int - The ID of the item to delete.</param>
        /// <returns>Task&lt;ServiceResponse&gt; - The response indicating the success or failure of the operation.</returns>
        public static async Task<ServiceResponse> DeleteItem(int id)
        {
            ServiceResponse serviceResponse = new();
            try
            {
                // Get list of items
                var items = await ConfigurationHelper.GetList<GeneratedName>();
                if (GeneralHelper.IsNotNull(items))
                {
                    // Get the specified item
                    var item = items.Find(x => x.Id == id);
                    if (GeneralHelper.IsNotNull(item))
                    {
                        // Remove the item from the collection
                        items.Remove(item);

                        // Write items to file
                        await ConfigurationHelper.WriteList<GeneratedName>(items);
                        serviceResponse.Success = true;
                    }
                    else
                    {
                        serviceResponse.ResponseObject = "Generated Name not found!";
                    }
                }
                else
                {
                    serviceResponse.ResponseObject = "Generated Name not found!";
                }
            }
            catch (Exception ex)
            {
                AdminLogService.PostItem(new AdminLogMessage() { Title = "ERROR", Message = ex.Message });
                serviceResponse.ResponseObject = ex;
                serviceResponse.Success = false;
            }
            return serviceResponse;
        }

        /// <summary>
        /// This function deletes all the items.
        /// </summary>
        /// <returns>ServiceResponse - The response indicating the success or failure of the operation.</returns>
        public static async Task<ServiceResponse> DeleteAllItems()
        {
            ServiceResponse serviceResponse = new();
            try
            {
                List<GeneratedName> items = [];
                await ConfigurationHelper.WriteList<GeneratedName>(items);
                serviceResponse.Success = true;
            }
            catch (Exception ex)
            {
                AdminLogService.PostItem(new AdminLogMessage { Title = "Error", Message = ex.Message });
                serviceResponse.Success = false;
            }
            return serviceResponse;
        }

        /// <summary>
        /// This function posts the configuration items.
        /// </summary>
        /// <param name="items">List of GeneratedName - The configuration items to be posted.</param>
        /// <returns>ServiceResponse - The response indicating the success or failure of the operation.</returns>
        public static async Task<ServiceResponse> PostConfig(List<GeneratedName> items)
        {
            ServiceResponse serviceResponse = new();
            try
            {
                // Get list of items
                var newitems = new List<GeneratedName>();
                int i = 1;

                // Determine new item id
                foreach (GeneratedName item in items)
                {
                    item.Id = i;
                    newitems.Add(item);
                    i += 1;
                }

                // Write items to file
                await ConfigurationHelper.WriteList<GeneratedName>(newitems);
                serviceResponse.Success = true;
            }
            catch (Exception ex)
            {
                AdminLogService.PostItem(new AdminLogMessage() { Title = "ERROR", Message = ex.Message });
                serviceResponse.ResponseObject = ex;
                serviceResponse.Success = false;
            }
            return serviceResponse;
        }

        /// <summary>
        /// Gets paginated generated names with filtering support.
        /// </summary>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="filter">The filter criteria.</param>
        /// <returns>The service response with paginated results.</returns>
        public async Task<ServiceResponse> GetPagedItems(
            int page = 1,
            int pageSize = 50,
            GeneratedNameFilter? filter = null)
        {
            ServiceResponse serviceResponse = new();
            try
            {
                if (_useDatabaseStorage && _repository != null)
                {
                    var result = await _repository.GetPagedAsync(page, pageSize, filter);

                    // Convert entities back to GeneratedName models for compatibility
                    var generatedNames = result.Items.Select(entity => new GeneratedName
                    {
                        Id = entity.Id,
                        CreatedOn = entity.CreatedOn,
                        ResourceName = entity.ResourceName,
                        ResourceTypeName = entity.ResourceTypeName,
                        User = entity.User,
                        Message = entity.Message,
                        Components = entity.Components
                            .OrderBy(c => c.SortOrder)
                            .Select(c => new string[] { c.ComponentName, c.ComponentValue })
                            .ToList()
                    }).ToList();

                    serviceResponse.ResponseObject = new
                    {
                        Items = generatedNames,
                        TotalCount = result.TotalCount,
                        Page = result.Page,
                        PageSize = result.PageSize,
                        TotalPages = result.TotalPages,
                        HasPreviousPage = result.HasPreviousPage,
                        HasNextPage = result.HasNextPage
                    };
                    serviceResponse.Success = true;
                }
                else
                {
                    // Fall back to legacy method
                    return await GetItems();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get paged generated names");
                AdminLogService.PostItem(new AdminLogMessage { Title = "ERROR", Message = ex.Message });
                serviceResponse.Success = false;
            }
            return serviceResponse;
        }

        /// <summary>
        /// Helper methods for audit information
        /// </summary>
        private string? GetClientIPAddress()
        {
            var context = _httpContextAccessor?.HttpContext;
            if (context == null) return null;

            // Check for forwarded IP first (for load balancers/proxies)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            return context.Connection.RemoteIpAddress?.ToString();
        }

        private string? GetUserAgent()
        {
            var context = _httpContextAccessor?.HttpContext;
            return context?.Request.Headers["User-Agent"].FirstOrDefault();
        }

        private string? GetSessionId()
        {
            var context = _httpContextAccessor?.HttpContext;
            return context?.Session?.Id;
        }

        private string? GetRequestId()
        {
            var context = _httpContextAccessor?.HttpContext;
            return context?.TraceIdentifier;
        }
    }
}