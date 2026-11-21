using Itinerario.Application.Configuration;
using Itinerario.Application.DTOs;
using Itinerario.Application.Services;
using Itinerario.Domain.Entities;
using Itinerario.Domain.Enums;
using Itinerario.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace Itinerario.UnitTests.Services;

public class AsistenteServiceTests
{
    [Fact]
    public async Task ResponderAsync_NoMiembro_LanzaNotFound()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var service = CrearServicio(viaje, new LugaresClientFalso(), new TelemetriaFalsa());

        await Assert.ThrowsAsync<NotFoundException>(() => service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto { Mensaje = "¿Qué me recomendás?" },
            Guid.NewGuid()));
    }

    [Fact]
    public async Task ResponderAsync_Admin_DevuelveRecomendacionesYPuedeAplicar()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var lugares = new LugaresClientFalso
        {
            ResultadoSemantico = new[]
            {
                TestData.CrearLugar("Café"),
                TestData.CrearLugar("Museo", "museos_galerias")
            }
        };
        var telemetria = new TelemetriaFalsa();
        var service = CrearServicio(viaje, lugares, telemetria);

        var respuesta = await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto { Mensaje = "café y museos" },
            admin);

        Assert.True(respuesta.PuedeAplicar);
        Assert.Equal(2, respuesta.Recomendaciones.Count);
        Assert.All(respuesta.Recomendaciones, r => Assert.False(string.IsNullOrWhiteSpace(r.Motivo)));
        Assert.Contains("retrieval", telemetria.Etapas);
        Assert.Contains("total", telemetria.Etapas);
        Assert.Contains("ok", telemetria.Resultados);
    }

    [Fact]
    public async Task ResponderAsync_MiembroPuedeConsultarPeroNoAplicar()
    {
        var admin = Guid.NewGuid();
        var miembro = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin, (miembro, RolMiembro.Miembro));
        var lugares = new LugaresClientFalso
        {
            ResultadoSemantico = new[] { TestData.CrearLugar() }
        };
        var service = CrearServicio(viaje, lugares, new TelemetriaFalsa());

        var respuesta = await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto { Mensaje = "café" },
            miembro);

        Assert.False(respuesta.PuedeAplicar);
        Assert.Single(respuesta.Recomendaciones);
    }

    [Fact]
    public async Task ResponderAsync_PocosResultados_UsaFallback()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var lugares = new LugaresClientFalso
        {
            ResultadoSemantico = Array.Empty<LugarCandidatoDto>(),
            ResultadoCatalogo = new[]
            {
                TestData.CrearLugar("Café"),
                TestData.CrearLugar("Museo", "museos_galerias")
            }
        };
        var telemetria = new TelemetriaFalsa();
        var service = CrearServicio(viaje, lugares, telemetria);

        var respuesta = await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto { Mensaje = "algo para hacer" },
            admin);

        Assert.Equal(2, respuesta.Recomendaciones.Count);
        Assert.Contains("catálogo filtrado", respuesta.Advertencia);
        Assert.Contains("fallback", telemetria.Resultados);
    }

    [Fact]
    public async Task ResponderAsync_SemanticoCaido_UsaFallback()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var lugares = new LugaresClientFalso
        {
            FallarSemantico = true,
            ResultadoCatalogo = new[] { TestData.CrearLugar() }
        };
        var service = CrearServicio(viaje, lugares, new TelemetriaFalsa());

        var respuesta = await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto { Mensaje = "café" },
            admin);

        Assert.Single(respuesta.Recomendaciones);
        Assert.Contains("No se pudo usar la búsqueda semántica", respuesta.Advertencia);
    }

    [Fact]
    public async Task ResponderAsync_ExcluyeParadasActualesYPuntoDePartida()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        viaje.LugarPartidaId = Guid.NewGuid();

        var itinerario = TestData.CrearItinerario(viaje);
        var paradaActual = TestData.CrearLugar("Parada actual");
        TestData.CrearParada(itinerario, paradaActual.Id);

        var lugares = new LugaresClientFalso
        {
            ResultadoSemantico = new[] { TestData.CrearLugar("Nuevo") }
        };
        lugares.PorId[paradaActual.Id] = paradaActual;

        var service = CrearServicio(viaje, lugares, new TelemetriaFalsa());

        await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto { Mensaje = "café" },
            admin);

        var busqueda = Assert.Single(lugares.Busquedas);
        Assert.NotNull(busqueda.ExcluirIds);
        Assert.Contains(paradaActual.Id, busqueda.ExcluirIds!);
        Assert.Contains(viaje.LugarPartidaId!.Value, busqueda.ExcluirIds!);
    }

    [Fact]
    public async Task ResponderAsync_MensajeCerca_DefineRadioYReferencia()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var itinerario = TestData.CrearItinerario(viaje);
        var paradaActual = TestData.CrearLugar("Parada actual", latitud: -34.60m, longitud: -58.38m);
        TestData.CrearParada(itinerario, paradaActual.Id);

        var lugares = new LugaresClientFalso
        {
            ResultadoSemantico = new[] { TestData.CrearLugar("Nuevo") }
        };
        lugares.PorId[paradaActual.Id] = paradaActual;

        var service = CrearServicio(viaje, lugares, new TelemetriaFalsa());

        await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto { Mensaje = "algo cerca de la primera parada" },
            admin);

        var busqueda = Assert.Single(lugares.Busquedas);
        Assert.Equal(-34.60m, busqueda.LatitudReferencia);
        Assert.Equal(-58.38m, busqueda.LongitudReferencia);
        Assert.Equal(800, busqueda.RadioMetrosMaximo);
    }

    [Fact]
    public async Task ResponderAsync_MensajeVacio_LanzaReglaDeNegocio()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var service = CrearServicio(viaje, new LugaresClientFalso(), new TelemetriaFalsa());

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto { Mensaje = "   " },
            admin));
    }

    [Fact]
    public async Task ResponderAsync_DiaInexistente_LanzaReglaDeNegocio()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var service = CrearServicio(viaje, new LugaresClientFalso(), new TelemetriaFalsa());

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto { Mensaje = "café", NumeroDia = 2 },
            admin));
    }

    [Fact]
    public async Task ResponderAsync_ReemplazoAmbiguo_DevuelveAclaracionSinBuscar()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var itinerario = TestData.CrearItinerario(viaje);

        var cafeUno = TestData.CrearLugar("Café Uno");
        var cafeDos = TestData.CrearLugar("Café Dos");
        TestData.CrearParada(itinerario, cafeUno.Id, Turno.Manana, 1);
        TestData.CrearParada(itinerario, cafeDos.Id, Turno.Tarde, 2);

        var lugares = new LugaresClientFalso();
        lugares.PorId[cafeUno.Id] = cafeUno;
        lugares.PorId[cafeDos.Id] = cafeDos;

        var telemetria = new TelemetriaFalsa();
        var service = CrearServicio(viaje, lugares, telemetria);

        var respuesta = await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto
            {
                Mensaje = "Reemplazá el café por un bar",
                NumeroDia = 1
            },
            admin);

        Assert.NotNull(respuesta.Aclaracion);
        Assert.Equal(2, respuesta.Aclaracion!.Opciones.Count);
        Assert.Empty(respuesta.Recomendaciones);
        Assert.Null(respuesta.Accion);
        Assert.Empty(lugares.Busquedas);
        Assert.Contains("aclaracion", telemetria.Resultados);
    }

    [Fact]
    public async Task ResponderAsync_Reemplazo_UsaIntencionYProponeAccion()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var itinerario = TestData.CrearItinerario(viaje, version: "v7");
        var museo = TestData.CrearLugar("Museo de Arte", "museos_galerias");
        var parada = TestData.CrearParada(itinerario, museo.Id);
        var nuevo = TestData.CrearLugar("Parque", "parques_aire_libre");

        var lugares = new LugaresClientFalso
        {
            ResultadoSemantico = new[] { nuevo }
        };
        lugares.PorId[museo.Id] = museo;

        var service = CrearServicio(viaje, lugares, new TelemetriaFalsa());

        var respuesta = await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto
            {
                Mensaje = "Reemplazá el museo por algo al aire libre",
                NumeroDia = 1
            },
            admin);

        var busqueda = Assert.Single(lugares.Busquedas);
        Assert.Equal("aire libre", busqueda.Query);
        Assert.True(busqueda.SoloAlAireLibre);
        Assert.Contains(museo.Id, busqueda.ExcluirIds!);

        Assert.NotNull(respuesta.Accion);
        Assert.Equal("ReemplazarParada", respuesta.Accion!.Tipo);
        Assert.Equal(nuevo.Id, respuesta.Accion.LugarId);
        Assert.Equal(parada.Id, respuesta.Accion.ParadaId);
        Assert.Equal("v7", respuesta.Accion.Version);
    }

    [Fact]
    public async Task ResponderAsync_Agregar_ProponeAccionConTurno()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var itinerario = TestData.CrearItinerario(viaje, version: "v3");
        var bar = TestData.CrearLugar("Bar", "bares_vermu");

        var lugares = new LugaresClientFalso
        {
            ResultadoSemantico = new[] { bar }
        };

        var service = CrearServicio(viaje, lugares, new TelemetriaFalsa());

        var respuesta = await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto
            {
                Mensaje = "Agregá un bar para la tarde",
                NumeroDia = 1
            },
            admin);

        var busqueda = Assert.Single(lugares.Busquedas);
        Assert.Equal("bar", busqueda.Query);
        Assert.Contains("bares_vermu", busqueda.CategoriaCodigos!);

        Assert.NotNull(respuesta.Accion);
        Assert.Equal("AgregarParada", respuesta.Accion!.Tipo);
        Assert.Null(respuesta.Accion.ParadaId);
        Assert.Equal("Tarde", respuesta.Accion.Turno);
        Assert.Equal("v3", respuesta.Accion.Version);
        Assert.Equal(itinerario.NumeroDia, respuesta.Accion.NumeroDia);
    }

    [Fact]
    public async Task ResponderAsync_MiembroNoRecibeAccion()
    {
        var admin = Guid.NewGuid();
        var miembro = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin, (miembro, RolMiembro.Miembro));
        TestData.CrearItinerario(viaje, version: "v1");
        var bar = TestData.CrearLugar("Bar", "bares_vermu");

        var lugares = new LugaresClientFalso
        {
            ResultadoSemantico = new[] { bar }
        };

        var service = CrearServicio(viaje, lugares, new TelemetriaFalsa());

        var respuesta = await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto
            {
                Mensaje = "Agregá un bar",
                NumeroDia = 1
            },
            miembro);

        Assert.False(respuesta.PuedeAplicar);
        Assert.Null(respuesta.Accion);
        Assert.Single(respuesta.Recomendaciones);
    }

    [Fact]
    public async Task ResponderAsync_CategoriasDeIntencionReemplazanLasDelViaje()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        viaje.Etiquetas.Add(new EtiquetaViaje { CategoriaCodigo = "cafe_especialidad" });
        var museo = TestData.CrearLugar("Museo", "museos_galerias");

        var lugares = new LugaresClientFalso
        {
            ResultadoSemantico = new[] { museo }
        };

        var service = CrearServicio(viaje, lugares, new TelemetriaFalsa());

        await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto
            {
                Mensaje = "Recomendame un museo",
                NumeroDia = 1
            },
            admin);

        var busqueda = Assert.Single(lugares.Busquedas);
        Assert.Contains("museos_galerias", busqueda.CategoriaCodigos!);
        Assert.DoesNotContain("cafe_especialidad", busqueda.CategoriaCodigos!);
    }

    [Fact]
    public async Task ResponderAsync_Cerca_UsaLaParadaObjetivoComoReferencia()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var itinerario = TestData.CrearItinerario(viaje);
        var museo = TestData.CrearLugar(
            "Museo",
            "museos_galerias",
            latitud: -34.60m,
            longitud: -58.38m);
        TestData.CrearParada(itinerario, museo.Id);
        var bar = TestData.CrearLugar("Bar", "bares_vermu");

        var lugares = new LugaresClientFalso
        {
            ResultadoSemantico = new[] { bar }
        };
        lugares.PorId[museo.Id] = museo;

        var service = CrearServicio(viaje, lugares, new TelemetriaFalsa());

        await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto
            {
                Mensaje = "Reemplazá el museo por un bar cerca",
                NumeroDia = 1
            },
            admin);

        var busqueda = Assert.Single(lugares.Busquedas);
        Assert.Equal(-34.60m, busqueda.LatitudReferencia);
        Assert.Equal(-58.38m, busqueda.LongitudReferencia);
        Assert.Equal(800, busqueda.RadioMetrosMaximo);
    }

    [Fact]
    public async Task ResponderAsync_FallbackFiltraRestriccionDietaria()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var vegano = TestData.CrearLugar(
            "Café Vegano",
            opcionesDietarias: new[] { "vegano" });
        var carne = TestData.CrearLugar("Café Carnívoro");

        var lugares = new LugaresClientFalso
        {
            ResultadoSemantico = Array.Empty<LugarCandidatoDto>(),
            ResultadoCatalogo = new[] { vegano, carne }
        };

        var service = CrearServicio(viaje, lugares, new TelemetriaFalsa());

        var respuesta = await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto
            {
                Mensaje = "Agregá un café vegano",
                NumeroDia = 1
            },
            admin);

        Assert.Single(respuesta.Recomendaciones);
        Assert.Equal(vegano.Id, respuesta.Recomendaciones.Single().LugarId);
        Assert.NotNull(respuesta.Advertencia);
    }

    private static AsistenteService CrearServicio(
        Viaje viaje,
        LugaresClientFalso lugares,
        TelemetriaFalsa telemetria)
        => new(
            new ViajeRepositoryFalso { Viaje = viaje },
            lugares,
            new IntencionService(),
            telemetria,
            Options.Create(new AsistenteOptions
            {
                TopK = 5,
                MinResultados = 2,
                RadioMetrosDefault = 800
            }));
}
