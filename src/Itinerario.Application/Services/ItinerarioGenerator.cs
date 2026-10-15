using Itinerario.Application.DTOs;
using Itinerario.Application.Interfaces;
using Itinerario.Domain.Entities;
using Itinerario.Domain.Enums;

namespace Itinerario.Application.Services;

public class ItinerarioGenerator : IItinerarioGenerator
{
    private const short CuadrasRitmoTranquilo = 4;
    private const short CuadrasRitmoActivo = 8;

    private readonly IGeoCalculator _geo;

    public ItinerarioGenerator(IGeoCalculator geo)
    {
        _geo = geo;
    }

    public IReadOnlyCollection<ParadaItinerario> Generar(
        Viaje viaje,
        IReadOnlyCollection<LugarCandidatoDto> lugares)
    {
        var turnos = TurnosSegunAlcance(viaje.AlcanceTemporal);

        var disponibles = lugares
            .Where(l => DentroDePresupuesto(l.NivelPrecio, viaje.NivelPresupuesto))
            .Where(l => turnos.Contains(l.Turno))
            .ToList();

        if (disponibles.Count == 0)
        {
            return Array.Empty<ParadaItinerario>();
        }

        var objetivo = viaje.AlcanceTemporal == AlcanceTemporal.DiaCompleto ? 4 : 3;
        objetivo = Math.Min(objetivo, disponibles.Count);

        var limiteCuadras = viaje.LimiteCuadrasEntreParadas
            ?? (viaje.RitmoCaminata == RitmoCaminata.Tranquilo ? CuadrasRitmoTranquilo : CuadrasRitmoActivo);

        var paradas = new List<ParadaItinerario>();
        var ordenPorTurno = new Dictionary<Turno, short>();
        var horaPorTurno = new Dictionary<Turno, TimeOnly>();

        double? latAnterior = (double?)viaje.LatitudPartida;
        double? lonAnterior = (double?)viaje.LongitudPartida;

        for (var i = 0; i < objetivo; i++)
        {
            var turnoPreferido = turnos[i % turnos.Count];
            var candidato = ElegirMasCercano(disponibles, turnoPreferido, latAnterior, lonAnterior, limiteCuadras);

            if (candidato is null)
            {
                break;
            }

            disponibles.Remove(candidato);

            var orden = ordenPorTurno.TryGetValue(candidato.Turno, out var ordenActual)
                ? (short)(ordenActual + 1)
                : (short)1;
            ordenPorTurno[candidato.Turno] = orden;

            var hora = horaPorTurno.TryGetValue(candidato.Turno, out var horaActual)
                ? horaActual
                : HoraBase(candidato.Turno);
            horaPorTurno[candidato.Turno] = hora.Add(TimeSpan.FromMinutes(candidato.DuracionSugeridaMinutos + 15));

            int? distanciaMetros = null;
            short? cuadras = null;
            short? minutosAPie = null;

            if (latAnterior.HasValue && lonAnterior.HasValue)
            {
                var distancia = _geo.DistanciaMetros(
                    latAnterior.Value,
                    lonAnterior.Value,
                    (double)candidato.Latitud,
                    (double)candidato.Longitud);

                distanciaMetros = (int)Math.Round(distancia);
                cuadras = (short)_geo.Cuadras(distancia);
                minutosAPie = (short)_geo.MinutosAPie(distancia);
            }

            paradas.Add(new ParadaItinerario
            {
                Id = Guid.NewGuid(),
                ItinerarioId = Guid.Empty,
                LugarId = candidato.Id,
                Turno = candidato.Turno,
                OrdenParada = orden,
                HoraInicio = hora,
                DuracionMinutos = candidato.DuracionSugeridaMinutos,
                DistanciaMetrosDesdeAnterior = distanciaMetros,
                CuadrasDesdeAnterior = cuadras,
                MinutosAPieDesdeAnterior = minutosAPie,
                OrigenParada = OrigenParada.Ia,
                FechaCreacion = DateTimeOffset.UtcNow
            });

            latAnterior = (double)candidato.Latitud;
            lonAnterior = (double)candidato.Longitud;
        }

        return paradas;
    }

    private LugarCandidatoDto? ElegirMasCercano(
        IReadOnlyList<LugarCandidatoDto> candidatos,
        Turno turnoPreferido,
        double? latReferencia,
        double? lonReferencia,
        double limiteCuadras)
    {
        var mismoTurno = candidatos.Where(c => c.Turno == turnoPreferido).ToList();
        var ordenados = (mismoTurno.Count > 0 ? mismoTurno : candidatos.ToList())
            .OrderBy(c => DistanciaDesdeReferencia(c, latReferencia, lonReferencia))
            .ToList();

        if (latReferencia.HasValue && lonReferencia.HasValue)
        {
            var dentroDelLimite = ordenados.FirstOrDefault(c =>
                _geo.Cuadras(_geo.DistanciaMetros(
                    latReferencia.Value,
                    lonReferencia.Value,
                    (double)c.Latitud,
                    (double)c.Longitud)) <= limiteCuadras);

            if (dentroDelLimite is not null)
            {
                return dentroDelLimite;
            }
        }

        return ordenados.FirstOrDefault();
    }

    private double DistanciaDesdeReferencia(LugarCandidatoDto candidato, double? latReferencia, double? lonReferencia)
        => latReferencia.HasValue && lonReferencia.HasValue
            ? _geo.DistanciaMetros(latReferencia.Value, lonReferencia.Value, (double)candidato.Latitud, (double)candidato.Longitud)
            : 0;

    private static bool DentroDePresupuesto(NivelPresupuesto candidato, NivelPresupuesto tope)
        => candidato <= tope;

    private static IReadOnlyList<Turno> TurnosSegunAlcance(AlcanceTemporal alcance) => alcance switch
    {
        AlcanceTemporal.DiaCompleto => new[] { Turno.Manana, Turno.Tarde, Turno.Noche },
        AlcanceTemporal.TardeNoche => new[] { Turno.Tarde, Turno.Noche },
        AlcanceTemporal.MananaAlmuerzo => new[] { Turno.Manana, Turno.Tarde },
        _ => new[] { Turno.Tarde }
    };

    private static TimeOnly HoraBase(Turno turno) => turno switch
    {
        Turno.Manana => new TimeOnly(9, 0),
        Turno.Tarde => new TimeOnly(14, 0),
        Turno.Noche => new TimeOnly(19, 0),
        _ => new TimeOnly(9, 0)
    };
}
