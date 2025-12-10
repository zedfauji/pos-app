using CustomerApi.Data;
using CustomerApi.Services;
using CustomerApi.Services.Communication;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure PostgreSQL connection
string? connString = builder.Configuration.GetSection("Db:Postgres").GetValue<string>("ConnectionString");
if (string.IsNullOrEmpty(connString))
{
    connString = Environment.GetEnvironmentVariable("CloudRunSocketConnectionString");
}

if (string.IsNullOrEmpty(connString))
{
    throw new InvalidOperationException("No PostgreSQL connection string found in configuration or environment variables");
}

// Add PostgreSQL data source
builder.Services.AddSingleton<NpgsqlDataSource>(_ => 
    new NpgsqlDataSourceBuilder(connString).Build());

// Add Entity Framework
builder.Services.AddDbContext<CustomerDbContext>(options =>
    options.UseNpgsql(connString));

// Register existing services
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IMembershipService, MembershipService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<ILoyaltyService, LoyaltyService>();

// Register Customer Intelligence services
builder.Services.AddScoped<ISegmentationService, SegmentationService>();
builder.Services.AddScoped<IBehavioralTriggerService, BehavioralTriggerService>();
builder.Services.AddScoped<ICampaignService, CampaignService>();
builder.Services.AddScoped<ICommunicationService, CommunicationService>();

// Register communication providers
builder.Services.AddHttpClient<ICommunicationProvider, EmailProvider>();
builder.Services.AddHttpClient<ICommunicationProvider, SmsProvider>();
builder.Services.AddHttpClient<ICommunicationProvider, WhatsAppProvider>();
builder.Services.AddScoped<ICommunicationProvider, EmailProvider>();
builder.Services.AddScoped<ICommunicationProvider, SmsProvider>();
builder.Services.AddScoped<ICommunicationProvider, WhatsAppProvider>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Don't use HTTPS redirection in production (Cloud Run handles TLS termination)
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Test database connection without EnsureCreated
try
{
    using var scope = app.Services.CreateScope();
    var dataSource = scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
    using var connection = await dataSource.OpenConnectionAsync();
    app.Logger.LogInformation("CustomerApi database connection successful");
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "CustomerApi database connection failed, but continuing startup");
}

app.Logger.LogInformation("CustomerApi starting up...");

app.Run();
