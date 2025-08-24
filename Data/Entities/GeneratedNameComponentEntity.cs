using System.ComponentModel.DataAnnotations;

namespace AzureNamingTool.Data.Entities
{
    /// <summary>
    /// Database entity representing a component of a generated name.
    /// </summary>
    public class GeneratedNameComponentEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the component.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the ID of the generated name this component belongs to.
        /// </summary>
        public long GeneratedNameId { get; set; }

        /// <summary>
        /// Gets or sets the name of the component (e.g., "ResourceType", "Environment").
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string ComponentName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the value of the component (e.g., "vm", "prod").
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string ComponentValue { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sort order of this component within the generated name.
        /// </summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// Gets or sets the navigation property to the parent generated name.
        /// </summary>
        public virtual GeneratedNameEntity GeneratedName { get; set; } = null!;
    }
}
