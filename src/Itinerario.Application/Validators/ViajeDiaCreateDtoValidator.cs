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

        RuleFor(d => d.TipoViaje)
            .Must(tipo => !tipo.HasValue || Enum.IsDefined(tipo.Value))
            .WithMessage("La modalidad de viaje del día no es válida.");

        RuleFor(d => d.CategoriaCodigos)
            .Must(categorias => categorias is null
                || categorias.Count == 0
                || categorias.Count is >= 2 and <= 4)
            .WithMessage("Debe elegir entre 2 y 4 categorías por día.");

        RuleFor(d => d.CategoriaCodigos)
            .Must(categorias => categorias is null
                || categorias.All(codigo => !string.IsNullOrWhiteSpace(codigo)))
            .WithMessage("Los códigos de categoría del día no pueden estar vacíos.");

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
