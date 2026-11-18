using FluentValidation;
using Itinerario.Application.DTOs;
using Itinerario.Domain.Enums;

namespace Itinerario.Application.Validators;

public class ViajeDiaCreateDtoValidator : AbstractValidator<ViajeDiaCreateDto>
{
    public ViajeDiaCreateDtoValidator()
    {
        RuleFor(d => d.NumeroDia)
            .GreaterThan((short)0)
            .WithMessage("El número de día debe ser mayor a cero.");

        RuleFor(d => d.Fecha)
            .NotNull()
            .WithMessage("Cada día debe tener una fecha.");

        RuleFor(d => d.Barrio)
            .IsInEnum();

        RuleFor(d => d.TipoPuntoPartida)
            .IsInEnum();

        RuleFor(d => d.NombrePuntoPartida)
            .MaximumLength(150);

        RuleFor(d => d.DireccionPartida)
            .MaximumLength(200);

        RuleFor(d => d.LugarPartidaId)
            .NotNull()
            .When(d => d.TipoPuntoPartida is TipoPuntoPartida.HospedajePropio
                or TipoPuntoPartida.HospedajeSugerido)
            .WithMessage("Seleccioná un lugar de alojamiento para el día.");
    }
}
