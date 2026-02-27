using AgroSolutions.DataIngestion.Domain.Enums;
using AgroSolutions.DataIngestion.Domain.Interfaces;
using AgroSolutions.DataIngestion.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AgroSolutions.DataIngestion.API.Controllers;

[ApiController]
[Route("monitoring")]
[ApiExplorerSettings(IgnoreApi = true)]
public class MonitoringController : ControllerBase
{
    private readonly ISensorReadingRepository _repository;
    private readonly MongoDbContext _dbContext;

    public MonitoringController(ISensorReadingRepository repository, MongoDbContext dbContext)
    {
        _repository = repository;
        _dbContext = dbContext;
    }

    [HttpGet("readings")]
    public async Task<IActionResult> GetReadings(
        [FromQuery] string propertyId,
        [FromQuery] string? plotId,
        [FromQuery] string? sensorType,
        [FromQuery] int limit = 500)
    {
        if (string.IsNullOrEmpty(propertyId))
            return BadRequest("propertyId is required");

        if (limit < 1) limit = 1;
        if (limit > 1000) limit = 1000;

        SensorType? parsedType = null;
        if (!string.IsNullOrEmpty(sensorType) && Enum.TryParse<SensorType>(sensorType, true, out var st))
            parsedType = st;

        List<string>? plotIds = null;
        if (!string.IsNullOrEmpty(plotId))
            plotIds = plotId.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        var readings = await _repository.GetByPropertyAsync(propertyId, plotIds, parsedType, limit);

        // Build plot name lookup from Properties collection
        var plotNameMap = await GetPlotNameMap(propertyId);

        var result = readings.Select(r => new
        {
            r.PropertyId,
            r.PlotId,
            PlotNome = plotNameMap.GetValueOrDefault(r.PlotId, r.PlotId),
            SensorType = r.SensorType.ToString(),
            Value = (double)r.Value,
            r.Unit,
            r.Timestamp
        });

        return Ok(result);
    }

    private async Task<Dictionary<string, string>> GetPlotNameMap(string propertyId)
    {
        var map = new Dictionary<string, string>();
        try
        {
            var propertiesCollection = _dbContext.Database.GetCollection<BsonDocument>("properties");
            var property = await propertiesCollection
                .Find(Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(propertyId)))
                .FirstOrDefaultAsync();

            if (property != null && property.Contains("talhoes"))
            {
                foreach (var talhao in property["talhoes"].AsBsonArray)
                {
                    var doc = talhao.AsBsonDocument;
                    var id = doc["_id"].ToString()!;
                    var nome = doc.GetValue("nome", "").AsString;
                    map[id] = !string.IsNullOrEmpty(nome) ? nome : id;
                }
            }
        }
        catch
        {
            // If lookup fails, plotId will be used as fallback
        }

        return map;
    }
}
