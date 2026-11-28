using Itinerario.Application.DTOs;
using Itinerario.Application.Services;
using Itinerario.Application.Validators;
using Itinerario.Domain.Enums;

namespace Itinerario.UnitTests.Validators;

public class CrearViajeRecomendadoDtoValidatorTests
{
    private static readonly DateOnly Fecha = new(2026, 11, 27);

    private readonly CrearViajeRecomendadoDtoValidator _validator = new();

    [Fact]
    public void DtoValido_EsValido()
    {
        var resultado = _validator.Validate(CrearDto());

        Assert.True(resultado.IsValid, Mensajes(resultado));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void CantidadDeParadasFueraDeRango_EsInvalido(int cantidad)
    {
        var resultado = _validator.Validate(CrearDto(cantidad));

        Assert.False(resultado.IsValid);
        Assert.Contains(
            resultado.Errors,
            error => error.ErrorMessage == "El recorrido debe tener entre 2 y 4 paradas.");
    }

    [Fact]
    public void LugarIdsRepetidos_EsInvalido()
    {
        var lugarId = Guid.NewGuid();

        var resultado = _validator.Validate(
            CrearDto(ids: new[] { lugarId, lugarId }));

        Assert.False(resultado.IsValid);
        Assert.Contains(
            resultado.Errors,
            error => error.ErrorMessage == "No se permiten lugares repetidos en el recorrido.");
    }

    [Fact]
    public void TituloVacio_EsInvalido()
    {
        var dto = CrearDto();
        dto.Titulo = string.Empty;

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(
            resultado.Errors,
            error => error.PropertyName == nameof(CrearViajeRecomendadoDto.Titulo));
    }

    [Fact]
    public void FechaFinAnteriorAlInicio_EsInvalida()
    {
        var resultado = _validator.Validate(
            CrearDto(inicio: Fecha, fin: Fecha.AddDays(-1)));

        Assert.False(resultado.IsValid);
        Assert.Contains(
            resultado.Errors,
            error => error.ErrorMessage == "FechaFin no puede ser anterior a FechaInicio.");
    }

    [Fact]
    public void RangoMayorAlMaximo_EsInvalido()
    {
        var resultado = _validator.Validate(
            CrearDto(inicio: Fecha, fin: Fecha.AddDays(7)));

        Assert.False(resultado.IsValid);
        Assert.Contains(
            resultado.Errors,
            error => error.ErrorMessage == CalculadoraDuracion.MensajeRangoInvalido);
    }

    private static CrearViajeRecomendadoDto CrearDto(
        int cantidad = 4,
        DateOnly? inicio = null,
        DateOnly? fin = null,
        IReadOnlyCollection<Guid>? ids = null)
        => new()
        {
            Titulo = "Recorrido de prueba",
            Barrio = BarrioCaba.Palermo,
            CategoriaCodigo = "gastronomia_clasica",
            LugarIds = ids ?? Enumerable
                .Range(0, cantidad)
                .Select(_ => Guid.NewGuid())
                .ToList(),
            FechaInicio = inicio ?? Fecha,
            FechaFin = fin ?? Fecha
        };

    private static string Mensajes(FluentValidation.Results.ValidationResult resultado)
        => string.Join("; ", resultado.Errors.Select(error => error.ErrorMessage));
}
