using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs;

public class ItinerarioResponseDto
{
    public Guid Id { get; set; }
    public Guid ViajeId { get; set; }
    public short NumeroDia { get; set; }
    public DateOnly? Fecha { get; set; }
    public TipoPuntoPartida OrigenTipo { get; set; }
    public string? OrigenDescripcion { get; set; }
    public decimal? OrigenLatitud { get; set; }
    public decimal? OrigenLongitud { get; set; }
    public IReadOnlyCollection<ParadaItinerarioDto> Paradas { get; set; } = new List<ParadaItinerarioDto>();
}
