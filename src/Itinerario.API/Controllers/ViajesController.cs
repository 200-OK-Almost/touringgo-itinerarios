using Itinerario.Application.DTOs;
using Itinerario.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Itinerario.API.Controllers;

[ApiController]
[Route("api/viajes")]
public class ViajesController : ControllerBase
{
    private readonly IViajeService _service;
    private readonly ICurrentUser _currentUser;

    public ViajesController(IViajeService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ViajeResponseDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ViajeResponseDto>> Crear(
        [FromBody] ViajeCreateDto dto,
        CancellationToken cancellationToken)
    {
        var viaje = await _service.CrearAsync(dto, _currentUser.UsuarioId, cancellationToken);

        return CreatedAtAction(nameof(ObtenerPorId), new { id = viaje.Id }, viaje);
    }

    [HttpGet]
    public async Task<ActionResult<ResultadoPaginadoDto<ViajeResponseDto>>> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = 20,
        CancellationToken cancellationToken = default)
        => Ok(await _service.ObtenerTodosAsync(pagina, tamano, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ViajeResponseDto>> ObtenerPorId(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await _service.ObtenerPorIdAsync(id, cancellationToken));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ViajeResponseDto>> Actualizar(
        Guid id,
        [FromBody] ViajeUpdateDto dto,
        CancellationToken cancellationToken)
        => Ok(await _service.ActualizarAsync(id, dto, _currentUser.UsuarioId, cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        await _service.EliminarAsync(id, _currentUser.UsuarioId, cancellationToken);

        return NoContent();
    }

    [HttpPost("unirse")]
    public async Task<ActionResult<ViajeResponseDto>> Unirse(
        [FromBody] UnirseViajeDto dto,
        CancellationToken cancellationToken)
        => Ok(await _service.UnirseAsync(dto.CodigoInvitacion, _currentUser.UsuarioId, cancellationToken));
}
