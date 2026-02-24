using AgroSolutions.DataIngestion.API.DTOs;
using FluentValidation;

namespace AgroSolutions.DataIngestion.API.Validators;

public class RainfallReadingValidator : AbstractValidator<SensorReadingRequest>
{
    public RainfallReadingValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty().WithMessage("O propertyId e obrigatorio.");
        RuleFor(x => x.PlotId).NotEmpty().WithMessage("O plotId e obrigatorio.");
        RuleFor(x => x.Value)
            .GreaterThanOrEqualTo(0).WithMessage("A precipitacao deve ser maior ou igual a zero.");
        RuleFor(x => x.Timestamp).NotEmpty().WithMessage("O timestamp e obrigatorio.");
    }
}
