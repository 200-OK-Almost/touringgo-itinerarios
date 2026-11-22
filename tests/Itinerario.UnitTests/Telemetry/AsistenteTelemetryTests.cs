using System.Diagnostics;
using System.Diagnostics.Metrics;
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
    public void RegistrarEtapa_EmiteDuracionEnMilisegundos()
    {
        var telemetry = new AsistenteTelemetry(
            new ListLogger<AsistenteTelemetry>(),
            Options.Create(new AsistenteOptions()));

        double? valor = null;
        string? etapa = null;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == AsistenteMetrics.MeterName
                && instrument.Name == "asistente.etapa.duracion")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<double>((_, measurement, tags, _) =>
        {
            valor = measurement;

            foreach (var tag in tags)
            {
                if (tag.Key == "etapa")
                {
                    etapa = tag.Value?.ToString();
                }
            }
        });
        listener.Start();

        telemetry.RegistrarEtapa("retrieval", TimeSpan.FromMilliseconds(123), Guid.NewGuid());

        Assert.Equal(123d, valor);
        Assert.Equal("retrieval", etapa);
    }

    [Fact]
    public void RegistrarResultado_EmiteResultadoYRecomendaciones()
    {
        var telemetry = new AsistenteTelemetry(
            new ListLogger<AsistenteTelemetry>(),
            Options.Create(new AsistenteOptions()));

        var medidas = new List<(string Instrumento, long Valor)>();

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == AsistenteMetrics.MeterName
                && (instrument.Name == "asistente.resultados"
                    || instrument.Name == "asistente.recomendaciones.cantidad"))
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) =>
        {
            medidas.Add((instrument.Name, measurement));
        });
        listener.Start();

        telemetry.RegistrarResultado("ok", 3, Guid.NewGuid());

        Assert.Contains(("asistente.resultados", 1L), medidas);
        Assert.Contains(("asistente.recomendaciones.cantidad", 3L), medidas);
    }

    [Fact]
    public void IniciarActividad_EmiteActividadConViajeId()
    {
        var telemetry = new AsistenteTelemetry(
            new ListLogger<AsistenteTelemetry>(),
            Options.Create(new AsistenteOptions()));

        Activity? iniciada = null;

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == AsistenteMetrics.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllData,
            ActivityStarted = activity => iniciada = activity
        };
        ActivitySource.AddActivityListener(listener);

        var viajeId = Guid.NewGuid();
        using var actividad = telemetry.IniciarActividad("asistente.consulta", viajeId);

        Assert.NotNull(actividad);
        Assert.Equal("asistente.consulta", iniciada?.DisplayName);
        Assert.Equal(viajeId, iniciada?.GetTagItem("asistente.viaje_id"));
    }

    [Fact]
    public void RegistrarRateLimitRechazado_EmiteContadorConPolitica()
    {
        long? valor = null;
        string? politica = null;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == AsistenteMetrics.MeterName
                && instrument.Name == "asistente.rate_limit.rechazos")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, measurement, tags, _) =>
        {
            valor = measurement;

            foreach (var tag in tags)
            {
                if (tag.Key == "politica")
                {
                    politica = tag.Value?.ToString();
                }
            }
        });
        listener.Start();

        AsistenteMetrics.RegistrarRateLimitRechazado("asistente-viaje");

        Assert.Equal(1L, valor);
        Assert.Equal("asistente-viaje", politica);
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
