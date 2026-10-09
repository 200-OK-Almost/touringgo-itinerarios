using Itinerario.Domain.Enums;

namespace Itinerario.Domain.Entities;

public class Itinerario
{
    public Guid Id { get; set; }
    public Guid ViajeId { get; set; }
    public short NumeroDia { get; set; }
    public DateOnly? Fecha { get; set; }
    public TipoPuntoPartida OrigenTipo { get; set; }
    public string? OrigenDescripcion { get; set; }
    public decimal? OrigenLatitud { get; set; }
    public decimal? OrigenLongitud { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
    public DateTimeOffset FechaActualizacion { get; set; }

    public Viaje? Viaje { get; set; }
    public ICollection<ParadaItinerario> Paradas { get; set; } = new List<ParadaItinerario>();
}
