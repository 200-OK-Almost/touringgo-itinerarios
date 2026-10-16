using Itinerario.Domain.Entities;

namespace Itinerario.Application.Interfaces;

public interface IViajeRepository
{
    Task<Viaje?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ResultadoPaginado<Viaje>> ObtenerPaginadoAsync(int pagina, int tamano, CancellationToken cancellationToken = default);
    Task<Viaje?> ObtenerPorCodigoInvitacionAsync(string codigoInvitacion, CancellationToken cancellationToken = default);
    Task<bool> ExisteCodigoInvitacionAsync(string codigoInvitacion, CancellationToken cancellationToken = default);
    Task AgregarAsync(Viaje viaje, CancellationToken cancellationToken = default);
    void Actualizar(Viaje viaje);
    void Eliminar(Viaje viaje);
    void Descartar(Viaje viaje);
    Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default);
}
