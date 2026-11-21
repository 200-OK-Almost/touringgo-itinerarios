using System.Text;
using Itinerario.API.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace Itinerario.UnitTests.RateLimiting;

public class AsistenteRateLimitTests
{
    [Fact]
    public void Usuario_ConValor_DevuelveElMismoIdentificador()
    {
        Assert.Equal("usuario-1", AsistenteRateLimitKeys.Usuario("usuario-1"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Usuario_SinValor_DevuelveAnonimo(string? usuarioId)
    {
        Assert.Equal("anonimo", AsistenteRateLimitKeys.Usuario(usuarioId));
    }

    [Fact]
    public void Viaje_ConViaje_DevuelveClaveCompuesta()
    {
        Assert.Equal("usuario-1:viaje-1", AsistenteRateLimitKeys.Viaje("usuario-1", "viaje-1"));
    }

    [Fact]
    public void Viaje_SinViaje_UsaGlobal()
    {
        Assert.Equal("usuario-1:global", AsistenteRateLimitKeys.Viaje("usuario-1", null));
    }

    [Fact]
    public async Task EscribirAsync_Devuelve429ConRetryAfterYMensaje()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await AsistenteRateLimitResponse.EscribirAsync(
            context,
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
        Assert.Equal("30", context.Response.Headers["Retry-After"].ToString());
        Assert.StartsWith("application/json", context.Response.ContentType);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        Assert.Contains("Demasiadas consultas", body);
    }

    [Fact]
    public async Task EscribirAsync_RedondeaRetryAfterHaciaArriba()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await AsistenteRateLimitResponse.EscribirAsync(
            context,
            TimeSpan.FromMilliseconds(1500),
            CancellationToken.None);

        Assert.Equal("2", context.Response.Headers["Retry-After"].ToString());
    }
}
