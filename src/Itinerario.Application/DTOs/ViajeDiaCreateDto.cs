using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs;

public class ViajeDiaCreateDto
{
    public short NumeroDia { get; set; }
    public DateOnly? Fecha { get; set; }
    public BarrioCaba Barrio { get; set; }
    public TipoPuntoPartida TipoPuntoPartida { get; set; }
    public Guid? LugarPartidaId { get; set; }
    public string? NombrePuntoPartida { get; set; }
    public string? DireccionPartida { get; set; }
    public decimal? LatitudPartida { get; set; }
    public decimal? LongitudPartida { get; set; }
}
