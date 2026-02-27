using AgroSolutions.Property.Domain.Entities;
using AgroSolutions.Property.Domain.Interfaces;
using AgroSolutions.Property.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AgroSolutions.Property.API.Controllers;

[ApiController]
[Route("monitoring")]
[ApiExplorerSettings(IgnoreApi = true)]
public class MonitoringController : ControllerBase
{
    private readonly IPropertyRepository _repository;
    private readonly MongoDbContext _dbContext;

    public MonitoringController(IPropertyRepository repository, MongoDbContext dbContext)
    {
        _repository = repository;
        _dbContext = dbContext;
    }

    [HttpGet("properties")]
    public async Task<IActionResult> GetProperties()
    {
        var properties = await _repository.GetAllActiveAsync();
        var result = properties.Select(p => new { p.Id, p.Nome });
        return Ok(result);
    }

    [HttpGet("properties/{propertyId}/plots")]
    public async Task<IActionResult> GetPlots(string propertyId)
    {
        var property = await _repository.GetByIdAsync(propertyId);
        if (property == null)
            return Ok(Array.Empty<object>());

        var result = property.Talhoes.Select(t => new { t.Id, t.Nome });
        return Ok(result);
    }

    [HttpGet("plots-status")]
    public async Task<IActionResult> GetPlotsStatus(
        [FromQuery] string? propertyId,
        [FromQuery] string? plotId)
    {
        List<FarmProperty> properties;

        if (!string.IsNullOrEmpty(propertyId))
        {
            var prop = await _repository.GetByIdAsync(propertyId);
            properties = prop != null ? [prop] : [];
        }
        else
        {
            properties = await _repository.GetAllActiveAsync();
        }

        var plots = properties.SelectMany(p => p.Talhoes.Select(t => new
        {
            PropertyId = p.Id,
            PropertyNome = p.Nome,
            PlotId = t.Id,
            PlotNome = t.Nome,
            t.Area,
            CulturaNome = t.Cultura.Nome,
            Status = t.Cultura.Status,
            Umidade = (double)t.Cultura.UmidadeAtual,
            Temperatura = (double)t.Cultura.TemperaturaAtual,
            Precipitacao = (double)t.Cultura.PrecipitacaoAtual,
            UltimaAtualizacao = t.Cultura.UltimaAtualizacao
        }));

        if (!string.IsNullOrEmpty(plotId))
        {
            var plotIds = plotId.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
            if (plotIds.Count > 0)
                plots = plots.Where(p => plotIds.Contains(p.PlotId));
        }

        return Ok(plots);
    }

    [HttpGet("alerts-history")]
    public async Task<IActionResult> GetAlertsHistory(
        [FromQuery] string? propertyId,
        [FromQuery] string? plotId,
        [FromQuery] int limit = 100)
    {
        if (limit < 1) limit = 1;
        if (limit > 500) limit = 500;

        var alertsCollection = _dbContext.Database.GetCollection<BsonDocument>("alerts");

        var filterBuilder = Builders<BsonDocument>.Filter;
        var filter = filterBuilder.Empty;

        if (!string.IsNullOrEmpty(propertyId))
            filter &= filterBuilder.Eq("propertyId", propertyId);
        if (!string.IsNullOrEmpty(plotId))
        {
            var plotIds = plotId.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            if (plotIds.Count == 1)
                filter &= filterBuilder.Eq("plotId", plotIds[0]);
            else if (plotIds.Count > 1)
                filter &= filterBuilder.In("plotId", plotIds);
        }

        var alerts = await alertsCollection
            .Find(filter)
            .SortByDescending(a => a["createdAt"])
            .Limit(limit)
            .ToListAsync();

        // Collect property/plot names for enrichment
        var propertyIds = alerts.Select(a => a.GetValue("propertyId", "").AsString).Distinct().ToList();
        var propertiesMap = new Dictionary<string, FarmProperty>();
        foreach (var pid in propertyIds)
        {
            if (string.IsNullOrEmpty(pid)) continue;
            var prop = await _repository.GetByIdAsync(pid);
            if (prop != null) propertiesMap[pid] = prop;
        }

        var result = alerts.Select(a =>
        {
            var pId = a.GetValue("propertyId", "").AsString;
            var plId = a.GetValue("plotId", "").AsString;
            propertiesMap.TryGetValue(pId, out var prop);
            var plot = prop?.Talhoes.FirstOrDefault(t => t.Id == plId);

            return new
            {
                Id = a["_id"].ToString(),
                PropertyId = pId,
                PropertyNome = prop?.Nome ?? pId,
                PlotId = plId,
                PlotNome = plot?.Nome ?? plId,
                AlertType = a.GetValue("alertType", "").AsString,
                SensorType = a.GetValue("sensorType", "").AsString,
                SensorValue = a.Contains("sensorValue") ? a["sensorValue"].ToDouble() : 0,
                Threshold = a.Contains("threshold") ? a["threshold"].ToDouble() : 0,
                Message = a.GetValue("message", "").AsString,
                IsActive = a.GetValue("isActive", false).AsBoolean,
                CreatedAt = a.Contains("createdAt") ? a["createdAt"].ToUniversalTime() : DateTime.MinValue
            };
        });

        return Ok(result);
    }
}
