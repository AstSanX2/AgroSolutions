using AgroSolutions.Alert.Domain.Entities;
using AgroSolutions.Alert.Domain.Enums;

namespace AgroSolutions.Alert.Domain.Rules;

public interface IAlertRule
{
    SensorType ApplicableSensorType { get; }
    bool ShouldTrigger(decimal value);
    AlertRecord CreateAlert(string propertyId, string plotId, decimal value);
}
