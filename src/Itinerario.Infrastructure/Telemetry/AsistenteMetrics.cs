using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Itinerario.Infrastructure.Telemetry;

public static class AsistenteMetrics
{
    public const string MeterName = "TouringGo.Itinerario.Asistente";
    public const string ActivitySourceName = "TouringGo.Itinerario.Asistente";

    public static readonly Meter Meter = new(MeterName, "1.0.0");
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, "1.0.0");

    public static readonly Histogram<double> DuracionEtapa = Meter.CreateHistogram<double>(
        "asistente.etapa.duracion",
        unit: "ms",
        description: "Duración de cada etapa del asistente.");

    public static readonly Counter<long> Resultados = Meter.CreateCounter<long>(
        "asistente.resultados",
        description: "Cantidad de resultados por tipo.");

    public static readonly Histogram<long> Recomendaciones = Meter.CreateHistogram<long>(
        "asistente.recomendaciones.cantidad",
        description: "Cantidad de recomendaciones devueltas.");

    public static readonly Counter<long> RateLimitRechazos = Meter.CreateCounter<long>(
        "asistente.rate_limit.rechazos",
        description: "Cantidad de requests rechazados por rate limiting.");

    public static Activity? IniciarActividad(string nombre, Guid? viajeId = null)
    {
        var actividad = ActivitySource.StartActivity(nombre, ActivityKind.Internal);

        if (actividad is not null && viajeId.HasValue)
        {
            actividad.SetTag("asistente.viaje_id", viajeId.Value);
        }

        return actividad;
    }

    public static void RegistrarRateLimitRechazado(string politica)
    {
        var tags = new TagList
        {
            { "politica", politica }
        };

        RateLimitRechazos.Add(1, tags);
    }
}
