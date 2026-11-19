using Itinerario.Application.DTOs;
using Itinerario.Application.Services;
using Itinerario.Domain.Enums;

namespace Itinerario.UnitTests.Services;

public class ReglasTurnoTests
{
    [Theory]
    [InlineData("cafe_especialidad", Turno.Manana, true)]
    [InlineData("cafe_especialidad", Turno.Mediodia, false)]
    [InlineData("cafe_especialidad", Turno.Tarde, true)]
    [InlineData("cafe_especialidad", Turno.Noche, false)]
    [InlineData("parques_aire_libre", Turno.Manana, true)]
    [InlineData("parques_aire_libre", Turno.Mediodia, false)]
    [InlineData("parques_aire_libre", Turno.Tarde, true)]
    [InlineData("parques_aire_libre", Turno.Noche, false)]
    [InlineData("gastronomia_clasica", Turno.Manana, false)]
    [InlineData("gastronomia_clasica", Turno.Mediodia, true)]
    [InlineData("gastronomia_clasica", Turno.Tarde, false)]
    [InlineData("gastronomia_clasica", Turno.Noche, true)]
    [InlineData("bares_vermu", Turno.Manana, false)]
    [InlineData("bares_vermu", Turno.Mediodia, false)]
    [InlineData("bares_vermu", Turno.Tarde, true)]
    [InlineData("bares_vermu", Turno.Noche, true)]
    [InlineData("cocteleria_autor", Turno.Tarde, false)]
    [InlineData("cocteleria_autor", Turno.Noche, true)]
    [InlineData("museos_galerias", Turno.Manana, true)]
    [InlineData("museos_galerias", Turno.Tarde, true)]
    [InlineData("librerias_diseno", Turno.Manana, true)]
    [InlineData("librerias_diseno", Turno.Tarde, true)]
    public void EsCompatible_RespetaLaCategoriaDelTurno(
        string categoria,
        Turno turno,
        bool esperado)
    {
        var lugar = CrearLugar(categoria);

        var resultado = ReglasTurno.EsCompatible(turno, lugar);

        Assert.Equal(esperado, resultado);
    }

    [Fact]
    public void EsCompatible_ConVariasCategorias_BastaUnaCompatible()
    {
        var lugar = CrearLugar("museos_galerias", "bares_vermu");

        Assert.True(ReglasTurno.EsCompatible(Turno.Tarde, lugar));
        Assert.False(ReglasTurno.EsCompatible(Turno.Mediodia, lugar));
    }

    private static LugarCandidatoDto CrearLugar(params string[] categorias) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Lugar de prueba",
        Latitud = -34.58m,
        Longitud = -58.42m,
        NivelPrecio = NivelPresupuesto.Equilibrado,
        DuracionSugeridaMinutos = 60,
        Categorias = categorias
    };
}
