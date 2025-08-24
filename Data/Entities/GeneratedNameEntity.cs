using System.ComponentModel.DataAnnotations;

namespace AzureNamingTool.Data.Entities
{
    /// <summary>
    /// Database entity representing a generated name with enhanced audit information.
    /// </summary>
    public class GeneratedNameEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the generated name.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the name was generated.
        /// </summary>
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the generated resource name.
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string ResourceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the resource type name.
        /// </summary>
        [MaxLength(255)]
        public string ResourceTypeName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user who generated the name.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string User { get; set; } = "General";

        /// <summary>
        /// Gets or sets any message associated with the name generation.
        /// </summary>
        [MaxLength(2000)]
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the IP address of the client that generated the name.
        /// </summary>
        [MaxLength(45)] // IPv6 support
        public string? IPAddress { get; set; }

        /// <summary>
        /// Gets or sets the user agent of the client that generated the name.
        /// </summary>
        [MaxLength(1000)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Gets or sets the session ID associated with the name generation.
        /// </summary>
        [MaxLength(100)]
        public string? SessionId { get; set; }

        /// <summary>
        /// Gets or sets the request ID for tracing purposes.
        /// </summary>
        [MaxLength(100)]
        public string? RequestId { get; set; }

        /// <summary>
        /// Gets or sets who created this record.
        /// </summary>
        [MaxLength(100)]
        public string CreatedBy { get; set; } = "System";

        /// <summary>
        /// Gets or sets the date and time when the record was last updated.
        /// </summary>
        public DateTime? UpdatedOn { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this record is soft deleted.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Gets or sets the collection of components that make up this generated name.
        /// </summary>
        public virtual ICollection<GeneratedNameComponentEntity> Components { get; set; } = new List<GeneratedNameComponentEntity>();
    }
}
