using InventoryApi.Repositories;
using InventoryApi.Services;
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Initialize database schema
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { ok = true, service = "InventoryApi", ts = DateTimeOffset.UtcNow }))
   .WithName("Health");

app.Run();
