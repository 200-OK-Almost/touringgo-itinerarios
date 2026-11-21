using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs;

public class ParadaResolubleDto
{
    public Guid ParadaId { get; set; }
    public Guid LugarId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public Turno Turno { get; set; }
    public short OrdenParada { get; set; }
    public IReadOnlyCollection<string> Categorias { get; set; } = new List<string>();
}
