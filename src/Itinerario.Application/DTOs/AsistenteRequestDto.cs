namespace Itinerario.Application.DTOs;

public class AsistenteRequestDto
{
    public string Mensaje { get; set; } = string.Empty;
    public short? NumeroDia { get; set; }
    public Guid? ReferenciaLugarId { get; set; }
    public IReadOnlyCollection<MensajeHistorialDto>? Historial { get; set; }
}
