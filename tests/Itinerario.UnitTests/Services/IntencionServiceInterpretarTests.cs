using Itinerario.Application.DTOs;
using Itinerario.Application.Services;
using Itinerario.Domain.Enums;

namespace Itinerario.UnitTests.Services;

public class IntencionServiceInterpretarTests
{
    private readonly IntencionService _service = new();

    [Fact]
    public void Interpretar_Reemplazo_QueryLimpiaSinObjetivo()
    {
        var museo = CrearParada("Museo de Arte", Turno.Manana, 1, "museos_galerias");

        var intencion = _service.Interpretar(
            "Reemplazá el museo por algo al aire libre",
            new[] { museo });

        Assert.Equal(TipoAccionAsistente.Reemplazar, intencion.Accion);
        Assert.Equal("aire libre", intencion.QuerySemantica);
        Assert.True(intencion.Filtros.SoloAlAireLibre);
        Assert.Equal(museo.ParadaId, intencion.ParadaObjetivo!.ParadaId);
        Assert.Null(intencion.Aclaracion);
    }

    [Fact]
    public void Interpretar_Agregar_QuerySinBarrioNiPrecio()
    {
        var intencion = _service.Interpretar(
            "Agregá un café económico en Palermo",
            Array.Empty<ParadaResolubleDto>());

        Assert.Equal(TipoAccionAsistente.Agregar, intencion.Accion);
        Assert.Equal("cafe", intencion.QuerySemantica);
        Assert.Equal((short?)1, intencion.Filtros.NivelPrecioMaximo);
        Assert.Contains("cafe_especialidad", intencion.Filtros.CategoriaCodigos);
        Assert.DoesNotContain("palermo", intencion.QuerySemantica);
        Assert.DoesNotContain("economico", intencion.QuerySemantica);
    }

    [Fact]
    public void Interpretar_AireLibreYAptoNinos()
    {
        var intencion = _service.Interpretar(
            "Agregá un lugar al aire libre para niños",
            Array.Empty<ParadaResolubleDto>());

        Assert.Equal("aire libre", intencion.QuerySemantica);
        Assert.True(intencion.Filtros.SoloAlAireLibre);
        Assert.Null(intencion.Filtros.SoloBajoTecho);
        Assert.True(intencion.Filtros.AptoNinos);
    }

    [Fact]
    public void Interpretar_BajoTechoYPasoTurno()
    {
        var intencion = _service.Interpretar(
            "Agregá un lugar bajo techo para la tarde",
            Array.Empty<ParadaResolubleDto>());

        Assert.True(intencion.Filtros.SoloBajoTecho);
        Assert.Null(intencion.Filtros.SoloAlAireLibre);
        Assert.Equal(Turno.Tarde, intencion.Filtros.Turno);
    }

    [Fact]
    public void Interpretar_DietaYAccesibilidad()
    {
        var intencion = _service.Interpretar(
            "Agregá un restaurante vegano accesible para silla de ruedas",
            Array.Empty<ParadaResolubleDto>());

        Assert.Equal("vegano", intencion.Filtros.RestriccionDietaria);
        Assert.True(intencion.Filtros.AccesibleMovilidadReducida);
        Assert.Contains("gastronomia_clasica", intencion.Filtros.CategoriaCodigos);
        Assert.Equal("restaurante", intencion.QuerySemantica);
    }

    [Fact]
    public void Interpretar_DetectaVariasCategorias()
    {
        var intencion = _service.Interpretar(
            "Recomendame un museo y una librería",
            Array.Empty<ParadaResolubleDto>());

        Assert.Contains("museos_galerias", intencion.Filtros.CategoriaCodigos);
        Assert.Contains("librerias_diseno", intencion.Filtros.CategoriaCodigos);
    }

    [Fact]
    public void Interpretar_Cerca_UsaRadioConfigurado()
    {
        var conRadio = _service.Interpretar(
            "Agregá un bar cerca",
            Array.Empty<ParadaResolubleDto>(),
            radioMetrosDefault: 1200);

        var sinRadio = _service.Interpretar(
            "Agregá un bar",
            Array.Empty<ParadaResolubleDto>(),
            radioMetrosDefault: 1200);

        Assert.Equal(1200, conRadio.RadioMetros);
        Assert.Null(sinRadio.RadioMetros);
        Assert.Equal("bar", conRadio.QuerySemantica);
    }

    [Fact]
    public void Interpretar_ReemplazoSinNuevoLugar_QueryFallback()
    {
        var museo = CrearParada("Museo de Arte", Turno.Manana, 1, "museos_galerias");

        var intencion = _service.Interpretar("Cambiá el museo", new[] { museo });

        Assert.Equal(TipoAccionAsistente.Reemplazar, intencion.Accion);
        Assert.Equal("lugar", intencion.QuerySemantica);
        Assert.Equal(museo.ParadaId, intencion.ParadaObjetivo!.ParadaId);
    }

    [Fact]
    public void Interpretar_Ambiguedad_MantieneAclaracion()
    {
        var primero = CrearParada("Café Uno", Turno.Manana, 1, "cafe_especialidad");
        var segundo = CrearParada("Café Dos", Turno.Tarde, 2, "cafe_especialidad");

        var intencion = _service.Interpretar(
            "Reemplazá el café por un bar",
            new[] { primero, segundo });

        Assert.Null(intencion.ParadaObjetivo);
        Assert.NotNull(intencion.Aclaracion);
        Assert.Equal("bar", intencion.QuerySemantica);
    }

    [Fact]
    public void Interpretar_EnVezDe_TomaElLugarNuevo()
    {
        var manana = CrearParada("Café", Turno.Manana, 1, "cafe_especialidad");
        var noche = CrearParada("Cena", Turno.Noche, 1, "gastronomia_clasica");

        var intencion = _service.Interpretar(
            "En vez de la última parada, poné un bar",
            new[] { manana, noche });

        Assert.Equal(TipoAccionAsistente.Reemplazar, intencion.Accion);
        Assert.Equal(noche.ParadaId, intencion.ParadaObjetivo!.ParadaId);
        Assert.Equal("bar", intencion.QuerySemantica);
    }

    [Fact]
    public void Interpretar_Recomendar_NoResuelveParada()
    {
        var museo = CrearParada("Museo de Arte", Turno.Manana, 1, "museos_galerias");

        var intencion = _service.Interpretar(
            "¿Qué lugar me recomendás para comer?",
            new[] { museo });

        Assert.Equal(TipoAccionAsistente.Recomendar, intencion.Accion);
        Assert.Null(intencion.ParadaObjetivo);
        Assert.Null(intencion.Aclaracion);
        Assert.Equal("comer", intencion.QuerySemantica);
    }

    [Fact]
    public void Interpretar_Reemplazo_QuerySinBarrioNiObjetivo()
    {
        var museo = CrearParada("Museo de Arte", Turno.Manana, 1, "museos_galerias");

        var intencion = _service.Interpretar(
            "Reemplazá el museo por un café en Palermo",
            new[] { museo });

        Assert.Equal(TipoAccionAsistente.Reemplazar, intencion.Accion);
        Assert.Equal("cafe", intencion.QuerySemantica);
        Assert.DoesNotContain("museo", intencion.QuerySemantica);
        Assert.DoesNotContain("palermo", intencion.QuerySemantica);
    }

    [Fact]
    public void Interpretar_PrecioModerado()
    {
        var intencion = _service.Interpretar(
            "Agregá un café moderado",
            Array.Empty<ParadaResolubleDto>());

        Assert.Equal((short?)2, intencion.Filtros.NivelPrecioMaximo);
    }

    [Fact]
    public void Interpretar_DietaSinGluten()
    {
        var intencion = _service.Interpretar(
            "Agregá un restaurante sin gluten",
            Array.Empty<ParadaResolubleDto>());

        Assert.Equal("sin gluten", intencion.Filtros.RestriccionDietaria);
        Assert.Contains("gastronomia_clasica", intencion.Filtros.CategoriaCodigos);
    }

    [Fact]
    public void Interpretar_AgregarConParadas_NoResuelveObjetivo()
    {
        var cafe = CrearParada("Café", Turno.Manana, 1, "cafe_especialidad");

        var intencion = _service.Interpretar("Agregá un café", new[] { cafe });

        Assert.Equal(TipoAccionAsistente.Agregar, intencion.Accion);
        Assert.Null(intencion.ParadaObjetivo);
        Assert.Null(intencion.Aclaracion);
        Assert.Equal("cafe", intencion.QuerySemantica);
    }

    [Fact]
    public void Interpretar_TargetInexistente_DevuelveAclaracion()
    {
        var cafe = CrearParada("Café", Turno.Manana, 1, "cafe_especialidad");

        var intencion = _service.Interpretar(
            "Reemplazá la librería por un bar",
            new[] { cafe });

        Assert.Null(intencion.ParadaObjetivo);
        Assert.NotNull(intencion.Aclaracion);
        Assert.Single(intencion.Aclaracion!.Opciones);
        Assert.Equal("bar", intencion.QuerySemantica);
    }

    [Fact]
    public void Interpretar_TurnoUsaLaPrimeraMencion()
    {
        var intencion = _service.Interpretar(
            "Agregá un bar para la noche o la tarde",
            Array.Empty<ParadaResolubleDto>());

        Assert.Equal(Turno.Noche, intencion.Filtros.Turno);
    }

    [Fact]
    public void Interpretar_MensajeVacio_DevuelveValoresSeguros()
    {
        var intencion = _service.Interpretar("   ");

        Assert.Equal(TipoAccionAsistente.Recomendar, intencion.Accion);
        Assert.Equal("lugar", intencion.QuerySemantica);
        Assert.Empty(intencion.Filtros.CategoriaCodigos);
        Assert.Null(intencion.RadioMetros);
        Assert.Null(intencion.ParadaObjetivo);
        Assert.Null(intencion.Aclaracion);
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
