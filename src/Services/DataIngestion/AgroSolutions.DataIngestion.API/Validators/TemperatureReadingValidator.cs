using AgroSolutions.DataIngestion.API.DTOs;
using FluentValidation;

namespace AgroSolutions.DataIngestion.API.Validators;

public class TemperatureReadingValidator : AbstractValidator<SensorReadingRequest>
{
    public TemperatureReadingValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty().WithMessage("O propertyId e obrigatorio.");
        RuleFor(x => x.PlotId).NotEmpty().WithMessage("O plotId e obrigatorio.");
        RuleFor(x => x.Value)
            .InclusiveBetween(-50, 60).WithMessage("A temperatura deve estar entre -50 e 60 graus Celsius.");
        RuleFor(x => x.Timestamp).NotEmpty().WithMessage("O timestamp e obrigatorio.");
    }
}
