using FluentValidation;
using Itinerario.Application.DTOs;

namespace Itinerario.Application.Validators;

public class UnirseViajeDtoValidator : AbstractValidator<UnirseViajeDto>
{
    public UnirseViajeDtoValidator()
    {
        RuleFor(u => u.CodigoInvitacion)
            .NotEmpty()
            .MaximumLength(12);
    }
}
