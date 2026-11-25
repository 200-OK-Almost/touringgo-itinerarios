using Itinerario.Application.DTOs;
using Itinerario.Application.Services;
using Itinerario.Domain.Enums;

namespace Itinerario.UnitTests.Services;

public class IntencionServiceParadaObjetivoTests
{
    private readonly IntencionService _service = new();

    [Fact]
    public void ResolverPorNombre_IgnoraElLugarNuevoDespuesDePor()
    {
        var museo = CrearParada("Museo de Arte", Turno.Manana, 1, "museos_galerias");
        var cafe = CrearParada("Café Especialidad", Turno.Tarde, 2, "cafe_especialidad");

        var resultado = _service.ResolverParadaObjetivo(
            "Reemplazá el museo por un café",
            new[] { museo, cafe });

        Assert.Null(resultado.Aclaracion);
        Assert.Equal(museo.ParadaId, resultado.Parada!.ParadaId);
    }

    [Fact]
    public void ResolverPorCategoria_CuandoElNombreNoCoincide()
    {
        var museo = CrearParada("Bellas Artes", Turno.Manana, 1, "museos_galerias");
        var bar = CrearParada("Bar de la esquina", Turno.Tarde, 2, "bares_vermu");

        var resultado = _service.ResolverParadaObjetivo(
            "Cambiá los museos por un bar",
            new[] { museo, bar });

        Assert.Null(resultado.Aclaracion);
        Assert.Equal(museo.ParadaId, resultado.Parada!.ParadaId);
    }

    [Fact]
    public void ResolverPorOrden_UsaElOrdenDelDia()
    {
        var tarde = CrearParada("Bar", Turno.Tarde, 1, "bares_vermu");
        var manana = CrearParada("Café", Turno.Manana, 1, "cafe_especialidad");
        var noche = CrearParada("Cena", Turno.Noche, 1, "gastronomia_clasica");

        var resultado = _service.ResolverParadaObjetivo(
            "Reemplazá la primera parada por un bar",
            new[] { tarde, manana, noche });

        Assert.Null(resultado.Aclaracion);
        Assert.Equal(manana.ParadaId, resultado.Parada!.ParadaId);
    }

    [Fact]
    public void ResolverPorUltimaParada()
    {
        var manana = CrearParada("Café", Turno.Manana, 1, "cafe_especialidad");
        var noche = CrearParada("Cena", Turno.Noche, 1, "gastronomia_clasica");

        var resultado = _service.ResolverParadaObjetivo(
            "Cambiá la última parada por un bar",
            new[] { noche, manana });

        Assert.Null(resultado.Aclaracion);
        Assert.Equal(noche.ParadaId, resultado.Parada!.ParadaId);
    }

    [Fact]
    public void ResolverPorSegundaParada()
    {
        var manana = CrearParada("Café", Turno.Manana, 1, "cafe_especialidad");
        var tarde = CrearParada("Bar", Turno.Tarde, 1, "bares_vermu");
        var noche = CrearParada("Cena", Turno.Noche, 1, "gastronomia_clasica");

        var resultado = _service.ResolverParadaObjetivo(
            "Sustituí la segunda parada por un parque",
            new[] { manana, tarde, noche });

        Assert.Null(resultado.Aclaracion);
        Assert.Equal(tarde.ParadaId, resultado.Parada!.ParadaId);
    }

    [Fact]
    public void Ambiguedad_DevuelveAclaracionConOpciones()
    {
        var primero = CrearParada("Café Uno", Turno.Manana, 1, "cafe_especialidad");
        var segundo = CrearParada("Café Dos", Turno.Tarde, 2, "cafe_especialidad");

        var resultado = _service.ResolverParadaObjetivo(
            "Reemplazá el café por un bar",
            new[] { primero, segundo });

        Assert.Null(resultado.Parada);
        Assert.NotNull(resultado.Aclaracion);
        Assert.Contains("parada te refer", resultado.Aclaracion!.Pregunta);
        Assert.Equal(2, resultado.Aclaracion.Opciones.Count);
        Assert.Contains(
            resultado.Aclaracion.Opciones,
            opcion => opcion.Id == primero.ParadaId.ToString()
                && opcion.Etiqueta.Contains("Mañana"));
        Assert.Contains(
            resultado.Aclaracion.Opciones,
            opcion => opcion.Id == segundo.ParadaId.ToString()
                && opcion.Etiqueta.Contains("Tarde"));
    }

    [Fact]
    public void SinCoincidencia_DevuelveTodasLasParadasComoOpciones()
    {
        var cafe = CrearParada("Café", Turno.Manana, 1, "cafe_especialidad");
        var museo = CrearParada("Museo", Turno.Tarde, 2, "museos_galerias");

        var resultado = _service.ResolverParadaObjetivo(
            "Reemplazá la librería por un bar",
            new[] { cafe, museo });

        Assert.Null(resultado.Parada);
        Assert.NotNull(resultado.Aclaracion);
        Assert.Contains("No encontr", resultado.Aclaracion!.Pregunta);
        Assert.Contains("librer", resultado.Aclaracion.Pregunta);
        Assert.Equal(2, resultado.Aclaracion.Opciones.Count);
    }

    [Fact]
    public void SinParadas_NoDevuelveParadaNiAclaracion()
    {
        var resultado = _service.ResolverParadaObjetivo(
            "Reemplazá el museo por un café",
            Array.Empty<ParadaResolubleDto>());

        Assert.Null(resultado.Parada);
        Assert.Null(resultado.Aclaracion);
    }

    [Fact]
    public void UnaSolaParadaSinReferencia_LaResuelve()
    {
        var cafe = CrearParada("Café", Turno.Manana, 1, "cafe_especialidad");

        var resultado = _service.ResolverParadaObjetivo(
            "Reemplazá por un bar",
            new[] { cafe });

        Assert.Null(resultado.Aclaracion);
        Assert.Equal(cafe.ParadaId, resultado.Parada!.ParadaId);
    }

    [Fact]
    public void ResolverConEnLugarDe()
    {
        var manana = CrearParada("Café", Turno.Manana, 1, "cafe_especialidad");
        var noche = CrearParada("Cena", Turno.Noche, 1, "gastronomia_clasica");

        var resultado = _service.ResolverParadaObjetivo(
            "En lugar de la primera parada, poné un bar",
            new[] { manana, noche });

        Assert.Null(resultado.Aclaracion);
        Assert.Equal(manana.ParadaId, resultado.Parada!.ParadaId);
    }

    [Fact]
    public void ResolverConEnVezDe()
    {
        var manana = CrearParada("Café", Turno.Manana, 1, "cafe_especialidad");
        var noche = CrearParada("Cena", Turno.Noche, 1, "gastronomia_clasica");

        var resultado = _service.ResolverParadaObjetivo(
            "En vez de la última parada, poné un bar",
            new[] { manana, noche });

        Assert.Null(resultado.Aclaracion);
        Assert.Equal(noche.ParadaId, resultado.Parada!.ParadaId);
    }

    [Fact]
    public void OrdenFueraDeRango_DevuelveAclaracion()
    {
        var manana = CrearParada("Café", Turno.Manana, 1, "cafe_especialidad");
        var noche = CrearParada("Cena", Turno.Noche, 1, "gastronomia_clasica");

        var resultado = _service.ResolverParadaObjetivo(
            "Reemplazá la quinta parada por un bar",
            new[] { manana, noche });

        Assert.Null(resultado.Parada);
        Assert.NotNull(resultado.Aclaracion);
        Assert.Equal(2, resultado.Aclaracion!.Opciones.Count);
    }

    private static ParadaResolubleDto CrearParada(
        string nombre,
        Turno turno,
        short orden,
        params string[] categorias) => new()
    {
        ParadaId = Guid.NewGuid(),
        LugarId = Guid.NewGuid(),
        Nombre = nombre,
        Turno = turno,
        OrdenParada = orden,
        Categorias = categorias
    };
}
