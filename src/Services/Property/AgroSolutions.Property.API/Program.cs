using AgroSolutions.Common.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseRequestLogging("PROPERTY");
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { service = "Property API", status = "running" }));

app.Run();
