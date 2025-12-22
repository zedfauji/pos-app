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
builder.Services.AddScoped<ITableRepository, TableRepository>();
builder.Services.AddScoped<IBillingRepository, BillingRepository>();
builder.Services.AddScoped<IOrderIntegrationService, OrderIntegrationService>();
builder.Services.AddScoped<IBillingService, MagiDesk.Core.Services.BillingService>();
builder.Services.AddScoped<MagiDesk.Core.Interfaces.ICommandHandler<MagiDesk.Core.Commands.StopSessionCommand, MagiDesk.Core.Commands.StopSessionResult>, MagiDesk.Core.Commands.StopSessionCommandHandler>();

// Shift Controller
builder.Services.AddScoped<IShiftRepository, ShiftRepository>();
builder.Services.AddScoped<IShiftService, MagiDesk.Infrastructure.Services.ShiftService>();

// Database Init
builder.Services.AddHostedService<TablesApi.Services.DatabaseInitializer>();

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
app.MapGet("/health", () => Microsoft.AspNetCore.Http.Results.Ok("OK"));

app.Run();
