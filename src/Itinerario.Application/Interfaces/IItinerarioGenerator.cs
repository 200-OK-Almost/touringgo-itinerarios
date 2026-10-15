using Itinerario.Application.DTOs;
using Itinerario.Domain.Entities;

namespace Itinerario.Application.Interfaces;

public interface IItinerarioGenerator
{
    IReadOnlyCollection<ParadaItinerario> Generar(
        Viaje viaje,
        IReadOnlyCollection<LugarCandidatoDto> lugares);
}
