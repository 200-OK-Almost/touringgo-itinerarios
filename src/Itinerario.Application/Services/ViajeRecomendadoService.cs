using Itinerario.Application.DTOs;
using Itinerario.Application.Interfaces;
using Itinerario.Domain.Enums;
using Itinerario.Domain.Exceptions;

namespace Itinerario.Application.Services;

public class ViajeRecomendadoService : IViajeRecomendadoService
{
    private static readonly string[] CategoriasFallback =
    {
        "gastronomia_clasica",
        "cafe_especialidad",
        "museos_galerias",
        "parques_aire_libre"
    };

    private readonly IViajeService _viajeService;
    private readonly IItinerarioService _itinerarioService;
    private readonly ILugaresClient _lugaresClient;

    public ViajeRecomendadoService(
        IViajeService viajeService,
        IItinerarioService itinerarioService,
        ILugaresClient lugaresClient)
    {
        _viajeService = viajeService;
        _itinerarioService = itinerarioService;
        _lugaresClient = lugaresClient;
    }

    public async Task<ViajeResponseDto> CrearAsync(
        CrearViajeRecomendadoDto dto,
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        if (dto.LugarIds.Count is < 1 or > 6)
        {
            throw new ReglaDeNegocioException("El recorrido debe tener entre 1 y 6 paradas.");
        }

        var lugares = new List<LugarCandidatoDto>(dto.LugarIds.Count);
        foreach (var lugarId in dto.LugarIds)
        {
            var lugar = await _lugaresClient.ObtenerPorIdAsync(lugarId, cancellationToken)
                ?? throw new NotFoundException($"No se encontró el lugar con id {lugarId}.");

            lugares.Add(lugar);
        }

        var viajeCreateDto = new ViajeCreateDto
        {
            Titulo = dto.Titulo,
            TipoViaje = TipoViaje.Solo,
            Barrio = dto.Barrio,
            AlcanceTemporal = AlcanceTemporal.DiaCompleto,
            FechaInicio = dto.FechaInicio,
            FechaFin = dto.FechaFin,
            NivelPresupuesto = NivelPresupuesto.Equilibrado,
            RitmoCaminata = RitmoCaminata.Tranquilo,
            TipoPuntoPartida = TipoPuntoPartida.PrimeraParada,
            CategoriaCodigos = ConstruirCategorias(dto.CategoriaCodigo, lugares)
        };

        var viaje = await _viajeService.CrearAsync(viajeCreateDto, usuarioId, cancellationToken);

        try
        {
            await _itinerarioService.CrearManualAsync(
                viaje.Id,
                dto.LugarIds,
                usuarioId,
                cancellationToken);
        }
        catch
        {
            try
            {
                await _viajeService.EliminarAsync(
                    viaje.Id,
                    usuarioId,
                    CancellationToken.None);
            }
            catch
            {
                // Ignorar error de compensación para no tapar la excepción original.
            }

            throw;
        }

        return await _viajeService.ObtenerPorIdAsync(viaje.Id, usuarioId, cancellationToken);
    }

    private static List<string> ConstruirCategorias(
        string? categoriaCodigo,
        IReadOnlyCollection<LugarCandidatoDto> lugares)
    {
        var categorias = new List<string>();

        void Agregar(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo) || categorias.Count >= 4)
            {
                return;
            }

            if (!categorias.Contains(codigo))
            {
                categorias.Add(codigo);
            }
        }

        Agregar(categoriaCodigo);

        foreach (var categoria in lugares.SelectMany(lugar => lugar.Categorias))
        {
            Agregar(categoria);
        }

        foreach (var fallback in CategoriasFallback)
        {
            if (categorias.Count >= 2)
            {
                break;
            }

            Agregar(fallback);
        }

        return categorias;
    }
}
