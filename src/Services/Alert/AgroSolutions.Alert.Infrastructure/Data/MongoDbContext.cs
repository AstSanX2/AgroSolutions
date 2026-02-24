using AgroSolutions.Alert.Domain.Entities;
using AgroSolutions.Alert.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AgroSolutions.Alert.Infrastructure.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.Database);

        CreateIndexes();
    }

    public IMongoCollection<AlertRecord> Alerts => _database.GetCollection<AlertRecord>("alerts");

    private void CreateIndexes()
    {
        // Index for active alert queries: propertyId + plotId + sensorType + isActive
        var activeAlertIndex = Builders<AlertRecord>.IndexKeys
            .Ascending(a => a.PropertyId)
            .Ascending(a => a.PlotId)
            .Ascending(a => a.SensorType)
            .Descending(a => a.IsActive);
        Alerts.Indexes.CreateOne(new CreateIndexModel<AlertRecord>(activeAlertIndex));

        // Index for listing by property
        var propertyIndex = Builders<AlertRecord>.IndexKeys
            .Ascending(a => a.PropertyId)
            .Descending(a => a.CreatedAt);
        Alerts.Indexes.CreateOne(new CreateIndexModel<AlertRecord>(propertyIndex));
    }
}
