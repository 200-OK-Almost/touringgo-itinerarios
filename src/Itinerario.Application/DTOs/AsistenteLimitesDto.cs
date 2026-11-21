namespace Itinerario.Application.DTOs;

public class AsistenteLimitesDto
{
    public int TopK { get; set; }
    public int MinResultados { get; set; }
    public int MaxMensajeCaracteres { get; set; }
    public int MaxHistorial { get; set; }
    public int RateLimitPorUsuario { get; set; }
    public int RateLimitPorViaje { get; set; }
    public int VentanaRateLimitSegundos { get; set; }
}
