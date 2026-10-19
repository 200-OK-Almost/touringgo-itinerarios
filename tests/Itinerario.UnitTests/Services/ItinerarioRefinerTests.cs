using Itinerario.Application.DTOs;
using Itinerario.Application.Interfaces;
using Itinerario.Application.Services;
using Itinerario.Domain.Entities;
using Itinerario.Domain.Enums;
using ItinerarioEntity = Itinerario.Domain.Entities.Itinerario;

namespace Itinerario.UnitTests.Services;

public class ItinerarioRefinerTests
{
    [Fact]
    public void Refinar_MasEconomico_SwapeaPorElMasBarato()
    {
        var refiner = CrearRefiner();
        var viaje = CrearViaje(presupuesto: NivelPresupuesto.SinRestricciones);
        var itinerario = CrearItinerario(viaje);
        var actualId = Guid.NewGuid();
        var baratoId = Guid.NewGuid();
        var equilibradoId = Guid.NewGuid();
        itinerario.Paradas.Add(CrearParada(actualId, Turno.Manana));

        var lugaresActuales = LugaresPorId(
            CrearLugar(id: actualId, turno: Turno.Manana, precio: NivelPresupuesto.SinRestricciones));

        var disponibles = new List<LugarCandidatoDto>
        {
            CrearLugar(id: equilibradoId, turno: Turno.Manana, precio: NivelPresupuesto.Equilibrado),
            CrearLugar(id: baratoId, turno: Turno.Manana, precio: NivelPresupuesto.Economico)
        };

        refiner.Refinar(TipoRefinamiento.MasEconomico, itinerario, lugaresActuales, disponibles, viaje);

        Assert.Equal(baratoId, itinerario.Paradas.Single().LugarId);
        Assert.Equal(OrigenParada.Refinamiento, itinerario.Paradas.Single().OrigenParada);
        Assert.DoesNotContain(disponibles, l => l.Id == baratoId);
        Assert.Contains(disponibles, l => l.Id == actualId);
    }

    [Fact]
    public void Refinar_MasEconomico_NoSwapeaSiNoHayMasBarato()
    {
        var refiner = CrearRefiner();
        var viaje = CrearViaje();
        var itinerario = CrearItinerario(viaje);
        var actualId = Guid.NewGuid();
        itinerario.Paradas.Add(CrearParada(actualId, Turno.Manana));

        var lugaresActuales = LugaresPorId(
            CrearLugar(id: actualId, turno: Turno.Manana, precio: NivelPresupuesto.Economico));

        var disponibles = new List<LugarCandidatoDto>
        {
            CrearLugar(turno: Turno.Manana, precio: NivelPresupuesto.Equilibrado)
        };

        refiner.Refinar(TipoRefinamiento.MasEconomico, itinerario, lugaresActuales, disponibles, viaje);

        Assert.Equal(actualId, itinerario.Paradas.Single().LugarId);
        Assert.Equal(OrigenParada.Ia, itinerario.Paradas.Single().OrigenParada);
    }

    [Fact]
    public void Refinar_ReducirCaminata_SwapeaPorElMasCercano()
    {
        var refiner = CrearRefiner();
        var viaje = CrearViaje();
        var itinerario = CrearItinerario(viaje, latitudOrigen: 0m, longitudOrigen: 0m);
        var actualId = Guid.NewGuid();
        var cercanoId = Guid.NewGuid();
        itinerario.Paradas.Add(CrearParada(actualId, Turno.Manana));

        var lugaresActuales = LugaresPorId(
            CrearLugar(id: actualId, turno: Turno.Manana, latitud: 0.01m, longitud: 0m));

        var disponibles = new List<LugarCandidatoDto>
        {
            CrearLugar(id: cercanoId, turno: Turno.Manana, latitud: 0.005m, longitud: 0m)
        };

        refiner.Refinar(TipoRefinamiento.ReducirCaminata, itinerario, lugaresActuales, disponibles, viaje);

        Assert.Equal(cercanoId, itinerario.Paradas.Single().LugarId);
        Assert.Equal(OrigenParada.Refinamiento, itinerario.Paradas.Single().OrigenParada);
        Assert.True(itinerario.Paradas.Single().DistanciaMetrosDesdeAnterior < 1000);
    }

    [Fact]
    public void Refinar_ReducirCaminata_NoSwapeaSiActualEsElMasCercano()
    {
        var refiner = CrearRefiner();
        var viaje = CrearViaje();
        var itinerario = CrearItinerario(viaje, latitudOrigen: 0m, longitudOrigen: 0m);
        var actualId = Guid.NewGuid();
        itinerario.Paradas.Add(CrearParada(actualId, Turno.Manana));

        var lugaresActuales = LugaresPorId(
            CrearLugar(id: actualId, turno: Turno.Manana, latitud: 0.001m, longitud: 0m));

        var disponibles = new List<LugarCandidatoDto>
        {
            CrearLugar(turno: Turno.Manana, latitud: 0.01m, longitud: 0m)
        };

        refiner.Refinar(TipoRefinamiento.ReducirCaminata, itinerario, lugaresActuales, disponibles, viaje);

        Assert.Equal(actualId, itinerario.Paradas.Single().LugarId);
        Assert.Equal(OrigenParada.Ia, itinerario.Paradas.Single().OrigenParada);
    }

    [Fact]
    public void Refinar_MasAireLibre_SwapeaParadaCerradaPorAireLibre()
    {
        var refiner = CrearRefiner();
        var viaje = CrearViaje();
        var itinerario = CrearItinerario(viaje);
        var actualId = Guid.NewGuid();
        var aireLibreId = Guid.NewGuid();
        itinerario.Paradas.Add(CrearParada(actualId, Turno.Manana));

        var lugaresActuales = LugaresPorId(
            CrearLugar(id: actualId, turno: Turno.Manana, aireLibre: false));

        var disponibles = new List<LugarCandidatoDto>
        {
            CrearLugar(id: aireLibreId, turno: Turno.Manana, aireLibre: true)
        };

        refiner.Refinar(TipoRefinamiento.MasAireLibre, itinerario, lugaresActuales, disponibles, viaje);

        Assert.Equal(aireLibreId, itinerario.Paradas.Single().LugarId);
        Assert.Equal(OrigenParada.Refinamiento, itinerario.Paradas.Single().OrigenParada);
    }

    [Fact]
    public void Refinar_MasAireLibre_NoSwapeaSiActualYaEsAireLibre()
    {
        var refiner = CrearRefiner();
        var viaje = CrearViaje();
        var itinerario = CrearItinerario(viaje);
        var actualId = Guid.NewGuid();
        itinerario.Paradas.Add(CrearParada(actualId, Turno.Manana));

        var lugaresActuales = LugaresPorId(
            CrearLugar(id: actualId, turno: Turno.Manana, aireLibre: true));

        var disponibles = new List<LugarCandidatoDto>
        {
            CrearLugar(turno: Turno.Manana, aireLibre: true)
        };

        refiner.Refinar(TipoRefinamiento.MasAireLibre, itinerario, lugaresActuales, disponibles, viaje);

        Assert.Equal(actualId, itinerario.Paradas.Single().LugarId);
        Assert.Equal(OrigenParada.Ia, itinerario.Paradas.Single().OrigenParada);
    }

    [Fact]
    public void Refinar_DevuelveElLugarReemplazadoAlPool()
    {
        var refiner = CrearRefiner();
        var viaje = CrearViaje(presupuesto: NivelPresupuesto.SinRestricciones);
        var itinerario = CrearItinerario(viaje);
        var parada1ActualId = Guid.NewGuid();
        var parada2ActualId = Guid.NewGuid();
        var candidatoId = Guid.NewGuid();

        itinerario.Paradas.Add(CrearParada(parada1ActualId, Turno.Manana, orden: 1));
        itinerario.Paradas.Add(CrearParada(parada2ActualId, Turno.Tarde, orden: 1));

        var lugaresActuales = LugaresPorId(
            CrearLugar(id: parada1ActualId, turno: Turno.Manana, precio: NivelPresupuesto.Equilibrado),
            CrearLugar(id: parada2ActualId, turno: Turno.Tarde, precio: NivelPresupuesto.SinRestricciones));

        var disponibles = new List<LugarCandidatoDto>
        {
            CrearLugar(id: candidatoId, turno: Turno.Manana, precio: NivelPresupuesto.Economico)
        };

        refiner.Refinar(TipoRefinamiento.MasEconomico, itinerario, lugaresActuales, disponibles, viaje);

        Assert.Equal(candidatoId, itinerario.Paradas.First(p => p.Turno == Turno.Manana).LugarId);
        Assert.Contains(disponibles, l => l.Id == parada1ActualId);
    }

    [Fact]
    public void SugerirTurnosLibres_CreaSugerenciaParaCadaTurnoVacio()
    {
        var refiner = CrearRefiner();
        var viaje = CrearViaje(alcance: AlcanceTemporal.DiaCompleto);
        var itinerario = CrearItinerario(viaje);
        itinerario.Paradas.Add(CrearParada(Guid.NewGuid(), Turno.Manana));

        var disponibles = new List<LugarCandidatoDto>
        {
            CrearLugar(turno: Turno.Tarde, precio: NivelPresupuesto.Economico),
            CrearLugar(turno: Turno.Noche, precio: NivelPresupuesto.Economico)
        };

        var sugerencias = refiner.SugerirTurnosLibres(itinerario, disponibles, viaje);

        Assert.Equal(2, sugerencias.Count);
        Assert.Contains(sugerencias, s => s.Turno == Turno.Tarde);
        Assert.Contains(sugerencias, s => s.Turno == Turno.Noche);
        Assert.All(sugerencias, s => Assert.Equal(OrigenParada.SugerenciaHueco, s.OrigenParada));
        Assert.All(sugerencias, s => Assert.Equal(itinerario.Id, s.ItinerarioId));
        Assert.Empty(disponibles);
    }

    [Fact]
    public void SugerirTurnosLibres_RespetaPresupuesto()
    {
        var refiner = CrearRefiner();
        var viaje = CrearViaje(alcance: AlcanceTemporal.DiaCompleto, presupuesto: NivelPresupuesto.Economico);
        var itinerario = CrearItinerario(viaje);
        itinerario.Paradas.Add(CrearParada(Guid.NewGuid(), Turno.Manana));

        var disponibles = new List<LugarCandidatoDto>
        {
            CrearLugar(turno: Turno.Tarde, precio: NivelPresupuesto.Equilibrado)
        };

        var sugerencias = refiner.SugerirTurnosLibres(itinerario, disponibles, viaje);

        Assert.Empty(sugerencias);
        Assert.Single(disponibles);
    }

    [Fact]
    public void SugerirTurnosLibres_NoSugiereParaTurnosOcupados()
    {
        var refiner = CrearRefiner();
        var viaje = CrearViaje(alcance: AlcanceTemporal.MananaAlmuerzo);
        var itinerario = CrearItinerario(viaje);
        itinerario.Paradas.Add(CrearParada(Guid.NewGuid(), Turno.Manana));
        itinerario.Paradas.Add(CrearParada(Guid.NewGuid(), Turno.Tarde));

        var disponibles = new List<LugarCandidatoDto>
        {
            CrearLugar(turno: Turno.Manana),
            CrearLugar(turno: Turno.Tarde),
            CrearLugar(turno: Turno.Noche)
        };

        var sugerencias = refiner.SugerirTurnosLibres(itinerario, disponibles, viaje);

        Assert.Empty(sugerencias);
        Assert.Equal(3, disponibles.Count);
    }

    private static ItinerarioRefiner CrearRefiner() => new(new GeoFalsoVariable());

    private static Viaje CrearViaje(
        AlcanceTemporal alcance = AlcanceTemporal.DiaCompleto,
        NivelPresupuesto presupuesto = NivelPresupuesto.SinRestricciones)
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
                RitmoCaminata.Activo,
                null,
                TipoPuntoPartida.NodoUrbano,
                null,
                null,
                null,
                0m,
                0m),
            Guid.NewGuid(),
            "CODIGO1234",
            new short[] { 1, 2 },
            null);

    private static ItinerarioEntity CrearItinerario(
        Viaje viaje,
        short numeroDia = 1,
        decimal latitudOrigen = -34.58m,
        decimal longitudOrigen = -58.42m)
        => new()
        {
            Id = Guid.NewGuid(),
            ViajeId = viaje.Id,
            NumeroDia = numeroDia,
            Fecha = new DateOnly(2026, 10, 19),
            OrigenTipo = TipoPuntoPartida.NodoUrbano,
            OrigenDescripcion = "Origen",
            OrigenLatitud = latitudOrigen,
            OrigenLongitud = longitudOrigen,
            Paradas = new List<ParadaItinerario>()
        };

    private static ParadaItinerario CrearParada(
        Guid lugarId,
        Turno turno,
        short orden = 1)
        => new()
        {
            Id = Guid.NewGuid(),
            LugarId = lugarId,
            Turno = turno,
            OrdenParada = orden,
            HoraInicio = new TimeOnly(9, 0),
            DuracionMinutos = 60,
            DistanciaMetrosDesdeAnterior = 1000,
            CuadrasDesdeAnterior = 10,
            MinutosAPieDesdeAnterior = 12,
            OrigenParada = OrigenParada.Ia,
            FechaCreacion = DateTimeOffset.UtcNow
        };

    private static LugarCandidatoDto CrearLugar(
        Guid? id = null,
        Turno? turno = null,
        NivelPresupuesto precio = NivelPresupuesto.Economico,
        bool aireLibre = false,
        decimal latitud = -34.58m,
        decimal longitud = -58.42m,
        short duracionMinutos = 60)
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            Nombre = "Lugar",
            Latitud = latitud,
            Longitud = longitud,
            Turno = turno ?? Turno.Manana,
            CategoriaId = 1,
            NivelPrecio = precio,
            EsAireLibre = aireLibre,
            DuracionSugeridaMinutos = duracionMinutos
        };

    private static IReadOnlyDictionary<Guid, LugarCandidatoDto> LugaresPorId(params LugarCandidatoDto[] lugares)
        => lugares.ToDictionary(l => l.Id);

    private sealed class GeoFalsoVariable : IGeoCalculator
    {
        private const double Escala = 100_000;

        public double DistanciaMetros(double latitud1, double longitud1, double latitud2, double longitud2)
        {
            var dLat = latitud2 - latitud1;
            var dLon = longitud2 - longitud1;
            return Math.Sqrt(dLat * dLat + dLon * dLon) * Escala;
        }

        public int Cuadras(double metros)
            => (int)Math.Round(metros / 100, MidpointRounding.AwayFromZero);

        public int MinutosAPie(double metros)
            => (int)Math.Round(metros / 80, MidpointRounding.AwayFromZero);
    }
}
