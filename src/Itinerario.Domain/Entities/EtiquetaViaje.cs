namespace Itinerario.Domain.Entities;

public class EtiquetaViaje
{
    public Guid ViajeId { get; set; }
    public string CategoriaCodigo { get; set; } = string.Empty;

    public Viaje? Viaje { get; set; }
}
