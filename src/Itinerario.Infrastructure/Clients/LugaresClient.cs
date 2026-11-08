using System.Net;
using System.Net.Http.Json;
using Itinerario.Application.DTOs;
using Itinerario.Application.Interfaces;
using Itinerario.Domain.Enums;
using Itinerario.Domain.Exceptions;

namespace Itinerario.Infrastructure.Clients;

public class LugaresClient : ILugaresClient
{
    private const int TamanoPagina = 100;
    private const int MaximoPaginas = 100;

    private readonly HttpClient _httpClient;

    public LugaresClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LugarCandidatoDto?> ObtenerPorIdAsync(Guid lugarId, CancellationToken cancellationToken = default)
    {
        try
        {
            var lugar = await _httpClient.GetFromJsonAsync<LugarItemDto>(
                $"/api/lugares/{lugarId}", cancellationToken);

            return lugar is null ? null : Mapear(lugar);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (HttpRequestException ex)
        {
            throw new ServicioExternoNoDisponibleException("No se pudo contactar con el catálogo de Lugares.", ex);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ServicioExternoNoDisponibleException("El catálogo de Lugares no respondió a tiempo.");
        }
    }

    public async Task<IReadOnlyCollection<LugarCandidatoDto>> BuscarAsync(
        BarrioCaba barrio,
        IReadOnlyCollection<string> categoriaCodigos,
        CancellationToken cancellationToken = default)
    {
        var resultados = new List<LugarCandidatoDto>();

        for (var pagina = 1; pagina <= MaximoPaginas; pagina++)
        {
            var respuesta = await ObtenerPaginaAsync(barrio, categoriaCodigos, pagina, cancellationToken);

            if (respuesta is null)
            {
                break;
            }

            resultados.AddRange(respuesta.Items.Select(Mapear));

            if (respuesta.Items.Count < TamanoPagina || resultados.Count >= respuesta.Total)
            {
                break;
            }
        }

        return resultados;
    }

    private async Task<LugaresPaginadosDto?> ObtenerPaginaAsync(
        BarrioCaba barrio,
        IReadOnlyCollection<string> categoriaCodigos,
        int pagina,
        CancellationToken cancellationToken)
    {
        var parametros = new List<string>
        {
            $"barrio={Uri.EscapeDataString(barrio.ToString())}",
            $"pagina={pagina}",
            $"tamano={TamanoPagina}"
        };

        foreach (var codigo in categoriaCodigos)
        {
            parametros.Add($"categoriaCodigo={Uri.EscapeDataString(codigo)}");
        }

        var url = "/api/lugares?" + string.Join('&', parametros);

        try
        {
            return await _httpClient.GetFromJsonAsync<LugaresPaginadosDto>(url, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ServicioExternoNoDisponibleException("No se pudo contactar con el catálogo de Lugares.", ex);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ServicioExternoNoDisponibleException("El catálogo de Lugares no respondió a tiempo.");
        }
    }

    private static LugarCandidatoDto Mapear(LugarItemDto lugar) => new()
    {
        Id = lugar.Id,
        Nombre = lugar.Nombre,
        Latitud = lugar.Latitud,
        Longitud = lugar.Longitud,
        NivelPrecio = MapearNivelPrecio(lugar.NivelPrecio),
        EsAireLibre = lugar.EsAlAireLibre,
        DuracionSugeridaMinutos = lugar.DuracionEstimadaMinutos
    };

    private static NivelPresupuesto MapearNivelPrecio(short nivelPrecio) => nivelPrecio switch
    {
        1 => NivelPresupuesto.Economico,
        2 => NivelPresupuesto.Equilibrado,
        _ => NivelPresupuesto.SinRestricciones
    };
}
