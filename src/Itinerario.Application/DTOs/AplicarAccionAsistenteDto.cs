namespace Itinerario.Application.DTOs;

public class AplicarAccionAsistenteDto
{
    public string Tipo { get; set; } = string.Empty;
    public short NumeroDia { get; set; }
    public Guid LugarId { get; set; }
    public Guid? ParadaId { get; set; }
    public string? Turno { get; set; }
    public string Version { get; set; } = string.Empty;
}
