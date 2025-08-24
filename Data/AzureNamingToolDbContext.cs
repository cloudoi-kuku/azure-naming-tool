using AzureNamingTool.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AzureNamingTool.Data
{
    /// <summary>
    /// Database context for the Azure Naming Tool application.
    /// </summary>
    public class AzureNamingToolDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureNamingToolDbContext"/> class.
        /// </summary>
        /// <param name="options">The database context options.</param>
        public AzureNamingToolDbContext(DbContextOptions<AzureNamingToolDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the generated names DbSet.
        /// </summary>
        public DbSet<GeneratedNameEntity> GeneratedNames { get; set; }

        /// <summary>
        /// Gets or sets the generated name components DbSet.
        /// </summary>
        public DbSet<GeneratedNameComponentEntity> GeneratedNameComponents { get; set; }

        /// <summary>
        /// Configures the model that was discovered by convention from the entity types.
        /// </summary>
        /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure GeneratedNameEntity
            modelBuilder.Entity<GeneratedNameEntity>(entity =>
            {
                entity.ToTable("GeneratedNames");
                entity.HasKey(e => e.Id);
                
                // Property configurations
                entity.Property(e => e.ResourceName)
                      .IsRequired()
                      .HasMaxLength(255);
                      
                entity.Property(e => e.ResourceTypeName)
                      .HasMaxLength(255);
                      
                entity.Property(e => e.User)
                      .IsRequired()
                      .HasMaxLength(100)
                      .HasDefaultValue("General");
                      
                entity.Property(e => e.Message)
                      .HasMaxLength(2000);
                      
                entity.Property(e => e.IPAddress)
                      .HasMaxLength(45); // IPv6 support
                      
                entity.Property(e => e.UserAgent)
                      .HasMaxLength(1000);
                      
                entity.Property(e => e.SessionId)
                      .HasMaxLength(100);
                      
                entity.Property(e => e.RequestId)
                      .HasMaxLength(100);
                      
                entity.Property(e => e.CreatedBy)
                      .HasMaxLength(100)
                      .HasDefaultValue("System");
                      
                entity.Property(e => e.IsDeleted)
                      .HasDefaultValue(false);

                entity.Property(e => e.CreatedOn)
                      .HasDefaultValueSql("datetime('now')");

                // Indexes for performance
                entity.HasIndex(e => e.CreatedOn)
                      .HasDatabaseName("IX_GeneratedNames_CreatedOn");
                      
                entity.HasIndex(e => e.User)
                      .HasDatabaseName("IX_GeneratedNames_User");
                      
                entity.HasIndex(e => e.ResourceTypeName)
                      .HasDatabaseName("IX_GeneratedNames_ResourceTypeName");
                      
                entity.HasIndex(e => e.ResourceName)
                      .HasDatabaseName("IX_GeneratedNames_ResourceName");
                      
                entity.HasIndex(e => e.IsDeleted)
                      .HasDatabaseName("IX_GeneratedNames_IsDeleted");

                entity.HasIndex(e => e.IPAddress)
                      .HasDatabaseName("IX_GeneratedNames_IPAddress");

                // Global query filter for soft deletes
                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configure GeneratedNameComponentEntity
            modelBuilder.Entity<GeneratedNameComponentEntity>(entity =>
            {
                entity.ToTable("GeneratedNameComponents");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.ComponentName)
                      .IsRequired()
                      .HasMaxLength(100);
                      
                entity.Property(e => e.ComponentValue)
                      .IsRequired()
                      .HasMaxLength(200);
                      
                entity.Property(e => e.SortOrder)
                      .HasDefaultValue(0);

                // Foreign key relationship
                entity.HasOne(e => e.GeneratedName)
                      .WithMany(e => e.Components)
                      .HasForeignKey(e => e.GeneratedNameId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Indexes
                entity.HasIndex(e => e.GeneratedNameId)
                      .HasDatabaseName("IX_GeneratedNameComponents_GeneratedNameId");
                      
                entity.HasIndex(e => new { e.ComponentName, e.ComponentValue })
                      .HasDatabaseName("IX_GeneratedNameComponents_Name_Value");
            });
        }

        /// <summary>
        /// Configures the database context options.
        /// </summary>
        /// <param name="optionsBuilder">The options builder.</param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Fallback configuration if not configured in DI
                optionsBuilder.UseSqlite("Data Source=azurenamingtool.db");
            }
            
            // Enable detailed errors in development
            #if DEBUG
            optionsBuilder.EnableDetailedErrors();
            optionsBuilder.EnableSensitiveDataLogging();
            #endif
        }
    }
}
