using FluentValidation;
using Itinerario.Application.DTOs;
using Itinerario.Domain.Enums;

namespace Itinerario.Application.Validators;

public class ViajeCreateDtoValidator : AbstractValidator<ViajeCreateDto>
{
    public ViajeCreateDtoValidator()
    {
        RuleFor(v => v.Titulo)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(v => v.TipoViaje)
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

        RuleFor(v => v.FechaFin)
            .GreaterThanOrEqualTo(v => v.FechaInicio!.Value)
            .When(v => v.FechaInicio.HasValue && v.FechaFin.HasValue)
            .WithMessage("FechaFin no puede ser anterior a FechaInicio.");

        RuleFor(v => v.CategoriaIds)
            .Must(c => c is { Count: >= 2 and <= 4 })
            .WithMessage("Debe elegir entre 2 y 4 etiquetas.");

        RuleFor(v => v.CreadoPorUsuarioId)
            .NotEmpty();

        RuleFor(v => v.ParametrosFamilia)
            .Null()
            .When(v => v.TipoViaje != TipoViaje.Familia)
            .WithMessage("ParametrosFamilia solo aplica a viajes de tipo Familia.");

        RuleFor(v => v.ParametrosFamilia)
            .SetValidator(new ParametrosFamiliaDtoValidator()!)
            .When(v => v.ParametrosFamilia is not null);
    }
}
