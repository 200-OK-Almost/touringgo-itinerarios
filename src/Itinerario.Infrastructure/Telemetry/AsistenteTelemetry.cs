using Itinerario.Application.Configuration;
using Itinerario.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Itinerario.Infrastructure.Telemetry;

public class AsistenteTelemetry : IAsistenteTelemetry
{
    private readonly ILogger<AsistenteTelemetry> _logger;
    private readonly AsistenteOptions _options;

    public AsistenteTelemetry(
        ILogger<AsistenteTelemetry> logger,
        IOptions<AsistenteOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public void RegistrarEtapa(string etapa, TimeSpan duracion, Guid? viajeId = null)
    {
        _logger.LogInformation(
            "Asistente etapa {Etapa} ViajeId {ViajeId} DuracionMs {DuracionMs}",
            etapa,
            viajeId,
            (long)duracion.TotalMilliseconds);
    }

    public void RegistrarResultado(string resultado, int cantidadRecomendaciones, Guid? viajeId = null)
    {
        _logger.LogInformation(
            "Asistente resultado {Resultado} ViajeId {ViajeId} Recomendaciones {CantidadRecomendaciones}",
            resultado,
            viajeId,
            cantidadRecomendaciones);
    }

    public void RegistrarDetalle(string detalle, Guid? viajeId = null)
    {
        if (!_options.VerboseLogging)
        {
            return;
        }

        _logger.LogDebug(
            "Asistente detalle ViajeId {ViajeId} Detalle {Detalle}",
            viajeId,
            detalle);
    }
}
