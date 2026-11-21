using Itinerario.API.RateLimiting;
using Itinerario.Application.Configuration;
using Itinerario.Application.DTOs;
using Itinerario.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Itinerario.API.Controllers;

[ApiController]
[Route("api/viajes")]
[Authorize]
public class ViajesController : ControllerBase
{
    private readonly IViajeService _service;
    private readonly IItinerarioService _itinerarioService;
    private readonly IInterpretacionService _interpretacionService;
    private readonly IAsistenteService _asistenteService;
    private readonly ICurrentUser _currentUser;
    private readonly IOptions<AsistenteOptions> _asistenteOptions;

    public ViajesController(
        IViajeService service,
        IItinerarioService itinerarioService,
        IInterpretacionService interpretacionService,
        IAsistenteService asistenteService,
        ICurrentUser currentUser,
        IOptions<AsistenteOptions> asistenteOptions)
    {
        _service = service;
        _itinerarioService = itinerarioService;
        _interpretacionService = interpretacionService;
        _asistenteService = asistenteService;
        _currentUser = currentUser;
        _asistenteOptions = asistenteOptions;
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

    [HttpPost("bosquejo")]
    [ProducesResponseType(typeof(BosquejoViajeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<BosquejoViajeDto>> InterpretarBosquejo(
        [FromBody] BosquejoRequestDto dto,
        CancellationToken cancellationToken)
        => Ok(await _interpretacionService.InterpretarAsync(dto.Texto, cancellationToken));

    [HttpGet]
    public async Task<ActionResult<ResultadoPaginadoDto<ViajeResponseDto>>> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = 20,
        CancellationToken cancellationToken = default)
        => Ok(await _service.ObtenerTodosAsync(_currentUser.UsuarioId, pagina, tamano, cancellationToken));

    [HttpGet("asistente/capacidades")]
    [EnableRateLimiting(AsistenteRateLimitPolicies.Usuario)]
    [ProducesResponseType(typeof(AsistenteCapacidadesDto), StatusCodes.Status200OK)]
    public ActionResult<AsistenteCapacidadesDto> ObtenerCapacidadesAsistente()
        => Ok(AsistenteCapacidadesDto.Desde(_asistenteOptions.Value));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ViajeResponseDto>> ObtenerPorId(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await _service.ObtenerPorIdAsync(id, _currentUser.UsuarioId, cancellationToken));

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

    [HttpPost("{id:guid}/itinerario")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ItinerarioResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IReadOnlyCollection<ItinerarioResponseDto>>> GenerarItinerario(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await _itinerarioService.GenerarAsync(id, _currentUser.UsuarioId, cancellationToken));

    [HttpGet("{id:guid}/itinerario")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ItinerarioResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<ItinerarioResponseDto>>> ObtenerItinerario(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await _itinerarioService.ObtenerAsync(id, _currentUser.UsuarioId, cancellationToken));

    [HttpPost("{id:guid}/asistente")]
    [EnableRateLimiting(AsistenteRateLimitPolicies.Viaje)]
    [ProducesResponseType(typeof(AsistenteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AsistenteResponseDto>> Asistente(
        Guid id,
        [FromBody] AsistenteRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (!_asistenteOptions.Value.Enabled)
        {
            return NotFound(new { mensaje = "El asistente no está habilitado." });
        }

        var respuesta = await _asistenteService.ResponderAsync(
            id,
            dto,
            _currentUser.UsuarioId,
            cancellationToken);

        return Ok(respuesta);
    }

    [HttpPost("{id:guid}/refinar")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ItinerarioResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IReadOnlyCollection<ItinerarioResponseDto>>> Refinar(
        Guid id,
        [FromBody] RefinamientoDto dto,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(dto.Texto))
        {
            return Ok(await _itinerarioService.RefinarPorTextoAsync(
                id, dto.Texto, dto.NumeroDia, _currentUser.UsuarioId, cancellationToken));
        }

        return Ok(await _itinerarioService.RefinarAsync(
            id, dto.Tipo, dto.NumeroDia, _currentUser.UsuarioId, cancellationToken));
    }

    [HttpPost("{id:guid}/sugerencias-turnos-libres")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ItinerarioResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IReadOnlyCollection<ItinerarioResponseDto>>> SugerirTurnosLibres(
        Guid id,
        [FromBody] SugerenciasTurnoLibreDto dto,
        CancellationToken cancellationToken)
        => Ok(await _itinerarioService.SugerirTurnosLibresAsync(id, dto.NumeroDia, _currentUser.UsuarioId, cancellationToken));
}
