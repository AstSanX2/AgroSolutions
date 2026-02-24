using AgroSolutions.Property.Domain.Entities;
using AgroSolutions.Property.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AgroSolutions.Property.Infrastructure.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.Database);

        CreateIndexes();
    }

    public IMongoCollection<FarmProperty> Properties => _database.GetCollection<FarmProperty>("properties");

    private void CreateIndexes()
    {
        var indexKeys = Builders<FarmProperty>.IndexKeys
            .Ascending(p => p.ProprietarioId)
            .Ascending(p => p.Nome);
        var indexOptions = new CreateIndexOptions { Unique = true };
        Properties.Indexes.CreateOne(new CreateIndexModel<FarmProperty>(indexKeys, indexOptions));
    }
}
