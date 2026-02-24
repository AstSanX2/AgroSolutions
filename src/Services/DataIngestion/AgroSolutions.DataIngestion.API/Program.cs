using AgroSolutions.Common.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseRequestLogging("DATAINGESTION");
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { service = "DataIngestion API", status = "running" }));

app.Run();
