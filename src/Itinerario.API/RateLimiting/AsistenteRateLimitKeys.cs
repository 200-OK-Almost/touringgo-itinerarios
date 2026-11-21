namespace Itinerario.API.RateLimiting;

public static class AsistenteRateLimitKeys
{
    public static string Usuario(string? usuarioId)
        => string.IsNullOrWhiteSpace(usuarioId) ? "anonimo" : usuarioId;

    public static string Viaje(string usuarioId, string? viajeId)
        => $"{Usuario(usuarioId)}:{(string.IsNullOrWhiteSpace(viajeId) ? "global" : viajeId)}";
}
