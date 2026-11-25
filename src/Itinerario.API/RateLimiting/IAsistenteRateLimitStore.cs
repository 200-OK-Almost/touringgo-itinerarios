namespace Itinerario.API.RateLimiting;

public interface IAsistenteRateLimitStore
{
    Task<AsistenteRateLimitDecision> AdquirirAsync(
        string clave,
        int limite,
        TimeSpan ventana,
        CancellationToken cancellationToken = default);
}
