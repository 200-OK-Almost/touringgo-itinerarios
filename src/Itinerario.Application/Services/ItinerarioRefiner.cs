using Itinerario.Application.DTOs;
using Itinerario.Application.Interfaces;
using Itinerario.Domain.Entities;
using Itinerario.Domain.Enums;
using ItinerarioEntity = Itinerario.Domain.Entities.Itinerario;

namespace Itinerario.Application.Services;

public class ItinerarioRefiner : IItinerarioRefiner
{
    private readonly IGeoCalculator _geo;

    public ItinerarioRefiner(IGeoCalculator geo)
    {
        _geo = geo;
    }

    public void Refinar(
        TipoRefinamiento tipo,
        ItinerarioEntity itinerario,
        IReadOnlyDictionary<Guid, LugarCandidatoDto> lugaresActuales,
        ICollection<LugarCandidatoDto> candidatos,
        Viaje viaje)
    {
        var paradas = itinerario.Paradas
            .OrderBy(p => p.Turno)
            .ThenBy(p => p.OrdenParada)
            .ToList();

        foreach (var parada in paradas)
        {
            if (!lugaresActuales.TryGetValue(parada.LugarId, out var actual))
            {
                continue;
            }

            var referencia = ReferenciaPara(parada, paradas, itinerario, viaje, lugaresActuales);
            var reemplazo = tipo switch
            {
                TipoRefinamiento.MasEconomico => ElegirMasEconomico(parada, actual, candidatos),
                TipoRefinamiento.ReducirCaminata => ElegirMasCercano(parada, actual, candidatos, referencia),
                TipoRefinamiento.MasAireLibre => ElegirAireLibre(parada, actual, candidatos, referencia),
                _ => null
            };

            if (reemplazo is null)
            {
                continue;
            }

            candidatos.Remove(reemplazo);
            candidatos.Add(actual);

            AplicarReemplazo(parada, reemplazo, referencia);
        }
    }

    public IReadOnlyCollection<ParadaItinerario> SugerirTurnosLibres(
        ItinerarioEntity itinerario,
        ICollection<LugarCandidatoDto> candidatos,
        Viaje viaje)
    {
        var turnosEsperados = TurnosSegunAlcance(viaje.AlcanceTemporal);
        var turnosOcupados = new HashSet<Turno>(itinerario.Paradas.Select(p => p.Turno));
        var sugerencias = new List<ParadaItinerario>();

        foreach (var turno in turnosEsperados)
        {
            if (turnosOcupados.Contains(turno))
            {
                continue;
            }

            var candidato = candidatos
                .Where(c => c.Turno == turno)
                .Where(c => DentroDePresupuesto(c.NivelPrecio, viaje.NivelPresupuesto))
                .OrderBy(c => c.NivelPrecio)
                .ThenBy(c => c.Id)
                .FirstOrDefault();

            if (candidato is null)
            {
                continue;
            }

            candidatos.Remove(candidato);
            sugerencias.Add(CrearSugerencia(itinerario, candidato));
        }

        return sugerencias;
    }

    private LugarCandidatoDto? ElegirMasEconomico(
        ParadaItinerario parada,
        LugarCandidatoDto actual,
        ICollection<LugarCandidatoDto> candidatos)
        => candidatos
            .Where(c => c.Turno == parada.Turno)
            .Where(c => c.NivelPrecio < actual.NivelPrecio)
            .OrderBy(c => c.NivelPrecio)
            .ThenBy(c => c.Id)
            .FirstOrDefault();

    private LugarCandidatoDto? ElegirMasCercano(
        ParadaItinerario parada,
        LugarCandidatoDto actual,
        ICollection<LugarCandidatoDto> candidatos,
        (double lat, double lon)? referencia)
    {
        if (referencia is null)
        {
            return null;
        }

        var distanciaActual = Distancia(actual, referencia.Value);

        return candidatos
            .Where(c => c.Turno == parada.Turno)
            .Select(c => new { Candidato = c, Distancia = Distancia(c, referencia.Value) })
            .Where(x => x.Distancia < distanciaActual)
            .OrderBy(x => x.Distancia)
            .ThenBy(x => x.Candidato.Id)
            .Select(x => x.Candidato)
            .FirstOrDefault();
    }

    private LugarCandidatoDto? ElegirAireLibre(
        ParadaItinerario parada,
        LugarCandidatoDto actual,
        ICollection<LugarCandidatoDto> candidatos,
        (double lat, double lon)? referencia)
    {
        if (actual.EsAireLibre)
        {
            return null;
        }

        var aireLibre = candidatos
            .Where(c => c.Turno == parada.Turno && c.EsAireLibre)
            .ToList();

        if (aireLibre.Count == 0)
        {
            return null;
        }

        if (referencia is null)
        {
            return aireLibre.OrderBy(c => c.Id).First();
        }

        return aireLibre
            .Select(c => new { Candidato = c, Distancia = Distancia(c, referencia.Value) })
            .OrderBy(x => x.Distancia)
            .ThenBy(x => x.Candidato.Id)
            .Select(x => x.Candidato)
            .FirstOrDefault();
    }

    private void AplicarReemplazo(
        ParadaItinerario parada,
        LugarCandidatoDto reemplazo,
        (double lat, double lon)? referencia)
    {
        parada.LugarId = reemplazo.Id;
        parada.DuracionMinutos = reemplazo.DuracionSugeridaMinutos;
        parada.OrigenParada = OrigenParada.Refinamiento;

        if (referencia is not null)
        {
            var metros = Distancia(reemplazo, referencia.Value);
            parada.DistanciaMetrosDesdeAnterior = (int)Math.Round(metros);
            parada.CuadrasDesdeAnterior = (short)_geo.Cuadras(metros);
            parada.MinutosAPieDesdeAnterior = (short)_geo.MinutosAPie(metros);
        }

        // v1: no recalcula HoraInicio.
    }

    private static ParadaItinerario CrearSugerencia(ItinerarioEntity itinerario, LugarCandidatoDto candidato)
        => new()
        {
            Id = Guid.NewGuid(),
            ItinerarioId = itinerario.Id,
            LugarId = candidato.Id,
            Turno = candidato.Turno,
            OrdenParada = 1,
            DuracionMinutos = candidato.DuracionSugeridaMinutos,
            OrigenParada = OrigenParada.SugerenciaHueco,
            FechaCreacion = DateTimeOffset.UtcNow
        };

    private (double lat, double lon)? ReferenciaPara(
        ParadaItinerario parada,
        IList<ParadaItinerario> paradasOrdenadas,
        ItinerarioEntity itinerario,
        Viaje viaje,
        IReadOnlyDictionary<Guid, LugarCandidatoDto> lugaresActuales)
    {
        var indice = paradasOrdenadas.IndexOf(parada);
        if (indice > 0)
        {
            var anterior = paradasOrdenadas[indice - 1];
            if (lugaresActuales.TryGetValue(anterior.LugarId, out var lugarAnterior))
            {
                return ((double)lugarAnterior.Latitud, (double)lugarAnterior.Longitud);
            }
        }

        return Origen(itinerario, viaje);
    }

    private static (double lat, double lon)? Origen(ItinerarioEntity itinerario, Viaje viaje)
    {
        if (itinerario.OrigenLatitud.HasValue && itinerario.OrigenLongitud.HasValue)
        {
            return ((double)itinerario.OrigenLatitud.Value, (double)itinerario.OrigenLongitud.Value);
        }

        if (viaje.LatitudPartida.HasValue && viaje.LongitudPartida.HasValue)
        {
            return ((double)viaje.LatitudPartida.Value, (double)viaje.LongitudPartida.Value);
        }

        return null;
    }

    private double Distancia(LugarCandidatoDto lugar, (double lat, double lon) referencia)
        => _geo.DistanciaMetros((double)lugar.Latitud, (double)lugar.Longitud, referencia.lat, referencia.lon);

    private static bool DentroDePresupuesto(NivelPresupuesto candidato, NivelPresupuesto tope)
        => candidato <= tope;

    private static IReadOnlyList<Turno> TurnosSegunAlcance(AlcanceTemporal alcance) => alcance switch
    {
        AlcanceTemporal.DiaCompleto => new[] { Turno.Manana, Turno.Tarde, Turno.Noche },
        AlcanceTemporal.TardeNoche => new[] { Turno.Tarde, Turno.Noche },
        AlcanceTemporal.MananaAlmuerzo => new[] { Turno.Manana, Turno.Tarde },
        _ => new[] { Turno.Tarde }
    };
}
