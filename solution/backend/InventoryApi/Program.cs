using MagiDesk.Core.Interfaces;
using MagiDesk.Infrastructure.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Core & Infrastructure
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddHostedService<InventoryApi.Services.DatabaseInitializer>();

// CORS
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGet("/health", () => Microsoft.AspNetCore.Http.Results.Ok("InventoryApi OK"));

app.Run();
