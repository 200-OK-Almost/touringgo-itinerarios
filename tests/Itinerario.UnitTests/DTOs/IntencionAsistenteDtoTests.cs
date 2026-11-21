using Itinerario.Application.DTOs;
using Itinerario.Domain.Enums;

namespace Itinerario.UnitTests.DTOs;

public class IntencionAsistenteDtoTests
{
    [Fact]
    public void PorDefecto_UsaRecomendarYFiltrosVacios()
    {
        var intencion = new IntencionAsistenteDto();

        Assert.Equal(TipoAccionAsistente.Recomendar, intencion.Accion);
        Assert.Equal(string.Empty, intencion.QuerySemantica);
        Assert.NotNull(intencion.Filtros);
        Assert.Empty(intencion.Filtros.CategoriaCodigos);
        Assert.Null(intencion.ParadaObjetivo);
        Assert.Null(intencion.RadioMetros);
        Assert.Null(intencion.Aclaracion);
    }

    [Fact]
    public void Filtros_PorDefecto_NoAplicanRestricciones()
    {
        var filtros = new FiltrosIntencionDto();

        Assert.Null(filtros.SoloAlAireLibre);
        Assert.Null(filtros.SoloBajoTecho);
        Assert.Null(filtros.AptoNinos);
        Assert.Null(filtros.AccesibleMovilidadReducida);
        Assert.Null(filtros.RestriccionDietaria);
        Assert.Null(filtros.Turno);
        Assert.Null(filtros.NivelPrecioMaximo);
    }

    [Fact]
    public void PuedeRepresentarAccionConParadaObjetivo()
    {
        var paradaId = Guid.NewGuid();

        var intencion = new IntencionAsistenteDto
        {
            Accion = TipoAccionAsistente.Reemplazar,
            QuerySemantica = "lugar al aire libre",
            Filtros = new FiltrosIntencionDto
            {
                SoloAlAireLibre = true,
                Turno = Turno.Tarde
            },
            ParadaObjetivo = new ParadaObjetivoDto
            {
                ParadaId = paradaId,
                Nombre = "Museo",
                Turno = Turno.Manana
            },
            RadioMetros = 800
        };

        Assert.Equal(TipoAccionAsistente.Reemplazar, intencion.Accion);
        Assert.Equal(paradaId, intencion.ParadaObjetivo.ParadaId);
        Assert.True(intencion.Filtros.SoloAlAireLibre);
        Assert.Equal(Turno.Tarde, intencion.Filtros.Turno);
        Assert.Equal(800, intencion.RadioMetros);
    }
}
