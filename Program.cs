using Serilog;
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
});

// Add HTTP client with longer timeout for Teams
builder.Services.AddHttpClient<ITeamsService, TeamsService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add Teams service
builder.Services.AddScoped<ITeamsService, TeamsService>();

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
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TI Teams Webhook API v1"));
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

try
{
    Log.Information("🚀 Starting TI Teams Webhook service");
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
