using Itinerario.Application.Services;
using Itinerario.Domain.Entities;
using Itinerario.Domain.Enums;
using Itinerario.Domain.Exceptions;

namespace Itinerario.UnitTests.Services;

public class ItinerarioServiceCrearManualTests
{
    [Fact]
    public async Task CrearManual_RespetaElOrdenYAsignaTurnos()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var lugares = new[]
        {
            TestData.CrearLugar("Uno", duracionMinutos: 30),
            TestData.CrearLugar("Dos", duracionMinutos: 45),
            TestData.CrearLugar("Tres", duracionMinutos: 60),
            TestData.CrearLugar("Cuatro", duracionMinutos: 90)
        };

        var lugaresClient = new LugaresClientFalso();
        foreach (var lugar in lugares)
        {
            lugaresClient.PorId[lugar.Id] = lugar;
        }

        var repo = new ItinerarioRepositoryFalso();
        var service = CrearService(viaje, lugaresClient, repo);
        var ids = lugares.Select(lugar => lugar.Id).ToList();

        var resultado = await service.CrearManualAsync(viaje.Id, ids, admin);

        var itinerario = Assert.Single(resultado);
        Assert.Equal(viaje.Id, itinerario.ViajeId);
        Assert.Equal((short)1, itinerario.NumeroDia);
        Assert.Equal(EstadoViaje.Planificado, viaje.Estado);

        Assert.Equal(ids, itinerario.Paradas.Select(parada => parada.LugarId));
        Assert.Equal(
            new[] { Turno.Manana, Turno.Mediodia, Turno.Tarde, Turno.Noche },
            itinerario.Paradas.Select(parada => parada.Turno));
        Assert.Equal(
            new short[] { 30, 45, 60, 90 },
            itinerario.Paradas.Select(parada => parada.DuracionMinutos!.Value));
        Assert.Equal(
            new[]
            {
                new TimeOnly(9, 0),
                new TimeOnly(13, 0),
                new TimeOnly(16, 0),
                new TimeOnly(20, 0)
            },
            itinerario.Paradas.Select(parada => parada.HoraInicio!.Value));
        Assert.All(
            itinerario.Paradas,
            parada => Assert.Equal(OrigenParada.Manual, parada.OrigenParada));

        var primera = itinerario.Paradas.First();
        Assert.Null(primera.DistanciaMetrosDesdeAnterior);

        foreach (var parada in itinerario.Paradas.Skip(1))
        {
            Assert.Equal(250, parada.DistanciaMetrosDesdeAnterior);
            Assert.Equal((short?)3, parada.CuadrasDesdeAnterior);
            Assert.Equal((short?)3, parada.MinutosAPieDesdeAnterior);
        }

        Assert.Single(repo.Itinerarios);
    }

    [Fact]
    public async Task CrearManual_NoAdministrador_LanzaAccesoDenegado()
    {
        var admin = Guid.NewGuid();
        var miembro = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin, (miembro, RolMiembro.Miembro));
        var service = CrearService(viaje);

        await Assert.ThrowsAsync<AccesoDenegadoException>(() =>
            service.CrearManualAsync(
                viaje.Id,
                new[] { Guid.NewGuid(), Guid.NewGuid() },
                miembro));
    }

    [Fact]
    public async Task CrearManual_LugarInexistente_LanzaNotFound()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var service = CrearService(viaje);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.CrearManualAsync(
                viaje.Id,
                new[] { Guid.NewGuid(), Guid.NewGuid() },
                admin));
    }

    [Fact]
    public async Task CrearManual_LugaresDuplicados_LanzaReglaDeNegocio()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var service = CrearService(viaje);
        var lugarId = Guid.NewGuid();

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
            service.CrearManualAsync(
                viaje.Id,
                new[] { lugarId, lugarId },
                admin));
    }

    [Fact]
    public async Task CrearManual_ViajeDeVariosDias_LanzaReglaDeNegocio()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        viaje.FechaFin = viaje.FechaInicio!.Value.AddDays(1);
        var service = CrearService(viaje);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
            service.CrearManualAsync(
                viaje.Id,
                new[] { Guid.NewGuid(), Guid.NewGuid() },
                admin));
    }

    [Fact]
    public async Task CrearManual_SinParadas_LanzaReglaDeNegocio()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var service = CrearService(viaje);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
            service.CrearManualAsync(viaje.Id, Array.Empty<Guid>(), admin));
    }

    private static ItinerarioService CrearService(
        Viaje viaje,
        LugaresClientFalso? lugares = null,
        ItinerarioRepositoryFalso? repo = null)
    {
        var geo = new GeoFalso();
        repo ??= new ItinerarioRepositoryFalso();

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
}
