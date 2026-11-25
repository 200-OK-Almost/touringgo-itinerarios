using Itinerario.Application.Interfaces;
using Itinerario.Application.Services;
using Itinerario.Domain.Enums;
using Itinerario.Infrastructure.Data;
using Itinerario.Infrastructure.Repositories;
using Itinerario.UnitTests.Services;
using Microsoft.EntityFrameworkCore;

namespace Itinerario.UnitTests.Integration;

[Trait("Category", "Integration")]
public class EliminarParadaPostgresTests
{
    [PostgresFact]
    public async Task EliminarParada_PersisteYRecalculaElTurno()
    {
        var admin = Guid.NewGuid();
        var sembrado = await SembrarAsync(admin);

        var (servicio, contexto) = await CrearServicioAsync();
        await using var _ = contexto;

        var resultado = await servicio.EliminarParadaAsync(
            sembrado.ViajeId,
            1,
            sembrado.ParadaNocheId,
            sembrado.Version,
            admin);

        var dia = Assert.Single(resultado);
        var restante = Assert.Single(dia.Paradas);
        Assert.Equal(sembrado.ParadaTardeId, restante.Id);
        Assert.Equal(Turno.Tarde, restante.Turno);
        Assert.Equal((short)1, restante.OrdenParada);
        Assert.Equal(new TimeOnly(14, 0), restante.HoraInicio);

        await using var verificacion = await PostgresFixture.Instance.CreateContextAsync();
        var itinerario = await verificacion.Itinerarios
            .Include(i => i.Paradas)
            .SingleAsync(i => i.ViajeId == sembrado.ViajeId && i.NumeroDia == 1);

        var parada = Assert.Single(itinerario.Paradas);
        Assert.Equal(sembrado.ParadaTardeId, parada.Id);
        Assert.Equal((short)1, parada.OrdenParada);
        Assert.Equal(new TimeOnly(14, 0), parada.HoraInicio);
    }

    [PostgresFact]
    public async Task EliminarParada_InvalidaLaVersionAnterior()
    {
        var admin = Guid.NewGuid();
        var sembrado = await SembrarAsync(admin);

        var (servicio, contexto) = await CrearServicioAsync();
        await using var _ = contexto;

        await servicio.EliminarParadaAsync(
            sembrado.ViajeId,
            1,
            sembrado.ParadaNocheId,
            sembrado.Version,
            admin);

        await using var verificacion = await PostgresFixture.Instance.CreateContextAsync();
        var repositorio = new ItinerarioRepository(verificacion);
        var itinerarios = await repositorio.ObtenerPorViajeAsync(sembrado.ViajeId);

        Assert.NotEqual(
            sembrado.Version,
            itinerarios.Single(i => i.NumeroDia == 1).Version);
    }

    private static async Task<(ItinerarioService Servicio, ApplicationDbContext Contexto)>
        CrearServicioAsync()
    {
        var contexto = await PostgresFixture.Instance.CreateContextAsync();
        var geo = new GeoFalso();

        var servicio = new ItinerarioService(
            new ViajeRepository(contexto),
            new ItinerarioRepository(contexto),
            new LugaresClientFalso(),
            new ItinerarioGenerator(geo),
            new ItinerarioRefiner(geo),
            geo,
            new UsuariosClientFalso(),
            new OllamaClientFalso(),
            new InterpretacionServiceFalso());

        return (servicio, contexto);
    }

    private static async Task<ViajeSembrado> SembrarAsync(Guid admin)
    {
        await using var contexto = await PostgresFixture.Instance.CreateContextAsync();

        var viaje = TestData.CrearViaje(admin);
        viaje.CodigoInvitacion = $"IT{Guid.NewGuid():N}"[..12];

        var itinerario = TestData.CrearItinerario(viaje, numeroDia: 1);
        itinerario.OrigenLatitud = -34.60m;
        itinerario.OrigenLongitud = -58.38m;

        var tarde = TestData.CrearParada(
            itinerario,
            Guid.NewGuid(),
            Turno.Tarde,
            orden: 1);
        tarde.HoraInicio = new TimeOnly(14, 0);
        tarde.DuracionMinutos = 60;

        var noche = TestData.CrearParada(
            itinerario,
            Guid.NewGuid(),
            Turno.Noche,
            orden: 1);
        noche.HoraInicio = new TimeOnly(20, 0);
        noche.DuracionMinutos = 45;

        contexto.Viajes.Add(viaje);
        await contexto.SaveChangesAsync();

        await using var carga = await PostgresFixture.Instance.CreateContextAsync();
        var repositorio = new ItinerarioRepository(carga);
        var itinerarios = await repositorio.ObtenerPorViajeAsync(viaje.Id);
        var version = itinerarios.Single(i => i.NumeroDia == 1).Version!;

        return new ViajeSembrado(
            viaje.Id,
            version,
            tarde.Id,
            noche.Id);
    }

    private sealed record ViajeSembrado(
        Guid ViajeId,
        string Version,
        Guid ParadaTardeId,
        Guid ParadaNocheId);
}
