using Itinerario.Application.DTOs;
using Itinerario.Application.Interfaces;
using Itinerario.Domain.Entities;
using Itinerario.Domain.Enums;
using Itinerario.Domain.Exceptions;
using ItinerarioEntity = Itinerario.Domain.Entities.Itinerario;

namespace Itinerario.Application.Services;

public class ItinerarioService : IItinerarioService
{
    private readonly IViajeRepository _viajeRepository;
    private readonly IItinerarioRepository _itinerarioRepository;
    private readonly ILugaresClient _lugaresClient;
    private readonly IItinerarioGenerator _generator;
    private readonly IItinerarioRefiner _refiner;

    public ItinerarioService(
        IViajeRepository viajeRepository,
        IItinerarioRepository itinerarioRepository,
        ILugaresClient lugaresClient,
        IItinerarioGenerator generator,
        IItinerarioRefiner refiner)
    {
        _viajeRepository = viajeRepository;
        _itinerarioRepository = itinerarioRepository;
        _lugaresClient = lugaresClient;
        _generator = generator;
        _refiner = refiner;
    }

    public async Task<IReadOnlyCollection<ItinerarioResponseDto>> GenerarAsync(
        Guid viajeId,
        Guid usuarioIdSolicitante,
        CancellationToken cancellationToken = default)
    {
        var viaje = await _viajeRepository.ObtenerPorIdAsync(viajeId, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el viaje con id {viajeId}.");

        if (!viaje.EsAdministrador(usuarioIdSolicitante))
        {
            throw new AccesoDenegadoException("Solo un administrador del viaje puede generar el itinerario.");
        }

        var categoriaIds = viaje.Etiquetas.Select(e => e.CategoriaId).ToList();
        var lugares = await _lugaresClient.BuscarAsync(viaje.Barrio, categoriaIds, cancellationToken);

        var disponibles = lugares.ToList();
        var itinerarios = new List<ItinerarioEntity>();

        foreach (var dia in CalcularDias(viaje))
        {
            var paradas = _generator.Generar(viaje, disponibles).ToList();

            var itinerario = new ItinerarioEntity
            {
                Id = Guid.NewGuid(),
                ViajeId = viaje.Id,
                NumeroDia = dia.NumeroDia,
                Fecha = dia.Fecha,
                OrigenTipo = viaje.TipoPuntoPartida,
                OrigenDescripcion = viaje.NombrePuntoPartida ?? viaje.DireccionPartida,
                OrigenLatitud = viaje.LatitudPartida,
                OrigenLongitud = viaje.LongitudPartida,
                FechaCreacion = DateTimeOffset.UtcNow,
                FechaActualizacion = DateTimeOffset.UtcNow,
                Paradas = paradas
            };

            foreach (var parada in paradas)
            {
                parada.ItinerarioId = itinerario.Id;
                disponibles.RemoveAll(l => l.Id == parada.LugarId);
            }

            itinerarios.Add(itinerario);
        }

        viaje.CambiarEstado(EstadoViaje.Planificado);

        await _itinerarioRepository.ReemplazarAsync(viaje.Id, itinerarios, cancellationToken);

        return itinerarios.Select(MapearRespuesta).ToList();
    }

    public async Task<IReadOnlyCollection<ItinerarioResponseDto>> ObtenerAsync(
        Guid viajeId,
        CancellationToken cancellationToken = default)
    {
        _ = await _viajeRepository.ObtenerPorIdAsync(viajeId, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el viaje con id {viajeId}.");

        var itinerarios = await _itinerarioRepository.ObtenerPorViajeAsync(viajeId, cancellationToken);

        if (itinerarios.Count == 0)
        {
            throw new NotFoundException($"El viaje {viajeId} no tiene un itinerario generado.");
        }

        return itinerarios.Select(MapearRespuesta).ToList();
    }

    public async Task<IReadOnlyCollection<ItinerarioResponseDto>> RefinarAsync(
        Guid viajeId,
        TipoRefinamiento tipo,
        short? numeroDia,
        Guid usuarioIdSolicitante,
        CancellationToken cancellationToken = default)
    {
        var (viaje, itinerarios) = await PrepararModificacionAsync(
            viajeId, numeroDia, usuarioIdSolicitante, cancellationToken);

        var lugares = await BuscarLugaresAsync(viaje, cancellationToken);
        var lugaresActuales = ConstruirLugaresActuales(itinerarios, lugares);
        var disponibles = ConstruirDisponibles(itinerarios, lugares, viaje);

        foreach (var itinerario in itinerarios)
        {
            _refiner.Refinar(tipo, itinerario, lugaresActuales, disponibles, viaje);
        }

        await _itinerarioRepository.GuardarCambiosAsync(cancellationToken);

        return itinerarios.Select(MapearRespuesta).ToList();
    }

    public async Task<IReadOnlyCollection<ItinerarioResponseDto>> SugerirTurnosLibresAsync(
        Guid viajeId,
        short? numeroDia,
        Guid usuarioIdSolicitante,
        CancellationToken cancellationToken = default)
    {
        var (viaje, itinerarios) = await PrepararModificacionAsync(
            viajeId, numeroDia, usuarioIdSolicitante, cancellationToken);

        var lugares = await BuscarLugaresAsync(viaje, cancellationToken);
        var disponibles = ConstruirDisponibles(itinerarios, lugares, viaje);

        foreach (var itinerario in itinerarios)
        {
            var sugerencias = _refiner.SugerirTurnosLibres(itinerario, disponibles, viaje);
            foreach (var sugerencia in sugerencias)
            {
                _itinerarioRepository.AgregarParada(sugerencia);
                itinerario.Paradas.Add(sugerencia);
            }
        }

        await _itinerarioRepository.GuardarCambiosAsync(cancellationToken);

        return itinerarios.Select(MapearRespuesta).ToList();
    }

    private async Task<(Viaje Viaje, IReadOnlyCollection<ItinerarioEntity> Itinerarios)> PrepararModificacionAsync(
        Guid viajeId,
        short? numeroDia,
        Guid usuarioIdSolicitante,
        CancellationToken cancellationToken)
    {
        var viaje = await _viajeRepository.ObtenerPorIdAsync(viajeId, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el viaje con id {viajeId}.");

        if (!viaje.EsAdministrador(usuarioIdSolicitante))
        {
            throw new AccesoDenegadoException("Solo un administrador del viaje puede modificar el itinerario.");
        }

        var itinerarios = await _itinerarioRepository.ObtenerPorViajeAsync(viajeId, cancellationToken);

        if (itinerarios.Count == 0)
        {
            throw new NotFoundException($"El viaje {viajeId} no tiene un itinerario generado.");
        }

        if (numeroDia.HasValue && !itinerarios.Any(i => i.NumeroDia == numeroDia.Value))
        {
            throw new ReglaDeNegocioException($"El día {numeroDia.Value} no existe en el itinerario.");
        }

        var destino = numeroDia.HasValue
            ? itinerarios.Where(i => i.NumeroDia == numeroDia.Value).ToList()
            : itinerarios;

        return (viaje, destino);
    }

    private Task<IReadOnlyCollection<LugarCandidatoDto>> BuscarLugaresAsync(Viaje viaje, CancellationToken cancellationToken)
    {
        var categoriaIds = viaje.Etiquetas.Select(e => e.CategoriaId).ToList();
        return _lugaresClient.BuscarAsync(viaje.Barrio, categoriaIds, cancellationToken);
    }

    private static IReadOnlyDictionary<Guid, LugarCandidatoDto> ConstruirLugaresActuales(
        IReadOnlyCollection<ItinerarioEntity> itinerarios,
        IReadOnlyCollection<LugarCandidatoDto> lugares)
    {
        var porId = lugares.ToDictionary(l => l.Id);
        var actuales = new Dictionary<Guid, LugarCandidatoDto>();

        foreach (var parada in itinerarios.SelectMany(i => i.Paradas))
        {
            if (porId.TryGetValue(parada.LugarId, out var lugar))
            {
                actuales[parada.LugarId] = lugar;
            }
        }

        return actuales;
    }

    private static List<LugarCandidatoDto> ConstruirDisponibles(
        IReadOnlyCollection<ItinerarioEntity> itinerarios,
        IReadOnlyCollection<LugarCandidatoDto> lugares,
        Viaje viaje)
    {
        var usados = new HashSet<Guid>(itinerarios.SelectMany(i => i.Paradas).Select(p => p.LugarId));
        return lugares
            .Where(l => !usados.Contains(l.Id))
            .Where(l => DentroDePresupuesto(l.NivelPrecio, viaje.NivelPresupuesto))
            .ToList();
    }

    private static bool DentroDePresupuesto(NivelPresupuesto candidato, NivelPresupuesto tope)
        => candidato <= tope;

    private static IReadOnlyList<DiaPlanificado> CalcularDias(Viaje viaje)
    {
        if (viaje.FechaInicio is null || viaje.FechaFin is null || viaje.FechaFin < viaje.FechaInicio)
        {
            return new[] { new DiaPlanificado(1, viaje.FechaInicio) };
        }

        var dias = new List<DiaPlanificado>();

        for (var fecha = viaje.FechaInicio.Value; fecha <= viaje.FechaFin.Value; fecha = fecha.AddDays(1))
        {
            dias.Add(new DiaPlanificado((short)(dias.Count + 1), fecha));
        }

        return dias;
    }

    private static ItinerarioResponseDto MapearRespuesta(ItinerarioEntity itinerario) => new()
    {
        Id = itinerario.Id,
        ViajeId = itinerario.ViajeId,
        NumeroDia = itinerario.NumeroDia,
        Fecha = itinerario.Fecha,
        OrigenTipo = itinerario.OrigenTipo,
        OrigenDescripcion = itinerario.OrigenDescripcion,
        OrigenLatitud = itinerario.OrigenLatitud,
        OrigenLongitud = itinerario.OrigenLongitud,
        Paradas = itinerario.Paradas
            .OrderBy(p => p.Turno)
            .ThenBy(p => p.OrdenParada)
            .Select(MapearParada)
            .ToList()
    };

    private static ParadaItinerarioDto MapearParada(ParadaItinerario parada) => new()
    {
        Id = parada.Id,
        LugarId = parada.LugarId,
        Turno = parada.Turno,
        OrdenParada = parada.OrdenParada,
        HoraInicio = parada.HoraInicio,
        DuracionMinutos = parada.DuracionMinutos,
        DistanciaMetrosDesdeAnterior = parada.DistanciaMetrosDesdeAnterior,
        CuadrasDesdeAnterior = parada.CuadrasDesdeAnterior,
        MinutosAPieDesdeAnterior = parada.MinutosAPieDesdeAnterior,
        OrigenParada = parada.OrigenParada,
        SugerenciaIa = parada.SugerenciaIa,
        NotasPersonales = parada.NotasPersonales
    };

    private sealed record DiaPlanificado(short NumeroDia, DateOnly? Fecha);
}
