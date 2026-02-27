using AgroSolutions.DataIngestion.Domain.Entities;
using AgroSolutions.DataIngestion.Domain.Enums;

namespace AgroSolutions.DataIngestion.Domain.Interfaces;

public interface ISensorReadingRepository
{
    Task CreateAsync(SensorReading reading);
    Task<List<SensorReading>> GetByPlotAsync(string propertyId, string plotId, SensorType? sensorType, DateTime? startDate, DateTime? endDate, int page, int pageSize);
    Task<int> CountByPlotAsync(string propertyId, string plotId, SensorType? sensorType, DateTime? startDate, DateTime? endDate);
    Task<List<SensorReading>> GetLatestByPlotAsync(string propertyId, string plotId);
    Task<List<SensorReading>> GetByPropertyAsync(string propertyId, IEnumerable<string>? plotIds, SensorType? sensorType, int limit);
}
