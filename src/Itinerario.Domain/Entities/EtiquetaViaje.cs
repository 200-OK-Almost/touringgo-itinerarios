namespace Itinerario.Domain.Entities;

public class EtiquetaViaje
{
    public Guid ViajeId { get; set; }
    public short CategoriaId { get; set; }

    public Viaje? Viaje { get; set; }
}
