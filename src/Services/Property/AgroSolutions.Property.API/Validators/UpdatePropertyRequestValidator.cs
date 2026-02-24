using AgroSolutions.Property.API.DTOs;
using FluentValidation;

namespace AgroSolutions.Property.API.Validators;

public class UpdatePropertyRequestValidator : AbstractValidator<UpdatePropertyRequest>
{
    public UpdatePropertyRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("O nome da propriedade e obrigatorio.")
            .MinimumLength(3).WithMessage("O nome deve ter no minimo 3 caracteres.")
            .MaximumLength(200).WithMessage("O nome deve ter no maximo 200 caracteres.");

        RuleFor(x => x.Endereco)
            .NotEmpty().WithMessage("O endereco e obrigatorio.")
            .MaximumLength(500).WithMessage("O endereco deve ter no maximo 500 caracteres.");
    }
}
