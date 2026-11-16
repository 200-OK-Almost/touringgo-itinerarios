using Itinerario.Application.Services;

namespace Itinerario.UnitTests.Services;

public class PlanificadorDiasTests
{
    private static readonly DateOnly Inicio = new(2026, 3, 10);

    [Fact]
    public void Calcular_UnSoloDia_DevuelveEseDia()
    {
        var dias = PlanificadorDias.Calcular(Inicio, Inicio);

        var dia = Assert.Single(dias);
        Assert.Equal(1, dia.NumeroDia);
        Assert.Equal(Inicio, dia.Fecha);
    }

    [Fact]
    public void Calcular_CuatroDias_DevuelveDiasConsecutivos()
    {
        var dias = PlanificadorDias.Calcular(Inicio, Inicio.AddDays(3));

        Assert.Equal(4, dias.Count);
        Assert.Equal(Enumerable.Range(1, 4), dias.Select(d => (int)d.NumeroDia));
        Assert.Equal(
            new DateOnly?[] { Inicio, Inicio.AddDays(1), Inicio.AddDays(2), Inicio.AddDays(3) },
            dias.Select(d => d.Fecha));
    }

    [Fact]
    public void Calcular_SieteDias_DevuelveSieteDias()
    {
        var dias = PlanificadorDias.Calcular(Inicio, Inicio.AddDays(6));

        Assert.Equal(7, dias.Count);
        Assert.Equal(1, dias.First().NumeroDia);
        Assert.Equal(7, dias.Last().NumeroDia);
        Assert.Equal(Inicio, dias.First().Fecha);
        Assert.Equal(Inicio.AddDays(6), dias.Last().Fecha);
    }

    [Fact]
    public void Calcular_OchoDiasLegacy_DevuelveTodoElRango()
    {
        var dias = PlanificadorDias.Calcular(Inicio, Inicio.AddDays(7));

        Assert.Equal(8, dias.Count);
        Assert.Equal(Inicio.AddDays(7), dias.Last().Fecha);
    }

    [Fact]
    public void Calcular_FechasNulas_DevuelveUnDiaSinFecha()
    {
        var dias = PlanificadorDias.Calcular(null, null);

        var dia = Assert.Single(dias);
        Assert.Equal(1, dia.NumeroDia);
        Assert.Null(dia.Fecha);
    }

    [Fact]
    public void Calcular_SoloFechaDeInicio_DevuelveUnDiaConEsaFecha()
    {
        var dias = PlanificadorDias.Calcular(Inicio, null);

        var dia = Assert.Single(dias);
        Assert.Equal(1, dia.NumeroDia);
        Assert.Equal(Inicio, dia.Fecha);
    }

    [Fact]
    public void Calcular_FinAnteriorAlInicio_DevuelveUnDiaConLaFechaDeInicio()
    {
        var dias = PlanificadorDias.Calcular(Inicio, Inicio.AddDays(-1));

        var dia = Assert.Single(dias);
        Assert.Equal(1, dia.NumeroDia);
        Assert.Equal(Inicio, dia.Fecha);
    }

    [Fact]
    public void Calcular_NoGeneraDiasFueraDelRango()
    {
        var fin = Inicio.AddDays(2);

        var dias = PlanificadorDias.Calcular(Inicio, fin);

        Assert.All(dias, dia => Assert.InRange(dia.Fecha!.Value, Inicio, fin));
    }
}
