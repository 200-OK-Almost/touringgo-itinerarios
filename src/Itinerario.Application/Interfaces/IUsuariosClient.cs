using Itinerario.Application.DTOs;

namespace Itinerario.Application.Interfaces;

public interface IUsuariosClient
{
    Task<PerfilViajeroDto> ObtenerPerfilAsync(CancellationToken cancellationToken = default);
    Task<List<UsuarioResumenDto>> ObtenerResumenesAsync(
        IEnumerable<Guid> usuariosIds,
        CancellationToken cancellationToken = default);
}
