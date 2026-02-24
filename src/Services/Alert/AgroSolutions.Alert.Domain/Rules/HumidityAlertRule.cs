using AgroSolutions.Alert.Domain.Entities;
using AgroSolutions.Alert.Domain.Enums;

namespace AgroSolutions.Alert.Domain.Rules;

public class HumidityCriticalRule : IAlertRule
{
    public SensorType ApplicableSensorType => SensorType.Humidity;
    public bool ShouldTrigger(decimal value) => value < 20;

    public AlertRecord CreateAlert(string propertyId, string plotId, decimal value) => new()
    {
        PropertyId = propertyId,
        PlotId = plotId,
        AlertType = AlertType.DroughtAlert,
        Message = "Alerta de Seca Critico",
        SensorType = SensorType.Humidity,
        SensorValue = value,
        Threshold = 20
    };
}

public class HumidityWarningRule : IAlertRule
{
    public SensorType ApplicableSensorType => SensorType.Humidity;
    public bool ShouldTrigger(decimal value) => value < 30;

    public AlertRecord CreateAlert(string propertyId, string plotId, decimal value) => new()
    {
        PropertyId = propertyId,
        PlotId = plotId,
        AlertType = AlertType.DroughtAlert,
        Message = "Alerta de Seca",
        SensorType = SensorType.Humidity,
        SensorValue = value,
        Threshold = 30
    };
}
