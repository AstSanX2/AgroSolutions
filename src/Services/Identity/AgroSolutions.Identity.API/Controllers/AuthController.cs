using System.Security.Claims;
using AgroSolutions.Identity.API.DTOs;
using AgroSolutions.Identity.Domain.Entities;
using AgroSolutions.Identity.Domain.Interfaces;
using AgroSolutions.Identity.Infrastructure.Settings;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AgroSolutions.Identity.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;

    public AuthController(
        IUserRepository userRepository,
        ITokenService tokenService,
        IOptions<JwtSettings> jwtSettings)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _jwtSettings = jwtSettings.Value;
    }

    /// <summary>
    /// Registrar um novo usuario
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        [FromServices] IValidator<RegisterRequest> validator)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        if (await _userRepository.EmailExistsAsync(request.Email))
            return Conflict(new { message = "Ja existe um usuario com este email." });

        var user = new User
        {
            Nome = request.Nome,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Senha)
        };

        await _userRepository.CreateAsync(user);

        var token = _tokenService.GenerateToken(user);
        var response = BuildAuthResponse(user, token);

        return Created($"/api/auth/me", response);
    }

    /// <summary>
    /// Autenticar um usuario existente
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] IValidator<LoginRequest> validator)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Senha, user.PasswordHash))
            return Unauthorized(new { message = "Email ou senha invalidos." });

        if (!user.Ativo)
            return Unauthorized(new { message = "Usuario inativo." });

        var token = _tokenService.GenerateToken(user);
        var response = BuildAuthResponse(user, token);

        return Ok(response);
    }

    /// <summary>
    /// Obter dados do usuario autenticado
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub");

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return Unauthorized();

        return Ok(new UserDto(user.Id, user.Nome, user.Email, user.DataCadastro));
    }

    private AuthResponse BuildAuthResponse(User user, string token)
    {
        var expiresIn = _jwtSettings.ExpirationInHours * 3600;
        var userDto = new UserDto(user.Id, user.Nome, user.Email, user.DataCadastro);
        return new AuthResponse(token, expiresIn, userDto);
    }
}
