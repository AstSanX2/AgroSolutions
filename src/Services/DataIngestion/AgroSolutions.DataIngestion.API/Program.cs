using AgroSolutions.Common.Extensions;
using AgroSolutions.DataIngestion.Domain.Interfaces;
using AgroSolutions.DataIngestion.Infrastructure.Data;
using AgroSolutions.DataIngestion.Infrastructure.Repositories;
using AgroSolutions.DataIngestion.Infrastructure.Settings;
using AgroSolutions.EventBus.RabbitMQ.Extensions;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Settings
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDB"));

// Infrastructure
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<ISensorReadingRepository, SensorReadingRepository>();

// OpenTelemetry
builder.Services.AddAgroTelemetry("DataIngestionAPI", builder.Configuration, "AgroSolutions.DataIngestion");

// RabbitMQ EventBus
builder.Services.AddRabbitMqEventBus(builder.Configuration);

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AgroSolutions DataIngestion API",
        Version = "v1",
        Description = "Servico de ingestao de dados de sensores IoT"
    });
});

// Health checks
builder.Services.AddHealthChecks()
    .AddMongoDb(
        builder.Configuration["MongoDB:ConnectionString"] ?? "mongodb://localhost:27017/agrosolutions",
        name: "mongodb",
        tags: new[] { "ready" })
    .AddRabbitMQ(
        new Uri(builder.Configuration["RabbitMQ:ConnectionString"] ?? "amqp://guest:guest@localhost:5672"),
        name: "rabbitmq",
        tags: new[] { "ready" });

var app = builder.Build();

// PathBase para funcionar atras do Gateway (/api/sensors)
var pathBase = builder.Configuration["PathBase"] ?? "";
if (!string.IsNullOrEmpty(pathBase))
    app.UsePathBase(pathBase);

app.UseRequestLogging("DATAINGESTION");

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("v1/swagger.json", "DataIngestion API v1"));

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapControllers();

app.Run();

public partial class Program { }
