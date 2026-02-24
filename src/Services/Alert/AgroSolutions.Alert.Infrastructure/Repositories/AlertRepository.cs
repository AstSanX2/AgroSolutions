using AgroSolutions.Alert.Domain.Entities;
using AgroSolutions.Alert.Domain.Enums;
using AgroSolutions.Alert.Domain.Interfaces;
using AgroSolutions.Alert.Infrastructure.Data;
using MongoDB.Driver;

namespace AgroSolutions.Alert.Infrastructure.Repositories;

public class AlertRepository : IAlertRepository
{
    private readonly MongoDbContext _context;

    public AlertRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(AlertRecord alert)
    {
        await _context.Alerts.InsertOneAsync(alert);
    }

    public async Task<AlertRecord?> GetActiveAlertAsync(string propertyId, string plotId, SensorType sensorType)
    {
        return await _context.Alerts
            .Find(a => a.PropertyId == propertyId
                       && a.PlotId == plotId
                       && a.SensorType == sensorType
                       && a.IsActive)
            .SortByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task DeactivateAsync(string id)
    {
        var update = Builders<AlertRecord>.Update.Set(a => a.IsActive, false);
        await _context.Alerts.UpdateOneAsync(a => a.Id == id, update);
    }
}
