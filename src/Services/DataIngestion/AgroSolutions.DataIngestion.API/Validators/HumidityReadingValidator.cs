using AgroSolutions.DataIngestion.API.DTOs;
using FluentValidation;

namespace AgroSolutions.DataIngestion.API.Validators;

public class HumidityReadingValidator : AbstractValidator<SensorReadingRequest>
{
    public HumidityReadingValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty().WithMessage("O propertyId e obrigatorio.");
        RuleFor(x => x.PlotId).NotEmpty().WithMessage("O plotId e obrigatorio.");
        RuleFor(x => x.Value)
            .InclusiveBetween(0, 100).WithMessage("A umidade deve estar entre 0 e 100%.");
        RuleFor(x => x.Timestamp).NotEmpty().WithMessage("O timestamp e obrigatorio.");
    }
}
