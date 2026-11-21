using Itinerario.Application.Configuration;
using Itinerario.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Itinerario.UnitTests.Telemetry;

public class AsistenteTelemetryTests
{
    [Fact]
    public void RegistrarEtapa_LogueaEtapaYDuracion()
    {
        var logger = new ListLogger<AsistenteTelemetry>();
        var telemetry = new AsistenteTelemetry(
            logger,
            Options.Create(new AsistenteOptions()));

        telemetry.RegistrarEtapa("retrieval", TimeSpan.FromMilliseconds(123), Guid.NewGuid());

        var entrada = Assert.Single(logger.Entradas);
        Assert.Equal(LogLevel.Information, entrada.Level);
        Assert.Contains("retrieval", entrada.Mensaje);
        Assert.Contains("123", entrada.Mensaje);
    }

    [Fact]
    public void RegistrarResultado_LogueaResultadoYCantidad()
    {
        var logger = new ListLogger<AsistenteTelemetry>();
        var telemetry = new AsistenteTelemetry(
            logger,
            Options.Create(new AsistenteOptions()));

        telemetry.RegistrarResultado("ok", 3, Guid.NewGuid());

        var entrada = Assert.Single(logger.Entradas);
        Assert.Equal(LogLevel.Information, entrada.Level);
        Assert.Contains("ok", entrada.Mensaje);
        Assert.Contains("3", entrada.Mensaje);
    }

    [Fact]
    public void RegistrarDetalle_SinVerbose_NoLoguea()
    {
        var logger = new ListLogger<AsistenteTelemetry>();
        var telemetry = new AsistenteTelemetry(
            logger,
            Options.Create(new AsistenteOptions { VerboseLogging = false }));

        telemetry.RegistrarDetalle("mensaje sensible", Guid.NewGuid());

        Assert.Empty(logger.Entradas);
    }

    [Fact]
    public void RegistrarDetalle_ConVerbose_LogueaEnDebug()
    {
        var logger = new ListLogger<AsistenteTelemetry>();
        var telemetry = new AsistenteTelemetry(
            logger,
            Options.Create(new AsistenteOptions { VerboseLogging = true }));

        telemetry.RegistrarDetalle("detalle de diagnóstico", Guid.NewGuid());

        var entrada = Assert.Single(logger.Entradas);
        Assert.Equal(LogLevel.Debug, entrada.Level);
        Assert.Contains("detalle de diagnóstico", entrada.Mensaje);
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Mensaje)> Entradas { get; } = new();

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
            => NoopDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entradas.Add((logLevel, formatter(state, exception)));
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
