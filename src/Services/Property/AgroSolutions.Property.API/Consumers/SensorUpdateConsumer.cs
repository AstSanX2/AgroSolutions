using AgroSolutions.Property.Domain.Interfaces;
using AgroSolutions.EventBus.RabbitMQ;

namespace AgroSolutions.Property.API.Consumers;

public record PropertySensorUpdateMessage(
    string PropertyId,
    string PlotId,
    string SensorType,
    decimal Value,
    DateTime Timestamp
);

public record PropertyAlertStatusMessage(
    string PropertyId,
    string PlotId,
    string AlertType,
    string Message,
    bool IsActive
);

public class SensorUpdateConsumer : BackgroundService
{
    private readonly IEventBus _eventBus;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SensorUpdateConsumer> _logger;

    private const string SensorUpdateQueue = "property-sensor-update-queue";
    private const string AlertStatusQueue = "property-alert-status-queue";

    public SensorUpdateConsumer(
        IEventBus eventBus,
        IServiceScopeFactory scopeFactory,
        ILogger<SensorUpdateConsumer> logger)
    {
        _eventBus = eventBus;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SensorUpdateConsumer iniciado. Aguardando mensagens...");

        _eventBus.Subscribe<PropertySensorUpdateMessage>(SensorUpdateQueue, async message =>
        {
            _logger.LogInformation(
                "Atualizacao de sensor recebida: PropertyId={PropertyId}, PlotId={PlotId}, SensorType={SensorType}, Value={Value}",
                message.PropertyId, message.PlotId, message.SensorType, message.Value);

            await ProcessSensorUpdateAsync(message);
        });

        _eventBus.Subscribe<PropertyAlertStatusMessage>(AlertStatusQueue, async message =>
        {
            _logger.LogInformation(
                "Status de alerta recebido: PropertyId={PropertyId}, PlotId={PlotId}, AlertType={AlertType}, IsActive={IsActive}",
                message.PropertyId, message.PlotId, message.AlertType, message.IsActive);

            await ProcessAlertStatusAsync(message);
        });

        return Task.CompletedTask;
    }

    private async Task ProcessSensorUpdateAsync(PropertySensorUpdateMessage message)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IPropertyRepository>();

            var property = await repository.GetByIdAsync(message.PropertyId);
            if (property == null)
            {
                _logger.LogWarning("Propriedade nao encontrada: {PropertyId}", message.PropertyId);
                return;
            }

            var plot = property.Talhoes.FirstOrDefault(t => t.Id == message.PlotId);
            if (plot == null)
            {
                _logger.LogWarning("Talhao nao encontrado: {PlotId} na propriedade {PropertyId}", message.PlotId, message.PropertyId);
                return;
            }

            switch (message.SensorType.ToLower())
            {
                case "humidity":
                    plot.Cultura.UmidadeAtual = message.Value;
                    break;
                case "temperature":
                    plot.Cultura.TemperaturaAtual = message.Value;
                    break;
                case "rainfall":
                    plot.Cultura.PrecipitacaoAtual = message.Value;
                    break;
                default:
                    _logger.LogWarning("Tipo de sensor desconhecido: {SensorType}", message.SensorType);
                    return;
            }

            plot.Cultura.UltimaAtualizacao = message.Timestamp;
            await repository.UpdateAsync(property);

            _logger.LogInformation("Dados do talhao atualizados: PropertyId={PropertyId}, PlotId={PlotId}", message.PropertyId, message.PlotId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar atualizacao de sensor: {Message}", ex.Message);
        }
    }

    private async Task ProcessAlertStatusAsync(PropertyAlertStatusMessage message)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IPropertyRepository>();

            var property = await repository.GetByIdAsync(message.PropertyId);
            if (property == null)
            {
                _logger.LogWarning("Propriedade nao encontrada: {PropertyId}", message.PropertyId);
                return;
            }

            var plot = property.Talhoes.FirstOrDefault(t => t.Id == message.PlotId);
            if (plot == null)
            {
                _logger.LogWarning("Talhao nao encontrado: {PlotId} na propriedade {PropertyId}", message.PlotId, message.PropertyId);
                return;
            }

            plot.Cultura.Status = message.IsActive ? message.Message : "Normal";
            await repository.UpdateAsync(property);

            _logger.LogInformation(
                "Status do talhao atualizado: PropertyId={PropertyId}, PlotId={PlotId}, Status={Status}",
                message.PropertyId, message.PlotId, plot.Cultura.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar status de alerta: {Message}", ex.Message);
        }
    }
}
