using Itinerario.Application.DTOs;

namespace Itinerario.Application.Interfaces;

public interface IItinerarioService
{
    Task<IReadOnlyCollection<ItinerarioResponseDto>> GenerarAsync(Guid viajeId, Guid usuarioIdSolicitante, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ItinerarioResponseDto>> ObtenerAsync(Guid viajeId, CancellationToken cancellationToken = default);
}
