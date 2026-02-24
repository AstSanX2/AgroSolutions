using System.Security.Claims;
using AgroSolutions.Property.API.DTOs;
using AgroSolutions.Property.Domain.Entities;
using AgroSolutions.Property.Domain.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroSolutions.Property.API.Controllers;

[ApiController]
[Route("{propertyId}/plots")]
[Authorize]
public class PlotsController : ControllerBase
{
    private readonly IPropertyRepository _repository;

    public PlotsController(IPropertyRepository repository)
    {
        _repository = repository;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException();

    private async Task<FarmProperty?> GetOwnedPropertyAsync(string propertyId)
    {
        var property = await _repository.GetByIdAsync(propertyId);
        if (property is null || property.ProprietarioId != GetUserId())
            return null;
        return property;
    }

    /// <summary>
    /// Listar talhoes da propriedade
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(string propertyId)
    {
        var property = await GetOwnedPropertyAsync(propertyId);
        if (property is null)
            return NotFound(new { message = "Propriedade nao encontrada." });

        var plots = property.Talhoes.Select(PropertiesController.MapPlotToDto).ToList();
        return Ok(plots);
    }

    /// <summary>
    /// Obter talhao especifico
    /// </summary>
    [HttpGet("{plotId}")]
    [ProducesResponseType(typeof(PlotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string propertyId, string plotId)
    {
        var property = await GetOwnedPropertyAsync(propertyId);
        if (property is null)
            return NotFound(new { message = "Propriedade nao encontrada." });

        var plot = property.Talhoes.FirstOrDefault(t => t.Id == plotId);
        if (plot is null)
            return NotFound(new { message = "Talhao nao encontrado." });

        return Ok(PropertiesController.MapPlotToDto(plot));
    }

    /// <summary>
    /// Criar talhao na propriedade
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PlotDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        string propertyId,
        [FromBody] CreatePlotRequest request,
        [FromServices] IValidator<CreatePlotRequest> validator)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var property = await GetOwnedPropertyAsync(propertyId);
        if (property is null)
            return NotFound(new { message = "Propriedade nao encontrada." });

        var plot = new Plot
        {
            Nome = request.Nome,
            Area = request.Area,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Cultura = new Crop { Nome = request.CulturaNome }
        };

        property.Talhoes.Add(plot);
        await _repository.UpdateAsync(property);

        return Created($"/properties/{propertyId}/plots/{plot.Id}", PropertiesController.MapPlotToDto(plot));
    }

    /// <summary>
    /// Atualizar talhao
    /// </summary>
    [HttpPut("{plotId}")]
    [ProducesResponseType(typeof(PlotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        string propertyId,
        string plotId,
        [FromBody] UpdatePlotRequest request,
        [FromServices] IValidator<UpdatePlotRequest> validator)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var property = await GetOwnedPropertyAsync(propertyId);
        if (property is null)
            return NotFound(new { message = "Propriedade nao encontrada." });

        var plot = property.Talhoes.FirstOrDefault(t => t.Id == plotId);
        if (plot is null)
            return NotFound(new { message = "Talhao nao encontrado." });

        plot.Nome = request.Nome;
        plot.Area = request.Area;
        plot.Latitude = request.Latitude;
        plot.Longitude = request.Longitude;
        plot.Cultura.Nome = request.CulturaNome;

        await _repository.UpdateAsync(property);

        return Ok(PropertiesController.MapPlotToDto(plot));
    }

    /// <summary>
    /// Remover talhao
    /// </summary>
    [HttpDelete("{plotId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string propertyId, string plotId)
    {
        var property = await GetOwnedPropertyAsync(propertyId);
        if (property is null)
            return NotFound(new { message = "Propriedade nao encontrada." });

        var plot = property.Talhoes.FirstOrDefault(t => t.Id == plotId);
        if (plot is null)
            return NotFound(new { message = "Talhao nao encontrado." });

        property.Talhoes.Remove(plot);
        await _repository.UpdateAsync(property);

        return NoContent();
    }
}
