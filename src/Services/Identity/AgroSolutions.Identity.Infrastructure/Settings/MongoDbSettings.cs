namespace AgroSolutions.Identity.Infrastructure.Settings;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017/agrosolutions";
    public string Database { get; set; } = "agrosolutions";
}
