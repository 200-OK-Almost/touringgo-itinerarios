using Itinerario.Application;
using Itinerario.Application.DTOs;
using Itinerario.Application.Interfaces;
using Itinerario.Application.Services;
using Itinerario.Domain.Entities;
using Itinerario.Domain.Enums;
using Itinerario.Domain.Exceptions;
using ItinerarioEntity = Itinerario.Domain.Entities.Itinerario;

namespace Itinerario.UnitTests.Services;

public class ItinerarioServiceTests
{
    private static readonly DateOnly Inicio = new(2026, 11, 18);

    [Fact]
    public async Task GenerarAsync_UsaElBarrioDeCadaDia()
    {
        var viaje = CrearViajeConDias(
            (BarrioCaba.Palermo, Inicio),
            (BarrioCaba.Recoleta, Inicio.AddDays(1)));

        var lugares = new LugaresClientFalso();
        var service = CrearService(viaje, lugares, new ItinerarioRepositoryFalso());

        var resultado = await service.GenerarAsync(viaje.Id, viaje.CreadoPorUsuarioId);

        var idsPalermo = lugares.LugaresDe(BarrioCaba.Palermo).Select(l => l.Id).ToHashSet();
        var idsRecoleta = lugares.LugaresDe(BarrioCaba.Recoleta).Select(l => l.Id).ToHashSet();

        Assert.Equal(2, resultado.Count);
        Assert.All(
            resultado.Single(i => i.NumeroDia == 1).Paradas,
            parada => Assert.Contains(parada.LugarId, idsPalermo));
        Assert.All(
            resultado.Single(i => i.NumeroDia == 2).Paradas,
            parada => Assert.Contains(parada.LugarId, idsRecoleta));
        Assert.Contains(BarrioCaba.Palermo, lugares.BarriosSolicitados);
        Assert.Contains(BarrioCaba.Recoleta, lugares.BarriosSolicitados);
        Assert.All(
            lugares.Solicitudes,
            solicitud => Assert.Contains("cafe_especialidad", solicitud.Categorias));
    }

    [Fact]
    public async Task GenerarAsync_UsaLasCategoriasDeCadaDia()
    {
        var viaje = CrearViajeConDias(
            (BarrioCaba.Palermo, Inicio),
            (BarrioCaba.Recoleta, Inicio.AddDays(1)));
        AgregarCategoriasDia(viaje, 1, "cafe_especialidad", "museos_galerias");
        AgregarCategoriasDia(viaje, 2, "parques_aire_libre", "gastronomia_clasica");

        var lugares = new LugaresClientFalso();
        var service = CrearService(viaje, lugares, new ItinerarioRepositoryFalso());

        await service.GenerarAsync(viaje.Id, viaje.CreadoPorUsuarioId);

        Assert.Contains(
            lugares.Solicitudes,
            solicitud => solicitud.Barrio == BarrioCaba.Palermo
                && solicitud.Categorias.Contains("cafe_especialidad")
                && solicitud.Categorias.Contains("museos_galerias"));
        Assert.Contains(
            lugares.Solicitudes,
            solicitud => solicitud.Barrio == BarrioCaba.Recoleta
                && solicitud.Categorias.Contains("parques_aire_libre")
                && solicitud.Categorias.Contains("gastronomia_clasica"));
    }

    [Fact]
    public async Task GenerarAsync_RespetaLasReglasHorariasDeCadaTurno()
    {
        var viaje = CrearViajeConDias((BarrioCaba.Palermo, Inicio));
        AgregarCategoriasDia(
            viaje,
            1,
            "cafe_especialidad",
            "gastronomia_clasica",
            "bares_vermu",
            "cocteleria_autor");

        var cafe = CrearLugarConCategoria("cafe_especialidad");
        var restaurante = CrearLugarConCategoria("gastronomia_clasica");
        var bar = CrearLugarConCategoria("bares_vermu");
        var cocteleria = CrearLugarConCategoria("cocteleria_autor");

        var lugares = new LugaresClientFalso();
        lugares.EstablecerLugares(
            BarrioCaba.Palermo,
            cafe,
            restaurante,
            bar,
            cocteleria);

        var service = CrearService(viaje, lugares, new ItinerarioRepositoryFalso());

        var resultado = await service.GenerarAsync(viaje.Id, viaje.CreadoPorUsuarioId);
        var paradas = resultado.Single().Paradas.ToList();

        Assert.Equal(4, paradas.Count);
        Assert.Equal(Turno.Manana, paradas[0].Turno);
        Assert.Equal(cafe.Id, paradas[0].LugarId);
        Assert.Equal(Turno.Mediodia, paradas[1].Turno);
        Assert.Equal(restaurante.Id, paradas[1].LugarId);
        Assert.Equal(Turno.Tarde, paradas[2].Turno);
        Assert.Equal(bar.Id, paradas[2].LugarId);
        Assert.Equal(Turno.Noche, paradas[3].Turno);
        Assert.Equal(cocteleria.Id, paradas[3].LugarId);
    }

    [Fact]
    public async Task GenerarAsync_SinDias_UsaElBarrioDelViaje()
    {
        var viaje = CrearViajeConDias(
            (BarrioCaba.Palermo, Inicio),
            (BarrioCaba.Recoleta, Inicio.AddDays(1)));
        viaje.Dias.Clear();

        var lugares = new LugaresClientFalso();
        var service = CrearService(viaje, lugares, new ItinerarioRepositoryFalso());

        var resultado = await service.GenerarAsync(viaje.Id, viaje.CreadoPorUsuarioId);

        var idsPalermo = lugares.LugaresDe(BarrioCaba.Palermo).Select(l => l.Id).ToHashSet();

        Assert.Equal(2, resultado.Count);
        Assert.All(
            resultado.SelectMany(itinerario => itinerario.Paradas),
            parada => Assert.Contains(parada.LugarId, idsPalermo));
        Assert.DoesNotContain(BarrioCaba.Recoleta, lugares.BarriosSolicitados);
        Assert.All(
            lugares.Solicitudes,
            solicitud => Assert.Contains("cafe_especialidad", solicitud.Categorias));
    }

    [Fact]
    public async Task RefinarAsync_ConsultaElBarrioDeCadaDia()
    {
        var viaje = CrearViajeConDias(
            (BarrioCaba.Palermo, Inicio),
            (BarrioCaba.Recoleta, Inicio.AddDays(1)));

        var lugares = new LugaresClientFalso();
        var itinerarios = new[]
        {
            CrearItinerario(viaje, 1, lugares.LugaresDe(BarrioCaba.Palermo).First().Id),
            CrearItinerario(viaje, 2, lugares.LugaresDe(BarrioCaba.Recoleta).First().Id)
        };
        var repo = new ItinerarioRepositoryFalso { Itinerarios = itinerarios };
        var service = CrearService(viaje, lugares, repo);

        await service.RefinarAsync(
            viaje.Id,
            TipoRefinamiento.MasEconomico,
            null,
            viaje.CreadoPorUsuarioId);

        Assert.Contains(BarrioCaba.Palermo, lugares.BarriosSolicitados);
        Assert.Contains(BarrioCaba.Recoleta, lugares.BarriosSolicitados);
        Assert.All(
            lugares.Solicitudes,
            solicitud => Assert.Contains("cafe_especialidad", solicitud.Categorias));
    }

    [Fact]
    public async Task RefinarAsync_ConsultaCategoriasDeCadaDia()
    {
        var viaje = CrearViajeConDias(
            (BarrioCaba.Palermo, Inicio),
            (BarrioCaba.Recoleta, Inicio.AddDays(1)));
        AgregarCategoriasDia(viaje, 1, "cafe_especialidad", "museos_galerias");
        AgregarCategoriasDia(viaje, 2, "parques_aire_libre", "gastronomia_clasica");

        var lugares = new LugaresClientFalso();
        var itinerarios = new[]
        {
            CrearItinerario(viaje, 1, lugares.LugaresDe(BarrioCaba.Palermo).First().Id),
            CrearItinerario(viaje, 2, lugares.LugaresDe(BarrioCaba.Recoleta).First().Id)
        };
        var repo = new ItinerarioRepositoryFalso { Itinerarios = itinerarios };
        var service = CrearService(viaje, lugares, repo);

        await service.RefinarAsync(
            viaje.Id,
            TipoRefinamiento.MasEconomico,
            null,
            viaje.CreadoPorUsuarioId);

        Assert.Contains(
            lugares.Solicitudes,
            solicitud => solicitud.Barrio == BarrioCaba.Palermo
                && solicitud.Categorias.Contains("cafe_especialidad"));
        Assert.Contains(
            lugares.Solicitudes,
            solicitud => solicitud.Barrio == BarrioCaba.Recoleta
                && solicitud.Categorias.Contains("parques_aire_libre"));
    }

    [Fact]
    public async Task SugerirTurnosLibresAsync_UsaElBarrioDeCadaDia()
    {
        var viaje = CrearViajeConDias(
            (BarrioCaba.Palermo, Inicio),
            (BarrioCaba.Recoleta, Inicio.AddDays(1)));
        AgregarCategoriasDia(viaje, 1, "cafe_especialidad", "museos_galerias");
        AgregarCategoriasDia(viaje, 2, "parques_aire_libre", "gastronomia_clasica");

        var lugares = new LugaresClientFalso();
        var itinerario1 = CrearItinerario(viaje, 1);
        var itinerario2 = CrearItinerario(viaje, 2);
        var repo = new ItinerarioRepositoryFalso
        {
            Itinerarios = new[] { itinerario1, itinerario2 }
        };
        var service = CrearService(viaje, lugares, repo);

        await service.SugerirTurnosLibresAsync(
            viaje.Id,
            null,
            viaje.CreadoPorUsuarioId);

        var idsPalermo = lugares.LugaresDe(BarrioCaba.Palermo).Select(l => l.Id).ToHashSet();
        var idsRecoleta = lugares.LugaresDe(BarrioCaba.Recoleta).Select(l => l.Id).ToHashSet();

        Assert.Contains(BarrioCaba.Palermo, lugares.BarriosSolicitados);
        Assert.Contains(BarrioCaba.Recoleta, lugares.BarriosSolicitados);
        Assert.All(
            repo.ParadasAgregadas.Where(p => p.ItinerarioId == itinerario1.Id),
            parada => Assert.Contains(parada.LugarId, idsPalermo));
        Assert.All(
            repo.ParadasAgregadas.Where(p => p.ItinerarioId == itinerario2.Id),
            parada => Assert.Contains(parada.LugarId, idsRecoleta));
        Assert.Contains(
            lugares.Solicitudes,
            solicitud => solicitud.Barrio == BarrioCaba.Palermo
                && solicitud.Categorias.Contains("cafe_especialidad"));
        Assert.Contains(
            lugares.Solicitudes,
            solicitud => solicitud.Barrio == BarrioCaba.Recoleta
                && solicitud.Categorias.Contains("parques_aire_libre"));
    }

    private static ItinerarioService CrearService(
        Viaje viaje,
        LugaresClientFalso lugares,
        ItinerarioRepositoryFalso itinerarios)
    {
        var geo = new GeoFalso();

        return new ItinerarioService(
            new ViajeRepositoryFalso(viaje),
            itinerarios,
            lugares,
            new ItinerarioGenerator(geo),
            new ItinerarioRefiner(geo),
            new UsuariosClientFalso(),
            new OllamaClientFalso(),
            new InterpretacionServiceFalso());
    }

    private static Viaje CrearViajeConDias(
        params (BarrioCaba Barrio, DateOnly Fecha)[] dias)
    {
        var viaje = Viaje.Crear(
            Guid.NewGuid(),
            new DatosViaje(
                Titulo: "Viaje por día",
                TipoViaje: TipoViaje.Solo,
                Barrio: dias[0].Barrio,
                AlcanceTemporal: AlcanceTemporal.DiaCompleto,
                FechaInicio: dias[0].Fecha,
                FechaFin: dias[^1].Fecha,
                NivelPresupuesto: NivelPresupuesto.SinRestricciones,
                RitmoCaminata: RitmoCaminata.Activo,
                LimiteCuadrasEntreParadas: null,
                TipoPuntoPartida: TipoPuntoPartida.NodoUrbano,
                LugarPartidaId: null,
                NombrePuntoPartida: null,
                DireccionPartida: null,
                LatitudPartida: -34.58m,
                LongitudPartida: -58.42m),
            Guid.NewGuid(),
            "CODIGO1234",
            new[] { "cafe_especialidad", "museos_galerias" },
            null);

        for (var indice = 0; indice < dias.Length; indice++)
        {
            viaje.Dias.Add(new ViajeDia
            {
                Id = Guid.NewGuid(),
                ViajeId = viaje.Id,
                NumeroDia = (short)(indice + 1),
                Fecha = dias[indice].Fecha,
                Barrio = dias[indice].Barrio,
                TipoPuntoPartida = TipoPuntoPartida.NodoUrbano,
                LatitudPartida = -34.58m,
                LongitudPartida = -58.42m
            });
        }

        return viaje;
    }

    private static LugarCandidatoDto CrearLugarConCategoria(
        string categoriaCodigo)
        => new()
        {
            Id = Guid.NewGuid(),
            Nombre = categoriaCodigo,
            Barrio = BarrioCaba.Palermo,
            Latitud = -34.58m,
            Longitud = -58.42m,
            NivelPrecio = NivelPresupuesto.SinRestricciones,
            DuracionSugeridaMinutos = 60,
            Categorias = new[] { categoriaCodigo }
        };

    private static void AgregarCategoriasDia(
        Viaje viaje,
        short numeroDia,
        params string[] categoriaCodigos)
    {
        var dia = viaje.Dias.Single(d => d.NumeroDia == numeroDia);

        foreach (var categoriaCodigo in categoriaCodigos)
        {
            dia.Etiquetas.Add(new EtiquetaViajeDia
            {
                ViajeDiaId = dia.Id,
                CategoriaCodigo = categoriaCodigo
            });
        }
    }

    private static ItinerarioEntity CrearItinerario(
        Viaje viaje,
        short numeroDia,
        Guid? lugarId = null)
    {
        var dia = viaje.Dias.Single(d => d.NumeroDia == numeroDia);
        var itinerarioId = Guid.NewGuid();
        var paradas = new List<ParadaItinerario>();

        if (lugarId.HasValue)
        {
            paradas.Add(new ParadaItinerario
            {
                Id = Guid.NewGuid(),
                ItinerarioId = itinerarioId,
                LugarId = lugarId.Value,
                Turno = Turno.Manana,
                OrdenParada = 1,
                DuracionMinutos = 60,
                OrigenParada = OrigenParada.Ia,
                FechaCreacion = DateTimeOffset.UtcNow
            });
        }

        return new ItinerarioEntity
        {
            Id = itinerarioId,
            ViajeId = viaje.Id,
            NumeroDia = numeroDia,
            Fecha = dia.Fecha,
            OrigenTipo = dia.TipoPuntoPartida,
            OrigenDescripcion = "Origen",
            OrigenLatitud = dia.LatitudPartida,
            OrigenLongitud = dia.LongitudPartida,
            Paradas = paradas
        };
    }

    private sealed class LugaresClientFalso : ILugaresClient
    {
        private readonly Dictionary<BarrioCaba, IReadOnlyCollection<LugarCandidatoDto>> _lugares = new();

        public List<BarrioCaba> BarriosSolicitados { get; } = new();

        public List<(BarrioCaba Barrio, IReadOnlyCollection<string> Categorias)> Solicitudes { get; } = new();

        public void EstablecerLugares(
            BarrioCaba barrio,
            params LugarCandidatoDto[] lugares)
            => _lugares[barrio] = lugares;

        public IReadOnlyCollection<LugarCandidatoDto> LugaresDe(BarrioCaba barrio)
        {
            if (!_lugares.TryGetValue(barrio, out var lugares))
            {
                lugares = Enumerable.Range(1, 4)
                    .Select(indice => CrearLugar(barrio, indice))
                    .ToList();
                _lugares[barrio] = lugares;
            }

            return lugares;
        }

        public Task<IReadOnlyCollection<LugarCandidatoDto>> BuscarAsync(
            BarrioCaba barrio,
            IReadOnlyCollection<string> categoriaCodigos,
            CancellationToken cancellationToken = default)
        {
            BarriosSolicitados.Add(barrio);
            Solicitudes.Add((barrio, categoriaCodigos.ToList()));
            return Task.FromResult(LugaresDe(barrio));
        }

        public Task<IReadOnlyCollection<LugarCandidatoDto>> BuscarSemanticoAsync(
            BusquedaSemanticaLugaresDto busqueda,
            CancellationToken cancellationToken = default)
            => throw new ServicioExternoNoDisponibleException("Sin búsqueda semántica en tests.");

        public Task<LugarCandidatoDto?> ObtenerPorIdAsync(
            Guid lugarId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<LugarCandidatoDto?>(null);

        private static LugarCandidatoDto CrearLugar(BarrioCaba barrio, int indice) => new()
        {
            Id = Guid.NewGuid(),
            Nombre = $"{barrio}-{indice}",
            Barrio = barrio,
            Latitud = -34.58m,
            Longitud = -58.42m,
            NivelPrecio = NivelPresupuesto.SinRestricciones,
            DuracionSugeridaMinutos = 60
        };
    }

    private sealed class ViajeRepositoryFalso : IViajeRepository
    {
        private readonly Viaje _viaje;

        public ViajeRepositoryFalso(Viaje viaje)
        {
            _viaje = viaje;
        }

        public Task<Viaje?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<Viaje?>(_viaje.Id == id ? _viaje : null);

        public Task<ResultadoPaginado<Viaje>> ObtenerPaginadoPorUsuarioAsync(
            Guid usuarioId,
            int pagina,
            int tamano,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Viaje?> ObtenerPorCodigoInvitacionAsync(
            string codigoInvitacion,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> ExisteCodigoInvitacionAsync(
            string codigoInvitacion,
            CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task AgregarAsync(Viaje viaje, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void Actualizar(Viaje viaje)
        {
        }

        public void Eliminar(Viaje viaje)
        {
        }

        public void AgregarMiembro(MiembroViaje miembro)
        {
        }

        public void SincronizarEtiquetas(
            Viaje viaje,
            IReadOnlyCollection<string> categoriaCodigos)
        {
        }

        public void SincronizarDias(Viaje viaje, IReadOnlyCollection<ViajeDia> dias)
        {
        }

        public void EstablecerParametrosFamilia(
            Viaje viaje,
            ParametrosFamilia? parametros)
        {
        }

        public void Descartar(Viaje viaje)
        {
        }

        public Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(0);
    }

    private sealed class ItinerarioRepositoryFalso : IItinerarioRepository
    {
        public IReadOnlyCollection<ItinerarioEntity> Itinerarios { get; set; }
            = Array.Empty<ItinerarioEntity>();

        public IReadOnlyCollection<ItinerarioEntity>? Reemplazados { get; private set; }

        public List<ParadaItinerario> ParadasAgregadas { get; } = new();

        public Task<IReadOnlyCollection<ItinerarioEntity>> ObtenerPorViajeAsync(
            Guid viajeId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Itinerarios);

        public Task ReemplazarAsync(
            Guid viajeId,
            IReadOnlyCollection<ItinerarioEntity> itinerarios,
            CancellationToken cancellationToken = default)
        {
            Reemplazados = itinerarios;
            Itinerarios = itinerarios;
            return Task.CompletedTask;
        }

        public Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public void AgregarParada(ParadaItinerario parada)
            => ParadasAgregadas.Add(parada);
    }

    private sealed class UsuariosClientFalso : IUsuariosClient
    {
        public Task<PerfilViajeroDto> ObtenerPerfilAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult(new PerfilViajeroDto());
    }

    private sealed class OllamaClientFalso : IOllamaClient
    {
        public Task<T> ChatJsonAsync<T>(
            string prompt,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<string> ChatTextoAsync(
            string prompt,
            CancellationToken cancellationToken = default)
            => Task.FromResult("Sugerencia de prueba");
    }

    private sealed class InterpretacionServiceFalso : IInterpretacionService
    {
        public Task<BosquejoViajeDto> InterpretarAsync(
            string texto,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TipoRefinamiento> InterpretarRefinamientoAsync(
            string texto,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class GeoFalso : IGeoCalculator
    {
        public double DistanciaMetros(
            double latitud1,
            double longitud1,
            double latitud2,
            double longitud2)
            => 0;

        public int Cuadras(double metros)
            => (int)Math.Round(metros / 100d, MidpointRounding.AwayFromZero);

        public int MinutosAPie(double metros)
            => (int)Math.Round(metros / 80d, MidpointRounding.AwayFromZero);
    }
}
