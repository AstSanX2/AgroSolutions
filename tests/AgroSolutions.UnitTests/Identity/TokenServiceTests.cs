using System.IdentityModel.Tokens.Jwt;
using System.Text;
using AgroSolutions.Identity.Domain.Entities;
using AgroSolutions.Identity.Infrastructure.Services;
using AgroSolutions.Identity.Infrastructure.Settings;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace AgroSolutions.UnitTests.Identity;

public class TokenServiceTests
{
    private readonly TokenService _tokenService;
    private readonly JwtSettings _jwtSettings;

    public TokenServiceTests()
    {
        _jwtSettings = new JwtSettings
        {
            Secret = "AgroSolutionsTestSecretKey2024SuperSecure!",
            Issuer = "AgroSolutions",
            Audience = "AgroSolutions",
            ExpirationInHours = 24
        };

        var options = Mock.Of<IOptions<JwtSettings>>(o => o.Value == _jwtSettings);
        _tokenService = new TokenService(options);
    }

    [Fact]
    public void GenerateToken_DeveGerarJwtValidoComClaimsCorretas()
    {
        // Arrange
        var user = new User
        {
            Id = "507f1f77bcf86cd799439011",
            Nome = "Joao Produtor",
            Email = "joao@fazenda.com"
        };

        // Act
        var token = _tokenService.GenerateToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();

        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudience = _jwtSettings.Audience,
            IssuerSigningKey = key
        };

        var principal = handler.ValidateToken(token, validationParams, out var validatedToken);

        validatedToken.Should().BeOfType<JwtSecurityToken>();
        var jwt = (JwtSecurityToken)validatedToken;

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        jwt.Claims.Should().Contain(c => c.Type == "nome" && c.Value == user.Nome);
        jwt.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddHours(24), TimeSpan.FromMinutes(1));
    }
}
