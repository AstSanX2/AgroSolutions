using AgroSolutions.Common.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseRequestLogging("GATEWAY");
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { service = "API Gateway", status = "running" }));
app.MapReverseProxy();

app.Run();
