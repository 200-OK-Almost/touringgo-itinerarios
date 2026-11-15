using Itinerario.Domain.Entities;
using Itinerario.Domain.Enums;
using Itinerario.Domain.Exceptions;
using Xunit;

namespace Itinerario.UnitTests.Domain;

public class ViajeEstadoTests
{
    private static Viaje CrearViajeBase(EstadoViaje estadoInicial)
    {
        var viaje = Viaje.Crear(
            Guid.NewGuid(),
            new DatosViaje(
                "Test",
                TipoViaje.Solo,
                BarrioCaba.Palermo,
                AlcanceTemporal.DiaCompleto,
                null,
                null,
                NivelPresupuesto.Equilibrado,
                RitmoCaminata.Moderado,
                null,
                TipoPuntoPartida.PrimeraParada,
                null,
                null,
                null,
                null,
                null),
            Guid.NewGuid(),
            "ABC123",
            new[] { "cafe_especialidad" },
            null);

        viaje.Estado = estadoInicial;
        return viaje;
    }

    [Theory]
    [InlineData(EstadoViaje.Borrador, EstadoViaje.EnPlanificacion)]
    [InlineData(EstadoViaje.EnPlanificacion, EstadoViaje.Planificado)]
    [InlineData(EstadoViaje.EnPlanificacion, EstadoViaje.Borrador)]
    [InlineData(EstadoViaje.Planificado, EstadoViaje.EnPlanificacion)]
    [InlineData(EstadoViaje.Planificado, EstadoViaje.Finalizado)]
    public void CambiarEstado_TransicionesValidas_ActualizaEstado(EstadoViaje origen, EstadoViaje destino)
    {
        var viaje = CrearViajeBase(origen);

        viaje.CambiarEstado(destino);

        Assert.Equal(destino, viaje.Estado);
    }

    [Theory]
    [InlineData(EstadoViaje.Borrador, EstadoViaje.Finalizado)]
    [InlineData(EstadoViaje.EnPlanificacion, EstadoViaje.Finalizado)]
    [InlineData(EstadoViaje.Finalizado, EstadoViaje.Borrador)]
    [InlineData(EstadoViaje.Finalizado, EstadoViaje.EnPlanificacion)]
    [InlineData(EstadoViaje.Finalizado, EstadoViaje.Planificado)]
    public void CambiarEstado_TransicionesInvalidas_LanzaReglaDeNegocioException(EstadoViaje origen, EstadoViaje destino)
    {
        var viaje = CrearViajeBase(origen);

        var ex = Assert.Throws<ReglaDeNegocioException>(() => viaje.CambiarEstado(destino));
        Assert.Contains("No se puede pasar del estado", ex.Message);
    }

    [Fact]
    public void CambiarEstado_MismoEstado_NoHaceNada()
    {
        var viaje = CrearViajeBase(EstadoViaje.Borrador);

        viaje.CambiarEstado(EstadoViaje.Borrador);

        Assert.Equal(EstadoViaje.Borrador, viaje.Estado);
    }
}

