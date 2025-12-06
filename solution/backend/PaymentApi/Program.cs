using Npgsql;
using MagiDesk.Shared.Authorization.Requirements;
using MagiDesk.Shared.Authorization.Handlers;
using MagiDesk.Shared.Authorization.Middleware;
using MagiDesk.Shared.Authorization.Services;
using Microsoft.AspNetCore.Authorization;
using MagiDesk.Shared.DTOs.Users;

var builder = WebApplication.CreateBuilder(args);

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

// Npgsql DataSource
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

builder.Services.AddHostedService<PaymentApi.Services.DatabaseInitializer>();

builder.Services.AddScoped<PaymentApi.Repositories.IPaymentRepository, PaymentApi.Repositories.PaymentRepository>();
builder.Services.AddScoped<PaymentApi.Services.IPaymentService, PaymentApi.Services.PaymentService>();
builder.Services.AddScoped<PaymentApi.Services.ImmutableIdService>();

var app = builder.Build();

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

app.MapGet("/health", () => Results.Ok("OK"));

// DB debug endpoint
app.MapGet("/debug/db", async (NpgsqlDataSource? dataSource, CancellationToken ct) =>
{
    if (dataSource is null) return Results.Ok(new { configured = false });
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
