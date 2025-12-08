using Npgsql;
using UsersApi.Services;
using UsersApi.Repositories;
using MagiDesk.Shared.Authorization.Requirements;
using MagiDesk.Shared.Authorization.Handlers;
using MagiDesk.Shared.Authorization.Middleware;
using MagiDesk.Shared.Authorization.Services;
using Microsoft.AspNetCore.Authorization;
using MagiDesk.Shared.DTOs.Users;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// RBAC Services
builder.Services.AddScoped<IUsersRepository, UsersRepository>();
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<IRbacRepository, RbacRepository>();
builder.Services.AddScoped<UsersApi.Services.IRbacService, RbacService>();

// Register shared IRbacService interface (for authorization handlers)
builder.Services.AddScoped<MagiDesk.Shared.Authorization.Services.IRbacService>(sp => 
    sp.GetRequiredService<UsersApi.Services.IRbacService>());

// Authorization Services
builder.Services.AddAuthorization(options =>
{
    // Register authorization policies for each permission
    // Policy name format: "Permission:{permission_name}"
    foreach (var permission in Permissions.AllPermissions)
    {
        options.AddPolicy($"Permission:{permission}", policy =>
        {
            policy.Requirements.Add(new PermissionRequirement(permission));
        });
    }
});

// Register permission requirement handler (from shared library)
builder.Services.AddSingleton<IAuthorizationHandler, MagiDesk.Shared.Authorization.Handlers.PermissionRequirementHandler>();

// Npgsql DataSource (dev: use LocalConnectionString, prod: use CloudRunSocketConnectionString)
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
if (string.IsNullOrWhiteSpace(connString))
{
    // Fallback to environment variable or default
    connString = builder.Configuration["ConnectionStrings:DefaultConnection"]
        ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
        ?? throw new InvalidOperationException("PostgreSQL connection string not configured. Set Postgres:CloudRunSocketConnectionString or ConnectionStrings:DefaultConnection");
}

var dataSource = new NpgsqlDataSourceBuilder(connString).Build();
builder.Services.AddSingleton(dataSource);
builder.Services.AddHostedService<DatabaseInitializer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add middleware to extract user ID from requests (from shared library)
app.UseMiddleware<MagiDesk.Shared.Authorization.Middleware.UserIdExtractionMiddleware>();

// Map all controllers - versioning is handled via attribute routing
// v1 controllers: [Route("api/[controller]")] and [Route("api/v1/[controller]")]
// v2 controllers: [Route("api/v2/[controller]")]
app.MapControllers();

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
