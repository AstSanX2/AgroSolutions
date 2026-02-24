using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AgroSolutions.Common.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly string _serviceName;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger, string serviceName)
    {
        _next = next;
        _logger = logger;
        _serviceName = serviceName;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "/";

        if (IsHealthCheck(path))
        {
            await _next(context);
            return;
        }

        _logger.LogInformation("[{Service}] {Method} {Path} received", _serviceName, method, path);
        await _next(context);
        _logger.LogInformation("[{Service}] {Method} {Path} -> {StatusCode}", _serviceName, method, path, context.Response.StatusCode);
    }

    private static bool IsHealthCheck(string path) =>
        path.Equals("/health", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("/liveness", StringComparison.OrdinalIgnoreCase);
}
