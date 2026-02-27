using AgroSolutions.Common.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// OpenTelemetry
builder.Services.AddAgroTelemetry("Gateway", builder.Configuration);

// Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseRequestLogging("GATEWAY");
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapGet("/", () => Results.Ok(new { service = "API Gateway", status = "running" }));
app.MapReverseProxy();

app.Run();
