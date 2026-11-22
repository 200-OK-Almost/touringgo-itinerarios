using Itinerario.Application.DTOs;

namespace Itinerario.Application.Interfaces;

public interface IIntencionLlmService
{
    Task<IntencionLlmCruda?> InterpretarAsync(
        IntencionLlmContextoDto contexto,
        CancellationToken cancellationToken = default);
}
