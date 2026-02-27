using AgroSolutions.Property.Domain.Entities;
using AgroSolutions.Property.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AgroSolutions.Property.API.Controllers;

[ApiController]
[Route("monitoring")]
[ApiExplorerSettings(IgnoreApi = true)]
public class MonitoringController : ControllerBase
{
    private readonly IPropertyRepository _repository;

    public MonitoringController(IPropertyRepository repository)
    {
        _repository = repository;
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
            plots = plots.Where(p => p.PlotId == plotId);

        return Ok(plots);
    }
}
