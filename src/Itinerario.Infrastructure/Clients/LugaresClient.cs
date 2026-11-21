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
            var lugar = await _httpClient.GetFromJsonAsync<LugarSemanticoItemDto>(
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

    public async Task<IReadOnlyCollection<LugarCandidatoDto>> BuscarSemanticoAsync(
        BusquedaSemanticaLugaresDto busqueda,
        CancellationToken cancellationToken = default)
    {
        var request = new BusquedaSemanticaRequest
        {
            Query = busqueda.Query,
            Cantidad = busqueda.Cantidad,
            Barrio = busqueda.Barrio?.ToString(),
            CategoriaCodigos = busqueda.CategoriaCodigos,
            NivelPrecioMaximo = busqueda.NivelPrecioMaximo,
            SoloAlAireLibre = busqueda.SoloAlAireLibre,
            SoloBajoTecho = busqueda.SoloBajoTecho,
            AptoNinos = busqueda.AptoNinos,
            AccesibleMovilidadReducida = busqueda.AccesibleMovilidadReducida,
            RestriccionDietaria = busqueda.RestriccionDietaria,
            ExcluirIds = busqueda.ExcluirIds,
            LatitudReferencia = busqueda.LatitudReferencia,
            LongitudReferencia = busqueda.LongitudReferencia,
            RadioMetrosMaximo = busqueda.RadioMetrosMaximo,
            SimilitudMinima = busqueda.SimilitudMinima
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("/api/lugares", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var items = await response.Content.ReadFromJsonAsync<List<LugarSemanticoItemDto>>(cancellationToken: cancellationToken);
            return (items ?? new List<LugarSemanticoItemDto>()).Select(Mapear).ToList();
        }
        catch (HttpRequestException ex)
        {
            throw new ServicioExternoNoDisponibleException("No se pudo realizar la búsqueda semántica en Lugares.", ex);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ServicioExternoNoDisponibleException("El catálogo de Lugares no respondió a tiempo.");
        }
    }

    private sealed class BusquedaSemanticaRequest
    {
        public string Query { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public string? Barrio { get; set; }
        public IReadOnlyCollection<string>? CategoriaCodigos { get; set; }
        public short? NivelPrecioMaximo { get; set; }
        public bool? SoloAlAireLibre { get; set; }
        public bool? SoloBajoTecho { get; set; }
        public bool? AptoNinos { get; set; }
        public bool? AccesibleMovilidadReducida { get; set; }
        public string? RestriccionDietaria { get; set; }
        public IReadOnlyCollection<Guid>? ExcluirIds { get; set; }
        public decimal? LatitudReferencia { get; set; }
        public decimal? LongitudReferencia { get; set; }
        public double? RadioMetrosMaximo { get; set; }
        public double? SimilitudMinima { get; set; }
    }

    private static LugarCandidatoDto Mapear(LugarListadoItemDto lugar) => new()
    {
        Id = lugar.Id,
        Nombre = lugar.Nombre,
        Barrio = ParseBarrio(lugar.Barrio),
        Subzona = lugar.Subzona,
        Direccion = lugar.Direccion,
        Latitud = lugar.Latitud,
        Longitud = lugar.Longitud,
        NivelPrecio = MapearNivelPrecio(lugar.NivelPrecio),
        Puntuacion = lugar.Puntuacion,
        CantidadResenas = lugar.CantidadResenas,
        EsAireLibre = lugar.EsAlAireLibre,
        EsBajoTecho = lugar.EsBajoTecho,
        AptoNinos = lugar.AptoNinos,
        AccesibleMovilidadReducida = lugar.AccesibleMovilidadReducida,
        OfreceMenuInfantil = lugar.OfreceMenuInfantil,
        FotoPortadaUrl = lugar.FotoPortadaUrl,
        DuracionSugeridaMinutos = lugar.DuracionEstimadaMinutos,
        Categorias = lugar.Categorias
    };

    private static LugarCandidatoDto Mapear(LugarSemanticoItemDto lugar) => new()
    {
        Id = lugar.Id,
        Nombre = lugar.Nombre,
        Descripcion = lugar.Descripcion,
        Barrio = ParseBarrio(lugar.Barrio),
        Subzona = lugar.Subzona,
        Direccion = lugar.Direccion,
        Latitud = lugar.Latitud,
        Longitud = lugar.Longitud,
        NivelPrecio = MapearNivelPrecio(lugar.NivelPrecio),
        Puntuacion = lugar.Puntuacion,
        CantidadResenas = lugar.CantidadResenas,
        EsAireLibre = lugar.EsAlAireLibre,
        EsBajoTecho = lugar.EsBajoTecho,
        AptoNinos = lugar.AptoNinos,
        AccesibleMovilidadReducida = lugar.AccesibleMovilidadReducida,
        OfreceMenuInfantil = lugar.OfreceMenuInfantil,
        FotoPortadaUrl = lugar.Fotos
            .FirstOrDefault(f => f.EsPortada)?.Url
            ?? lugar.Fotos.OrderBy(f => f.Orden).FirstOrDefault()?.Url,
        DuracionSugeridaMinutos = lugar.DuracionEstimadaMinutos,
        Categorias = lugar.Categorias
            .Select(categoria => categoria.Codigo)
            .ToList(),
        OpcionesDietarias = lugar.OpcionesDietarias,
        Similitud = lugar.Similitud
    };

    private static BarrioCaba ParseBarrio(string? barrio)
        => Enum.TryParse<BarrioCaba>(barrio, ignoreCase: true, out var result) ? result : BarrioCaba.Palermo;

    private static NivelPresupuesto MapearNivelPrecio(short nivelPrecio) => nivelPrecio switch
    {
        1 => NivelPresupuesto.Economico,
        2 => NivelPresupuesto.Equilibrado,
        _ => NivelPresupuesto.SinRestricciones
    };
}
