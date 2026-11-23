using Itinerario.Application.DTOs;
using Itinerario.Application.DTOs.Gastos;
using Itinerario.Application.Interfaces;
using Itinerario.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Authorize]
[Route("api/viajes/{viajeId:guid}/gastos")]
public sealed class GastosController : ControllerBase
{
    private readonly IGastosService _gastoService;
    private readonly ICurrentUser _currentUser;

    public GastosController(IGastosService gastoService, ICurrentUser currentUser)
    {
        _gastoService = gastoService;
        _currentUser = currentUser;

    }

    // GET /api/viajes/{viajeId}/gastos
    [HttpGet]
    [ProducesResponseType(
        typeof(ResultadoPaginadoDto<GastoDto>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultadoPaginadoDto<GastoDto>>> Listar(
        [FromRoute] Guid viajeId,
        [FromQuery] FiltroGastosDto filtro,
        CancellationToken ct)
    {
        var resultado = await _gastoService.ListarAsync(
            viajeId,
            _currentUser.UsuarioId,
            filtro,
            ct);

        return Ok(resultado);
    }

    // GET /api/viajes/{viajeId}/gastos/{gastoId}
    [HttpGet("{gastoId:guid}", Name = nameof(ObtenerPorId))]
    [ProducesResponseType(typeof(GastoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GastoDto>> ObtenerPorId(
        [FromRoute] Guid viajeId,
        [FromRoute] Guid gastoId,
        CancellationToken ct)
    {
        var gasto = await _gastoService.ObtenerPorIdAsync(
            viajeId,
            gastoId,
            _currentUser.UsuarioId,
            ct);

        return Ok(gasto);
    }

    // POST /api/viajes/{viajeId}/gastos
    [HttpPost]
    [ProducesResponseType(typeof(GastoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GastoDto>> Crear(
        [FromRoute] Guid viajeId,
        [FromBody] CrearGastoRequestDto request,
        CancellationToken ct)
    {

        var gasto = await _gastoService.CrearAsync(
            viajeId,
            _currentUser.UsuarioId,
            request,
            ct);

        return CreatedAtAction(
            nameof(ObtenerPorId),
            new
            {
                viajeId,
                gastoId = gasto.Id
            },
            gasto);
    }

    // PUT /api/viajes/{viajeId}/gastos/{gastoId}
    [HttpPut("{gastoId:guid}")]
    [ProducesResponseType(typeof(GastoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GastoDto>> Actualizar(
        [FromRoute] Guid viajeId,
        [FromRoute] Guid gastoId,
        [FromBody] ActualizarGastoRequestDto request,
        CancellationToken ct)
    {
        var gasto = await _gastoService.ActualizarAsync(
            viajeId,
            gastoId,
            _currentUser.UsuarioId,
            request,
            ct);

        return Ok(gasto);
    }

    // DELETE /api/viajes/{viajeId}/gastos/{gastoId}
    [HttpDelete("{gastoId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(
        [FromRoute] Guid viajeId,
        [FromRoute] Guid gastoId,
        CancellationToken ct)
    {
        await _gastoService.EliminarAsync(
            viajeId,
            gastoId,
            _currentUser.UsuarioId,
            ct);

        return NoContent();
    }

    // GET /api/viajes/{viajeId}/gastos/balance?moneda=ARS
    [HttpGet("balance")]
    [ProducesResponseType(
        typeof(BalanceGastosDto),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<BalanceGastosDto>> ObtenerBalance(
        [FromRoute] Guid viajeId,
        [FromQuery] TipoMoneda moneda,
        CancellationToken ct)
    {
        var balance = await _gastoService.ObtenerBalanceAsync(
            viajeId,
            _currentUser.UsuarioId,
            moneda,
            ct);

        return Ok(balance);
    }

    // POST /api/viajes/{viajeId}/gastos/liquidaciones
    [HttpPost("liquidaciones")]
    [ProducesResponseType(
        typeof(LiquidacionDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LiquidacionDto>> Liquidar(
        [FromRoute] Guid viajeId,
        [FromBody] LiquidarGastosRequestDto request,
        CancellationToken ct)
    {

        var liquidacion = await _gastoService.LiquidarAsync(
            viajeId,
            _currentUser.UsuarioId,
            request,
            ct);

        return Ok(liquidacion);
    }
}