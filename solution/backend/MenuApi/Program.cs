using MenuApi.Services;
using Npgsql;
using MenuApi.Repositories;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddScoped<IMenuRepository, MenuRepository>();
builder.Services.AddScoped<IMenuService, MenuService>();
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

// Health check endpoint for Cloud Run
app.MapGet("/health", () => Results.Ok("OK"));

// DB debug endpoint (no secrets). Useful for Cloud Run debugging
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

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapControllers();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
