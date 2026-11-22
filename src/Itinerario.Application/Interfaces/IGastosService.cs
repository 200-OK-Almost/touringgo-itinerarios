using Itinerario.Application.DTOs;
using Itinerario.Application.DTOs.Gastos;
using Itinerario.Domain.Enums;

public interface IGastoService
{
    Task<GastoDto> CrearAsync(
        Guid viajeId,
        Guid usuarioActualId,
        CrearGastoRequestDto request,
        CancellationToken ct = default);

    Task<GastoDto> ObtenerPorIdAsync(
        Guid viajeId,
        Guid gastoId,
        Guid usuarioActualId,
        CancellationToken ct = default);

    Task<ResultadoPaginadoDto<GastoDto>> ListarAsync(
        Guid viajeId,
        Guid usuarioActualId,
        FiltroGastosDto filtro,
        CancellationToken ct = default);

    Task<GastoDto> ActualizarAsync(
        Guid viajeId,
        Guid gastoId,
        Guid usuarioActualId,
        ActualizarGastoRequestDto request,
        CancellationToken ct = default);

    Task EliminarAsync(
        Guid viajeId,
        Guid gastoId,
        Guid usuarioActualId,
        CancellationToken ct = default);

    Task<BalanceGastosDto> ObtenerBalanceAsync(
        Guid viajeId,
        Guid usuarioActualId,
        TipoMoneda moneda,
        CancellationToken ct = default);

    Task<LiquidacionDto> LiquidarAsync(
        Guid viajeId,
        Guid usuarioActualId,
        LiquidarGastosRequestDto request,
        CancellationToken ct = default);
}