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
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseRequestLogging("DATAINGESTION");

// PathBase para funcionar atras do Gateway (/api/sensors)
var pathBase = builder.Configuration["PathBase"] ?? "";
if (!string.IsNullOrEmpty(pathBase))
    app.UsePathBase(pathBase);

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("v1/swagger.json", "DataIngestion API v1"));

app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { service = "DataIngestion API", status = "running" }));
app.MapControllers();

app.Run();

public partial class Program { }
