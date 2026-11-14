using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs;

public class LugarCandidatoDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public BarrioCaba Barrio { get; set; }
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public NivelPresupuesto NivelPrecio { get; set; }
    public bool EsAireLibre { get; set; }
    public short DuracionSugeridaMinutos { get; set; }
}
