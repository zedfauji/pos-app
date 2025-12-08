using InventoryApi.Repositories;
using InventoryApi.Services;
using MagiDesk.Shared.Authorization.Requirements;
using MagiDesk.Shared.Authorization.Handlers;
using MagiDesk.Shared.Authorization.Middleware;
using MagiDesk.Shared.Authorization.Services;
using Microsoft.AspNetCore.Authorization;
using MagiDesk.Shared.DTOs.Users;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Data source (PostgreSQL) - prefer appsettings Postgres section; fallback to env if provided
string? connString;
var pgSection = builder.Configuration.GetSection("Postgres");
if (builder.Environment.IsDevelopment())
{
    connString = pgSection.GetValue<string>("LocalConnectionString");
}
else
{
    connString = pgSection.GetValue<string>("CloudRunSocketConnectionString");
}
connString ??= builder.Configuration["INVENTORY_CONNECTION_STRING"]
    ?? builder.Configuration["POSTGRES_CONNECTION_STRING"]
    ?? "Host=localhost;Port=5432;Username=posapp;Password=Campus_66;Database=postgres";
builder.Services.AddSingleton<NpgsqlDataSource>(_ =>
{
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connString);
    return dataSourceBuilder.Build();
});

// Connection string for repositories
builder.Services.AddSingleton<string>(_ => connString);

// Repositories
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IVendorRepository, VendorRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICashFlowRepository, CashFlowRepository>();

// Services
builder.Services.AddScoped<DatabaseInitializer>();

// MVC + Swagger + CORS
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(opts =>
{
    opts.AddDefaultPolicy(p => p
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// Authentication - Required for authorization to work properly
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "NoOp";
    options.DefaultChallengeScheme = "NoOp";
})
.AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, MagiDesk.Shared.Authorization.Authentication.NoOpAuthenticationHandler>("NoOp", options => { });

// Authorization Services for RBAC (using shared library)
builder.Services.AddAuthorization(options =>
{
    // Set default policy to allow requests (we'll use [RequiresPermission] for specific endpoints)
    options.FallbackPolicy = null;
    
    foreach (var permission in Permissions.AllPermissions)
    {
        options.AddPolicy($"Permission:{permission}", policy =>
        {
            policy.Requirements.Add(new MagiDesk.Shared.Authorization.Requirements.PermissionRequirement(permission));
        });
    }
});

// Register HTTP client for UsersApi
builder.Services.AddHttpClient<MagiDesk.Shared.Authorization.Services.IRbacService, MagiDesk.Shared.Authorization.Services.HttpRbacService>();

// Register permission requirement handler (from shared library)
builder.Services.AddSingleton<IAuthorizationHandler, MagiDesk.Shared.Authorization.Handlers.PermissionRequirementHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Add exception handling middleware (should be early in pipeline to catch all exceptions)
app.UseMiddleware<MagiDesk.Shared.Authorization.Middleware.AuthorizationExceptionHandlerMiddleware>();

// Add middleware to extract user ID from requests (from shared library)
app.UseMiddleware<MagiDesk.Shared.Authorization.Middleware.UserIdExtractionMiddleware>();

// Add authentication and authorization middleware (required for RBAC)
app.UseAuthentication();
app.UseAuthorization();

// Initialize database schema (non-blocking - run in background)
_ = Task.Run(async () =>
{
    try
    {
        using var scope = app.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        await initializer.InitializeAsync();
    }
    catch (Exception ex)
    {
        // Log error but don't block startup
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to initialize database schema");
    }
});

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { ok = true, service = "InventoryApi", ts = DateTimeOffset.UtcNow }))
   .WithName("Health");

app.Run();
