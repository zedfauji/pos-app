using Npgsql;
using OrderApi.Services;
using OrderApi.Repositories;
using MagiDesk.Shared.Authorization.Requirements;
using MagiDesk.Shared.Authorization.Handlers;
using MagiDesk.Shared.Authorization.Middleware;
using MagiDesk.Shared.Authorization.Services;
using Microsoft.AspNetCore.Authorization;
using MagiDesk.Shared.DTOs.Users;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

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
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IKitchenService, KitchenService>();

// HttpClient for InventoryApi with retry policy (for drinks only)
builder.Services.AddHttpClient("InventoryApi", (sp, http) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var baseUrl = cfg["InventoryApi:BaseUrl"] ?? Environment.GetEnvironmentVariable("INVENTORYAPI_BASEURL") ?? throw new InvalidOperationException("InventoryApi:BaseUrl not configured");
    http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    http.Timeout = TimeSpan.FromSeconds(10);
});

// Npgsql DataSource (dev: use LocalConnectionString)
var pgSection = builder.Configuration.GetSection("Postgres");
string? connString;
if (builder.Environment.IsDevelopment())
{
    connString = pgSection.GetValue<string>("LocalConnectionString");
}
else
{
    connString = pgSection.GetValue<string>("CloudRunSocketConnectionString");
}
if (!string.IsNullOrWhiteSpace(connString))
{
    var dataSource = new NpgsqlDataSourceBuilder(connString).Build();
    builder.Services.AddSingleton(dataSource);
}
builder.Services.AddHostedService<DatabaseInitializer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add exception handling middleware (should be early in pipeline to catch all exceptions)
app.UseMiddleware<MagiDesk.Shared.Authorization.Middleware.AuthorizationExceptionHandlerMiddleware>();

// Add middleware to extract user ID from requests (from shared library)
app.UseMiddleware<MagiDesk.Shared.Authorization.Middleware.UserIdExtractionMiddleware>();

// Add authentication and authorization middleware (required for RBAC)
app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint for Cloud Run
app.MapGet("/health", () => Results.Ok("OK"));

// DB debug endpoint (no secrets)
app.MapGet("/debug/db", async (NpgsqlDataSource? dataSource, CancellationToken ct) =>
{
    if (dataSource is null)
    {
        return Results.Ok(new { configured = false });
    }
    try
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        var serverVersion = conn.PostgreSqlVersion.ToString();
        string? db = null; string? user = null;
        await using (var cmd = new NpgsqlCommand("select current_database(), current_user", conn))
        await using (var rdr = await cmd.ExecuteReaderAsync(ct))
        {
            if (await rdr.ReadAsync(ct))
            {
                db = rdr.IsDBNull(0) ? null : rdr.GetString(0);
                user = rdr.IsDBNull(1) ? null : rdr.GetString(1);
            }
        }
        return Results.Ok(new { configured = true, canConnect = true, serverVersion, database = db, user });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { configured = true, canConnect = false, error = ex.Message });
    }
});

app.MapControllers();

app.Run();
