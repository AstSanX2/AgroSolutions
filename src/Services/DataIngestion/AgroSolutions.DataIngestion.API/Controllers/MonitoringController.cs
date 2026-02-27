using AgroSolutions.DataIngestion.Domain.Enums;
using AgroSolutions.DataIngestion.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AgroSolutions.DataIngestion.API.Controllers;

[ApiController]
[Route("monitoring")]
[ApiExplorerSettings(IgnoreApi = true)]
public class MonitoringController : ControllerBase
{
    private readonly ISensorReadingRepository _repository;

    public MonitoringController(ISensorReadingRepository repository)
    {
        _repository = repository;
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

        var readings = await _repository.GetByPropertyAsync(propertyId, plotId, parsedType, limit);

        var result = readings.Select(r => new
        {
            r.PropertyId,
            r.PlotId,
            SensorType = r.SensorType.ToString(),
            Value = (double)r.Value,
            r.Unit,
            r.Timestamp
        });

        return Ok(result);
    }
}
