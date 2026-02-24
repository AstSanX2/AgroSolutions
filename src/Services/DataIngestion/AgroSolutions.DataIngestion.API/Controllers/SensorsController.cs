using AgroSolutions.DataIngestion.API.DTOs;
using AgroSolutions.DataIngestion.API.Validators;
using AgroSolutions.DataIngestion.Domain.Entities;
using AgroSolutions.DataIngestion.Domain.Enums;
using AgroSolutions.DataIngestion.Domain.Interfaces;
using AgroSolutions.EventBus.RabbitMQ;
using Microsoft.AspNetCore.Mvc;

namespace AgroSolutions.DataIngestion.API.Controllers;

[ApiController]
[Route("")]
public class SensorsController : ControllerBase
{
    private readonly ISensorReadingRepository _repository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<SensorsController> _logger;

    private const string AlertQueue = "alert-sensor-queue";
    private const string PropertyUpdateQueue = "property-sensor-update-queue";

    public SensorsController(
        ISensorReadingRepository repository,
        IEventBus eventBus,
        ILogger<SensorsController> logger)
    {
        _repository = repository;
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <summary>
    /// Enviar leitura de umidade (0-100%)
    /// </summary>
    [HttpPost("humidity")]
    [ProducesResponseType(typeof(SensorReadingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostHumidity([FromBody] SensorReadingRequest request)
    {
        var validator = new HumidityReadingValidator();
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        return await ProcessReading(request, SensorType.Humidity, "%");
    }

    /// <summary>
    /// Enviar leitura de temperatura (-50 a 60 C)
    /// </summary>
    [HttpPost("temperature")]
    [ProducesResponseType(typeof(SensorReadingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostTemperature([FromBody] SensorReadingRequest request)
    {
        var validator = new TemperatureReadingValidator();
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        return await ProcessReading(request, SensorType.Temperature, "C");
    }

    /// <summary>
    /// Enviar leitura de precipitacao (>= 0 mm)
    /// </summary>
    [HttpPost("rainfall")]
    [ProducesResponseType(typeof(SensorReadingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostRainfall([FromBody] SensorReadingRequest request)
    {
        var validator = new RainfallReadingValidator();
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        return await ProcessReading(request, SensorType.Rainfall, "mm");
    }

    /// <summary>
    /// Listar leituras historicas de um talhao
    /// </summary>
    [HttpGet("{propertyId}/{plotId}")]
    [ProducesResponseType(typeof(PaginatedResponse<SensorReadingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(
        string propertyId, string plotId,
        [FromQuery] SensorType? sensorType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var readings = await _repository.GetByPlotAsync(propertyId, plotId, sensorType, startDate, endDate, page, pageSize);
        var total = await _repository.CountByPlotAsync(propertyId, plotId, sensorType, startDate, endDate);

        var items = readings.Select(MapToDto).ToList();
        var response = new PaginatedResponse<SensorReadingDto>(items, page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize));

        return Ok(response);
    }

    /// <summary>
    /// Retornar ultima leitura de cada tipo de sensor
    /// </summary>
    [HttpGet("{propertyId}/{plotId}/latest")]
    [ProducesResponseType(typeof(List<SensorReadingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLatest(string propertyId, string plotId)
    {
        var readings = await _repository.GetLatestByPlotAsync(propertyId, plotId);
        return Ok(readings.Select(MapToDto));
    }

    private async Task<IActionResult> ProcessReading(SensorReadingRequest request, SensorType type, string unit)
    {
        var reading = new SensorReading
        {
            PropertyId = request.PropertyId,
            PlotId = request.PlotId,
            SensorType = type,
            Value = request.Value,
            Unit = unit,
            Timestamp = request.Timestamp
        };

        // 1. Persist to MongoDB
        await _repository.CreateAsync(reading);

        // 2. Publish to alert queue
        try
        {
            _eventBus.Publish(AlertQueue, new AlertSensorMessage(
                reading.Id, reading.PropertyId, reading.PlotId,
                type.ToString(), reading.Value, unit, reading.Timestamp));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao publicar na fila {Queue}", AlertQueue);
        }

        // 3. Publish to property update queue
        try
        {
            _eventBus.Publish(PropertyUpdateQueue, new PropertySensorUpdateMessage(
                reading.PropertyId, reading.PlotId,
                type.ToString(), reading.Value, reading.Timestamp));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao publicar na fila {Queue}", PropertyUpdateQueue);
        }

        return Created($"/{request.PropertyId}/{request.PlotId}", MapToDto(reading));
    }

    private static SensorReadingDto MapToDto(SensorReading r) => new(
        r.Id, r.PropertyId, r.PlotId,
        r.SensorType.ToString(), r.Value, r.Unit,
        r.Timestamp, r.ReceivedAt
    );
}
