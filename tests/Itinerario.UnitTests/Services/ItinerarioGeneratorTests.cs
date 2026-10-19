using Itinerario.Application.DTOs;
using Itinerario.Application.Interfaces;
using Itinerario.Application.Services;
using Itinerario.Domain.Entities;
using Itinerario.Domain.Enums;

namespace Itinerario.UnitTests.Services;

public class ItinerarioGeneratorTests
{
    [Fact]
    public void Generar_DiaCompleto_GeneraHastaCuatroParadas()
    {
        var viaje = CrearViaje(AlcanceTemporal.DiaCompleto);
        var lugares = new[]
        {
            CrearLugar(Turno.Manana),
            CrearLugar(Turno.Manana),
            CrearLugar(Turno.Manana),
            CrearLugar(Turno.Manana)
        };

        var paradas = CrearGenerator().Generar(viaje, lugares);

        Assert.Equal(4, paradas.Count);
    }

    [Fact]
    public void Generar_MedioDia_GeneraHastaTresParadas()
    {
        var viaje = CrearViaje(AlcanceTemporal.TardeNoche);
        var lugares = new[]
        {
            CrearLugar(Turno.Tarde),
            CrearLugar(Turno.Tarde),
            CrearLugar(Turno.Tarde),
            CrearLugar(Turno.Tarde)
        };

        var paradas = CrearGenerator().Generar(viaje, lugares);

        Assert.Equal(3, paradas.Count);
    }

    [Fact]
    public void Generar_ExcluyeLugaresFueraDelAlcanceTemporal()
    {
        var viaje = CrearViaje(AlcanceTemporal.MananaAlmuerzo);
        var lugares = new[]
        {
            CrearLugar(Turno.Manana),
            CrearLugar(Turno.Noche),
            CrearLugar(Turno.Noche)
        };

        var paradas = CrearGenerator().Generar(viaje, lugares);

        var unica = Assert.Single(paradas);
        Assert.Equal(Turno.Manana, unica.Turno);
    }

    [Fact]
    public void Generar_ExcluyeLugaresFueraDelPresupuesto()
    {
        var viaje = CrearViaje(presupuesto: NivelPresupuesto.Economico);
        var lugares = new[]
        {
            CrearLugar(Turno.Manana, NivelPresupuesto.Economico),
            CrearLugar(Turno.Manana, NivelPresupuesto.Equilibrado),
            CrearLugar(Turno.Manana, NivelPresupuesto.SinRestricciones)
        };

        var paradas = CrearGenerator().Generar(viaje, lugares);

        var unica = Assert.Single(paradas);
        Assert.Equal(Turno.Manana, unica.Turno);
    }

    [Fact]
    public void Generar_SinCandidatosDentroDelLimiteDeCuadras_DevuelveVacio()
    {
        var viaje = CrearViaje(
            limiteCuadras: 4,
            latitudPartida: -34.58m,
            longitudPartida: -58.42m);
        var lugares = new[]
        {
            CrearLugar(Turno.Tarde),
            CrearLugar(Turno.Tarde)
        };

        var paradas = CrearGenerator(metrosEntreLugares: 500).Generar(viaje, lugares);

        Assert.Empty(paradas);
    }

    [Fact]
    public void Generar_ConLimiteDeCuadrasSuficiente_GeneraParadas()
    {
        var viaje = CrearViaje(
            limiteCuadras: 10,
            latitudPartida: -34.58m,
            longitudPartida: -58.42m);
        var lugares = new[]
        {
            CrearLugar(Turno.Tarde),
            CrearLugar(Turno.Tarde)
        };

        var paradas = CrearGenerator(metrosEntreLugares: 500).Generar(viaje, lugares);

        Assert.Equal(2, paradas.Count);
    }

    [Fact]
    public void Generar_SinLugares_DevuelveVacio()
    {
        var viaje = CrearViaje();

        var paradas = CrearGenerator().Generar(viaje, Array.Empty<LugarCandidatoDto>());

        Assert.Empty(paradas);
    }

    [Fact]
    public void Generar_MarcaElOrigenDeLasParadasComoIa()
    {
        var viaje = CrearViaje();
        var lugares = new[]
        {
            CrearLugar(Turno.Manana),
            CrearLugar(Turno.Tarde)
        };

        var paradas = CrearGenerator().Generar(viaje, lugares);

        Assert.All(paradas, parada => Assert.Equal(OrigenParada.Ia, parada.OrigenParada));
    }

    [Fact]
    public void Generar_NoRepiteLugares()
    {
        var viaje = CrearViaje();
        var lugares = new[]
        {
            CrearLugar(Turno.Manana),
            CrearLugar(Turno.Tarde),
            CrearLugar(Turno.Noche),
            CrearLugar(Turno.Manana)
        };

        var paradas = CrearGenerator().Generar(viaje, lugares);

        Assert.Equal(paradas.Count, paradas.Select(p => p.LugarId).Distinct().Count());
    }

    private static ItinerarioGenerator CrearGenerator(double metrosEntreLugares = 100)
        => new(new GeoFalso(metrosEntreLugares));

    private static Viaje CrearViaje(
        AlcanceTemporal alcance = AlcanceTemporal.DiaCompleto,
        NivelPresupuesto presupuesto = NivelPresupuesto.SinRestricciones,
        RitmoCaminata ritmo = RitmoCaminata.Activo,
        short? limiteCuadras = null,
        decimal? latitudPartida = null,
        decimal? longitudPartida = null)
        => Viaje.Crear(
            Guid.NewGuid(),
            new DatosViaje(
                "Viaje de prueba",
                TipoViaje.Solo,
                BarrioCaba.Palermo,
                alcance,
                null,
                null,
                presupuesto,
                ritmo,
                limiteCuadras,
                TipoPuntoPartida.NodoUrbano,
                null,
                null,
                null,
                latitudPartida,
                longitudPartida),
            Guid.NewGuid(),
            "CODIGO1234",
            new short[] { 1, 2 },
            null);

    private static LugarCandidatoDto CrearLugar(
        Turno turno,
        NivelPresupuesto nivel = NivelPresupuesto.Economico)
        => new()
        {
            Id = Guid.NewGuid(),
            Nombre = "Lugar de prueba",
            Latitud = -34.58m,
            Longitud = -58.42m,
            Turno = turno,
            CategoriaId = 1,
            NivelPrecio = nivel,
            EsAireLibre = false,
            DuracionSugeridaMinutos = 60
        };

    private sealed class GeoFalso : IGeoCalculator
    {
        private readonly double _metros;

        public GeoFalso(double metros)
        {
            _metros = metros;
        }

        public double DistanciaMetros(double latitud1, double longitud1, double latitud2, double longitud2)
            => _metros;

        public int Cuadras(double metros)
            => (int)Math.Round(metros / 100, MidpointRounding.AwayFromZero);

        public int MinutosAPie(double metros)
            => (int)Math.Round(metros / 80, MidpointRounding.AwayFromZero);
    }
}
