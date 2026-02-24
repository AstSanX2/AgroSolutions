using AgroSolutions.Common.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseRequestLogging("IDENTITY");
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { service = "Identity API", status = "running" }));

app.Run();
