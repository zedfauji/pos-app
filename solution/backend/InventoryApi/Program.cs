using InventoryApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Options
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));

// Services
builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
// Removed IOrdersService registration due to migration

// MVC + Swagger + CORS
builder.Services.AddControllers();
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
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { ok = true, service = "InventoryApi", ts = DateTimeOffset.UtcNow }))
   .WithName("Health");

app.Run();
