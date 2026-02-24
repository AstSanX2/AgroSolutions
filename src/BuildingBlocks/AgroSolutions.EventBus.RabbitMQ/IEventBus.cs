namespace AgroSolutions.EventBus.RabbitMQ;

public interface IEventBus
{
    void Publish<T>(string queueName, T message) where T : class;
    void Subscribe<T>(string queueName, Func<T, Task> handler) where T : class;
}
