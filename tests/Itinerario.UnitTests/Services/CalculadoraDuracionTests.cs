using Itinerario.Application.Services;

namespace Itinerario.UnitTests.Services;

public class CalculadoraDuracionTests
{
    private static readonly DateOnly Inicio = new(2026, 3, 10);

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 1)]
    [InlineData(2, 3)]
    [InlineData(6, 7)]
    [InlineData(7, 8)]
    public void ContarDiasInclusivos_CuentaElDiaDeInicioYFin(int diasDeDiferencia, int esperado)
    {
        var fin = Inicio.AddDays(diasDeDiferencia);

        Assert.Equal(esperado, CalculadoraDuracion.ContarDiasInclusivos(Inicio, fin));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(7, true)]
    [InlineData(8, false)]
    public void EsDuracionValida_AceptaSoloEntreUnoYSieteDias(int dias, bool esperado)
        => Assert.Equal(esperado, CalculadoraDuracion.EsDuracionValida(dias));

    [Fact]
    public void EsRangoValido_UnSoloDia_EsValido()
        => Assert.True(CalculadoraDuracion.EsRangoValido(Inicio, Inicio));

    [Fact]
    public void EsRangoValido_SieteDiasInclusivos_EsValido()
        => Assert.True(CalculadoraDuracion.EsRangoValido(Inicio, Inicio.AddDays(6)));

    [Fact]
    public void EsRangoValido_OchoDiasInclusivos_NoEsValido()
        => Assert.False(CalculadoraDuracion.EsRangoValido(Inicio, Inicio.AddDays(7)));

    [Fact]
    public void EsRangoValido_FinAnteriorAlInicio_NoEsValido()
        => Assert.False(CalculadoraDuracion.EsRangoValido(Inicio, Inicio.AddDays(-1)));

    [Fact]
    public void EsRangoValido_FechasNulas_NoEsValido()
        => Assert.False(CalculadoraDuracion.EsRangoValido(null, null));

    [Fact]
    public void EsRangoValido_UnaSolaFecha_NoEsValido()
    {
        Assert.False(CalculadoraDuracion.EsRangoValido(Inicio, null));
        Assert.False(CalculadoraDuracion.EsRangoValido(null, Inicio));
    }
}
