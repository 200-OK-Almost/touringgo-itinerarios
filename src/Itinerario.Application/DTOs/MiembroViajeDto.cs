using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs
{
    public class MiembroViajeDto
    {
        public Guid UsuarioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? FotoUrl { get; set; }
        public RolMiembro Rol { get; set; }
        public DateTimeOffset FechaIncorporacion { get; set; }

    }
}
