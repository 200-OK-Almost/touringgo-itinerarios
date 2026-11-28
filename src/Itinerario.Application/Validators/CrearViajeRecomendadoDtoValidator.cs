using FluentValidation;
using Itinerario.Application.DTOs;
using Itinerario.Application.Services;

namespace Itinerario.Application.Validators;

public class CrearViajeRecomendadoDtoValidator : AbstractValidator<CrearViajeRecomendadoDto>
{
    public CrearViajeRecomendadoDtoValidator()
    {
        RuleFor(v => v.Titulo)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(v => v.Barrio)
            .IsInEnum();

        RuleFor(v => v.CategoriaCodigo)
            .MaximumLength(100);

        RuleFor(v => v.LugarIds)
            .NotNull()
            .Must(ids => ids is { Count: >= 2 and <= 4 })
            .WithMessage("El recorrido debe tener entre 2 y 4 paradas.");

        RuleFor(v => v.LugarIds)
            .Must(ids => ids!.Distinct().Count() == ids!.Count)
            .When(v => v.LugarIds is not null)
            .WithMessage("No se permiten lugares repetidos en el recorrido.");

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
    }
}
