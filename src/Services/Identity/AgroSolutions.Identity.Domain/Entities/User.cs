using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AgroSolutions.Identity.Domain.Entities;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("nome")]
    public string Nome { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    [BsonElement("dataCadastro")]
    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

    [BsonElement("ativo")]
    public bool Ativo { get; set; } = true;
}
