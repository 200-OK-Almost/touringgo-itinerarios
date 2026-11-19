using Itinerario.Domain.Enums;

namespace Itinerario.Domain.Entities;

public class ViajeDia
{
    public Guid Id { get; set; }
    public Guid ViajeId { get; set; }
    public short NumeroDia { get; set; }
    public DateOnly? Fecha { get; set; }
    public BarrioCaba Barrio { get; set; }
    public TipoPuntoPartida TipoPuntoPartida { get; set; }
    public Guid? LugarPartidaId { get; set; }
    public string? NombrePuntoPartida { get; set; }
    public string? DireccionPartida { get; set; }
    public decimal? LatitudPartida { get; set; }
    public decimal? LongitudPartida { get; set; }
    public TipoViaje? TipoViaje { get; set; }

    public Viaje? Viaje { get; set; }
    public ICollection<EtiquetaViajeDia> Etiquetas { get; set; } = new List<EtiquetaViajeDia>();
}
