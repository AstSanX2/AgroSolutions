using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AgroSolutions.EventBus.RabbitMQ;

public class RabbitMqEventBus : IEventBus, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public RabbitMqEventBus(IOptions<RabbitMqSettings> settings, ILogger<RabbitMqEventBus> logger)
    {
        _logger = logger;

        var factory = new ConnectionFactory
        {
            Uri = new Uri(settings.Value.ConnectionString),
            DispatchConsumersAsync = true
        };

        var retries = 5;
        for (var i = 0; i < retries; i++)
        {
            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                _logger.LogInformation("Conectado ao RabbitMQ");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Tentativa {Attempt}/{Retries} de conexao ao RabbitMQ falhou: {Message}", i + 1, retries, ex.Message);
                if (i == retries - 1) throw;
                Thread.Sleep(3000);
            }
        }

        throw new InvalidOperationException("Falha ao conectar ao RabbitMQ");
    }

    public void Publish<T>(string queueName, T message) where T : class
    {
        _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);

        var json = JsonSerializer.Serialize(message, _jsonOptions);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;

        _channel.BasicPublish("", queueName, properties, body);
        _logger.LogDebug("Mensagem publicada na fila {Queue}", queueName);
    }

    public void Subscribe<T>(string queueName, Func<T, Task> handler) where T : class
    {
        _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
        _channel.BasicQos(0, 1, false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var message = JsonSerializer.Deserialize<T>(json, _jsonOptions);
                if (message != null)
                {
                    await handler(message);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem da fila {Queue}", queueName);
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel.BasicConsume(queueName, autoAck: false, consumer);
        _logger.LogInformation("Inscrito na fila {Queue}", queueName);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
