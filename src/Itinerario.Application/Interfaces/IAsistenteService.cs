using Itinerario.Application.DTOs;

namespace Itinerario.Application.Interfaces;

public interface IAsistenteService
{
    Task<AsistenteResponseDto> ResponderAsync(
        Guid viajeId,
        AsistenteRequestDto request,
        Guid usuarioIdSolicitante,
        CancellationToken cancellationToken = default);
}
