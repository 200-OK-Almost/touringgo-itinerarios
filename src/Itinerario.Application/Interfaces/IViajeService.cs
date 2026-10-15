using Itinerario.Application.DTOs;

namespace Itinerario.Application.Interfaces;

public interface IViajeService
{
    Task<ViajeResponseDto> CrearAsync(ViajeCreateDto dto, CancellationToken cancellationToken = default);
    Task<ViajeResponseDto> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ResultadoPaginadoDto<ViajeResponseDto>> ObtenerTodosAsync(int pagina = 1, int tamano = 20, CancellationToken cancellationToken = default);
    Task<ViajeResponseDto> ActualizarAsync(Guid id, ViajeUpdateDto dto, Guid usuarioIdSolicitante, CancellationToken cancellationToken = default);
    Task EliminarAsync(Guid id, Guid usuarioIdSolicitante, CancellationToken cancellationToken = default);
    Task<ViajeResponseDto> UnirseAsync(string codigoInvitacion, Guid usuarioId, CancellationToken cancellationToken = default);
}
