using Itinerario.Application.Services;

namespace Itinerario.UnitTests.Services;

public class GeoCalculatorTests
{
    private readonly GeoCalculator _geo = new();

    [Fact]
    public void DistanciaMetros_MismoPunto_EsCero()
    {
        var distancia = _geo.DistanciaMetros(-34.6037, -58.3816, -34.6037, -58.3816);

        Assert.Equal(0, distancia, 6);
    }

    [Fact]
    public void DistanciaMetros_UnGradoDeLatitud_EsAproximadamente111Km()
    {
        var distancia = _geo.DistanciaMetros(0, 0, 1, 0);

        Assert.InRange(distancia, 111_190, 111_200);
    }

    [Fact]
    public void DistanciaMetros_EsSimetrica()
    {
        var ida = _geo.DistanciaMetros(-34.6, -58.4, -34.7, -58.5);
        var vuelta = _geo.DistanciaMetros(-34.7, -58.5, -34.6, -58.4);

        Assert.Equal(ida, vuelta, 6);
    }

    [Theory]
    [InlineData(0d, 0)]
    [InlineData(99d, 1)]
    [InlineData(100d, 1)]
    [InlineData(149d, 1)]
    [InlineData(150d, 2)]
    [InlineData(450d, 5)]
    public void Cuadras_RedondeaAUnaCuadraPor100Metros(double metros, int esperado)
        => Assert.Equal(esperado, _geo.Cuadras(metros));

    [Theory]
    [InlineData(0d, 0)]
    [InlineData(40d, 1)]
    [InlineData(80d, 1)]
    [InlineData(120d, 2)]
    [InlineData(160d, 2)]
    [InlineData(200d, 3)]
    public void MinutosAPie_RedondeaAOchentaMetrosPorMinuto(double metros, int esperado)
        => Assert.Equal(esperado, _geo.MinutosAPie(metros));
}
