using Itinerario.API.RateLimiting;
using Itinerario.Application.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Itinerario.UnitTests.RateLimiting;

public class AsistenteRateLimitStoreTests
{
    [Fact]
    public async Task AdquirirAsync_SinRedis_UsaMemoriaYPermiteHastaElLimite()
    {
        var tiempo = new TimeProviderFalso();
        var store = CrearStore(tiempo);

        var primero = await store.AdquirirAsync("usuario:1", 2, TimeSpan.FromSeconds(60));
        var segundo = await store.AdquirirAsync("usuario:1", 2, TimeSpan.FromSeconds(60));
        var tercero = await store.AdquirirAsync("usuario:1", 2, TimeSpan.FromSeconds(60));

        Assert.True(primero.Permitido);
        Assert.True(segundo.Permitido);
        Assert.False(tercero.Permitido);
        Assert.True(tercero.RetryAfter > TimeSpan.Zero);
    }

    [Fact]
    public async Task AdquirirAsync_ClavesDistintas_SonIndependientes()
    {
        var store = CrearStore(new TimeProviderFalso());

        var primero = await store.AdquirirAsync("usuario:1", 1, TimeSpan.FromSeconds(60));
        var segundo = await store.AdquirirAsync("usuario:2", 1, TimeSpan.FromSeconds(60));

        Assert.True(primero.Permitido);
        Assert.True(segundo.Permitido);
    }

    [Fact]
    public async Task AdquirirAsync_AlExpirarLaVentana_ReiniciaElContador()
    {
        var tiempo = new TimeProviderFalso();
        var store = CrearStore(tiempo);

        Assert.True((await store.AdquirirAsync("usuario:1", 1, TimeSpan.FromSeconds(60))).Permitido);
        Assert.False((await store.AdquirirAsync("usuario:1", 1, TimeSpan.FromSeconds(60))).Permitido);

        tiempo.Ahora = tiempo.Ahora.AddSeconds(61);

        Assert.True((await store.AdquirirAsync("usuario:1", 1, TimeSpan.FromSeconds(60))).Permitido);
    }

    private static RedisAsistenteRateLimitStore CrearStore(TimeProvider tiempo)
        => new(
            Options.Create(new AsistenteOptions
            {
                RateLimit = new AsistenteRateLimitOptions
                {
                    RedisConnectionString = null
                }
            }),
            NullLogger<RedisAsistenteRateLimitStore>.Instance,
            tiempo);

    private sealed class TimeProviderFalso : TimeProvider
    {
        public DateTimeOffset Ahora { get; set; } = DateTimeOffset.UnixEpoch;

        public override DateTimeOffset GetUtcNow() => Ahora;
    }
}
