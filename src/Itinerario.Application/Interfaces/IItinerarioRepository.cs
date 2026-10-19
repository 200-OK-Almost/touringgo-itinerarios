using ItinerarioEntity = Itinerario.Domain.Entities.Itinerario;

namespace Itinerario.Application.Interfaces;

public interface IItinerarioRepository
{
    Task<IReadOnlyCollection<ItinerarioEntity>> ObtenerPorViajeAsync(Guid viajeId, CancellationToken cancellationToken = default);
    Task ReemplazarAsync(Guid viajeId, IReadOnlyCollection<ItinerarioEntity> itinerarios, CancellationToken cancellationToken = default);
    Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default);
}
