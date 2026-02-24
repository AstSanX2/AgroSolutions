using AgroSolutions.Identity.Domain.Entities;
using AgroSolutions.Identity.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AgroSolutions.Identity.Infrastructure.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.Database);

        CreateIndexes();
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>("users");

    private void CreateIndexes()
    {
        var indexKeys = Builders<User>.IndexKeys.Ascending(u => u.Email);
        var indexOptions = new CreateIndexOptions { Unique = true };
        Users.Indexes.CreateOne(new CreateIndexModel<User>(indexKeys, indexOptions));
    }
}
