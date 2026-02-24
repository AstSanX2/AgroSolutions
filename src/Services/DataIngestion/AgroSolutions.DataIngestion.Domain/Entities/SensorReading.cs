using AgroSolutions.DataIngestion.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AgroSolutions.DataIngestion.Domain.Entities;

public class SensorReading
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("propertyId")]
    public string PropertyId { get; set; } = string.Empty;

    [BsonElement("plotId")]
    public string PlotId { get; set; } = string.Empty;

    [BsonElement("sensorType")]
    [BsonRepresentation(BsonType.String)]
    public SensorType SensorType { get; set; }

    [BsonElement("value")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Value { get; set; }

    [BsonElement("unit")]
    public string Unit { get; set; } = string.Empty;

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }

    [BsonElement("receivedAt")]
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}
