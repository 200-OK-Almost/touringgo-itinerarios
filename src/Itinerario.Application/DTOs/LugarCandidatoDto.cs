using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs;

public class LugarCandidatoDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public Turno Turno { get; set; }
    public short CategoriaId { get; set; }
    public NivelPresupuesto NivelPrecio { get; set; }
    public bool EsAireLibre { get; set; }
    public short DuracionSugeridaMinutos { get; set; }
}
