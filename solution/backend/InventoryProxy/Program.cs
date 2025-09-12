using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddHttpClient();
builder.Services.AddCors(opts =>
{
    opts.AddDefaultPolicy(p => p
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();

// Log all requests
app.Use(async (context, next) =>
{
    var request = context.Request;
    var requestBody = "";
    
    if (request.ContentLength > 0)
    {
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        requestBody = await reader.ReadToEndAsync();
        request.Body.Position = 0;
    }
    
    Console.WriteLine($"=== REQUEST ===");
    Console.WriteLine($"Method: {request.Method}");
    Console.WriteLine($"Path: {request.Path}");
    Console.WriteLine($"Query: {request.QueryString}");
    Console.WriteLine($"Headers: {JsonSerializer.Serialize(request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()))}");
    if (!string.IsNullOrEmpty(requestBody))
    {
        Console.WriteLine($"Body: {requestBody}");
    }
    Console.WriteLine($"===============");
    
    await next();
});

// Proxy endpoint
app.MapFallback(async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var targetUrl = "https://magidesk-inventory-904541739138.northamerica-south1.run.app" + context.Request.Path + context.Request.QueryString;
    
    Console.WriteLine($"Proxying to: {targetUrl}");
    
    var requestMessage = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUrl);
    
    // Copy headers
    foreach (var header in context.Request.Headers)
    {
        if (!header.Key.StartsWith(":") && header.Key != "Host")
        {
            requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
    }
    
    // Copy body for POST/PUT requests
    if (context.Request.ContentLength > 0)
    {
        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;
        
        if (!string.IsNullOrEmpty(body))
        {
            requestMessage.Content = new StringContent(body, Encoding.UTF8, context.Request.ContentType ?? "application/json");
        }
    }
    
    try
    {
        var response = await client.SendAsync(requestMessage);
        var responseBody = await response.Content.ReadAsStringAsync();
        
        Console.WriteLine($"=== RESPONSE ===");
        Console.WriteLine($"Status: {response.StatusCode}");
        Console.WriteLine($"Headers: {JsonSerializer.Serialize(response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()))}");
        Console.WriteLine($"Body: {responseBody}");
        Console.WriteLine($"================");
        
        context.Response.StatusCode = (int)response.StatusCode;
        
        foreach (var header in response.Headers)
        {
            context.Response.Headers.TryAdd(header.Key, header.Value.ToArray());
        }
        
        foreach (var header in response.Content.Headers)
        {
            context.Response.Headers.TryAdd(header.Key, header.Value.ToArray());
        }
        
        await context.Response.WriteAsync(responseBody);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"=== ERROR ===");
        Console.WriteLine($"Exception: {ex.Message}");
        Console.WriteLine($"Stack: {ex.StackTrace}");
        Console.WriteLine($"=============");
        
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
    }
});

app.Run("http://localhost:5001");
