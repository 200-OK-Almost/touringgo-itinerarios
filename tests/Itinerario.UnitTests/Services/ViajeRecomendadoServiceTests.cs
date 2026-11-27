using Itinerario.Application.DTOs;
using Itinerario.Application.Interfaces;
using Itinerario.Application.Services;
using Itinerario.Domain.Enums;
using Itinerario.Domain.Exceptions;

namespace Itinerario.UnitTests.Services;

public class ViajeRecomendadoServiceTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static readonly Guid ViajeId = Guid.NewGuid();
    private static readonly DateOnly Fecha = new(2026, 11, 27);

    [Fact]
    public async Task CrearAsync_ResuelveLugaresCreaViajeYItinerario()
    {
        var lugares = new[]
        {
            TestData.CrearLugar("Uno", "museos_galerias"),
            TestData.CrearLugar("Dos", "cafe_especialidad"),
            TestData.CrearLugar("Tres", "gastronomia_clasica"),
            TestData.CrearLugar("Cuatro", "parques_aire_libre")
        };

        var lugaresClient = new LugaresClientFalso();
        foreach (var lugar in lugares)
        {
            lugaresClient.PorId[lugar.Id] = lugar;
        }

        var esperado = new ViajeResponseDto
        {
            Id = ViajeId,
            Estado = EstadoViaje.Planificado
        };

        var viajeService = new ViajeServiceFalso
        {
            ViajeObtenido = esperado
        };
        var itinerarioService = new ItinerarioServiceFalso();

        var service = new ViajeRecomendadoService(
            viajeService,
            itinerarioService,
            lugaresClient);

        var dto = CrearDto(lugares.Select(lugar => lugar.Id).ToList());
        var resultado = await service.CrearAsync(dto, UsuarioId);

        Assert.Same(esperado, resultado);

        var capturado = viajeService.DtoCreado;
        Assert.NotNull(capturado);
        Assert.Equal(dto.Titulo, capturado!.Titulo);
        Assert.Equal(dto.Barrio, capturado.Barrio);
        Assert.Equal(TipoViaje.Solo, capturado.TipoViaje);
        Assert.Equal(AlcanceTemporal.DiaCompleto, capturado.AlcanceTemporal);
        Assert.Equal(NivelPresupuesto.Equilibrado, capturado.NivelPresupuesto);
        Assert.Equal(RitmoCaminata.Tranquilo, capturado.RitmoCaminata);
        Assert.Equal(TipoPuntoPartida.PrimeraParada, capturado.TipoPuntoPartida);
        Assert.Equal(dto.FechaInicio, capturado.FechaInicio);
        Assert.Equal(dto.FechaFin, capturado.FechaFin);
        Assert.Equal(
            new[]
            {
                "museos_galerias",
                "cafe_especialidad",
                "gastronomia_clasica",
                "parques_aire_libre"
            },
            capturado.CategoriaCodigos);

        Assert.Equal(1, itinerarioService.LlamadasCrearManual);
        Assert.Equal(ViajeId, itinerarioService.ViajeIdRecibido);
        Assert.Equal(dto.LugarIds, itinerarioService.LugarIdsRecibidos);
        Assert.Equal(0, viajeService.LlamadasEliminar);
    }

    [Fact]
    public async Task CrearAsync_FallaElItinerario_EliminaElViajeYRelanza()
    {
        var lugar1 = TestData.CrearLugar("Uno");
        var lugar2 = TestData.CrearLugar("Dos");
        var lugaresClient = new LugaresClientFalso();
        lugaresClient.PorId[lugar1.Id] = lugar1;
        lugaresClient.PorId[lugar2.Id] = lugar2;

        var viajeService = new ViajeServiceFalso();
        var itinerarioService = new ItinerarioServiceFalso
        {
            ErrorAlCrearManual = new InvalidOperationException("boom")
        };

        var service = new ViajeRecomendadoService(
            viajeService,
            itinerarioService,
            lugaresClient);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CrearAsync(CrearDto(new[] { lugar1.Id, lugar2.Id }), UsuarioId));

        Assert.Equal(1, viajeService.LlamadasEliminar);
        Assert.Equal(ViajeId, Assert.Single(viajeService.Eliminados));
    }

    [Fact]
    public async Task CrearAsync_LugarInexistente_LanzaNotFoundSinCrearViaje()
    {
        var viajeService = new ViajeServiceFalso();
        var itinerarioService = new ItinerarioServiceFalso();

        var service = new ViajeRecomendadoService(
            viajeService,
            itinerarioService,
            new LugaresClientFalso());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.CrearAsync(CrearDto(new[] { Guid.NewGuid(), Guid.NewGuid() }), UsuarioId));

        Assert.Equal(0, viajeService.LlamadasCrear);
        Assert.Equal(0, itinerarioService.LlamadasCrearManual);
    }

    [Fact]
    public async Task CrearAsync_SinCategorias_UsaFallbackDeDosCategorias()
    {
        var lugares = new[]
        {
            TestData.CrearLugar("Uno"),
            TestData.CrearLugar("Dos")
        };
        foreach (var lugar in lugares)
        {
            lugar.Categorias = Array.Empty<string>();
        }

        var lugaresClient = new LugaresClientFalso();
        foreach (var lugar in lugares)
        {
            lugaresClient.PorId[lugar.Id] = lugar;
        }

        var viajeService = new ViajeServiceFalso();
        var itinerarioService = new ItinerarioServiceFalso();

        var service = new ViajeRecomendadoService(
            viajeService,
            itinerarioService,
            lugaresClient);

        var dto = CrearDto(lugares.Select(lugar => lugar.Id).ToList());
        dto.CategoriaCodigo = null;

        await service.CrearAsync(dto, UsuarioId);

        var capturado = viajeService.DtoCreado;
        Assert.NotNull(capturado);
        Assert.Equal(2, capturado!.CategoriaCodigos.Count);
        Assert.Contains("gastronomia_clasica", capturado.CategoriaCodigos);
        Assert.Contains("cafe_especialidad", capturado.CategoriaCodigos);
    }

    private static CrearViajeRecomendadoDto CrearDto(IReadOnlyCollection<Guid> lugarIds)
        => new()
        {
            Titulo = "Recorrido de prueba",
            Barrio = BarrioCaba.Palermo,
            CategoriaCodigo = "museos_galerias",
            LugarIds = lugarIds,
            FechaInicio = Fecha,
            FechaFin = Fecha
        };

    private sealed class ViajeServiceFalso : IViajeService
    {
        public ViajeCreateDto? DtoCreado { get; private set; }

        public ViajeResponseDto? ViajeObtenido { get; set; }

        public int LlamadasCrear { get; private set; }

        public int LlamadasEliminar { get; private set; }

        public List<Guid> Eliminados { get; } = new();

        public Task<ViajeResponseDto> CrearAsync(
            ViajeCreateDto dto,
            Guid creadoPorUsuarioId,
            CancellationToken cancellationToken = default)
        {
            LlamadasCrear++;
            DtoCreado = dto;

            return Task.FromResult(new ViajeResponseDto { Id = ViajeId });
        }

        public Task<ViajeResponseDto> ObtenerPorIdAsync(
            Guid id,
            Guid usuarioId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                ViajeObtenido ?? new ViajeResponseDto
                {
                    Id = id,
                    Estado = EstadoViaje.Planificado
                });

        public Task<ResultadoPaginadoDto<ViajeResponseDto>> ObtenerTodosAsync(
            Guid usuarioId,
            int pagina = 1,
            int tamano = 20,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ViajeResponseDto> ActualizarAsync(
            Guid id,
            ViajeUpdateDto dto,
            Guid usuarioIdSolicitante,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task EliminarAsync(
            Guid id,
            Guid usuarioIdSolicitante,
            CancellationToken cancellationToken = default)
        {
            LlamadasEliminar++;
            Eliminados.Add(id);
            return Task.CompletedTask;
        }

        public Task<ViajeResponseDto> UnirseAsync(
            string codigoInvitacion,
            Guid usuarioId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ICollection<MiembroViajeDto>> ObtenerMiembrosViaje(
            Guid viajeId,
            Guid usuarioId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class ItinerarioServiceFalso : IItinerarioService
    {
        public int LlamadasCrearManual { get; private set; }

        public Guid? ViajeIdRecibido { get; private set; }

        public List<Guid> LugarIdsRecibidos { get; } = new();

        public Exception? ErrorAlCrearManual { get; set; }

        public Task<IReadOnlyCollection<ItinerarioResponseDto>> CrearManualAsync(
            Guid viajeId,
            IReadOnlyCollection<Guid> lugarIds,
            Guid usuarioIdSolicitante,
            CancellationToken cancellationToken = default)
        {
            if (ErrorAlCrearManual is not null)
            {
                throw ErrorAlCrearManual;
            }

            LlamadasCrearManual++;
            ViajeIdRecibido = viajeId;
            LugarIdsRecibidos.Clear();
            LugarIdsRecibidos.AddRange(lugarIds);

            return Task.FromResult<IReadOnlyCollection<ItinerarioResponseDto>>(
                Array.Empty<ItinerarioResponseDto>());
        }

        public Task<IReadOnlyCollection<ItinerarioResponseDto>> GenerarAsync(
            Guid viajeId,
            Guid usuarioIdSolicitante,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<ItinerarioResponseDto>> ObtenerAsync(
            Guid viajeId,
            Guid usuarioId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<ItinerarioResponseDto>> EliminarParadaAsync(
            Guid viajeId,
            short numeroDia,
            Guid paradaId,
            string version,
            Guid usuarioIdSolicitante,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<ItinerarioResponseDto>> RefinarAsync(
            Guid viajeId,
            TipoRefinamiento tipo,
            short? numeroDia,
            Guid usuarioIdSolicitante,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<ItinerarioResponseDto>> RefinarPorTextoAsync(
            Guid viajeId,
            string texto,
            short? numeroDia,
            Guid usuarioIdSolicitante,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<ItinerarioResponseDto>> SugerirTurnosLibresAsync(
            Guid viajeId,
            short? numeroDia,
            Guid usuarioIdSolicitante,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<ItinerarioResponseDto>> AplicarAccionAsistenteAsync(
            Guid viajeId,
            AplicarAccionAsistenteDto dto,
            Guid usuarioIdSolicitante,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
