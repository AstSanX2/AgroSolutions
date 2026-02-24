using AgroSolutions.Alert.Domain.Entities;
using AgroSolutions.Alert.Domain.Enums;

namespace AgroSolutions.Alert.Domain.Interfaces;

public interface IAlertRepository
{
    Task CreateAsync(AlertRecord alert);
    Task<AlertRecord?> GetActiveAlertAsync(string propertyId, string plotId, SensorType sensorType);
    Task DeactivateAsync(string id);
}
