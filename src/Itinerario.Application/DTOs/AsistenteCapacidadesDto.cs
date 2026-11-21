using Itinerario.Application.Configuration;

namespace Itinerario.Application.DTOs;

public class AsistenteCapacidadesDto
{
    public bool Habilitado { get; set; }
    public AsistenteLimitesDto Limites { get; set; } = new();

    public static AsistenteCapacidadesDto Desde(AsistenteOptions options)
        => new()
        {
            Habilitado = options.Enabled,
            Limites = new AsistenteLimitesDto
            {
                TopK = options.TopK,
                MinResultados = options.MinResultados,
                MaxMensajeCaracteres = options.MaxMensajeCaracteres,
                MaxHistorial = options.MaxHistorial,
                RateLimitPorUsuario = options.RateLimit.PermitLimitPorUsuario,
                RateLimitPorViaje = options.RateLimit.PermitLimitPorViaje,
                VentanaRateLimitSegundos = options.RateLimit.WindowSeconds
            }
        };
}
