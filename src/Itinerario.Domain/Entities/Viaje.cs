using Itinerario.Domain.Enums;

namespace Itinerario.Domain.Entities;

public class Viaje
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public TipoViaje TipoViaje { get; set; }
    public EstadoViaje Estado { get; set; }
    public BarrioCaba Barrio { get; set; }
    public AlcanceTemporal AlcanceTemporal { get; set; }
    public DateOnly? FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public NivelPresupuesto NivelPresupuesto { get; set; }
    public RitmoCaminata RitmoCaminata { get; set; }
    public short? LimiteCuadrasEntreParadas { get; set; }
    public TipoPuntoPartida TipoPuntoPartida { get; set; }
    public Guid? LugarPartidaId { get; set; }
    public string? NombrePuntoPartida { get; set; }
    public string? DireccionPartida { get; set; }
    public decimal? LatitudPartida { get; set; }
    public decimal? LongitudPartida { get; set; }
    public string CodigoInvitacion { get; set; } = string.Empty;
    public Guid CreadoPorUsuarioId { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
    public DateTimeOffset FechaActualizacion { get; set; }

    public ParametrosFamilia? ParametrosFamilia { get; set; }
    public ICollection<MiembroViaje> Miembros { get; set; } = new List<MiembroViaje>();
    public ICollection<EtiquetaViaje> Etiquetas { get; set; } = new List<EtiquetaViaje>();
    public ICollection<Itinerario> Itinerarios { get; set; } = new List<Itinerario>();
}
