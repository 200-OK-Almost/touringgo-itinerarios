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

    private static AsistenteService CrearServicio(
        Viaje viaje,
        LugaresClientFalso lugares,
        TelemetriaFalsa telemetria)
        => new(
            new ViajeRepositoryFalso { Viaje = viaje },
            lugares,
            telemetria,
            Options.Create(new AsistenteOptions
            {
                TopK = 5,
                MinResultados = 2,
                RadioMetrosDefault = 800
            }));
}
