namespace Itinerario.Application.Configuration;

public class AsistenteOptions
{
    public const string SectionName = "Asistente";

    public bool Enabled { get; set; }
    public int TopK { get; set; } = 12;
    public int MinResultados { get; set; } = 3;
    public double? SimilitudMinima { get; set; }
    public double RadioMetrosDefault { get; set; } = 800;
    public int MaxMensajeCaracteres { get; set; } = 1000;
    public int MaxHistorial { get; set; } = 6;
    public int IntentTimeoutSeconds { get; set; } = 10;
    public int GenerationTimeoutSeconds { get; set; } = 45;
    public bool VerboseLogging { get; set; }
    public AsistenteRateLimitOptions RateLimit { get; set; } = new();
}

public class AsistenteRateLimitOptions
{
    public int PermitLimitPorUsuario { get; set; } = 20;
    public int PermitLimitPorViaje { get; set; } = 40;
    public int WindowSeconds { get; set; } = 60;
    public int QueueLimit { get; set; }
}
