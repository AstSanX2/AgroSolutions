namespace AgroSolutions.Property.API.DTOs;

// Requests
public record CreatePlotRequest(string Nome, decimal Area, decimal Latitude, decimal Longitude, string CulturaNome);
public record UpdatePlotRequest(string Nome, decimal Area, decimal Latitude, decimal Longitude, string CulturaNome);

// Responses
public record PlotDto(
    string Id,
    string Nome,
    decimal Area,
    decimal Latitude,
    decimal Longitude,
    CropDto Cultura
);

public record CropDto(
    string Nome,
    string Status,
    decimal UmidadeAtual,
    decimal TemperaturaAtual,
    decimal PrecipitacaoAtual,
    DateTime? UltimaAtualizacao
);
