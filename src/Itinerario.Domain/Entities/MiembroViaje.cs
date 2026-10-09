using Itinerario.Domain.Enums;

namespace Itinerario.Domain.Entities;

public class MiembroViaje
{
    public Guid ViajeId { get; set; }
    public Guid UsuarioId { get; set; }
    public RolMiembro Rol { get; set; }
    public DateTimeOffset FechaIncorporacion { get; set; }

    public Viaje? Viaje { get; set; }
}
