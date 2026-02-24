using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AgroSolutions.Property.Domain.Entities;

public class FarmProperty
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("nome")]
    public string Nome { get; set; } = string.Empty;

    [BsonElement("endereco")]
    public string Endereco { get; set; } = string.Empty;

    [BsonElement("proprietarioId")]
    public string ProprietarioId { get; set; } = string.Empty;

    [BsonElement("dataCadastro")]
    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

    [BsonElement("ativo")]
    public bool Ativo { get; set; } = true;

    [BsonElement("talhoes")]
    public List<Plot> Talhoes { get; set; } = new();
}
