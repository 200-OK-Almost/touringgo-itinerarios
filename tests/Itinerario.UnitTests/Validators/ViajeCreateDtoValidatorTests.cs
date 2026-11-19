using Itinerario.Application.DTOs;
using Itinerario.Application.Services;
using Itinerario.Application.Validators;
using Itinerario.Domain.Enums;

namespace Itinerario.UnitTests.Validators;

public class ViajeCreateDtoValidatorTests
{
    private static readonly DateOnly Inicio = new(2026, 3, 10);

    private readonly ViajeCreateDtoValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(6)]
    public void RangoDeUnoASieteDias_EsValido(int diasDeDiferencia)
    {
        var dto = CrearDto(Inicio, Inicio.AddDays(diasDeDiferencia));

        var resultado = _validator.Validate(dto);

        Assert.True(resultado.IsValid, Mensajes(resultado));
    }

    [Fact]
    public void OchoDiasInclusivos_EsInvalido()
    {
        var dto = CrearDto(Inicio, Inicio.AddDays(7));

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(
            resultado.Errors,
            error => error.ErrorMessage == CalculadoraDuracion.MensajeRangoInvalido);
    }

    [Fact]
    public void FechaFinAnteriorAlInicio_EsInvalida()
    {
        var dto = CrearDto(Inicio, Inicio.AddDays(-1));

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(
            resultado.Errors,
            error => error.ErrorMessage == "FechaFin no puede ser anterior a FechaInicio.");
    }

    [Fact]
    public void SoloFechaDeInicio_EsInvalido()
    {
        var dto = CrearDto(Inicio, null);

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(
            resultado.Errors,
            error => error.ErrorMessage == "Debe indicar la fecha de inicio y la fecha de fin.");
    }

    [Fact]
    public void SoloFechaDeFin_EsInvalido()
    {
        var dto = CrearDto(null, Inicio);

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(
            resultado.Errors,
            error => error.ErrorMessage == "Debe indicar la fecha de inicio y la fecha de fin.");
    }

    [Fact]
    public void AmbasFechasNulas_EsValidoPorCompatibilidadLegacy()
    {
        var dto = CrearDto(null, null);

        var resultado = _validator.Validate(dto);

        Assert.True(resultado.IsValid, Mensajes(resultado));
    }

    [Fact]
    public void DiasValidos_ConRangoCoincidente_EsValido()
    {
        var dto = CrearDto(Inicio, Inicio.AddDays(2));
        dto.Dias = CrearDias(Inicio, 3);

        var resultado = _validator.Validate(dto);

        Assert.True(resultado.IsValid, Mensajes(resultado));
    }

    [Fact]
    public void DiasQueNoCoincidenConElRango_EsInvalido()
    {
        var dto = CrearDto(Inicio, Inicio.AddDays(2));
        dto.Dias = CrearDias(Inicio.AddDays(1), 3);

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(
            resultado.Errors,
            error => error.ErrorMessage == "Los días deben coincidir con el rango de fechas del viaje.");
    }

    [Fact]
    public void MasDeSieteDias_EsInvalido()
    {
        var dto = CrearDto(Inicio, Inicio.AddDays(7));
        dto.Dias = CrearDias(Inicio, 8);

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(
            resultado.Errors,
            error => error.ErrorMessage == CalculadoraDuracion.MensajeRangoInvalido);
    }

    [Fact]
    public void DiaConHospedajeSinLugar_EsInvalido()
    {
        var dto = CrearDto(Inicio, Inicio);
        dto.Dias = CrearDias(Inicio, 1);
        dto.Dias.First().TipoPuntoPartida = TipoPuntoPartida.HospedajePropio;

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(
            resultado.Errors,
            error => error.ErrorMessage == "Seleccioná un lugar de alojamiento para el día.");
    }

    [Fact]
    public void DiaConCategoriasValidas_EsValido()
    {
        var dto = CrearDto(Inicio, Inicio);
        dto.Dias = CrearDias(Inicio, 1);
        dto.Dias.First().CategoriaCodigos = new[]
        {
            "cafe_especialidad",
            "museos_galerias"
        };
        dto.Dias.First().TipoViaje = TipoViaje.Familia;

        var resultado = _validator.Validate(dto);

        Assert.True(resultado.IsValid, Mensajes(resultado));
    }

    [Fact]
    public void DiaConUnaSolaCategoria_EsInvalido()
    {
        var dto = CrearDto(Inicio, Inicio);
        dto.Dias = CrearDias(Inicio, 1);
        dto.Dias.First().CategoriaCodigos = new[] { "cafe_especialidad" };

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(
            resultado.Errors,
            error => error.ErrorMessage.Contains("2 y 4"));
    }

    [Fact]
    public void DiaConModalidadInvalida_EsInvalido()
    {
        var dto = CrearDto(Inicio, Inicio);
        dto.Dias = CrearDias(Inicio, 1);
        dto.Dias.First().TipoViaje = (TipoViaje)99;

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(
            resultado.Errors,
            error => error.ErrorMessage.Contains("modalidad de viaje"));
    }

    [Fact]
    public void DiasSinFechasDelViaje_EsInvalido()
    {
        var dto = CrearDto(null, null);
        dto.Dias = CrearDias(Inicio, 1);

        var resultado = _validator.Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(
            resultado.Errors,
            error => error.ErrorMessage == "Debe indicar la fecha de inicio y la fecha de fin.");
    }

    private static IReadOnlyCollection<ViajeDiaCreateDto> CrearDias(DateOnly inicio, int cantidad)
        => Enumerable.Range(0, cantidad)
            .Select(indice => new ViajeDiaCreateDto
            {
                NumeroDia = (short)(indice + 1),
                Fecha = inicio.AddDays(indice),
                Barrio = BarrioCaba.Palermo,
                TipoPuntoPartida = TipoPuntoPartida.NodoUrbano
            })
            .ToList();

    private static ViajeCreateDto CrearDto(DateOnly? fechaInicio, DateOnly? fechaFin)
        => new()
        {
            Titulo = "Viaje de prueba",
            TipoViaje = TipoViaje.Solo,
            Barrio = BarrioCaba.Palermo,
            AlcanceTemporal = AlcanceTemporal.DiaCompleto,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            NivelPresupuesto = NivelPresupuesto.Equilibrado,
            RitmoCaminata = RitmoCaminata.Tranquilo,
            TipoPuntoPartida = TipoPuntoPartida.NodoUrbano,
            CategoriaCodigos = new[] { "cafe_especialidad", "museos_galerias" }
        };

    private static string Mensajes(FluentValidation.Results.ValidationResult resultado)
        => string.Join(" | ", resultado.Errors.Select(error => error.ErrorMessage));
}
