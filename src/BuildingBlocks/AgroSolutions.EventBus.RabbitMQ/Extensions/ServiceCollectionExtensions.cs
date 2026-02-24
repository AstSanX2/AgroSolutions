using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgroSolutions.EventBus.RabbitMQ.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMqEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqSettings>(configuration.GetSection("RabbitMQ"));
        services.AddSingleton<IEventBus, RabbitMqEventBus>();
        return services;
    }
}
