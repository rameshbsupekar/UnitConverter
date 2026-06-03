using UnitConverter.Core.Extensions;
using UnitConverter.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (OpenTelemetry, resilience)
builder.AddServiceDefaults();

// Add rate limiting from configuration
builder.Services.AddApplicationRateLimiting(builder.Configuration);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register HTTP clients for inter-service communication with resilience handlers
builder.Services
    .AddHttpClient("CatalogService", client =>
    {
        var url = builder.Configuration["Services:Catalog:Url"] ?? "https://localhost:7001";
        client.BaseAddress = new Uri(url);
        client.Timeout = TimeSpan.FromSeconds(10);
    })
    .AddStandardServiceResilienceHandler(maxRetries: 3);

builder.Services
    .AddHttpClient("ConversionService", client =>
    {
        var url = builder.Configuration["Services:Conversion:Url"] ?? "https://localhost:7002";
        client.BaseAddress = new Uri(url);
        client.Timeout = TimeSpan.FromSeconds(10);
    })
    .AddStandardServiceResilienceHandler(maxRetries: 3);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    // Map default health check endpoints
    app.MapDefaultEndpoints();
}

app.UseHttpsRedirection();

// Add rate limiting middleware EARLY (before routing and authorization)
app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

app.Run();
