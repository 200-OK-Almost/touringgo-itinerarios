using FluentValidation;
using Itinerario.Application.DTOs;

namespace Itinerario.Application.Validators;

public class MensajeHistorialDtoValidator : AbstractValidator<MensajeHistorialDto>
{
    public MensajeHistorialDtoValidator()
    {
        RuleFor(m => m.Rol)
            .NotEmpty()
            .Must(rol => string.Equals(rol, "user", StringComparison.OrdinalIgnoreCase)
                || string.Equals(rol, "assistant", StringComparison.OrdinalIgnoreCase))
            .WithMessage("El rol del historial debe ser user o assistant.");

        RuleFor(m => m.Contenido)
            .NotEmpty()
            .MaximumLength(2000);
    }
}
