using Itinerario.Domain.Entities;
using Itinerario.Domain.Enums;

namespace Itinerario.UnitTests.Domain;

public class ViajeEsMiembroTests
{
    private static readonly Guid CreadorId = Guid.NewGuid();

    private static Viaje CrearViaje()
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
            CreadorId,
            "ABC123",
            new[] { "cafe_especialidad" },
            null);

        return viaje;
    }

    [Fact]
    public void EsMiembro_Creador_EsMiembro()
    {
        var viaje = CrearViaje();

        Assert.True(viaje.EsMiembro(CreadorId));
    }

    [Fact]
    public void EsMiembro_UsuarioUnido_EsMiembro()
    {
        var viaje = CrearViaje();
        var usuarioId = Guid.NewGuid();
        viaje.AgregarMiembro(usuarioId, RolMiembro.Miembro);

        Assert.True(viaje.EsMiembro(usuarioId));
    }

    [Fact]
    public void EsMiembro_UsuarioAjeno_NoEsMiembro()
    {
        var viaje = CrearViaje();

        Assert.False(viaje.EsMiembro(Guid.NewGuid()));
    }
}
