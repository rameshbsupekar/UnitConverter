using UnitConverter.Auth.API.Middleware;
using UnitConverter.Auth.Infrastructure.Extensions;
using UnitConverter.Core.Extensions;

var builder = WebApplicationBuilder.CreateBuilder(args);

// Add service defaults (OpenTelemetry, resilience)
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("AuthDb") ?? "Data Source=data/converter.db;Cache=Shared";
var ensureMigrations = builder.Configuration.GetValue<bool>("Aspire:EnsureMigrationsApplied", defaultValue: true);

// Ensure data folder exists (for SQLite database file)
var dataFolder = Path.Combine(AppContext.BaseDirectory, "data");
if (!Directory.Exists(dataFolder))
{
    Directory.CreateDirectory(dataFolder);
}

// SQLite for all environments (dev, test, production)
builder.Services.AddInfrastructureServices(connectionString, useSqlite: true);

// Configure Core resilience (rate limiting and HTTP resilience)
// This demonstrates that UnitConverter.Core is truly decoupled and reusable
builder.Services.AddCoreResilience(builder.Configuration);

var app = builder.Build();

// Apply migrations automatically if configured
if (ensureMigrations)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<UnitConverter.Auth.Infrastructure.Data.AuthDbContext>();
    await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Middleware pipeline in correct order
app.UseHttpsRedirection();

// 1. Rate limiter (ASP.NET Core built-in) - MUST be early
app.UseRateLimiter();

// 2. Auth middleware (custom)
app.UseAuthMiddleware();

// 3. Routing
app.UseRouting();

// 4. Authorization
app.UseAuthorization();

// 5. Default endpoints (health checks)
app.MapDefaultEndpoints();

// 6. API endpoints
app.MapControllers();

app.Run();
