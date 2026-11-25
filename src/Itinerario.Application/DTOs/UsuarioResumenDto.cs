namespace Itinerario.Application.DTOs
{
    public class UsuarioResumenDto
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? FotoUrl { get; set; }
    }
}
