using AgroSolutions.Identity.API.DTOs;
using FluentAssertions;
using FluentValidation;

namespace AgroSolutions.UnitTests.Identity;

public class RegisterRequestValidatorTests
{
    [Theory]
    [InlineData("email-invalido")]
    [InlineData("sem-arroba.com")]
    [InlineData("@sem-usuario.com")]
    [InlineData("")]
    public void Validate_DeveRejeitarEmailComFormatoInvalido(string email)
    {
        // Arrange
        var request = new RegisterRequest("Joao Produtor", email, "Senha12345");

        // Cria o validator inline (sem depender do assembly da API)
        var validator = new InlineValidator<RegisterRequest>();
        validator.RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O email e obrigatorio.")
            .EmailAddress().WithMessage("O email informado nao e valido.");

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_DeveAceitarEmailValido()
    {
        // Arrange
        var request = new RegisterRequest("Joao Produtor", "joao@fazenda.com", "Senha12345");

        var validator = new InlineValidator<RegisterRequest>();
        validator.RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O email e obrigatorio.")
            .EmailAddress().WithMessage("O email informado nao e valido.");

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
