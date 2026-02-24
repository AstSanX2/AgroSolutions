using AgroSolutions.Alert.Domain.Entities;
using AgroSolutions.Alert.Domain.Enums;

namespace AgroSolutions.Alert.Domain.Rules;

public class TemperatureCriticalRule : IAlertRule
{
    public SensorType ApplicableSensorType => SensorType.Temperature;
    public bool ShouldTrigger(decimal value) => value > 40;

    public AlertRecord CreateAlert(string propertyId, string plotId, decimal value) => new()
    {
        PropertyId = propertyId,
        PlotId = plotId,
        AlertType = AlertType.HighTemperature,
        Message = "Temperatura Critica",
        SensorType = SensorType.Temperature,
        SensorValue = value,
        Threshold = 40
    };
}

public class TemperatureHighRule : IAlertRule
{
    public SensorType ApplicableSensorType => SensorType.Temperature;
    public bool ShouldTrigger(decimal value) => value > 35;

    public AlertRecord CreateAlert(string propertyId, string plotId, decimal value) => new()
    {
        PropertyId = propertyId,
        PlotId = plotId,
        AlertType = AlertType.HighTemperature,
        Message = "Temperatura Alta",
        SensorType = SensorType.Temperature,
        SensorValue = value,
        Threshold = 35
    };
}

public class TemperatureLowRule : IAlertRule
{
    public SensorType ApplicableSensorType => SensorType.Temperature;
    public bool ShouldTrigger(decimal value) => value < 5;

    public AlertRecord CreateAlert(string propertyId, string plotId, decimal value) => new()
    {
        PropertyId = propertyId,
        PlotId = plotId,
        AlertType = AlertType.LowTemperature,
        Message = "Temperatura Baixa",
        SensorType = SensorType.Temperature,
        SensorValue = value,
        Threshold = 5
    };
}
