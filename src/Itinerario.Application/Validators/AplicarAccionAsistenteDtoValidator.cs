using FluentValidation;
using Itinerario.Application.DTOs;
using Itinerario.Domain.Enums;

namespace Itinerario.Application.Validators;

public class AplicarAccionAsistenteDtoValidator : AbstractValidator<AplicarAccionAsistenteDto>
{
    public AplicarAccionAsistenteDtoValidator()
    {
        RuleFor(a => a.Tipo)
            .NotEmpty()
            .MaximumLength(32)
            .Must(EsTipoValido)
            .WithMessage("El tipo debe ser AgregarParada o ReemplazarParada.");

        RuleFor(a => a.NumeroDia)
            .GreaterThan((short)0)
            .LessThanOrEqualTo((short)365);

        RuleFor(a => a.LugarId)
            .NotEmpty();

        RuleFor(a => a.Version)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(a => a.ParadaId)
            .NotEmpty()
            .When(a => EsReemplazo(a.Tipo))
            .WithMessage("Para reemplazar una parada, ParadaId es obligatorio.");

        RuleFor(a => a.ParadaId)
            .Empty()
            .When(a => !EsReemplazo(a.Tipo))
            .WithMessage("ParadaId solo aplica a ReemplazarParada.");

        RuleFor(a => a.Turno)
            .Must(turno => string.IsNullOrWhiteSpace(turno)
                || Enum.GetNames<Turno>().Any(nombre =>
                    string.Equals(nombre, turno.Trim(), StringComparison.OrdinalIgnoreCase)))
            .WithMessage("El turno no es válido.");
    }

    private static bool EsTipoValido(string tipo)
        => string.Equals(tipo, "AgregarParada", StringComparison.OrdinalIgnoreCase)
            || string.Equals(tipo, "ReemplazarParada", StringComparison.OrdinalIgnoreCase);

    private static bool EsReemplazo(string tipo)
        => string.Equals(tipo, "ReemplazarParada", StringComparison.OrdinalIgnoreCase);
}
