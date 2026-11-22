using FluentValidation;
using Itinerario.Application.Configuration;
using Itinerario.Application.DTOs;
using Microsoft.Extensions.Options;

namespace Itinerario.Application.Validators;

public class AsistenteRequestDtoValidator : AbstractValidator<AsistenteRequestDto>
{
    public AsistenteRequestDtoValidator(IOptions<AsistenteOptions> options)
    {
        var asistente = options.Value;

        RuleFor(r => r.Mensaje)
            .NotEmpty()
            .MaximumLength(asistente.MaxMensajeCaracteres);

        RuleFor(r => r.NumeroDia)
            .GreaterThan((short)0)
            .LessThanOrEqualTo((short)365)
            .When(r => r.NumeroDia.HasValue);

        RuleFor(r => r.Historial)
            .Must(historial => historial is null
                || historial.Count <= asistente.MaxHistorial)
            .WithMessage(
                $"El historial no puede superar {asistente.MaxHistorial} mensajes.");

        RuleForEach(r => r.Historial)
            .SetValidator(new MensajeHistorialDtoValidator())
            .When(r => r.Historial is not null);
    }
}
