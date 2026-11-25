using System.Diagnostics;
using Itinerario.Application;
using Itinerario.Application.DTOs;
using Itinerario.Application.Interfaces;
using Itinerario.Domain.Entities;
using Itinerario.Domain.Enums;
using Itinerario.Domain.Exceptions;
using ItinerarioEntity = Itinerario.Domain.Entities.Itinerario;

namespace Itinerario.UnitTests.Services;

internal sealed class ViajeRepositoryFalso : IViajeRepository
{
    public Viaje? Viaje { get; set; }

    public Task<Viaje?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Viaje?.Id == id ? Viaje : null);

    public Task<ResultadoPaginado<Viaje>> ObtenerPaginadoPorUsuarioAsync(
        Guid usuarioId,
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<Viaje?> ObtenerPorCodigoInvitacionAsync(
        string codigoInvitacion,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<bool> ExisteCodigoInvitacionAsync(
        string codigoInvitacion,
        CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task AgregarAsync(Viaje viaje, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public void Actualizar(Viaje viaje)
    {
    }

    public void Eliminar(Viaje viaje)
    {
    }

    public void AgregarMiembro(MiembroViaje miembro)
    {
    }

    public void SincronizarEtiquetas(
        Viaje viaje,
        IReadOnlyCollection<string> categoriaCodigos)
    {
    }

    public void SincronizarDias(Viaje viaje, IReadOnlyCollection<ViajeDia> dias)
    {
    }

    public void EstablecerParametrosFamilia(
        Viaje viaje,
        ParametrosFamilia? parametros)
    {
    }

    public void Descartar(Viaje viaje)
    {
    }

    public Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(0);

    public Task<ICollection<MiembroViaje>> ObtenerMiembrosDeViajeAsync(Guid viajeId)
    {
        throw new NotImplementedException();
    }
}

internal sealed class ItinerarioRepositoryFalso : IItinerarioRepository
{
    private int _version = 1;

    public IReadOnlyCollection<ItinerarioEntity> Itinerarios { get; set; }
        = Array.Empty<ItinerarioEntity>();

    public bool ActualizarVersionAlGuardar { get; set; }

    public int Guardados { get; private set; }

    public List<ParadaItinerario> ParadasAgregadas { get; } = new();

    public List<ParadaItinerario> ParadasEliminadas { get; } = new();

    public List<ItinerarioEntity> Modificados { get; } = new();

    public Task<IReadOnlyCollection<ItinerarioEntity>> ObtenerPorViajeAsync(
        Guid viajeId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Itinerarios);

    public Task ReemplazarAsync(
        Guid viajeId,
        IReadOnlyCollection<ItinerarioEntity> itinerarios,
        CancellationToken cancellationToken = default)
    {
        Itinerarios = itinerarios;
        return Task.CompletedTask;
    }

    public Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default)
    {
        Guardados++;

        if (ActualizarVersionAlGuardar)
        {
            foreach (var itinerario in Itinerarios)
            {
                itinerario.Version = $"v{++_version}";
            }
        }

        return Task.FromResult(0);
    }

    public void AgregarParada(ParadaItinerario parada)
        => ParadasAgregadas.Add(parada);

    public void EliminarParada(ParadaItinerario parada)
        => ParadasEliminadas.Add(parada);

    public void MarcarModificado(ItinerarioEntity itinerario)
        => Modificados.Add(itinerario);
}

internal sealed class LugaresClientFalso : ILugaresClient
{
    public List<BusquedaSemanticaLugaresDto> Busquedas { get; } = new();

    public List<BarrioCaba> BarriosCatalogo { get; } = new();

    public IReadOnlyCollection<LugarCandidatoDto> ResultadoSemantico { get; set; }
        = Array.Empty<LugarCandidatoDto>();

    public IReadOnlyCollection<LugarCandidatoDto> ResultadoSemanticoSinBarrio { get; set; }
        = Array.Empty<LugarCandidatoDto>();

    public Dictionary<BarrioCaba, IReadOnlyCollection<LugarCandidatoDto>>
        ResultadosSemanticosPorBarrio { get; } = new();

    public IReadOnlyCollection<LugarCandidatoDto> ResultadoCatalogo { get; set; }
        = Array.Empty<LugarCandidatoDto>();

    public Dictionary<Guid, LugarCandidatoDto> PorId { get; } = new();

    public bool FallarSemantico { get; set; }

    public Task<IReadOnlyCollection<LugarCandidatoDto>> BuscarAsync(
        BarrioCaba barrio,
        IReadOnlyCollection<string> categoriaCodigos,
        CancellationToken cancellationToken = default)
    {
        BarriosCatalogo.Add(barrio);
        return Task.FromResult(ResultadoCatalogo);
    }

    public Task<IReadOnlyCollection<LugarCandidatoDto>> BuscarSemanticoAsync(
        BusquedaSemanticaLugaresDto busqueda,
        CancellationToken cancellationToken = default)
    {
        Busquedas.Add(busqueda);

        if (FallarSemantico)
        {
            throw new ServicioExternoNoDisponibleException(
                "Búsqueda semántica no disponible en tests.");
        }

        if (busqueda.Barrio is { } barrio
            && ResultadosSemanticosPorBarrio.TryGetValue(barrio, out var porBarrio))
        {
            return Task.FromResult(porBarrio);
        }

        return Task.FromResult(
            busqueda.Barrio is null
                ? ResultadoSemanticoSinBarrio
                : ResultadoSemantico);
    }

    public Task<LugarCandidatoDto?> ObtenerPorIdAsync(
        Guid lugarId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(
            PorId.TryGetValue(lugarId, out var lugar) ? lugar : null);
}

internal sealed class TelemetriaFalsa : IAsistenteTelemetry
{
    public List<string> Etapas { get; } = new();

    public List<string> Resultados { get; } = new();

    public Activity? IniciarActividad(string nombre, Guid? viajeId = null)
        => null;

    public void RegistrarEtapa(string etapa, TimeSpan duracion, Guid? viajeId = null)
        => Etapas.Add(etapa);

    public void RegistrarResultado(
        string resultado,
        int cantidadRecomendaciones,
        Guid? viajeId = null)
        => Resultados.Add(resultado);

    public void RegistrarDetalle(string detalle, Guid? viajeId = null)
    {
    }
}

internal sealed class GeoFalso : IGeoCalculator
{
    public double DistanciaMetrosResultado { get; set; } = 250;

    public double DistanciaMetros(
        double latitud1,
        double longitud1,
        double latitud2,
        double longitud2)
        => DistanciaMetrosResultado;

    public int Cuadras(double metros)
        => (int)Math.Round(metros / 100d, MidpointRounding.AwayFromZero);

    public int MinutosAPie(double metros)
        => (int)Math.Round(metros / 80d, MidpointRounding.AwayFromZero);
}

internal sealed class UsuariosClientFalso : IUsuariosClient
{
    public Task<PerfilViajeroDto> ObtenerPerfilAsync(
        CancellationToken cancellationToken = default)
        => Task.FromResult(new PerfilViajeroDto());

    public Task<List<UsuarioResumenDto>> ObtenerResumenesAsync(IEnumerable<Guid> usuariosIds, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

internal sealed class OllamaClientFalso : IOllamaClient
{
    public Task<T> ChatJsonAsync<T>(
        string prompt,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<string> ChatTextoAsync(
        string prompt,
        CancellationToken cancellationToken = default)
        => Task.FromResult("Sugerencia de prueba");
}

internal sealed class IntencionLlmServiceFalso : IIntencionLlmService
{
    public IntencionLlmCruda? Respuesta { get; set; }

    public bool FueLlamado { get; private set; }

    public IntencionLlmContextoDto? Contexto { get; private set; }

    public Task<IntencionLlmCruda?> InterpretarAsync(
        IntencionLlmContextoDto contexto,
        CancellationToken cancellationToken = default)
    {
        FueLlamado = true;
        Contexto = contexto;

        return Task.FromResult(Respuesta);
    }
}

internal sealed class MensajeLlmServiceFalso : IMensajeLlmService
{
    public string? Respuesta { get; set; }

    public bool FueLlamado { get; private set; }

    public MensajeLlmContextoDto? Contexto { get; private set; }

    public Task<string?> GenerarAsync(
        MensajeLlmContextoDto contexto,
        CancellationToken cancellationToken = default)
    {
        FueLlamado = true;
        Contexto = contexto;

        return Task.FromResult(Respuesta);
    }
}

internal sealed class InterpretacionServiceFalso : IInterpretacionService
{
    public Task<BosquejoViajeDto> InterpretarAsync(
        string texto,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<TipoRefinamiento> InterpretarRefinamientoAsync(
        string texto,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}

internal static class TestData
{
    public static Viaje CrearViaje(
        Guid? administradorId = null,
        params (Guid UsuarioId, RolMiembro Rol)[] miembros)
    {
        var viaje = new Viaje
        {
            Id = Guid.NewGuid(),
            Titulo = "Viaje de prueba",
            Barrio = BarrioCaba.Palermo,
            AlcanceTemporal = AlcanceTemporal.DiaCompleto,
            NivelPresupuesto = NivelPresupuesto.Equilibrado,
            RitmoCaminata = RitmoCaminata.Tranquilo,
            TipoPuntoPartida = TipoPuntoPartida.PrimeraParada,
            CreadoPorUsuarioId = administradorId ?? Guid.NewGuid(),
            CodigoInvitacion = "TEST123",
            FechaInicio = new DateOnly(2026, 11, 18),
            FechaFin = new DateOnly(2026, 11, 18)
        };

        if (administradorId.HasValue)
        {
            viaje.AgregarMiembro(administradorId.Value, RolMiembro.Administrador);
        }

        foreach (var (usuarioId, rol) in miembros)
        {
            viaje.AgregarMiembro(usuarioId, rol);
        }

        return viaje;
    }

    public static ItinerarioEntity CrearItinerario(
        Viaje viaje,
        short numeroDia = 1,
        string version = "v1")
    {
        var itinerario = new ItinerarioEntity
        {
            Id = Guid.NewGuid(),
            ViajeId = viaje.Id,
            NumeroDia = numeroDia,
            Fecha = viaje.FechaInicio,
            OrigenTipo = TipoPuntoPartida.PrimeraParada,
            Version = version,
            FechaCreacion = DateTimeOffset.UtcNow,
            FechaActualizacion = DateTimeOffset.UtcNow
        };

        viaje.Itinerarios.Add(itinerario);
        return itinerario;
    }

    public static ParadaItinerario CrearParada(
        ItinerarioEntity itinerario,
        Guid lugarId,
        Turno turno = Turno.Manana,
        short orden = 1)
    {
        var parada = new ParadaItinerario
        {
            Id = Guid.NewGuid(),
            ItinerarioId = itinerario.Id,
            LugarId = lugarId,
            Turno = turno,
            OrdenParada = orden,
            HoraInicio = new TimeOnly(9, 0),
            DuracionMinutos = 60,
            OrigenParada = OrigenParada.Ia,
            FechaCreacion = DateTimeOffset.UtcNow
        };

        itinerario.Paradas.Add(parada);
        return parada;
    }

    public static LugarCandidatoDto CrearLugar(
        string nombre = "Café de prueba",
        string categoria = "cafe_especialidad",
        short duracionMinutos = 60,
        decimal latitud = -34.58m,
        decimal longitud = -58.42m,
        IReadOnlyCollection<string>? opcionesDietarias = null,
        bool esAireLibre = false,
        bool esBajoTecho = true,
        bool aptoNinos = true,
        bool accesible = true)
        => new()
        {
            Id = Guid.NewGuid(),
            Nombre = nombre,
            Barrio = BarrioCaba.Palermo,
            Latitud = latitud,
            Longitud = longitud,
            NivelPrecio = NivelPresupuesto.Equilibrado,
            DuracionSugeridaMinutos = duracionMinutos,
            Categorias = new[] { categoria },
            EsAireLibre = esAireLibre,
            EsBajoTecho = esBajoTecho,
            AptoNinos = aptoNinos,
            AccesibleMovilidadReducida = accesible,
            OpcionesDietarias = opcionesDietarias ?? Array.Empty<string>()
        };
}
