using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs;

public class BosquejoViajeDto
{
    public string Titulo { get; set; } = string.Empty;
    public TipoViaje TipoViaje { get; set; }
    public BarrioCaba Barrio { get; set; }
    public AlcanceTemporal AlcanceTemporal { get; set; }
    public NivelPresupuesto NivelPresupuesto { get; set; }
    public RitmoCaminata RitmoCaminata { get; set; }
    public IReadOnlyCollection<string> CategoriaCodigos { get; set; } = new List<string>();
}
