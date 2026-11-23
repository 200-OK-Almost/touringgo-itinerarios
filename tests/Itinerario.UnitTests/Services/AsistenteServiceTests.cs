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
    public async Task ResponderAsync_RespetaTopKConfigurado()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var lugares = new LugaresClientFalso
        {
            ResultadoSemantico = Enumerable.Range(1, 10)
                .Select(numero => TestData.CrearLugar($"Café {numero}"))
                .ToList()
        };
        var service = CrearServicio(
            viaje,
            lugares,
            new TelemetriaFalsa(),
            new AsistenteOptions
            {
                TopK = 5,
                MinResultados = 3,
                RadioMetrosDefault = 800
            });

        var respuesta = await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto { Mensaje = "café" },
            admin);

        Assert.Equal(5, respuesta.Recomendaciones.Count);
        var busqueda = Assert.Single(lugares.Busquedas);
        Assert.Equal(5, busqueda.Cantidad);
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
            new AsistenteRequestDto { Mensaje = "algo tranquilo" },
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
    public async Task ResponderAsync_ReferenciaSolicitada_UsaSusCoordenadasYRadioDefault()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var itinerario = TestData.CrearItinerario(viaje);
        var paradaActual = TestData.CrearLugar(
            "Parada actual",
            latitud: -34.60m,
            longitud: -58.38m);
        TestData.CrearParada(itinerario, paradaActual.Id);

        var referencia = TestData.CrearLugar(
            "Parada eliminada",
            latitud: -34.70m,
            longitud: -58.50m);
        var bar = TestData.CrearLugar("Bar", "bares_vermu");

        var lugares = new LugaresClientFalso
        {
            ResultadoSemantico = new[] { bar }
        };
        lugares.PorId[paradaActual.Id] = paradaActual;
        lugares.PorId[referencia.Id] = referencia;

        var service = CrearServicio(viaje, lugares, new TelemetriaFalsa());

        await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto
            {
                Mensaje = "Recomendame un bar",
                NumeroDia = 1,
                ReferenciaLugarId = referencia.Id
            },
            admin);

        var busqueda = Assert.Single(lugares.Busquedas);
        Assert.Equal(-34.70m, busqueda.LatitudReferencia);
        Assert.Equal(-58.50m, busqueda.LongitudReferencia);
        Assert.Equal(800, busqueda.RadioMetrosMaximo);
    }

    [Fact]
    public async Task ResponderAsync_ReferenciaInexistente_UsaLaPrimeraParada()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var itinerario = TestData.CrearItinerario(viaje);
        var paradaActual = TestData.CrearLugar(
            "Parada actual",
            latitud: -34.60m,
            longitud: -58.38m);
        TestData.CrearParada(itinerario, paradaActual.Id);
        var bar = TestData.CrearLugar("Bar", "bares_vermu");

        var lugares = new LugaresClientFalso
        {
            ResultadoSemantico = new[] { bar }
        };
        lugares.PorId[paradaActual.Id] = paradaActual;

        var service = CrearServicio(viaje, lugares, new TelemetriaFalsa());

        await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto
            {
                Mensaje = "Recomendame un bar",
                NumeroDia = 1,
                ReferenciaLugarId = Guid.NewGuid()
            },
            admin);

        var busqueda = Assert.Single(lugares.Busquedas);
        Assert.Equal(-34.60m, busqueda.LatitudReferencia);
        Assert.Equal(-58.38m, busqueda.LongitudReferencia);
        Assert.Equal(800, busqueda.RadioMetrosMaximo);
    }

    [Fact]
    public async Task ResponderAsync_FallbackRespetaElRadioDeLaReferencia()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var referencia = TestData.CrearLugar(
            "Parada eliminada",
            latitud: -34.58m,
            longitud: -58.42m);
        var cerca = TestData.CrearLugar(
            "Cerca",
            "bares_vermu",
            latitud: -34.581m,
            longitud: -58.421m);
        var lejos = TestData.CrearLugar(
            "Lejos",
            "bares_vermu",
            latitud: -34.70m,
            longitud: -58.50m);

        var lugares = new LugaresClientFalso
        {
            ResultadoSemantico = Array.Empty<LugarCandidatoDto>(),
            ResultadoCatalogo = new[] { cerca, lejos }
        };
        lugares.PorId[referencia.Id] = referencia;

        var service = CrearServicio(viaje, lugares, new TelemetriaFalsa());

        var respuesta = await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto
            {
                Mensaje = "Recomendame un bar",
                NumeroDia = 1,
                ReferenciaLugarId = referencia.Id
            },
            admin);

        var recomendacion = Assert.Single(respuesta.Recomendaciones);
        Assert.Equal(cerca.Id, recomendacion.LugarId);
        Assert.NotNull(respuesta.Advertencia);
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

        Assert.True(respuesta.PuedeAplicar);
        Assert.NotNull(respuesta.Aclaracion);
        Assert.Equal(2, respuesta.Aclaracion!.Opciones.Count);
        Assert.Empty(respuesta.Recomendaciones);
        Assert.Null(respuesta.Accion);
        Assert.Null(respuesta.Advertencia);
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
    public async Task ResponderAsync_AgregarConTurnoOcupado_NoProponeAccion()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var itinerario = TestData.CrearItinerario(viaje, version: "v3");
        var existente = TestData.CrearLugar("Cafe", "cafe_especialidad");
        TestData.CrearParada(itinerario, existente.Id, Turno.Tarde, 1);
        var bar = TestData.CrearLugar("Bar", "bares_vermu");

        var lugares = new LugaresClientFalso
        {
            ResultadoSemantico = new[] { bar }
        };
        lugares.PorId[existente.Id] = existente;

        var service = CrearServicio(viaje, lugares, new TelemetriaFalsa());

        var respuesta = await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto
            {
                Mensaje = "Agrega un bar para la tarde",
                NumeroDia = 1
            },
            admin);

        Assert.Null(respuesta.Accion);
        Assert.Single(respuesta.Recomendaciones);
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

    [Fact]
    public async Task ResponderAsync_ReemplazoTargetInexistente_DevuelveAclaracionSinBuscar()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var itinerario = TestData.CrearItinerario(viaje);
        var cafe = TestData.CrearLugar("Café");
        TestData.CrearParada(itinerario, cafe.Id);

        var lugares = new LugaresClientFalso();
        lugares.PorId[cafe.Id] = cafe;

        var service = CrearServicio(viaje, lugares, new TelemetriaFalsa());

        var respuesta = await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto
            {
                Mensaje = "Reemplazá la librería por un bar",
                NumeroDia = 1
            },
            admin);

        Assert.True(respuesta.PuedeAplicar);
        Assert.NotNull(respuesta.Aclaracion);
        Assert.Single(respuesta.Aclaracion!.Opciones);
        Assert.Contains("No encontr", respuesta.Mensaje);
        Assert.Contains("librer", respuesta.Mensaje);
        Assert.Empty(respuesta.Recomendaciones);
        Assert.Empty(lugares.Busquedas);
        Assert.Null(respuesta.Accion);
        Assert.Null(respuesta.Advertencia);
    }

    [Fact]
    public async Task ResponderAsync_PresupuestoDeIntencionSeLimitaAlDelViaje()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        viaje.NivelPresupuesto = NivelPresupuesto.Economico;
        var bar = TestData.CrearLugar("Bar", "bares_vermu");

        var lugares = new LugaresClientFalso
        {
            ResultadoSemantico = new[] { bar }
        };

        var service = CrearServicio(viaje, lugares, new TelemetriaFalsa());

        await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto
            {
                Mensaje = "Agregá un bar moderado",
                NumeroDia = 1
            },
            admin);

        var busqueda = Assert.Single(lugares.Busquedas);
        Assert.Equal((short?)1, busqueda.NivelPrecioMaximo);
    }

    [Fact]
    public async Task ResponderAsync_FamiliaAgregaFiltrosDeNinosYAccesibilidad()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        viaje.TipoViaje = TipoViaje.Familia;
        viaje.ParametrosFamilia = new ParametrosFamilia
        {
            HayNinos = true,
            MovilidadReducida = true
        };
        var bar = TestData.CrearLugar("Bar", "bares_vermu");

        var lugares = new LugaresClientFalso
        {
            ResultadoSemantico = new[] { bar }
        };

        var service = CrearServicio(viaje, lugares, new TelemetriaFalsa());

        await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto
            {
                Mensaje = "Agregá un bar",
                NumeroDia = 1
            },
            admin);

        var busqueda = Assert.Single(lugares.Busquedas);
        Assert.True(busqueda.AptoNinos);
        Assert.True(busqueda.AccesibleMovilidadReducida);
    }

    [Fact]
    public async Task ResponderAsync_SinVersion_NoProponeAccion()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        TestData.CrearItinerario(viaje, version: string.Empty);
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
            admin);

        Assert.True(respuesta.PuedeAplicar);
        Assert.Null(respuesta.Accion);
    }

    [Fact]
    public async Task ResponderAsync_FallbackFiltraBajoTecho()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var abierto = TestData.CrearLugar(
            "Abierto",
            esAireLibre: true,
            esBajoTecho: false);
        var cubierto = TestData.CrearLugar(
            "Cubierto",
            esAireLibre: false,
            esBajoTecho: true);

        var lugares = new LugaresClientFalso
        {
            ResultadoSemantico = Array.Empty<LugarCandidatoDto>(),
            ResultadoCatalogo = new[] { abierto, cubierto }
        };

        var service = CrearServicio(viaje, lugares, new TelemetriaFalsa());

        var respuesta = await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto
            {
                Mensaje = "Agregá un lugar bajo techo",
                NumeroDia = 1
            },
            admin);

        Assert.Single(respuesta.Recomendaciones);
        Assert.Equal(cubierto.Id, respuesta.Recomendaciones.Single().LugarId);
        Assert.NotNull(respuesta.Advertencia);
    }

    [Fact]
    public async Task ResponderAsync_IntencionDebil_UsaYValidaLaSalidaDelLlm()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var bar = TestData.CrearLugar("Bar", "bares_vermu");
        var lugares = new LugaresClientFalso
        {
            ResultadoSemantico = new[] { bar }
        };
        var intencionLlm = new IntencionLlmServiceFalso
        {
            Respuesta = new IntencionLlmCruda
            {
                Accion = "Recomendar",
                QuerySemantica = "romantico",
                Categorias = new List<string> { "bares_vermu" }
            }
        };

        var service = CrearServicio(
            viaje,
            lugares,
            new TelemetriaFalsa(),
            new AsistenteOptions
            {
                TopK = 5,
                MinResultados = 2,
                RadioMetrosDefault = 800,
                LlmEnabled = true
            },
            intencionLlm);

        await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto
            {
                Mensaje = "Quiero algo romántico",
                NumeroDia = 1
            },
            admin);

        Assert.True(intencionLlm.FueLlamado);
        var busqueda = Assert.Single(lugares.Busquedas);
        Assert.Equal("romantico", busqueda.Query);
        Assert.Contains("bares_vermu", busqueda.CategoriaCodigos!);
    }

    [Fact]
    public async Task ResponderAsync_IntencionFuerte_NoLlamaAlLlm()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var itinerario = TestData.CrearItinerario(viaje);
        var museo = TestData.CrearLugar("Museo", "museos_galerias");
        TestData.CrearParada(itinerario, museo.Id);
        var parque = TestData.CrearLugar("Parque", "parques_aire_libre");

        var lugares = new LugaresClientFalso
        {
            ResultadoSemantico = new[] { parque }
        };
        lugares.PorId[museo.Id] = museo;

        var intencionLlm = new IntencionLlmServiceFalso
        {
            Respuesta = new IntencionLlmCruda
            {
                Accion = "Recomendar",
                QuerySemantica = "romantico"
            }
        };

        var service = CrearServicio(
            viaje,
            lugares,
            new TelemetriaFalsa(),
            new AsistenteOptions
            {
                TopK = 5,
                MinResultados = 2,
                RadioMetrosDefault = 800,
                LlmEnabled = true
            },
            intencionLlm);

        await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto
            {
                Mensaje = "Reemplazá el museo por algo al aire libre",
                NumeroDia = 1
            },
            admin);

        Assert.False(intencionLlm.FueLlamado);
        var busqueda = Assert.Single(lugares.Busquedas);
        Assert.Equal("aire libre", busqueda.Query);
    }

    [Fact]
    public async Task ResponderAsync_LlmInvalido_UsaReglas()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var bar = TestData.CrearLugar("Bar", "bares_vermu");
        var lugares = new LugaresClientFalso
        {
            ResultadoSemantico = new[] { bar }
        };
        var intencionLlm = new IntencionLlmServiceFalso
        {
            Respuesta = new IntencionLlmCruda { Accion = "NoExiste" }
        };

        var service = CrearServicio(
            viaje,
            lugares,
            new TelemetriaFalsa(),
            new AsistenteOptions
            {
                TopK = 5,
                MinResultados = 2,
                RadioMetrosDefault = 800,
                LlmEnabled = true
            },
            intencionLlm);

        await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto
            {
                Mensaje = "Quiero algo romántico",
                NumeroDia = 1
            },
            admin);

        Assert.True(intencionLlm.FueLlamado);
        var busqueda = Assert.Single(lugares.Busquedas);
        Assert.Equal("romantico", busqueda.Query);
    }

    [Fact]
    public async Task ResponderAsync_MensajeSinSenal_NoBuscaYUsaLlmSinRecomendaciones()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var lugares = new LugaresClientFalso
        {
            ResultadoSemantico = new[] { TestData.CrearLugar() }
        };
        var telemetria = new TelemetriaFalsa();
        var mensajeLlm = new MensajeLlmServiceFalso
        {
            Respuesta = "No entendí tu pedido. ¿Qué tipo de lugar buscás?"
        };
        var service = CrearServicio(
            viaje,
            lugares,
            telemetria,
            new AsistenteOptions
            {
                TopK = 5,
                MinResultados = 2,
                RadioMetrosDefault = 800,
                LlmGenerarMensaje = true
            },
            mensajeLlm: mensajeLlm);

        var respuesta = await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto
            {
                Mensaje = "asdf qwerty",
                NumeroDia = 1
            },
            admin);

        Assert.Empty(lugares.Busquedas);
        Assert.True(mensajeLlm.FueLlamado);
        Assert.NotNull(mensajeLlm.Contexto);
        Assert.True(mensajeLlm.Contexto!.SinSenal);
        Assert.Empty(mensajeLlm.Contexto.Recomendaciones);
        Assert.Empty(respuesta.Recomendaciones);
        Assert.Null(respuesta.Accion);
        Assert.Null(respuesta.Advertencia);
        Assert.Equal(
            "No entendí tu pedido. ¿Qué tipo de lugar buscás?",
            respuesta.Mensaje);
        Assert.Contains("sin_senal", telemetria.Resultados);
    }

    [Fact]
    public async Task ResponderAsync_MensajeSinSenalSinLlm_DevuelveMensajeDeterminista()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var lugares = new LugaresClientFalso
        {
            ResultadoSemantico = new[] { TestData.CrearLugar() }
        };
        var service = CrearServicio(viaje, lugares, new TelemetriaFalsa());

        var respuesta = await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto
            {
                Mensaje = "hola",
                NumeroDia = 1
            },
            admin);

        Assert.Empty(lugares.Busquedas);
        Assert.Empty(respuesta.Recomendaciones);
        Assert.Null(respuesta.Accion);
        Assert.Contains("No pude interpretar", respuesta.Mensaje);
    }

    [Fact]
    public async Task ResponderAsync_SinResultados_UsaLlmSinRecomendaciones()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var lugares = new LugaresClientFalso
        {
            ResultadoSemantico = Array.Empty<LugarCandidatoDto>(),
            ResultadoCatalogo = Array.Empty<LugarCandidatoDto>()
        };
        var mensajeLlm = new MensajeLlmServiceFalso
        {
            Respuesta = "No encontré opciones. ¿Querés probar con otra categoría?"
        };
        var service = CrearServicio(
            viaje,
            lugares,
            new TelemetriaFalsa(),
            new AsistenteOptions
            {
                TopK = 5,
                MinResultados = 2,
                RadioMetrosDefault = 800,
                LlmGenerarMensaje = true
            },
            mensajeLlm: mensajeLlm);

        var respuesta = await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto
            {
                Mensaje = "café",
                NumeroDia = 1
            },
            admin);

        Assert.True(mensajeLlm.FueLlamado);
        Assert.NotNull(mensajeLlm.Contexto);
        Assert.Empty(mensajeLlm.Contexto!.Recomendaciones);
        Assert.False(mensajeLlm.Contexto.SinSenal);
        Assert.Empty(respuesta.Recomendaciones);
        Assert.Equal(
            "No encontré opciones. ¿Querés probar con otra categoría?",
            respuesta.Mensaje);
    }

    [Fact]
    public async Task ResponderAsync_Aclaracion_NoLlamaAlLlm()
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

        var intencionLlm = new IntencionLlmServiceFalso();
        var service = CrearServicio(
            viaje,
            lugares,
            new TelemetriaFalsa(),
            new AsistenteOptions
            {
                TopK = 5,
                MinResultados = 2,
                RadioMetrosDefault = 800,
                LlmEnabled = true
            },
            intencionLlm);

        var respuesta = await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto
            {
                Mensaje = "Reemplazá el café por un bar",
                NumeroDia = 1
            },
            admin);

        Assert.NotNull(respuesta.Aclaracion);
        Assert.False(intencionLlm.FueLlamado);
        Assert.Empty(lugares.Busquedas);
    }

    [Fact]
    public async Task ResponderAsync_GeneracionLlm_UsaElMensajeGenerado()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var bar = TestData.CrearLugar("Bar", "bares_vermu");
        var lugares = new LugaresClientFalso
        {
            ResultadoSemantico = new[] { bar }
        };
        var mensajeLlm = new MensajeLlmServiceFalso
        {
            Respuesta = "Te recomiendo este bar para la tarde."
        };

        var service = CrearServicio(
            viaje,
            lugares,
            new TelemetriaFalsa(),
            new AsistenteOptions
            {
                TopK = 5,
                MinResultados = 2,
                RadioMetrosDefault = 800,
                LlmGenerarMensaje = true
            },
            mensajeLlm: mensajeLlm);

        var respuesta = await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto
            {
                Mensaje = "Agregá un bar",
                NumeroDia = 1
            },
            admin);

        Assert.True(mensajeLlm.FueLlamado);
        Assert.Equal("Te recomiendo este bar para la tarde.", respuesta.Mensaje);
        Assert.NotNull(mensajeLlm.Contexto);
        Assert.Single(mensajeLlm.Contexto!.Recomendaciones);
    }

    [Fact]
    public async Task ResponderAsync_GeneracionFallida_UsaMensajeDeterminista()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var bar = TestData.CrearLugar("Bar", "bares_vermu");
        var lugares = new LugaresClientFalso
        {
            ResultadoSemantico = new[] { bar }
        };
        var mensajeLlm = new MensajeLlmServiceFalso { Respuesta = null };

        var service = CrearServicio(
            viaje,
            lugares,
            new TelemetriaFalsa(),
            new AsistenteOptions
            {
                TopK = 5,
                MinResultados = 2,
                RadioMetrosDefault = 800,
                LlmGenerarMensaje = true
            },
            mensajeLlm: mensajeLlm);

        var respuesta = await service.ResponderAsync(
            viaje.Id,
            new AsistenteRequestDto
            {
                Mensaje = "Agregá un bar",
                NumeroDia = 1
            },
            admin);

        Assert.True(mensajeLlm.FueLlamado);
        Assert.Contains("Encontré", respuesta.Mensaje);
    }

    private static AsistenteService CrearServicio(
        Viaje viaje,
        LugaresClientFalso lugares,
        TelemetriaFalsa telemetria,
        AsistenteOptions? options = null,
        IntencionLlmServiceFalso? intencionLlm = null,
        MensajeLlmServiceFalso? mensajeLlm = null)
        => new(
            new ViajeRepositoryFalso { Viaje = viaje },
            lugares,
            new GeoCalculator(),
            new IntencionService(),
            intencionLlm ?? new IntencionLlmServiceFalso(),
            mensajeLlm ?? new MensajeLlmServiceFalso(),
            telemetria,
            Options.Create(options ?? new AsistenteOptions
            {
                TopK = 5,
                MinResultados = 2,
                RadioMetrosDefault = 800
            }));
}
