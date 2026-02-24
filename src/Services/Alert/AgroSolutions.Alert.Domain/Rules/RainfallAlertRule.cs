using AgroSolutions.Alert.Domain.Entities;
using AgroSolutions.Alert.Domain.Enums;

namespace AgroSolutions.Alert.Domain.Rules;

public class RainfallCriticalRule : IAlertRule
{
    public SensorType ApplicableSensorType => SensorType.Rainfall;
    public bool ShouldTrigger(decimal value) => value > 100;

    public AlertRecord CreateAlert(string propertyId, string plotId, decimal value) => new()
    {
        PropertyId = propertyId,
        PlotId = plotId,
        AlertType = AlertType.HeavyRain,
        Message = "Chuva Muito Intensa",
        SensorType = SensorType.Rainfall,
        SensorValue = value,
        Threshold = 100
    };
}

public class RainfallWarningRule : IAlertRule
{
    public SensorType ApplicableSensorType => SensorType.Rainfall;
    public bool ShouldTrigger(decimal value) => value > 50;

    public AlertRecord CreateAlert(string propertyId, string plotId, decimal value) => new()
    {
        PropertyId = propertyId,
        PlotId = plotId,
        AlertType = AlertType.HeavyRain,
        Message = "Chuva Intensa",
        SensorType = SensorType.Rainfall,
        SensorValue = value,
        Threshold = 50
    };
}
