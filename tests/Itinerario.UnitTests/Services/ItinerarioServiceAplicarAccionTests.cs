using Itinerario.Application.DTOs;
using Itinerario.Application.Services;
using Itinerario.Domain.Entities;
using Itinerario.Domain.Enums;
using Itinerario.Domain.Exceptions;

namespace Itinerario.UnitTests.Services;

public class ItinerarioServiceAplicarAccionTests
{
    [Fact]
    public async Task AplicarAccion_NoAdministrador_LanzaAccesoDenegado()
    {
        var admin = Guid.NewGuid();
        var miembro = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin, (miembro, RolMiembro.Miembro));
        var service = CrearService(viaje);

        await Assert.ThrowsAsync<AccesoDenegadoException>(() => service.AplicarAccionAsistenteAsync(
            viaje.Id,
            CrearAccion("AgregarParada", "v1"),
            miembro));
    }

    [Fact]
    public async Task AplicarAccion_VersionIncorrecta_LanzaConflicto()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var itinerario = TestData.CrearItinerario(viaje, version: "v1");
        var service = CrearService(viaje);

        await Assert.ThrowsAsync<ConflictoDeConcurrenciaException>(() =>
            service.AplicarAccionAsistenteAsync(
                viaje.Id,
                CrearAccion("AgregarParada", "v2"),
                admin));
    }

    [Fact]
    public async Task AplicarAccion_AgregarParada_AgregaCalculaYModifica()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var itinerario = TestData.CrearItinerario(viaje, version: "v1");
        itinerario.OrigenLatitud = -34.60m;
        itinerario.OrigenLongitud = -58.38m;

        var lugar = TestData.CrearLugar();
        var lugares = new LugaresClientFalso();
        lugares.PorId[lugar.Id] = lugar;

        var repo = new ItinerarioRepositoryFalso { Itinerarios = viaje.Itinerarios.ToList() };
        var service = CrearService(viaje, lugares, repo);

        var resultado = await service.AplicarAccionAsistenteAsync(
            viaje.Id,
            CrearAccion("AgregarParada", "v1", lugarId: lugar.Id),
            admin);

        var parada = Assert.Single(itinerario.Paradas);
        Assert.Equal(lugar.Id, parada.LugarId);
        Assert.Equal(Turno.Manana, parada.Turno);
        Assert.Equal(1, parada.OrdenParada);
        Assert.Equal(new TimeOnly(9, 0), parada.HoraInicio);
        Assert.Equal(250, parada.DistanciaMetrosDesdeAnterior);
        Assert.Equal((short?)3, parada.CuadrasDesdeAnterior);
        Assert.Equal((short?)3, parada.MinutosAPieDesdeAnterior);
        Assert.Contains(itinerario, repo.Modificados);
        Assert.Equal(1, repo.Guardados);
        Assert.Single(repo.ParadasAgregadas);
        Assert.Single(resultado);
    }

    [Fact]
    public async Task AplicarAccion_ReemplazarParada_ActualizaLaParada()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var itinerario = TestData.CrearItinerario(viaje, version: "v1");
        itinerario.OrigenLatitud = -34.60m;
        itinerario.OrigenLongitud = -58.38m;

        var anterior = TestData.CrearLugar("Anterior");
        var parada = TestData.CrearParada(itinerario, anterior.Id);
        var nuevo = TestData.CrearLugar("Nuevo", duracionMinutos: 90);

        var lugares = new LugaresClientFalso();
        lugares.PorId[nuevo.Id] = nuevo;

        var repo = new ItinerarioRepositoryFalso { Itinerarios = viaje.Itinerarios.ToList() };
        var service = CrearService(viaje, lugares, repo);

        await service.AplicarAccionAsistenteAsync(
            viaje.Id,
            CrearAccion("ReemplazarParada", "v1", lugarId: nuevo.Id, paradaId: parada.Id),
            admin);

        Assert.Equal(nuevo.Id, parada.LugarId);
        Assert.Equal((short?)90, parada.DuracionMinutos);
        Assert.Equal(OrigenParada.Ia, parada.OrigenParada);
        Assert.Contains(itinerario, repo.Modificados);
        Assert.Equal(1, repo.Guardados);
    }

    [Fact]
    public async Task AplicarAccion_LugarDuplicado_LanzaConflicto()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var itinerario = TestData.CrearItinerario(viaje, version: "v1");
        var existente = TestData.CrearLugar("Existente");
        TestData.CrearParada(itinerario, existente.Id);

        var lugares = new LugaresClientFalso();
        lugares.PorId[existente.Id] = existente;
        var service = CrearService(viaje, lugares);

        await Assert.ThrowsAsync<ConflictoDeConcurrenciaException>(() =>
            service.AplicarAccionAsistenteAsync(
                viaje.Id,
                CrearAccion("AgregarParada", "v1", lugarId: existente.Id),
                admin));
    }

    [Fact]
    public async Task AplicarAccion_ParadaInexistente_LanzaNotFound()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var itinerario = TestData.CrearItinerario(viaje, version: "v1");
        var lugar = TestData.CrearLugar();
        var lugares = new LugaresClientFalso();
        lugares.PorId[lugar.Id] = lugar;
        var service = CrearService(viaje, lugares);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.AplicarAccionAsistenteAsync(
                viaje.Id,
                CrearAccion(
                    "ReemplazarParada",
                    "v1",
                    lugarId: lugar.Id,
                    paradaId: Guid.NewGuid()),
                admin));
    }

    [Fact]
    public async Task AplicarAccion_VersionViejaDespuesDeGuardar_LanzaConflicto()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var itinerario = TestData.CrearItinerario(viaje, version: "v1");
        var lugar = TestData.CrearLugar();
        var lugares = new LugaresClientFalso();
        lugares.PorId[lugar.Id] = lugar;

        var repo = new ItinerarioRepositoryFalso
        {
            Itinerarios = viaje.Itinerarios.ToList(),
            ActualizarVersionAlGuardar = true
        };
        var service = CrearService(viaje, lugares, repo);

        await service.AplicarAccionAsistenteAsync(
            viaje.Id,
            CrearAccion("AgregarParada", "v1", lugarId: lugar.Id),
            admin);

        Assert.NotEqual("v1", itinerario.Version);

        await Assert.ThrowsAsync<ConflictoDeConcurrenciaException>(() =>
            service.AplicarAccionAsistenteAsync(
                viaje.Id,
                CrearAccion("AgregarParada", "v1", lugarId: Guid.NewGuid()),
                admin));
    }

    [Fact]
    public async Task AplicarAccion_CambioEnOtroDia_NoInvalidaElDiaActual()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var dia1 = TestData.CrearItinerario(viaje, numeroDia: 1, version: "v1");
        TestData.CrearItinerario(viaje, numeroDia: 2, version: "v2");

        var lugar = TestData.CrearLugar();
        var lugares = new LugaresClientFalso();
        lugares.PorId[lugar.Id] = lugar;
        var service = CrearService(viaje, lugares);

        await service.AplicarAccionAsistenteAsync(
            viaje.Id,
            CrearAccion("AgregarParada", "v1", lugarId: lugar.Id, numeroDia: 1),
            admin);

        Assert.Single(dia1.Paradas);
    }

    [Fact]
    public async Task AplicarAccion_VersionVacia_LanzaReglaDeNegocio()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        TestData.CrearItinerario(viaje);
        var service = CrearService(viaje);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
            service.AplicarAccionAsistenteAsync(
                viaje.Id,
                CrearAccion("AgregarParada", string.Empty),
                admin));
    }

    [Fact]
    public async Task AplicarAccion_DiaCompleto_LanzaMensajeDeItinerarioCompleto()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var itinerario = TestData.CrearItinerario(viaje, version: "v1");
        TestData.CrearParada(itinerario, Guid.NewGuid(), Turno.Manana, 1);
        TestData.CrearParada(itinerario, Guid.NewGuid(), Turno.Mediodia, 1);
        TestData.CrearParada(itinerario, Guid.NewGuid(), Turno.Tarde, 1);
        TestData.CrearParada(itinerario, Guid.NewGuid(), Turno.Noche, 1);

        var lugar = TestData.CrearLugar();
        var lugares = new LugaresClientFalso();
        lugares.PorId[lugar.Id] = lugar;
        var service = CrearService(viaje, lugares);

        var excepcion = await Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
            service.AplicarAccionAsistenteAsync(
                viaje.Id,
                CrearAccion("AgregarParada", "v1", lugarId: lugar.Id),
                admin));

        Assert.Equal(
            "Disculpa pero no puedo agregar una parada más al itinerario, ya está completo.",
            excepcion.Message);
    }

    private static ItinerarioService CrearService(
        Viaje viaje,
        LugaresClientFalso? lugares = null,
        ItinerarioRepositoryFalso? repo = null)
    {
        var geo = new GeoFalso();
        repo ??= new ItinerarioRepositoryFalso();
        repo.Itinerarios = viaje.Itinerarios.ToList();

        return new ItinerarioService(
            new ViajeRepositoryFalso { Viaje = viaje },
            repo,
            lugares ?? new LugaresClientFalso(),
            new ItinerarioGenerator(geo),
            new ItinerarioRefiner(geo),
            geo,
            new UsuariosClientFalso(),
            new OllamaClientFalso(),
            new InterpretacionServiceFalso());
    }

    private static AplicarAccionAsistenteDto CrearAccion(
        string tipo,
        string version,
        Guid? lugarId = null,
        Guid? paradaId = null,
        short numeroDia = 1)
        => new()
        {
            Tipo = tipo,
            Version = version,
            LugarId = lugarId ?? Guid.NewGuid(),
            ParadaId = paradaId,
            NumeroDia = numeroDia
        };
}
