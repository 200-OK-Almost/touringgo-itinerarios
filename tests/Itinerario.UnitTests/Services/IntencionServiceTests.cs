using Itinerario.Application.Services;
using Itinerario.Domain.Enums;

namespace Itinerario.UnitTests.Services;

public class IntencionServiceTests
{
    private readonly IntencionService _service = new();

    [Theory]
    [InlineData("  ¿AGREGÁ un CAFÉ?  ", "agrega un cafe")]
    [InlineData("Reemplazá el Museo por algo al aire libre.", "reemplaza el museo por algo al aire libre")]
    [InlineData("Sugerime   opciones\tpara comer", "sugerime opciones para comer")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Normalizar_LimpiaTexto(string? texto, string esperado)
    {
        Assert.Equal(esperado, _service.Normalizar(texto));
    }

    [Theory]
    [InlineData("Reemplazá el museo por un café")]
    [InlineData("Cambiá la primera parada por un parque")]
    [InlineData("Sustituí el bar por una librería")]
    [InlineData("Quiero algo en lugar de la parada actual")]
    public void DetectarAccion_Reemplazo(string mensaje)
    {
        Assert.Equal(TipoAccionAsistente.Reemplazar, _service.DetectarAccion(mensaje));
    }

    [Theory]
    [InlineData("Agregá un café al día 1")]
    [InlineData("Sumá un parque a la tarde")]
    [InlineData("Quiero incluir una librería")]
    [InlineData("Añadí una parada más")]
    public void DetectarAccion_Agregar(string mensaje)
    {
        Assert.Equal(TipoAccionAsistente.Agregar, _service.DetectarAccion(mensaje));
    }

    [Theory]
    [InlineData("¿Qué lugar me recomendás para comer?")]
    [InlineData("Sugerime opciones para la tarde")]
    [InlineData("¿Dónde puedo tomar algo?")]
    [InlineData("Dame ideas para el día 1")]
    public void DetectarAccion_Recomendar(string mensaje)
    {
        Assert.Equal(TipoAccionAsistente.Recomendar, _service.DetectarAccion(mensaje));
    }

    [Fact]
    public void DetectarAccion_PreguntaConAgregar_PriorizaRecomendar()
    {
        Assert.Equal(
            TipoAccionAsistente.Recomendar,
            _service.DetectarAccion("¿Qué lugar puedo agregar?"));
    }

    [Fact]
    public void DetectarAccion_ReemplazoGanaAnteConsulta()
    {
        Assert.Equal(
            TipoAccionAsistente.Reemplazar,
            _service.DetectarAccion("¿Qué lugar me recomendás para reemplazar el museo?"));
    }

    [Fact]
    public void DetectarAccion_MensajeVacio_Recomendar()
    {
        Assert.Equal(TipoAccionAsistente.Recomendar, _service.DetectarAccion("  "));
    }
}
