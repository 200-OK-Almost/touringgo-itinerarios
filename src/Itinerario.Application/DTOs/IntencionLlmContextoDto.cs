using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs;

public class IntencionLlmContextoDto
{
    public Guid ViajeId { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public short? NumeroDia { get; set; }
    public BarrioCaba Barrio { get; set; }
    public NivelPresupuesto NivelPresupuesto { get; set; }
    public IReadOnlyCollection<string> CategoriasViaje { get; set; } = new List<string>();
    public bool AptoNinos { get; set; }
    public bool AccesibleMovilidadReducida { get; set; }
    public IReadOnlyCollection<ParadaResolubleDto> Paradas { get; set; } = new List<ParadaResolubleDto>();
}
