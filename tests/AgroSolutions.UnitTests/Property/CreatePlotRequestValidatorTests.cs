using AgroSolutions.Property.API.DTOs;
using AgroSolutions.Property.API.Validators;
using FluentAssertions;

namespace AgroSolutions.UnitTests.Property;

public class CreatePlotRequestValidatorTests
{
    private readonly CreatePlotRequestValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.5)]
    public void Validate_DeveRejeitarAreaNegativaOuZero(decimal area)
    {
        // Arrange
        var request = new CreatePlotRequest("Talhao Norte", area, -23.5m, -46.6m, "Soja");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Area");
    }

    [Fact]
    public void Validate_DeveAceitarRequestValido()
    {
        // Arrange
        var request = new CreatePlotRequest("Talhao Norte", 50.5m, -23.5m, -46.6m, "Soja");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
