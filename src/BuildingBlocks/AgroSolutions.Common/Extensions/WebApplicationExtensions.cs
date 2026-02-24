using AgroSolutions.Common.Middleware;
using Microsoft.AspNetCore.Builder;

namespace AgroSolutions.Common.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseRequestLogging(this WebApplication app, string serviceName)
    {
        app.UseMiddleware<RequestLoggingMiddleware>(serviceName);
        return app;
    }
}
