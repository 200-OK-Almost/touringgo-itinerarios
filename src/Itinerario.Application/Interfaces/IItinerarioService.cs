using Itinerario.Application.DTOs;
using Itinerario.Domain.Enums;

namespace Itinerario.Application.Interfaces;

public interface IItinerarioService
{
    Task<IReadOnlyCollection<ItinerarioResponseDto>> GenerarAsync(Guid viajeId, Guid usuarioIdSolicitante, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ItinerarioResponseDto>> ObtenerAsync(Guid viajeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ItinerarioResponseDto>> RefinarAsync(Guid viajeId, TipoRefinamiento tipo, short? numeroDia, Guid usuarioIdSolicitante, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ItinerarioResponseDto>> SugerirTurnosLibresAsync(Guid viajeId, short? numeroDia, Guid usuarioIdSolicitante, CancellationToken cancellationToken = default);
}
