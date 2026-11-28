using Itinerario.Application.DTOs;

namespace Itinerario.Application.Interfaces;

public interface IViajeRecomendadoService
{
    Task<ViajeResponseDto> CrearAsync(
        CrearViajeRecomendadoDto dto,
        Guid usuarioId,
        CancellationToken cancellationToken = default);
}
