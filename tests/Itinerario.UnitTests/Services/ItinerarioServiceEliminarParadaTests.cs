using Itinerario.Application.DTOs;
using Itinerario.Application.Services;
using Itinerario.Domain.Entities;
using Itinerario.Domain.Enums;
using Itinerario.Domain.Exceptions;

namespace Itinerario.UnitTests.Services;

public class ItinerarioServiceEliminarParadaTests
{
    [Fact]
    public async Task EliminarParada_NoAdministrador_LanzaAccesoDenegado()
    {
        var admin = Guid.NewGuid();
        var miembro = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin, (miembro, RolMiembro.Miembro));
        var itinerario = TestData.CrearItinerario(viaje, version: "v1");
        var parada = TestData.CrearParada(itinerario, Guid.NewGuid());
        var service = CrearService(viaje);

        await Assert.ThrowsAsync<AccesoDenegadoException>(() =>
            service.EliminarParadaAsync(
                viaje.Id,
                1,
                parada.Id,
                "v1",
                miembro));
    }

    [Fact]
    public async Task EliminarParada_VersionIncorrecta_LanzaConflicto()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var itinerario = TestData.CrearItinerario(viaje, version: "v1");
        var parada = TestData.CrearParada(itinerario, Guid.NewGuid());
        var service = CrearService(viaje);

        await Assert.ThrowsAsync<ConflictoDeConcurrenciaException>(() =>
            service.EliminarParadaAsync(
                viaje.Id,
                1,
                parada.Id,
                "v2",
                admin));
    }

    [Fact]
    public async Task EliminarParada_VersionVacia_LanzaReglaDeNegocio()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var itinerario = TestData.CrearItinerario(viaje, version: "v1");
        var parada = TestData.CrearParada(itinerario, Guid.NewGuid());
        var service = CrearService(viaje);

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
            service.EliminarParadaAsync(
                viaje.Id,
                1,
                parada.Id,
                string.Empty,
                admin));
    }

    [Fact]
    public async Task EliminarParada_ParadaInexistente_LanzaNotFound()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        TestData.CrearItinerario(viaje, version: "v1");
        var service = CrearService(viaje);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.EliminarParadaAsync(
                viaje.Id,
                1,
                Guid.NewGuid(),
                "v1",
                admin));
    }

    [Fact]
    public async Task EliminarParada_ReordenaYRecalculaHorasDelTurno()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var itinerario = TestData.CrearItinerario(viaje, version: "v1");

        var primera = TestData.CrearParada(
            itinerario,
            Guid.NewGuid(),
            Turno.Manana,
            orden: 1);
        primera.HoraInicio = new TimeOnly(9, 0);
        primera.DuracionMinutos = 60;

        var eliminada = TestData.CrearParada(
            itinerario,
            Guid.NewGuid(),
            Turno.Manana,
            orden: 2);
        eliminada.HoraInicio = new TimeOnly(10, 15);
        eliminada.DuracionMinutos = 60;

        var tercera = TestData.CrearParada(
            itinerario,
            Guid.NewGuid(),
            Turno.Manana,
            orden: 3);
        tercera.HoraInicio = new TimeOnly(11, 30);
        tercera.DuracionMinutos = 60;

        var repo = new ItinerarioRepositoryFalso();
        var service = CrearService(viaje, repo: repo);

        var resultado = await service.EliminarParadaAsync(
            viaje.Id,
            1,
            eliminada.Id,
            "v1",
            admin);

        Assert.DoesNotContain(eliminada, itinerario.Paradas);
        Assert.Equal((short)1, primera.OrdenParada);
        Assert.Equal((short)2, tercera.OrdenParada);
        Assert.Equal(new TimeOnly(9, 0), primera.HoraInicio);
        Assert.Equal(new TimeOnly(10, 15), tercera.HoraInicio);

        Assert.Single(repo.ParadasEliminadas);
        Assert.Same(eliminada, repo.ParadasEliminadas[0]);
        Assert.Contains(itinerario, repo.Modificados);
        Assert.Equal(1, repo.Guardados);

        var dia = Assert.Single(resultado);
        Assert.Equal(2, dia.Paradas.Count);
    }

    [Fact]
    public async Task EliminarParada_DevuelveTodosLosDiasActualizados()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        var dia1 = TestData.CrearItinerario(viaje, numeroDia: 1, version: "v1");
        TestData.CrearItinerario(viaje, numeroDia: 2, version: "v2");
        var parada = TestData.CrearParada(dia1, Guid.NewGuid());
        var service = CrearService(viaje);

        var resultado = await service.EliminarParadaAsync(
            viaje.Id,
            1,
            parada.Id,
            "v1",
            admin);

        Assert.Equal(2, resultado.Count);
    }

    [Fact]
    public async Task EliminarParada_LiberaElTurnoParaAgregarOtraParada()
    {
        var admin = Guid.NewGuid();
        var viaje = TestData.CrearViaje(admin);
        viaje.AlcanceTemporal = AlcanceTemporal.TardeNoche;

        var itinerario = TestData.CrearItinerario(viaje, version: "v1");
        TestData.CrearParada(itinerario, Guid.NewGuid(), Turno.Tarde, orden: 1);
        var paradaNoche = TestData.CrearParada(
            itinerario,
            Guid.NewGuid(),
            Turno.Noche,
            orden: 1);

        var bar = TestData.CrearLugar("Bar nuevo", "bares_vermu");
        var lugares = new LugaresClientFalso();
        lugares.PorId[bar.Id] = bar;

        var repo = new ItinerarioRepositoryFalso();
        var service = CrearService(viaje, lugares, repo);

        await service.EliminarParadaAsync(
            viaje.Id,
            1,
            paradaNoche.Id,
            "v1",
            admin);

        await service.AplicarAccionAsistenteAsync(
            viaje.Id,
            new AplicarAccionAsistenteDto
            {
                Tipo = "AgregarParada",
                NumeroDia = 1,
                LugarId = bar.Id,
                Turno = "Noche",
                Version = "v1"
            },
            admin);

        Assert.Contains(
            itinerario.Paradas,
            parada => parada.Turno == Turno.Noche && parada.LugarId == bar.Id);
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
}
