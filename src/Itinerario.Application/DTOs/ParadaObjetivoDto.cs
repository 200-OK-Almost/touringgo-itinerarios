using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs;

public class ParadaObjetivoDto
{
    public Guid ParadaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public Turno Turno { get; set; }
}
