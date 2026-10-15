using Itinerario.Application.DTOs;
using Itinerario.Domain.Enums;

namespace Itinerario.Application.Interfaces;

public interface ILugaresClient
{
    Task<LugarCandidatoDto?> ObtenerPorIdAsync(Guid lugarId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<LugarCandidatoDto>> BuscarAsync(
        BarrioCaba barrio,
        IReadOnlyCollection<short> categoriaIds,
        CancellationToken cancellationToken = default);
}
