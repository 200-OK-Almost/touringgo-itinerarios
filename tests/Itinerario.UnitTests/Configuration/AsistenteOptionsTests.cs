using Itinerario.Application.Configuration;

namespace Itinerario.UnitTests.Configuration;

public class AsistenteOptionsTests
{
    [Fact]
    public void ValoresPorDefecto_SonSeguros()
    {
        var options = new AsistenteOptions();

        Assert.Equal("Asistente", AsistenteOptions.SectionName);
        Assert.False(options.Enabled);
        Assert.False(options.LlmEnabled);
        Assert.False(options.LlmGenerarMensaje);
        Assert.Equal(5, options.TopK);
        Assert.Equal(3, options.MinResultados);
        Assert.Null(options.SimilitudMinima);
        Assert.Equal(800d, options.RadioMetrosDefault);
        Assert.Equal(1000, options.MaxMensajeCaracteres);
        Assert.Equal(6, options.MaxHistorial);
        Assert.Equal(10, options.IntentTimeoutSeconds);
        Assert.Equal(45, options.GenerationTimeoutSeconds);
        Assert.False(options.VerboseLogging);
        Assert.Equal(20, options.RateLimit.PermitLimitPorUsuario);
        Assert.Equal(40, options.RateLimit.PermitLimitPorViaje);
        Assert.Equal(60, options.RateLimit.WindowSeconds);
        Assert.Equal(0, options.RateLimit.QueueLimit);
        Assert.Null(options.RateLimit.RedisConnectionString);
    }
}
