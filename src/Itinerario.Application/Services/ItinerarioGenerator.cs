using Itinerario.Application.DTOs;
using Itinerario.Application.Interfaces;
using Itinerario.Domain.Entities;
using Itinerario.Domain.Enums;

namespace Itinerario.Application.Services;

public class ItinerarioGenerator : IItinerarioGenerator
{
    private const short CuadrasRitmoTranquilo = 4;
    private const short CuadrasRitmoActivo = 8;
    private const int DescansoMinutos = 15;
    private const int UltimoMinutoDelDia = (24 * 60) - 1;

    private readonly IGeoCalculator _geo;

    public ItinerarioGenerator(IGeoCalculator geo)
    {
        _geo = geo;
    }

    // Selección greedy (vecino más cercano): no garantiza el recorrido total óptimo.
    // El turno se asigna por orden en el recorrido, no por el turno del lugar (limitación v1).
    public IReadOnlyCollection<ParadaItinerario> Generar(
        Viaje viaje,
        IReadOnlyCollection<LugarCandidatoDto> lugares)
    {
        var turnos = TurnosSegunAlcance(viaje.AlcanceTemporal);

        var disponibles = lugares
            .Where(l => DentroDePresupuesto(l.NivelPrecio, viaje.NivelPresupuesto))
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
        var minutoPorTurno = new Dictionary<Turno, int>();

        double? latAnterior = (double?)viaje.LatitudPartida;
        double? lonAnterior = (double?)viaje.LongitudPartida;
        Turno? turnoAnterior = null;

        for (var i = 0; i < objetivo; i++)
        {
            var turnoAsignado = turnos[i % turnos.Count];

            var seleccion = latAnterior.HasValue && lonAnterior.HasValue
                ? ElegirDelConjunto(disponibles, latAnterior.Value, lonAnterior.Value, limiteCuadras)
                : ElegirSemilla(disponibles);

            if (seleccion is null)
            {
                break;
            }

            var candidato = seleccion.Lugar;
            disponibles.Remove(candidato);

            var orden = ordenPorTurno.TryGetValue(turnoAsignado, out var ordenActual)
                ? (short)(ordenActual + 1)
                : (short)1;
            ordenPorTurno[turnoAsignado] = orden;

            var minutoActual = minutoPorTurno.TryGetValue(turnoAsignado, out var acumulado)
                ? acumulado
                : HoraBaseMinutos(turnoAsignado);

            if (turnoAnterior == turnoAsignado && seleccion.MinutosAPie is not null)
            {
                minutoActual += seleccion.MinutosAPie.Value;
            }

            var horaInicio = MinutosAHora(minutoActual);
            minutoPorTurno[turnoAsignado] = minutoActual + candidato.DuracionSugeridaMinutos + DescansoMinutos;

            paradas.Add(new ParadaItinerario
            {
                Id = Guid.NewGuid(),
                ItinerarioId = Guid.Empty,
                LugarId = candidato.Id,
                Turno = turnoAsignado,
                OrdenParada = orden,
                HoraInicio = horaInicio,
                DuracionMinutos = candidato.DuracionSugeridaMinutos,
                DistanciaMetrosDesdeAnterior = seleccion.DistanciaMetros,
                CuadrasDesdeAnterior = seleccion.Cuadras,
                MinutosAPieDesdeAnterior = seleccion.MinutosAPie,
                OrigenParada = OrigenParada.Ia,
                FechaCreacion = DateTimeOffset.UtcNow
            });

            latAnterior = (double)candidato.Latitud;
            lonAnterior = (double)candidato.Longitud;
            turnoAnterior = turnoAsignado;
        }

        return paradas;
    }

    private Aproximacion? ElegirDelConjunto(
        IEnumerable<LugarCandidatoDto> candidatos,
        double latReferencia,
        double lonReferencia,
        short limiteCuadras)
        => candidatos
            .Select(c =>
            {
                var metros = _geo.DistanciaMetros(latReferencia, lonReferencia, (double)c.Latitud, (double)c.Longitud);
                return new Aproximacion(
                    c,
                    (int)Math.Round(metros),
                    (short)_geo.Cuadras(metros),
                    (short)_geo.MinutosAPie(metros));
            })
            .Where(a => a.Cuadras <= limiteCuadras)
            .OrderBy(a => a.DistanciaMetros)
            .FirstOrDefault();

    private static Aproximacion? ElegirSemilla(
        IReadOnlyList<LugarCandidatoDto> candidatos)
    {
        var elegido = candidatos
            .OrderBy(c => c.Id)
            .FirstOrDefault();

        return elegido is null ? null : new Aproximacion(elegido, null, null, null);
    }

    private static bool DentroDePresupuesto(NivelPresupuesto candidato, NivelPresupuesto tope)
        => candidato <= tope;

    private static int HoraBaseMinutos(Turno turno) => turno switch
    {
        Turno.Manana => 9 * 60,
        Turno.Tarde => 14 * 60,
        Turno.Noche => 19 * 60,
        _ => 9 * 60
    };

    private static TimeOnly MinutosAHora(int minutos)
    {
        var limitado = Math.Clamp(minutos, 0, UltimoMinutoDelDia);
        return new TimeOnly(limitado / 60, limitado % 60);
    }

    private static IReadOnlyList<Turno> TurnosSegunAlcance(AlcanceTemporal alcance) => alcance switch
    {
        AlcanceTemporal.DiaCompleto => new[] { Turno.Manana, Turno.Tarde, Turno.Noche },
        AlcanceTemporal.TardeNoche => new[] { Turno.Tarde, Turno.Noche },
        AlcanceTemporal.MananaAlmuerzo => new[] { Turno.Manana, Turno.Tarde },
        _ => new[] { Turno.Tarde }
    };

    private sealed record Aproximacion(
        LugarCandidatoDto Lugar,
        int? DistanciaMetros,
        short? Cuadras,
        short? MinutosAPie);
}
