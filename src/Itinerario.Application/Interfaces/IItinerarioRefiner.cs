using Itinerario.Application.DTOs;
using Itinerario.Domain.Entities;
using Itinerario.Domain.Enums;
using ItinerarioEntity = Itinerario.Domain.Entities.Itinerario;

namespace Itinerario.Application.Interfaces;

public interface IItinerarioRefiner
{
    void Refinar(
        TipoRefinamiento tipo,
        ItinerarioEntity itinerario,
        IReadOnlyDictionary<Guid, LugarCandidatoDto> lugaresActuales,
        ICollection<LugarCandidatoDto> candidatos,
        Viaje viaje);

    IReadOnlyCollection<ParadaItinerario> SugerirTurnosLibres(
        ItinerarioEntity itinerario,
        ICollection<LugarCandidatoDto> candidatos,
        Viaje viaje);
}
