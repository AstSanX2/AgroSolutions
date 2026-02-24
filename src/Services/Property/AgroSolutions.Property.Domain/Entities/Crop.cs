using MongoDB.Bson.Serialization.Attributes;

namespace AgroSolutions.Property.Domain.Entities;

public class Crop
{
    [BsonElement("nome")]
    public string Nome { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = "Normal";

    [BsonElement("umidadeAtual")]
    public decimal UmidadeAtual { get; set; }

    [BsonElement("temperaturaAtual")]
    public decimal TemperaturaAtual { get; set; }

    [BsonElement("precipitacaoAtual")]
    public decimal PrecipitacaoAtual { get; set; }

    [BsonElement("ultimaAtualizacao")]
    public DateTime? UltimaAtualizacao { get; set; }
}
