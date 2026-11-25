using System.Collections.Concurrent;
using Itinerario.Application.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Itinerario.API.RateLimiting;

public sealed class RedisAsistenteRateLimitStore : IAsistenteRateLimitStore
{
    private const string PrefijoClave = "asistente:rate-limit:";

    private const string ScriptIncrementar = """
        local actual = redis.call('INCR', KEYS[1])
        if actual == 1 then
          redis.call('PEXPIRE', KEYS[1], ARGV[1])
        end
        local ttl = redis.call('PTTL', KEYS[1])
        return {actual, ttl}
        """;

    private readonly ILogger<RedisAsistenteRateLimitStore> _logger;
    private readonly AsistenteOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly Lazy<IConnectionMultiplexer?> _redis;
    private readonly ConcurrentDictionary<string, ContadorMemoria> _memoria = new();

    public RedisAsistenteRateLimitStore(
        IOptions<AsistenteOptions> options,
        ILogger<RedisAsistenteRateLimitStore> logger,
        TimeProvider timeProvider)
    {
        _options = options.Value;
        _logger = logger;
        _timeProvider = timeProvider;
        _redis = new Lazy<IConnectionMultiplexer?>(CrearConexion);
    }

    public async Task<AsistenteRateLimitDecision> AdquirirAsync(
        string clave,
        int limite,
        TimeSpan ventana,
        CancellationToken cancellationToken = default)
    {
        var redis = _redis.Value;

        if (redis is not null && redis.IsConnected)
        {
            try
            {
                return await AdquirirEnRedisAsync(
                    redis,
                    clave,
                    limite,
                    ventana,
                    cancellationToken);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException
                && cancellationToken.IsCancellationRequested))
            {
                _logger.LogWarning(
                    ex,
                    "No se pudo usar Redis para rate limiting; se usa memoria local.");
            }
        }

        return AdquirirEnMemoria(clave, limite, ventana);
    }

    private static async Task<AsistenteRateLimitDecision> AdquirirEnRedisAsync(
        IConnectionMultiplexer redis,
        string clave,
        int limite,
        TimeSpan ventana,
        CancellationToken cancellationToken)
    {
        var database = redis.GetDatabase();
        var milisegundos = Math.Max(1, (long)ventana.TotalMilliseconds);

        var resultado = (RedisResult[]?)await database.ScriptEvaluateAsync(
            ScriptIncrementar,
            new RedisKey[] { $"{PrefijoClave}{clave}" },
            new RedisValue[] { milisegundos });

        if (resultado is not { Length: >= 2 })
        {
            throw new InvalidOperationException(
                "Redis devolvió una respuesta inválida para rate limiting.");
        }

        var actual = (long)resultado[0];
        var ttl = (long)resultado[1];

        if (actual <= limite)
        {
            return new AsistenteRateLimitDecision(true, TimeSpan.Zero);
        }

        return new AsistenteRateLimitDecision(
            false,
            TimeSpan.FromMilliseconds(Math.Max(ttl, 1)));
    }

    private AsistenteRateLimitDecision AdquirirEnMemoria(
        string clave,
        int limite,
        TimeSpan ventana)
    {
        var ahora = _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        var duracion = Math.Max(1, (long)ventana.TotalMilliseconds);
        var contador = _memoria.GetOrAdd(clave, _ => new ContadorMemoria(ahora));

        lock (contador)
        {
            if (ahora - contador.Inicio >= duracion)
            {
                contador.Inicio = ahora;
                contador.Cantidad = 0;
            }

            contador.Cantidad++;

            if (contador.Cantidad <= limite)
            {
                return new AsistenteRateLimitDecision(true, TimeSpan.Zero);
            }

            var restante = contador.Inicio + duracion - ahora;

            return new AsistenteRateLimitDecision(
                false,
                TimeSpan.FromMilliseconds(Math.Max(restante, 1)));
        }
    }

    private IConnectionMultiplexer? CrearConexion()
    {
        var connectionString = _options.RateLimit.RedisConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        try
        {
            var configuration = ConfigurationOptions.Parse(connectionString);
            configuration.AbortOnConnectFail = false;

            return ConnectionMultiplexer.Connect(configuration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "No se pudo inicializar Redis para rate limiting; se usa memoria local.");
            return null;
        }
    }

    private sealed class ContadorMemoria
    {
        public ContadorMemoria(long inicio)
        {
            Inicio = inicio;
        }

        public long Inicio { get; set; }

        public int Cantidad { get; set; }
    }
}
