using Itinerario.Application.DTOs;

namespace Itinerario.Application.Interfaces;

public interface IUsuariosClient
{
    Task<PerfilViajeroDto> ObtenerPerfilAsync(CancellationToken cancellationToken = default);
}
