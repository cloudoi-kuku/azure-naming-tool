using AzureNamingTool.Attributes;
using AzureNamingTool.Components;
using AzureNamingTool.Data;
using AzureNamingTool.Helpers;
using AzureNamingTool.Interfaces;
using AzureNamingTool.Models;
using AzureNamingTool.Repositories;
using AzureNamingTool.Services;
using BlazorDownloadFile;
using Blazored.Modal;
using Blazored.Toast;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents().AddHubOptions(options =>
    {
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
        options.EnableDetailedErrors = false;
        options.HandshakeTimeout = TimeSpan.FromSeconds(15);
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        options.MaximumParallelInvocationsPerClient = 1;
        options.MaximumReceiveMessageSize = 102400000;
        options.StreamBufferCapacity = 10;
    });


builder.Services.AddHealthChecks();
builder.Services.AddBlazorDownloadFile();
builder.Services.AddBlazoredToast();
builder.Services.AddBlazoredModal();
builder.Services.AddHttpContextAccessor();

// Add session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddSingleton<StateContainer>();

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=azurenamingtool.db";

builder.Services.AddDbContext<AzureNamingToolDbContext>(options =>
{
    options.UseSqlite(connectionString);

    // Enable detailed errors in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();
    }
});

// Repository registration
builder.Services.AddScoped<IGeneratedNamesRepository, GeneratedNamesRepository>();

// Service registration
builder.Services.AddScoped<GeneratedNamesService>();
builder.Services.AddScoped<ResourceNamingRequestService>();

// Migration service for data migration
builder.Services.AddScoped<IDataMigrationService, DataMigrationService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<CustomHeaderSwaggerAttribute>();
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v" + ConfigurationHelper.GetAssemblyVersion(),
        Title = "Azure Naming Tool API",
        Description = "An ASP.NET Core Web API for managing the Azure Naming tool configuration. All API requests require the configured API Keys (found in the site Admin configuration). You can find more details in the <a href=\"https://github.com/mspnp/AzureNamingTool/wiki/Using-the-API\" target=\"_new\">Azure Naming Tool API documentation</a>."
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

// Add services to the container
builder.Services.AddBlazorDownloadFile();
builder.Services.AddBlazoredToast();
builder.Services.AddBlazoredModal();
builder.Services.AddMemoryCache();
builder.Services.AddMvcCore().AddApiExplorer();

var app = builder.Build();

// Ensure database is created and migrated
try
{
    using (var scope = app.Services.CreateScope())
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Initializing database...");

        var context = scope.ServiceProvider.GetRequiredService<AzureNamingToolDbContext>();

        // Ensure database is created
        var created = await context.Database.EnsureCreatedAsync();
        if (created)
        {
            logger.LogInformation("Database created successfully");
        }
        else
        {
            logger.LogInformation("Database already exists");
        }

        // Check if migration service is available and run migration if needed
        try
        {
            var migrationService = scope.ServiceProvider.GetRequiredService<IDataMigrationService>();
            logger.LogInformation("Running data migration if needed...");
            await migrationService.MigrateFromJsonIfNeeded();
            logger.LogInformation("Data migration completed");
        }
        catch (Exception migrationEx)
        {
            logger.LogWarning(migrationEx, "Data migration failed, but application will continue");
        }
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Database initialization failed, but application will continue");
}

app.MapHealthChecks("/healthcheck/ping");
app.MapHealthChecks("/health");

// Add a debug endpoint for database status
app.MapGet("/debug/database", async (AzureNamingToolDbContext context, ILogger<Program> logger) =>
{
    try
    {
        var canConnect = await context.Database.CanConnectAsync();
        var generatedNamesCount = await context.GeneratedNames.CountAsync();

        return Results.Ok(new
        {
            CanConnect = canConnect,
            GeneratedNamesCount = generatedNamesCount,
            DatabasePath = context.Database.GetConnectionString(),
            Timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database health check failed");
        return Results.Problem($"Database error: {ex.Message}");
    }
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AzureNamingToolAPI"));

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseSession();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.UseStatusCodePagesWithRedirects("/404");

app.MapControllers();
app.Run();


/// <summary>
/// Exists so can be used as reference for WebApplicationFactory in tests project
/// </summary>
public partial class Program
{
}