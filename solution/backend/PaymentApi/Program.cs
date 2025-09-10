using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

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
