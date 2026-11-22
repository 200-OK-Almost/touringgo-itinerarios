namespace Itinerario.API.RateLimiting;

public readonly record struct AsistenteRateLimitDecision(
    bool Permitido,
    TimeSpan RetryAfter);
