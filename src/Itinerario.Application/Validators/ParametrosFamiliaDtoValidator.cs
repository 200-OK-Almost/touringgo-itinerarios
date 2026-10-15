using FluentValidation;
using Itinerario.Application.DTOs;

namespace Itinerario.Application.Validators;

public class ParametrosFamiliaDtoValidator : AbstractValidator<ParametrosFamiliaDto>
{
    public ParametrosFamiliaDtoValidator()
    {
        RuleFor(p => p.CantidadAcompanantes)
            .GreaterThanOrEqualTo((short)0);

        RuleFor(p => p.CantidadNinos)
            .GreaterThanOrEqualTo((short)0);

        RuleFor(p => p.CantidadNinos)
            .Equal((short)0)
            .When(p => !p.HayNinos)
            .WithMessage("CantidadNinos debe ser 0 cuando HayNinos es falso.");
    }
}
