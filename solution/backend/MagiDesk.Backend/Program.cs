using MagiDesk.Backend.Services;
using MagiDesk.Shared.DTOs.Auth;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Controllers
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS (allow local tools and WinUI app)
const string CorsPolicy = "AllowAll";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Options
builder.Services.Configure<MagiDesk.Backend.Services.FirestoreOptions>(
    builder.Configuration.GetSection("Firestore"));

// App services
builder.Services.AddSingleton<MagiDesk.Backend.Services.IFirestoreService, MagiDesk.Backend.Services.FirestoreService>();
builder.Services.AddSingleton<MagiDesk.Backend.Services.ICashFlowService, MagiDesk.Backend.Services.CashFlowService>();
builder.Services.AddSingleton<MagiDesk.Backend.Services.IInventoryService, MagiDesk.Backend.Services.InventoryService>();
builder.Services.AddSingleton<MagiDesk.Backend.Services.IOrdersService, MagiDesk.Backend.Services.OrdersService>();
builder.Services.AddHttpClient<MagiDesk.Backend.Services.IEmailService, MagiDesk.Backend.Services.EmailService>();
builder.Services.AddSingleton<MagiDesk.Backend.Services.IUsersService, MagiDesk.Backend.Services.UsersService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicy);

app.MapControllers();

// Seed default admin user if not exists
using (var scope = app.Services.CreateScope())
{
    try
    {
        var users = scope.ServiceProvider.GetRequiredService<IUsersService>();
        var existing = await users.GetByUsernameAsync("admin");
        if (existing == null)
        {
            await users.CreateUserAsync(new CreateUserRequest
            {
                Username = "admin",
                Password = "1234",
                Role = "admin"
            });
        }
    }
    catch { }
}

app.Run();
