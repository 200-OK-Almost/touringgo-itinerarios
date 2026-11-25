using Itinerario.Application.DTOs;
using Itinerario.Application.Services;
using Itinerario.Domain.Enums;

namespace Itinerario.UnitTests.Services;

public class IntencionLlmSanitizerTests
{
    [Fact]
    public void Sanitizar_SalidaValida_MapeaTodosLosCampos()
    {
        var contexto = CrearContexto();
        var parada = contexto.Paradas.First();
        var crudo = new IntencionLlmCruda
        {
            Accion = "Reemplazar",
            QuerySemantica = "  aire   libre ",
            ParadaObjetivoId = parada.ParadaId.ToString(),
            Categorias = new List<string>
            {
                "PARQUES_AIRE_LIBRE",
                "categoria_inexistente",
                "CAFE_ESPECIALIDAD"
            },
            SoloAlAireLibre = true,
            SoloBajoTecho = null,
            AptoNinos = true,
            AccesibleMovilidadReducida = false,
            RestriccionDietaria = " vegano ",
            Turno = "tarde",
            NivelPrecioMaximo = 2,
            RadioMetros = 800
        };

        var resultado = IntencionLlmSanitizer.Sanitizar(crudo, contexto);

        Assert.NotNull(resultado);
        Assert.Equal(TipoAccionAsistente.Reemplazar, resultado!.Accion);
        Assert.Equal("aire libre", resultado.QuerySemantica);
        Assert.Equal(parada.ParadaId, resultado.ParadaObjetivo!.ParadaId);
        Assert.Equal(parada.Nombre, resultado.ParadaObjetivo.Nombre);
        Assert.Equal(parada.Turno, resultado.ParadaObjetivo.Turno);
        Assert.Equal(
            new[] { "parques_aire_libre", "cafe_especialidad" },
            resultado.Filtros.CategoriaCodigos.ToArray());
        Assert.True(resultado.Filtros.SoloAlAireLibre);
        Assert.Null(resultado.Filtros.SoloBajoTecho);
        Assert.True(resultado.Filtros.AptoNinos);
        Assert.Null(resultado.Filtros.AccesibleMovilidadReducida);
        Assert.Equal("vegano", resultado.Filtros.RestriccionDietaria);
        Assert.Equal(Turno.Tarde, resultado.Filtros.Turno);
        Assert.Equal((short?)2, resultado.Filtros.NivelPrecioMaximo);
        Assert.Equal(800d, resultado.RadioMetros);
    }

    [Fact]
    public void Sanitizar_AccionInvalida_DevuelveNull()
    {
        var resultado = IntencionLlmSanitizer.Sanitizar(
            new IntencionLlmCruda { Accion = "NoExiste" },
            CrearContexto());

        Assert.Null(resultado);
    }

    [Fact]
    public void Sanitizar_ReemplazoSinParadaValida_DevuelveNull()
    {
        var crudo = new IntencionLlmCruda
        {
            Accion = "Reemplazar",
            ParadaObjetivoId = Guid.NewGuid().ToString()
        };

        var resultado = IntencionLlmSanitizer.Sanitizar(crudo, CrearContexto());

        Assert.Null(resultado);
    }

    [Fact]
    public void Sanitizar_AgregarConParadaInexistente_IgnoraObjetivo()
    {
        var crudo = new IntencionLlmCruda
        {
            Accion = "Agregar",
            ParadaObjetivoId = Guid.NewGuid().ToString()
        };

        var resultado = IntencionLlmSanitizer.Sanitizar(crudo, CrearContexto());

        Assert.NotNull(resultado);
        Assert.Null(resultado!.ParadaObjetivo);
    }

    [Fact]
    public void Sanitizar_QueryLarga_SeRecorta()
    {
        var crudo = new IntencionLlmCruda
        {
            Accion = "Recomendar",
            QuerySemantica = new string('a', 250)
        };

        var resultado = IntencionLlmSanitizer.Sanitizar(crudo, CrearContexto());

        Assert.NotNull(resultado);
        Assert.Equal(200, resultado!.QuerySemantica.Length);
    }

    [Fact]
    public void Sanitizar_QueryVacia_UsaFallback()
    {
        var crudo = new IntencionLlmCruda
        {
            Accion = "Recomendar",
            QuerySemantica = "   "
        };

        var resultado = IntencionLlmSanitizer.Sanitizar(crudo, CrearContexto());

        Assert.NotNull(resultado);
        Assert.Equal("lugar", resultado!.QuerySemantica);
    }

    [Fact]
    public void Sanitizar_ConflictoAireTecho_PriorizaBajoTecho()
    {
        var crudo = new IntencionLlmCruda
        {
            Accion = "Recomendar",
            SoloAlAireLibre = true,
            SoloBajoTecho = true
        };

        var resultado = IntencionLlmSanitizer.Sanitizar(crudo, CrearContexto());

        Assert.NotNull(resultado);
        Assert.Null(resultado!.Filtros.SoloAlAireLibre);
        Assert.True(resultado.Filtros.SoloBajoTecho);
    }

    [Fact]
    public void Sanitizar_RadioYPrecio_SeClampean()
    {
        var crudo = new IntencionLlmCruda
        {
            Accion = "Recomendar",
            RadioMetros = 99999,
            NivelPrecioMaximo = 99
        };

        var resultado = IntencionLlmSanitizer.Sanitizar(crudo, CrearContexto());

        Assert.NotNull(resultado);
        Assert.Equal(5000d, resultado!.RadioMetros);
        Assert.Equal((short?)3, resultado.Filtros.NivelPrecioMaximo);
    }

    [Fact]
    public void Sanitizar_RadioInvalido_SeIgnora()
    {
        var crudo = new IntencionLlmCruda
        {
            Accion = "Recomendar",
            RadioMetros = -10,
            NivelPrecioMaximo = 0
        };

        var resultado = IntencionLlmSanitizer.Sanitizar(crudo, CrearContexto());

        Assert.NotNull(resultado);
        Assert.Null(resultado!.RadioMetros);
        Assert.Equal((short?)1, resultado.Filtros.NivelPrecioMaximo);
    }

    [Fact]
    public void Sanitizar_TurnoInvalido_SeIgnora()
    {
        var crudo = new IntencionLlmCruda
        {
            Accion = "Recomendar",
            Turno = "Madrugada"
        };

        var resultado = IntencionLlmSanitizer.Sanitizar(crudo, CrearContexto());

        Assert.NotNull(resultado);
        Assert.Null(resultado!.Filtros.Turno);
    }

    [Fact]
    public void Sanitizar_CrudoNull_DevuelveNull()
    {
        Assert.Null(IntencionLlmSanitizer.Sanitizar(null, CrearContexto()));
    }

    private static IntencionLlmContextoDto CrearContexto()
    {
        var paradaId = Guid.NewGuid();

        return new IntencionLlmContextoDto
        {
            ViajeId = Guid.NewGuid(),
            Mensaje = "Reemplazá el museo por algo al aire libre",
            NumeroDia = 1,
            Barrio = BarrioCaba.Palermo,
            NivelPresupuesto = NivelPresupuesto.Equilibrado,
            CategoriasViaje = new[] { "museos_galerias" },
            AptoNinos = true,
            AccesibleMovilidadReducida = false,
            Paradas = new[]
            {
                new ParadaResolubleDto
                {
                    ParadaId = paradaId,
                    LugarId = Guid.NewGuid(),
                    Nombre = "Museo de Arte",
                    Turno = Turno.Manana,
                    OrdenParada = 1,
                    Categorias = new[] { "museos_galerias" }
                }
            }
        };
    }
}
