using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AgroSolutions.Property.Domain.Entities;

public class Plot
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("nome")]
    public string Nome { get; set; } = string.Empty;

    [BsonElement("area")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Area { get; set; }

    [BsonElement("latitude")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Latitude { get; set; }

    [BsonElement("longitude")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Longitude { get; set; }

    [BsonElement("cultura")]
    public Crop Cultura { get; set; } = new();
}
