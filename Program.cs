using Serilog;
using TiTeamsWebhook.Models.TiApi;
using TiTeamsWebhook.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/webhook-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddNewtonsoftJson();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "TI Teams Webhook API", Version = "v1" });
    c.EnableAnnotations(); // Enable Swagger annotations for better documentation
});

// Add HTTP client with longer timeout for Teams
builder.Services.AddHttpClient<ITeamsService, TeamsService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Configure TI API Settings
builder.Services.Configure<TiApiSettings>(
    builder.Configuration.GetSection("TiApi"));

// Add HTTP Client for TI API
builder.Services.AddHttpClient<ITiAuthService, TiAuthService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "TiTeamsWebhook/1.0");
});

// Register services
builder.Services.AddScoped<ITiAuthService, TiAuthService>();
builder.Services.AddScoped<ITeamsService, TeamsService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
    .AddCheck<TiAuthHealthCheck>("ti-auth");

// Add CORS for testing
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TI Teams Webhook API v1");
        c.RoutePrefix = "swagger"; // Keep swagger at /swagger
    });
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseCors();
app.UseAuthorization();

// Add health check endpoint
app.MapHealthChecks("/health");

// Map controllers
app.MapControllers();

try
{
    Log.Information("🚀 Starting TI Teams Webhook service with TI API Authentication");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "💥 Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}