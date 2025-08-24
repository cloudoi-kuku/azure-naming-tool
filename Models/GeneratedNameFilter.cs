namespace AzureNamingTool.Models
{
    /// <summary>
    /// Represents filter criteria for querying generated names.
    /// </summary>
    public class GeneratedNameFilter
    {
        /// <summary>
        /// Gets or sets the user filter. Filters by user name containing this value.
        /// </summary>
        public string? User { get; set; }

        /// <summary>
        /// Gets or sets the resource type filter. Filters by resource type containing this value.
        /// </summary>
        public string? ResourceType { get; set; }

        /// <summary>
        /// Gets or sets the resource name filter. Filters by resource name containing this value.
        /// </summary>
        public string? ResourceName { get; set; }

        /// <summary>
        /// Gets or sets the start date filter. Filters records created on or after this date.
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// Gets or sets the end date filter. Filters records created on or before this date.
        /// </summary>
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// Gets or sets the IP address filter. Filters by exact IP address match.
        /// </summary>
        public string? IPAddress { get; set; }

        /// <summary>
        /// Gets or sets the search term. Searches across resource name, type, user, and components.
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include soft-deleted records.
        /// </summary>
        public bool IncludeDeleted { get; set; } = false;

        /// <summary>
        /// Gets or sets the component name filter. Filters by component name containing this value.
        /// </summary>
        public string? ComponentName { get; set; }

        /// <summary>
        /// Gets or sets the component value filter. Filters by component value containing this value.
        /// </summary>
        public string? ComponentValue { get; set; }

        /// <summary>
        /// Gets a value indicating whether any filters are applied.
        /// </summary>
        public bool HasFilters => !string.IsNullOrEmpty(User) ||
                                  !string.IsNullOrEmpty(ResourceType) ||
                                  !string.IsNullOrEmpty(ResourceName) ||
                                  FromDate.HasValue ||
                                  ToDate.HasValue ||
                                  !string.IsNullOrEmpty(IPAddress) ||
                                  !string.IsNullOrEmpty(SearchTerm) ||
                                  !string.IsNullOrEmpty(ComponentName) ||
                                  !string.IsNullOrEmpty(ComponentValue) ||
                                  IncludeDeleted;
    }
}
