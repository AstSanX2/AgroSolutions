using AgroSolutions.Identity.API.DTOs;
using FluentValidation;

namespace AgroSolutions.Identity.API.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O email e obrigatorio.")
            .EmailAddress().WithMessage("O email informado nao e valido.");

        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("A senha e obrigatoria.");
    }
}
