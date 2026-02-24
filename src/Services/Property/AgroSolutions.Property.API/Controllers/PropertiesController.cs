using System.Security.Claims;
using AgroSolutions.Property.API.DTOs;
using AgroSolutions.Property.Domain.Entities;
using AgroSolutions.Property.Domain.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroSolutions.Property.API.Controllers;

[ApiController]
[Route("")]
[Authorize]
public class PropertiesController : ControllerBase
{
    private readonly IPropertyRepository _repository;

    public PropertiesController(IPropertyRepository repository)
    {
        _repository = repository;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException();

    /// <summary>
    /// Listar propriedades do usuario autenticado
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<PropertyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50;

        var userId = GetUserId();
        var properties = await _repository.GetByOwnerAsync(userId, page, pageSize);
        var total = await _repository.CountByOwnerAsync(userId);

        var items = properties.Select(MapToDto).ToList();
        var response = new PaginatedResponse<PropertyDto>(items, page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize));

        return Ok(response);
    }

    /// <summary>
    /// Obter propriedade por ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PropertyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var property = await _repository.GetByIdAsync(id);
        if (property is null || property.ProprietarioId != GetUserId())
            return NotFound(new { message = "Propriedade nao encontrada." });

        return Ok(MapToDto(property));
    }

    /// <summary>
    /// Criar nova propriedade
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PropertyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePropertyRequest request,
        [FromServices] IValidator<CreatePropertyRequest> validator)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var userId = GetUserId();

        if (await _repository.NameExistsForOwnerAsync(userId, request.Nome))
            return Conflict(new { message = "Ja existe uma propriedade com este nome." });

        var property = new FarmProperty
        {
            Nome = request.Nome,
            Endereco = request.Endereco,
            ProprietarioId = userId
        };

        await _repository.CreateAsync(property);

        return Created($"/properties/{property.Id}", MapToDto(property));
    }

    /// <summary>
    /// Atualizar propriedade
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(PropertyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdatePropertyRequest request,
        [FromServices] IValidator<UpdatePropertyRequest> validator)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var property = await _repository.GetByIdAsync(id);
        if (property is null || property.ProprietarioId != GetUserId())
            return NotFound(new { message = "Propriedade nao encontrada." });

        if (await _repository.NameExistsForOwnerAsync(property.ProprietarioId, request.Nome, id))
            return Conflict(new { message = "Ja existe uma propriedade com este nome." });

        property.Nome = request.Nome;
        property.Endereco = request.Endereco;

        await _repository.UpdateAsync(property);

        return Ok(MapToDto(property));
    }

    /// <summary>
    /// Remover propriedade (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        var property = await _repository.GetByIdAsync(id);
        if (property is null || property.ProprietarioId != GetUserId())
            return NotFound(new { message = "Propriedade nao encontrada." });

        property.Ativo = false;
        await _repository.UpdateAsync(property);

        return NoContent();
    }

    private static PropertyDto MapToDto(FarmProperty p) => new(
        p.Id,
        p.Nome,
        p.Endereco,
        p.DataCadastro,
        p.Talhoes.Select(MapPlotToDto).ToList()
    );

    internal static PlotDto MapPlotToDto(Plot t) => new(
        t.Id,
        t.Nome,
        t.Area,
        t.Latitude,
        t.Longitude,
        new CropDto(
            t.Cultura.Nome,
            t.Cultura.Status,
            t.Cultura.UmidadeAtual,
            t.Cultura.TemperaturaAtual,
            t.Cultura.PrecipitacaoAtual,
            t.Cultura.UltimaAtualizacao
        )
    );
}
