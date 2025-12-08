using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SettingsApi.Services;
using MagiDesk.Shared.Authorization.Requirements;
using MagiDesk.Shared.Authorization.Handlers;
using MagiDesk.Shared.Authorization.Middleware;
using MagiDesk.Shared.Authorization.Services;
using Microsoft.AspNetCore.Authorization;
using MagiDesk.Shared.DTOs.Users;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

const string CorsPolicy = "AllowAll";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// Database connection (PostgreSQL)
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
connString ??= builder.Configuration["POSTGRES_CONNECTION_STRING"]
    ?? "Host=/cloudsql/bola8pos:northamerica-south1:pos-app-1;Port=5432;Username=posapp;Password=Campus_66;Database=postgres";

builder.Services.AddSingleton<NpgsqlDataSource>(_ =>
{
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connString);
    return dataSourceBuilder.Build();
});

// Options (keep for backward compatibility)
builder.Services.Configure<FirestoreOptions>(builder.Configuration.GetSection("Firestore"));

// App services
builder.Services.AddSingleton<ISettingsService, SettingsService>();
builder.Services.AddScoped<IHierarchicalSettingsService, HierarchicalSettingsService>();

// Authentication - Required for authorization to work properly
// We're using a simple no-op authentication since we handle auth via custom middleware
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

app.UseHttpsRedirection();
app.UseCors(CorsPolicy);

// Add exception handling middleware (should be early in pipeline to catch all exceptions)
app.UseMiddleware<MagiDesk.Shared.Authorization.Middleware.AuthorizationExceptionHandlerMiddleware>();

// Add middleware to extract user ID from requests (from shared library)
app.UseMiddleware<MagiDesk.Shared.Authorization.Middleware.UserIdExtractionMiddleware>();

// Add authorization middleware (required for RBAC)
app.UseAuthorization();

app.MapControllers();

app.Run();
