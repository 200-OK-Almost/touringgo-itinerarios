using Itinerario.Application.DTOs;

namespace Itinerario.Application.Interfaces;

public interface IMensajeLlmService
{
    Task<string?> GenerarAsync(
        MensajeLlmContextoDto contexto,
        CancellationToken cancellationToken = default);
}
