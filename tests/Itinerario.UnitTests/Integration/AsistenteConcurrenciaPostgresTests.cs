using Itinerario.Application.DTOs;
using Itinerario.Application.Interfaces;
using Itinerario.Application.Services;
using Itinerario.Domain.Entities;
using Itinerario.Domain.Enums;
using Itinerario.Domain.Exceptions;
using Itinerario.Infrastructure.Data;
using Itinerario.Infrastructure.Repositories;
using Itinerario.UnitTests.Services;
using Microsoft.EntityFrameworkCore;

namespace Itinerario.UnitTests.Integration;

[Trait("Category", "Integration")]
public class AsistenteConcurrenciaPostgresTests
{
    [PostgresFact]
    public async Task DosAdministradores_MismoDia_UnoGanaYUnoRecibeConflicto()
    {
        var admin1 = Guid.NewGuid();
        var admin2 = Guid.NewGuid();
        var sembrado = await SembrarAsync(admin1, admin2);
        var lugar = TestData.CrearLugar("Nuevo", "cafe_especialidad");
        var lugares = new LugaresClientBarrera(lugar);

        var (servicio1, contexto1) = await CrearServicioAsync(lugares);
        await using var _ = contexto1;
        var (servicio2, contexto2) = await CrearServicioAsync(lugares);
        await using var __ = contexto2;

        var tarea1 = servicio1.AplicarAccionAsistenteAsync(
            sembrado.ViajeId,
            CrearAccion(lugar.Id, sembrado.VersionDia1),
            admin1);

        var tarea2 = servicio2.AplicarAccionAsistenteAsync(
            sembrado.ViajeId,
            CrearAccion(lugar.Id, sembrado.VersionDia1),
            admin2);

        var resultados = await Task.WhenAll(
            ProtegerAsync(tarea1),
            ProtegerAsync(tarea2));

        var exitos = resultados.Where(resultado => resultado.Exitoso).ToList();
        var fallos = resultados.Where(resultado => !resultado.Exitoso).ToList();

        Assert.Single(exitos);
        var fallo = Assert.Single(fallos);
        Assert.IsType<ConflictoDeConcurrenciaException>(fallo.Error);

        await using var verificacion = await PostgresFixture.Instance.CreateContextAsync();
        var itinerario = await verificacion.Itinerarios
            .Include(i => i.Paradas)
            .SingleAsync(i => i.ViajeId == sembrado.ViajeId && i.NumeroDia == 1);

        Assert.Single(itinerario.Paradas);
    }

    [PostgresFact]
    public async Task VersionVieja_DespuesDeUnCambioReal_LanzaConflicto()
    {
        var admin1 = Guid.NewGuid();
        var admin2 = Guid.NewGuid();
        var sembrado = await SembrarAsync(admin1, admin2);
        var lugar1 = TestData.CrearLugar("Primero", "cafe_especialidad");
        var lugar2 = TestData.CrearLugar("Segundo", "museos_galerias");

        var lugares1 = new LugaresClientFalso();
        lugares1.PorId[lugar1.Id] = lugar1;

        var (servicio1, contexto1) = await CrearServicioAsync(lugares1);
        await using var _ = contexto1;

        await servicio1.AplicarAccionAsistenteAsync(
            sembrado.ViajeId,
            CrearAccion(lugar1.Id, sembrado.VersionDia1),
            admin1);

        var lugares2 = new LugaresClientFalso();
        lugares2.PorId[lugar2.Id] = lugar2;

        var (servicio2, contexto2) = await CrearServicioAsync(lugares2);
        await using var __ = contexto2;

        await Assert.ThrowsAsync<ConflictoDeConcurrenciaException>(() =>
            servicio2.AplicarAccionAsistenteAsync(
                sembrado.ViajeId,
                CrearAccion(lugar2.Id, sembrado.VersionDia1),
                admin2));

        await using var verificacion = await PostgresFixture.Instance.CreateContextAsync();
        var repositorio = new ItinerarioRepository(verificacion);
        var itinerarios = await repositorio.ObtenerPorViajeAsync(sembrado.ViajeId);

        Assert.NotEqual(
            sembrado.VersionDia1,
            itinerarios.Single(i => i.NumeroDia == 1).Version);
    }

    [PostgresFact]
    public async Task CambioEnDia1_NoInvalidaVersionDelDia2()
    {
        var admin1 = Guid.NewGuid();
        var admin2 = Guid.NewGuid();
        var sembrado = await SembrarAsync(admin1, admin2, dosDias: true);
        var lugar1 = TestData.CrearLugar("Día 1", "cafe_especialidad");
        var lugar2 = TestData.CrearLugar("Día 2", "museos_galerias");

        var lugares1 = new LugaresClientFalso();
        lugares1.PorId[lugar1.Id] = lugar1;

        var (servicio1, contexto1) = await CrearServicioAsync(lugares1);
        await using var _ = contexto1;

        await servicio1.AplicarAccionAsistenteAsync(
            sembrado.ViajeId,
            CrearAccion(lugar1.Id, sembrado.VersionDia1, numeroDia: 1),
            admin1);

        var lugares2 = new LugaresClientFalso();
        lugares2.PorId[lugar2.Id] = lugar2;

        var (servicio2, contexto2) = await CrearServicioAsync(lugares2);
        await using var __ = contexto2;

        await servicio2.AplicarAccionAsistenteAsync(
            sembrado.ViajeId,
            CrearAccion(lugar2.Id, sembrado.VersionDia2!, numeroDia: 2),
            admin2);

        await using var verificacion = await PostgresFixture.Instance.CreateContextAsync();
        var itinerarios = await verificacion.Itinerarios
            .Include(i => i.Paradas)
            .Where(i => i.ViajeId == sembrado.ViajeId)
            .ToListAsync();

        Assert.Single(itinerarios.Single(i => i.NumeroDia == 1).Paradas);
        Assert.Single(itinerarios.Single(i => i.NumeroDia == 2).Paradas);
    }

    private static async Task<(ItinerarioService Servicio, ApplicationDbContext Contexto)>
        CrearServicioAsync(ILugaresClient lugares)
    {
        var contexto = await PostgresFixture.Instance.CreateContextAsync();
        var geo = new GeoFalso();

        var servicio = new ItinerarioService(
            new ViajeRepository(contexto),
            new ItinerarioRepository(contexto),
            lugares,
            new ItinerarioGenerator(geo),
            new ItinerarioRefiner(geo),
            geo,
            new UsuariosClientFalso(),
            new OllamaClientFalso(),
            new InterpretacionServiceFalso());

        return (servicio, contexto);
    }

    private static async Task<ViajeSembrado> SembrarAsync(
        Guid admin1,
        Guid admin2,
        bool dosDias = false)
    {
        await using var contexto = await PostgresFixture.Instance.CreateContextAsync();

        var viaje = TestData.CrearViaje(
            admin1,
            (admin2, RolMiembro.Administrador));
        viaje.CodigoInvitacion = $"IT{Guid.NewGuid():N}"[..12];

        var dia1 = TestData.CrearItinerario(viaje, numeroDia: 1);
        dia1.OrigenLatitud = -34.60m;
        dia1.OrigenLongitud = -58.38m;

        if (dosDias)
        {
            var dia2 = TestData.CrearItinerario(viaje, numeroDia: 2);
            dia2.OrigenLatitud = -34.60m;
            dia2.OrigenLongitud = -58.38m;
        }

        contexto.Viajes.Add(viaje);
        await contexto.SaveChangesAsync();

        await using var carga = await PostgresFixture.Instance.CreateContextAsync();
        var repositorio = new ItinerarioRepository(carga);
        var itinerarios = await repositorio.ObtenerPorViajeAsync(viaje.Id);

        var version1 = itinerarios.Single(i => i.NumeroDia == 1).Version!;
        return new ViajeSembrado(
            viaje.Id,
            version1,
            itinerarios.FirstOrDefault(i => i.NumeroDia == 2)?.Version);
    }

    private static AplicarAccionAsistenteDto CrearAccion(
        Guid lugarId,
        string version,
        short numeroDia = 1) => new()
        {
            Tipo = "AgregarParada",
            NumeroDia = numeroDia,
            LugarId = lugarId,
            Version = version
        };

    private static async Task<ResultadoAccion> ProtegerAsync(
        Task<IReadOnlyCollection<ItinerarioResponseDto>> tarea)
    {
        try
        {
            await tarea;
            return new ResultadoAccion(true, null);
        }
        catch (Exception ex)
        {
            return new ResultadoAccion(false, ex);
        }
    }

    private sealed record ViajeSembrado(
        Guid ViajeId,
        string VersionDia1,
        string? VersionDia2);

    private sealed record ResultadoAccion(bool Exitoso, Exception? Error);

    private sealed class LugaresClientBarrera : ILugaresClient
    {
        private readonly LugarCandidatoDto _lugar;
        private readonly TaskCompletionSource _liberar = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private int _llegadas;

        public LugaresClientBarrera(LugarCandidatoDto lugar)
        {
            _lugar = lugar;
        }

        public async Task<LugarCandidatoDto?> ObtenerPorIdAsync(
            Guid lugarId,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _llegadas) >= 2)
            {
                _liberar.TrySetResult();
            }

            await _liberar.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
            return _lugar;
        }

        public Task<IReadOnlyCollection<LugarCandidatoDto>> BuscarAsync(
            BarrioCaba barrio,
            IReadOnlyCollection<string> categoriaCodigos,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<LugarCandidatoDto>> BuscarSemanticoAsync(
            BusquedaSemanticaLugaresDto busqueda,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
