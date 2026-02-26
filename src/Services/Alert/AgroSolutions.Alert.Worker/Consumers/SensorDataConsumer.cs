using System.Diagnostics.Metrics;
using AgroSolutions.Alert.Domain.Enums;
using AgroSolutions.Alert.Domain.Interfaces;
using AgroSolutions.Alert.Domain.Rules;
using AgroSolutions.Alert.Worker.Messages;
using AgroSolutions.EventBus.RabbitMQ;

namespace AgroSolutions.Alert.Worker.Consumers;

public class SensorDataConsumer : BackgroundService
{
    private static readonly Meter Meter = new("AgroSolutions.Alert", "1.0.0");
    private static readonly Counter<long> AlertsTriggeredCounter = Meter.CreateCounter<long>("alerts_triggered_total", description: "Total de alertas disparados");
    private static readonly Counter<long> AlertsNormalizedCounter = Meter.CreateCounter<long>("alerts_normalized_total", description: "Total de alertas normalizados");

    private readonly IEventBus _eventBus;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEnumerable<IAlertRule> _alertRules;
    private readonly ILogger<SensorDataConsumer> _logger;

    private const string AlertSensorQueue = "alert-sensor-queue";
    private const string PropertyAlertStatusQueue = "property-alert-status-queue";

    public SensorDataConsumer(
        IEventBus eventBus,
        IServiceScopeFactory scopeFactory,
        IEnumerable<IAlertRule> alertRules,
        ILogger<SensorDataConsumer> logger)
    {
        _eventBus = eventBus;
        _scopeFactory = scopeFactory;
        _alertRules = alertRules;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SensorDataConsumer iniciado. Aguardando mensagens na fila {Queue}...", AlertSensorQueue);

        _eventBus.Subscribe<AlertSensorMessage>(AlertSensorQueue, async message =>
        {
            _logger.LogInformation(
                "Mensagem recebida: PropertyId={PropertyId}, PlotId={PlotId}, SensorType={SensorType}, Value={Value}",
                message.PropertyId, message.PlotId, message.SensorType, message.Value);

            await ProcessSensorDataAsync(message);
        });

        return Task.CompletedTask;
    }

    private async Task ProcessSensorDataAsync(AlertSensorMessage message)
    {
        try
        {
            if (!Enum.TryParse<SensorType>(message.SensorType, true, out var sensorType))
            {
                _logger.LogWarning("Tipo de sensor desconhecido: {SensorType}", message.SensorType);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var alertRepository = scope.ServiceProvider.GetRequiredService<IAlertRepository>();

            var applicableRules = _alertRules
                .Where(r => r.ApplicableSensorType == sensorType)
                .OrderByDescending(r => r.ShouldTrigger(message.Value) ? 1 : 0);

            var activeAlert = await alertRepository.GetActiveAlertAsync(
                message.PropertyId, message.PlotId, sensorType);

            // Find the most critical triggered rule
            var triggeredRule = applicableRules.FirstOrDefault(r => r.ShouldTrigger(message.Value));

            if (triggeredRule != null)
            {
                if (activeAlert == null)
                {
                    var alert = triggeredRule.CreateAlert(message.PropertyId, message.PlotId, message.Value);
                    await alertRepository.CreateAsync(alert);
                    AlertsTriggeredCounter.Add(1, new KeyValuePair<string, object?>("alert_type", alert.AlertType.ToString()));

                    _logger.LogWarning(
                        "ALERTA CRIADO: {Message} - PropertyId={PropertyId}, PlotId={PlotId}, Valor={Value}",
                        alert.Message, message.PropertyId, message.PlotId, message.Value);

                    NotifyPropertyService(message.PropertyId, message.PlotId, alert.AlertType.ToString(), alert.Message, true);
                }
                else
                {
                    _logger.LogInformation(
                        "Alerta ja ativo para PropertyId={PropertyId}, PlotId={PlotId}, SensorType={SensorType}",
                        message.PropertyId, message.PlotId, sensorType);
                }
            }
            else if (activeAlert != null)
            {
                await alertRepository.DeactivateAsync(activeAlert.Id);
                AlertsNormalizedCounter.Add(1, new KeyValuePair<string, object?>("alert_type", activeAlert.AlertType.ToString()));

                _logger.LogInformation(
                    "ALERTA DESATIVADO: PropertyId={PropertyId}, PlotId={PlotId}, SensorType={SensorType} - Valor normalizado: {Value}",
                    message.PropertyId, message.PlotId, sensorType, message.Value);

                NotifyPropertyService(message.PropertyId, message.PlotId, activeAlert.AlertType.ToString(), "Valor normalizado", false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar dados do sensor: {Message}", ex.Message);
        }
    }

    private void NotifyPropertyService(string propertyId, string plotId, string alertType, string message, bool isActive)
    {
        try
        {
            var statusMessage = new PropertyAlertStatusMessage(propertyId, plotId, alertType, message, isActive);
            _eventBus.Publish(PropertyAlertStatusQueue, statusMessage);

            _logger.LogInformation("Status do alerta enviado para Property Service: {AlertType} IsActive={IsActive}", alertType, isActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao notificar Property Service: {Message}", ex.Message);
        }
    }
}
