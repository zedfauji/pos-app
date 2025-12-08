using Microsoft.EntityFrameworkCore;
using DiscountApi.Data;
using DiscountApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=/cloudsql/bola8pos:northamerica-south1:pos-app-1;Port=5432;Username=posapp;Password=Campus_66;Database=postgres;SSL Mode=Require;Trust Server Certificate=true";

builder.Services.AddDbContext<DiscountDbContext>(options =>
    options.UseNpgsql(connectionString));

// Services
builder.Services.AddScoped<IDiscountService, DiscountService>();
builder.Services.AddScoped<ICustomerAnalysisService, CustomerAnalysisService>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddScoped<IComboService, ComboService>();
builder.Services.AddScoped<IMigrationService, MigrationService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Run database migrations (non-blocking for Cloud Run startup)
_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(5000); // Wait 5 seconds for service to be ready
        using (var scope = app.Services.CreateScope())
        {
            var migrationService = scope.ServiceProvider.GetRequiredService<IMigrationService>();
            await migrationService.RunMigrationsAsync();
            app.Logger.LogInformation("Database migrations completed successfully");
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Database migration failed, but service will continue");
    }
});

app.Run();
