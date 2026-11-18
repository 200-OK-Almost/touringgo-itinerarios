using FluentValidation;
using Itinerario.Application.DTOs;
using Itinerario.Application.Services;
using Itinerario.Domain.Enums;

namespace Itinerario.Application.Validators;

public class ViajeUpdateDtoValidator : AbstractValidator<ViajeUpdateDto>
{
    public ViajeUpdateDtoValidator()
    {
        RuleFor(v => v.Titulo)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(v => v.TipoViaje)
            .IsInEnum();

        RuleFor(v => v.Estado)
            .IsInEnum();

        RuleFor(v => v.Barrio)
            .IsInEnum();

        RuleFor(v => v.AlcanceTemporal)
            .IsInEnum();

        RuleFor(v => v.NivelPresupuesto)
            .IsInEnum();

        RuleFor(v => v.TipoPuntoPartida)
            .IsInEnum();

        RuleFor(v => v.NombrePuntoPartida)
            .MaximumLength(150);

        RuleFor(v => v.DireccionPartida)
            .MaximumLength(200);

        RuleFor(v => v.RitmoCaminata)
            .Must(r => r is RitmoCaminata.Tranquilo or RitmoCaminata.Activo)
            .WithMessage("El ritmo de caminata solo puede ser Tranquilo o Activo.");

        RuleFor(v => v.FechaInicio)
            .NotNull()
            .When(v => v.FechaFin.HasValue)
            .WithMessage("Debe indicar la fecha de inicio y la fecha de fin.");

        RuleFor(v => v.FechaFin)
            .NotNull()
            .When(v => v.FechaInicio.HasValue)
            .WithMessage("Debe indicar la fecha de inicio y la fecha de fin.");

        RuleFor(v => v.FechaFin)
            .GreaterThanOrEqualTo(v => v.FechaInicio!.Value)
            .When(v => v.FechaInicio.HasValue && v.FechaFin.HasValue)
            .WithMessage("FechaFin no puede ser anterior a FechaInicio.");

        RuleFor(v => v.FechaFin)
            .Must((dto, fechaFin) =>
            {
                if (!dto.FechaInicio.HasValue || !fechaFin.HasValue)
                {
                    return true;
                }

                var dias = CalculadoraDuracion.ContarDiasInclusivos(
                    dto.FechaInicio.Value,
                    fechaFin.Value);

                return CalculadoraDuracion.EsDuracionValida(dias);
            })
            .When(v => v.FechaInicio.HasValue
                && v.FechaFin.HasValue
                && v.FechaFin.Value >= v.FechaInicio.Value)
            .WithMessage(CalculadoraDuracion.MensajeRangoInvalido);

        RuleFor(v => v.CategoriaCodigos)
            .Must(c => c is { Count: >= 2 and <= 4 })
            .WithMessage("Debe elegir entre 2 y 4 etiquetas.");

        RuleFor(v => v.CategoriaCodigos)
            .Must(c => c.All(x => !string.IsNullOrWhiteSpace(x)))
            .WithMessage("Los códigos de categoría no pueden estar vacíos.");

        RuleFor(v => v.FechaInicio)
            .NotNull()
            .When(v => v.Dias is { Count: > 0 })
            .WithMessage("Debe indicar la fecha de inicio y la fecha de fin.");

        RuleFor(v => v.FechaFin)
            .NotNull()
            .When(v => v.Dias is { Count: > 0 })
            .WithMessage("Debe indicar la fecha de inicio y la fecha de fin.");

        RuleFor(v => v.Dias)
            .Must(dias => dias is null || ViajeDiasValidation.CantidadValida(dias))
            .WithMessage(CalculadoraDuracion.MensajeRangoInvalido);

        RuleFor(v => v.Dias)
            .Must(dias => dias is null || ViajeDiasValidation.NumeracionValida(dias))
            .WithMessage("Los días deben estar numerados de 1 a N sin repetidos.");

        RuleFor(v => v.Dias)
            .Must((dto, dias) => ViajeDiasValidation.CoincidenConRango(
                dto.FechaInicio,
                dto.FechaFin,
                dias))
            .When(v => v.Dias is { Count: > 0 })
            .WithMessage("Los días deben coincidir con el rango de fechas del viaje.");

        RuleForEach(v => v.Dias)
            .SetValidator(new ViajeDiaCreateDtoValidator())
            .When(v => v.Dias is not null);

        RuleFor(v => v.ParametrosFamilia)
            .Null()
            .When(v => v.TipoViaje != TipoViaje.Familia)
            .WithMessage("ParametrosFamilia solo aplica a viajes de tipo Familia.");

        RuleFor(v => v.ParametrosFamilia)
            .SetValidator(new ParametrosFamiliaDtoValidator()!)
            .When(v => v.ParametrosFamilia is not null);
    }
}
