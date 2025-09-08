using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SettingsApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
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

// Options
builder.Services.Configure<FirestoreOptions>(builder.Configuration.GetSection("Firestore"));

// App services
builder.Services.AddSingleton<ISettingsService, SettingsService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicy);
app.MapControllers();

app.Run();
