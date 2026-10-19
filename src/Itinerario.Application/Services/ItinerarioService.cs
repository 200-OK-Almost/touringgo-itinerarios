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

    public ItinerarioService(
        IViajeRepository viajeRepository,
        IItinerarioRepository itinerarioRepository,
        ILugaresClient lugaresClient,
        IItinerarioGenerator generator)
    {
        _viajeRepository = viajeRepository;
        _itinerarioRepository = itinerarioRepository;
        _lugaresClient = lugaresClient;
        _generator = generator;
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

    public Task<IReadOnlyCollection<ItinerarioResponseDto>> RefinarAsync(
        Guid viajeId,
        TipoRefinamiento tipo,
        short? numeroDia,
        Guid usuarioIdSolicitante,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<IReadOnlyCollection<ItinerarioResponseDto>> SugerirTurnosLibresAsync(
        Guid viajeId,
        short? numeroDia,
        Guid usuarioIdSolicitante,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

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
