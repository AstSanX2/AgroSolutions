using AgroSolutions.Property.API.DTOs;
using FluentValidation;

namespace AgroSolutions.Property.API.Validators;

public class UpdatePlotRequestValidator : AbstractValidator<UpdatePlotRequest>
{
    public UpdatePlotRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("O nome do talhao e obrigatorio.")
            .MaximumLength(100).WithMessage("O nome deve ter no maximo 100 caracteres.");

        RuleFor(x => x.Area)
            .GreaterThan(0).WithMessage("A area deve ser maior que zero.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("A latitude deve estar entre -90 e 90.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("A longitude deve estar entre -180 e 180.");

        RuleFor(x => x.CulturaNome)
            .NotEmpty().WithMessage("O nome da cultura e obrigatorio.")
            .MaximumLength(100).WithMessage("O nome da cultura deve ter no maximo 100 caracteres.");
    }
}
