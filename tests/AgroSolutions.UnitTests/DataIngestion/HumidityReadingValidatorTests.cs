using AgroSolutions.DataIngestion.API.DTOs;
using AgroSolutions.DataIngestion.API.Validators;
using FluentAssertions;

namespace AgroSolutions.UnitTests.DataIngestion;

public class HumidityReadingValidatorTests
{
    private readonly HumidityReadingValidator _validator = new();

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.1)]
    [InlineData(100.1)]
    [InlineData(150)]
    public void Validate_DeveRejeitarUmidadeForaDoRange(decimal value)
    {
        var request = new SensorReadingRequest("prop1", "plot1", value, DateTime.UtcNow);
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Value");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void Validate_DeveAceitarUmidadeValida(decimal value)
    {
        var request = new SensorReadingRequest("prop1", "plot1", value, DateTime.UtcNow);
        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
