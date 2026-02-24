using AgroSolutions.Alert.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AgroSolutions.Alert.Domain.Entities;

public class AlertRecord
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("propertyId")]
    public string PropertyId { get; set; } = string.Empty;

    [BsonElement("plotId")]
    public string PlotId { get; set; } = string.Empty;

    [BsonElement("alertType")]
    [BsonRepresentation(BsonType.String)]
    public AlertType AlertType { get; set; }

    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("sensorType")]
    [BsonRepresentation(BsonType.String)]
    public SensorType SensorType { get; set; }

    [BsonElement("sensorValue")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal SensorValue { get; set; }

    [BsonElement("threshold")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Threshold { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}
