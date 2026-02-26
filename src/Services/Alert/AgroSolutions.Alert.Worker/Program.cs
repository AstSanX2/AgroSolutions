using AgroSolutions.Alert.Domain.Interfaces;
using AgroSolutions.Alert.Domain.Rules;
using AgroSolutions.Alert.Infrastructure.Data;
using AgroSolutions.Alert.Infrastructure.Repositories;
using AgroSolutions.Alert.Infrastructure.Settings;
using AgroSolutions.Alert.Worker.Consumers;
using AgroSolutions.Common.Extensions;
using AgroSolutions.EventBus.RabbitMQ.Extensions;

var builder = Host.CreateApplicationBuilder(args);

// OpenTelemetry
builder.Services.AddAgroTelemetry("AlertWorker", builder.Configuration, "AgroSolutions.Alert");

// MongoDB
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDB"));
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<IAlertRepository, AlertRepository>();

// RabbitMQ EventBus
builder.Services.AddRabbitMqEventBus(builder.Configuration);

// Alert Rules
builder.Services.AddSingleton<IAlertRule, HumidityCriticalRule>();
builder.Services.AddSingleton<IAlertRule, HumidityWarningRule>();
builder.Services.AddSingleton<IAlertRule, TemperatureCriticalRule>();
builder.Services.AddSingleton<IAlertRule, TemperatureHighRule>();
builder.Services.AddSingleton<IAlertRule, TemperatureLowRule>();
builder.Services.AddSingleton<IAlertRule, RainfallCriticalRule>();
builder.Services.AddSingleton<IAlertRule, RainfallWarningRule>();

// Consumers
builder.Services.AddHostedService<SensorDataConsumer>();

var host = builder.Build();
host.Run();
