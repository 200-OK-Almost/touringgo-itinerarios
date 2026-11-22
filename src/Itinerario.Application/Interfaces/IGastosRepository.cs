using Itinerario.Application;
using Itinerario.Application.DTOs.Gastos;
using Itinerario.Domain.Entities;
using Itinerario.Domain.Enums;

public interface IGastosRepository
{
    Task<Gasto?> ObtenerPorIdAsync(
        Guid viajeId,
        Guid gastoId,
        CancellationToken ct = default);

    Task<ResultadoPaginado<Gasto>> ListarAsync(
        Guid viajeId,
        FiltroGastosDto filtro,
        CancellationToken ct = default);

    Task<HashSet<Guid>> ObtenerMiembrosExistentesAsync(
        Guid viajeId,
        IEnumerable<Guid> usuarioIds,
        CancellationToken ct = default);

    Task<Dictionary<Guid, decimal>> ObtenerTotalPagadoPendienteAsync(
        Guid viajeId,
        TipoMoneda moneda,
        CancellationToken ct = default);

    Task<Dictionary<Guid, decimal>> ObtenerTotalAsignadoPendienteAsync(
        Guid viajeId,
        TipoMoneda moneda,
        CancellationToken ct = default);

    Task<List<Gasto>> ObtenerPendientesParaLiquidarAsync(
        Guid viajeId,
        IEnumerable<Guid> gastoIds,
        CancellationToken ct = default);

    void Agregar(Gasto gasto);
    void AgregarLiquidacion(Liquidacion liquidacion);
    void Eliminar(Gasto gasto);
    Task GuardarCambiosAsync(CancellationToken cancellationToken = default);

}