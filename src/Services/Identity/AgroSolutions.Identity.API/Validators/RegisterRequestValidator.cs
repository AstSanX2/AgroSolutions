using AgroSolutions.Identity.API.DTOs;
using FluentValidation;

namespace AgroSolutions.Identity.API.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("O nome e obrigatorio.")
            .MinimumLength(3).WithMessage("O nome deve ter no minimo 3 caracteres.")
            .MaximumLength(100).WithMessage("O nome deve ter no maximo 100 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O email e obrigatorio.")
            .EmailAddress().WithMessage("O email informado nao e valido.");

        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("A senha e obrigatoria.")
            .MinimumLength(8).WithMessage("A senha deve ter no minimo 8 caracteres.");
    }
}
