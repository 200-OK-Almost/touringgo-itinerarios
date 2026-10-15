using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs;

public class ViajeUpdateDto
{
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
    public IReadOnlyCollection<short> CategoriaIds { get; set; } = new List<short>();
    public ParametrosFamiliaDto? ParametrosFamilia { get; set; }
}
