using System.Globalization;
using Microsoft.Net.Http.Headers;

namespace Itinerario.API.RateLimiting;

public static class AsistenteRateLimitResponse
{
    public static async Task EscribirAsync(
        HttpContext context,
        TimeSpan retryAfter,
        CancellationToken cancellationToken)
    {
        var segundos = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));

        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.Headers[HeaderNames.RetryAfter] = segundos.ToString(CultureInfo.InvariantCulture);

        await context.Response.WriteAsJsonAsync(
            new { mensaje = "Demasiadas consultas al asistente. Intentá de nuevo en unos segundos." },
            cancellationToken);
    }
}
