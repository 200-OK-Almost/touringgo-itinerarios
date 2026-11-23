using Itinerario.Application;
using Itinerario.Application.DTOs;
using Itinerario.Application.Interfaces;
using Itinerario.Application.Services;
using Itinerario.Domain.Entities;
using Itinerario.Domain.Enums;
using Itinerario.Domain.Exceptions;

namespace Itinerario.UnitTests.Services;

public class ViajeServiceTests
{
    private static readonly DateOnly Inicio = new(2026, 12, 1);

    [Fact]
    public async Task CrearAsync_MapeaModalidadYCategoriasPorDia()
    {
        var repo = new ViajeRepositoryFalso();
        var service = new ViajeService(repo, new LugaresClientFalso(), new UsuariosClientFalso());
        var dto = CrearDto(
            CrearDia(1, Inicio, BarrioCaba.Palermo, TipoViaje.Solo,
                "cafe_especialidad", "museos_galerias"),
            CrearDia(2, Inicio.AddDays(1), BarrioCaba.Recoleta, TipoViaje.Familia,
                "parques_aire_libre", "gastronomia_clasica"));

        var respuesta = await service.CrearAsync(dto, Guid.NewGuid());

        Assert.NotNull(respuesta.Dias);
        var dia1 = respuesta.Dias.Single(dia => dia.NumeroDia == 1);
        var dia2 = respuesta.Dias.Single(dia => dia.NumeroDia == 2);

        Assert.Equal(TipoViaje.Solo, dia1.TipoViaje);
        Assert.Equal(
            new[] { "cafe_especialidad", "museos_galerias" },
            dia1.CategoriaCodigos);
        Assert.Equal(TipoViaje.Familia, dia2.TipoViaje);
        Assert.Equal(
            new[] { "parques_aire_libre", "gastronomia_clasica" },
            dia2.CategoriaCodigos);
    }

    [Fact]
    public async Task CrearAsync_SinPreferenciasPorDia_UsaLasGlobales()
    {
        var repo = new ViajeRepositoryFalso();
        var service = new ViajeService(repo, new LugaresClientFalso(), new UsuariosClientFalso());
        var dto = CrearDto(
            CrearDia(1, Inicio, BarrioCaba.Palermo, null));
        dto.TipoViaje = TipoViaje.Amigos;

        var respuesta = await service.CrearAsync(dto, Guid.NewGuid());

        var dia = respuesta.Dias!.Single();
        Assert.Equal(TipoViaje.Amigos, dia.TipoViaje);
        Assert.Equal(dto.CategoriaCodigos, dia.CategoriaCodigos);
    }

    [Fact]
    public async Task ActualizarAsync_SincronizaPreferenciasPorDia()
    {
        var viaje = CrearViajeExistente();
        var repo = new ViajeRepositoryFalso { ViajeExistente = viaje };
        var service = new ViajeService(repo, new LugaresClientFalso(), new UsuariosClientFalso());
        var dto = new ViajeUpdateDto
        {
            Titulo = "Viaje actualizado",
            TipoViaje = TipoViaje.Solo,
            Estado = EstadoViaje.Borrador,
            Barrio = BarrioCaba.Palermo,
            AlcanceTemporal = AlcanceTemporal.DiaCompleto,
            FechaInicio = Inicio,
            FechaFin = Inicio.AddDays(1),
            NivelPresupuesto = NivelPresupuesto.Equilibrado,
            RitmoCaminata = RitmoCaminata.Activo,
            TipoPuntoPartida = TipoPuntoPartida.NodoUrbano,
            CategoriaCodigos = new[] { "cafe_especialidad", "museos_galerias" },
            Dias = new[]
            {
                CrearDia(1, Inicio, BarrioCaba.Palermo, TipoViaje.Solo,
                    "cafe_especialidad", "museos_galerias"),
                CrearDia(2, Inicio.AddDays(1), BarrioCaba.Recoleta, TipoViaje.Familia,
                    "parques_aire_libre", "gastronomia_clasica")
            }
        };

        await service.ActualizarAsync(
            viaje.Id,
            dto,
            viaje.CreadoPorUsuarioId);

        Assert.NotNull(repo.DiasSincronizados);
        var dia2 = repo.DiasSincronizados!.Single(dia => dia.NumeroDia == 2);
        Assert.Equal(TipoViaje.Familia, dia2.TipoViaje);
        Assert.Equal(
            new[] { "gastronomia_clasica", "parques_aire_libre" },
            dia2.Etiquetas.Select(e => e.CategoriaCodigo).OrderBy(c => c).ToArray());
    }

    private static ViajeCreateDto CrearDto(params ViajeDiaCreateDto[] dias)
        => new()
        {
            Titulo = "Viaje por día",
            TipoViaje = TipoViaje.Solo,
            Barrio = dias[0].Barrio,
            AlcanceTemporal = AlcanceTemporal.DiaCompleto,
            FechaInicio = dias[0].Fecha,
            FechaFin = dias[^1].Fecha,
            NivelPresupuesto = NivelPresupuesto.Equilibrado,
            RitmoCaminata = RitmoCaminata.Activo,
            TipoPuntoPartida = TipoPuntoPartida.NodoUrbano,
            CategoriaCodigos = new[] { "cafe_especialidad", "museos_galerias" },
            Dias = dias
        };

    private static ViajeDiaCreateDto CrearDia(
        short numeroDia,
        DateOnly fecha,
        BarrioCaba barrio,
        TipoViaje? tipoViaje,
        params string[] categoriaCodigos)
        => new()
        {
            NumeroDia = numeroDia,
            Fecha = fecha,
            Barrio = barrio,
            TipoPuntoPartida = TipoPuntoPartida.NodoUrbano,
            TipoViaje = tipoViaje,
            CategoriaCodigos = categoriaCodigos
        };

    private static Viaje CrearViajeExistente()
        => Viaje.Crear(
            Guid.NewGuid(),
            new DatosViaje(
                Titulo: "Viaje existente",
                TipoViaje: TipoViaje.Solo,
                Barrio: BarrioCaba.Palermo,
                AlcanceTemporal: AlcanceTemporal.DiaCompleto,
                FechaInicio: Inicio,
                FechaFin: Inicio.AddDays(1),
                NivelPresupuesto: NivelPresupuesto.Equilibrado,
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

    private sealed class ViajeRepositoryFalso : IViajeRepository
    {
        public Viaje? ViajeExistente { get; set; }
        public IReadOnlyCollection<ViajeDia>? DiasSincronizados { get; private set; }

        public Task<Viaje?> ObtenerPorIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
            => Task.FromResult(ViajeExistente?.Id == id ? ViajeExistente : null);

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
            => DiasSincronizados = dias;

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

        public Task<ICollection<MiembroViaje>> ObtenerMiembrosDeViajeAsync(Guid viajeId)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class LugaresClientFalso : ILugaresClient
    {
        public Task<LugarCandidatoDto?> ObtenerPorIdAsync(
            Guid lugarId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<LugarCandidatoDto?>(null);

        public Task<IReadOnlyCollection<LugarCandidatoDto>> BuscarAsync(
            BarrioCaba barrio,
            IReadOnlyCollection<string> categoriaCodigos,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<LugarCandidatoDto>>(
                Array.Empty<LugarCandidatoDto>());

        public Task<IReadOnlyCollection<LugarCandidatoDto>> BuscarSemanticoAsync(
            BusquedaSemanticaLugaresDto busqueda,
            CancellationToken cancellationToken = default)
            => throw new ServicioExternoNoDisponibleException("Sin búsqueda semántica en tests.");
    }
}
