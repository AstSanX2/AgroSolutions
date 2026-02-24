using AgroSolutions.DataIngestion.Domain.Entities;
using AgroSolutions.DataIngestion.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AgroSolutions.DataIngestion.Infrastructure.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.Database);

        CreateIndexes();
    }

    public IMongoCollection<SensorReading> SensorReadings => _database.GetCollection<SensorReading>("sensor_readings");

    private void CreateIndexes()
    {
        // Index for history queries: propertyId + plotId + timestamp desc
        var historyIndex = Builders<SensorReading>.IndexKeys
            .Ascending(r => r.PropertyId)
            .Ascending(r => r.PlotId)
            .Descending(r => r.Timestamp);
        SensorReadings.Indexes.CreateOne(new CreateIndexModel<SensorReading>(historyIndex));

        // Index for type-filtered queries
        var typeIndex = Builders<SensorReading>.IndexKeys
            .Ascending(r => r.PropertyId)
            .Ascending(r => r.PlotId)
            .Ascending(r => r.SensorType);
        SensorReadings.Indexes.CreateOne(new CreateIndexModel<SensorReading>(typeIndex));
    }
}
