using Itinerario.Application.Configuration;
using Itinerario.Application.DTOs;

namespace Itinerario.UnitTests.DTOs;

public class AsistenteCapacidadesDtoTests
{
    [Fact]
    public void Desde_MapeaHabilitadoYLimites()
    {
        var options = new AsistenteOptions
        {
            Enabled = true,
            TopK = 9,
            MinResultados = 4,
            MaxMensajeCaracteres = 750,
            MaxHistorial = 5,
            RateLimit = new AsistenteRateLimitOptions
            {
                PermitLimitPorUsuario = 15,
                PermitLimitPorViaje = 30,
                WindowSeconds = 45
            }
        };

        var dto = AsistenteCapacidadesDto.Desde(options);

        Assert.True(dto.Habilitado);
        Assert.Equal(9, dto.Limites.TopK);
        Assert.Equal(4, dto.Limites.MinResultados);
        Assert.Equal(750, dto.Limites.MaxMensajeCaracteres);
        Assert.Equal(5, dto.Limites.MaxHistorial);
        Assert.Equal(15, dto.Limites.RateLimitPorUsuario);
        Assert.Equal(30, dto.Limites.RateLimitPorViaje);
        Assert.Equal(45, dto.Limites.VentanaRateLimitSegundos);
    }

    [Fact]
    public void Desde_ConAsistenteDeshabilitado_LoRefleja()
    {
        var options = new AsistenteOptions { Enabled = false };

        var dto = AsistenteCapacidadesDto.Desde(options);

        Assert.False(dto.Habilitado);
    }
}
