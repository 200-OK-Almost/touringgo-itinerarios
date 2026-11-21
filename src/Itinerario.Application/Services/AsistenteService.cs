using System.Diagnostics;
using Itinerario.Application.Configuration;
using Itinerario.Application.DTOs;
using Itinerario.Application.Interfaces;
using Itinerario.Domain.Entities;
using Itinerario.Domain.Enums;
using Itinerario.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace Itinerario.Application.Services;

public class AsistenteService : IAsistenteService
{
    private readonly IViajeRepository _viajeRepository;
    private readonly ILugaresClient _lugaresClient;
    private readonly IAsistenteTelemetry _telemetry;
    private readonly AsistenteOptions _options;

    public AsistenteService(
        IViajeRepository viajeRepository,
        ILugaresClient lugaresClient,
        IAsistenteTelemetry telemetry,
        IOptions<AsistenteOptions> options)
    {
        _viajeRepository = viajeRepository;
        _lugaresClient = lugaresClient;
        _telemetry = telemetry;
        _options = options.Value;
    }

    public async Task<AsistenteResponseDto> ResponderAsync(
        Guid viajeId,
        AsistenteRequestDto request,
        Guid usuarioIdSolicitante,
        CancellationToken cancellationToken = default)
    {
        var total = Stopwatch.StartNew();

        var mensaje = request.Mensaje?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(mensaje))
        {
            throw new ReglaDeNegocioException("El mensaje no puede estar vacío.");
        }

        if (mensaje.Length > _options.MaxMensajeCaracteres)
        {
            throw new ReglaDeNegocioException(
                $"El mensaje no puede superar {_options.MaxMensajeCaracteres} caracteres.");
        }

        var viaje = await _viajeRepository.ObtenerPorIdAsync(viajeId, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el viaje con id {viajeId}.");

        if (!viaje.EsMiembro(usuarioIdSolicitante))
        {
            throw new NotFoundException($"No se encontró el viaje con id {viajeId}.");
        }

        var (barrio, categorias) = ObtenerContextoDia(viaje, request.NumeroDia);
        var paradasActuales = await ObtenerParadasActualesAsync(
            viaje,
            request.NumeroDia,
            cancellationToken);

        var idsExcluidos = ObtenerIdsExcluidos(viaje);
        var referencia = ObtenerReferenciaCercania(mensaje, paradasActuales);

        var busqueda = new BusquedaSemanticaLugaresDto
        {
            Query = mensaje,
            Cantidad = _options.TopK,
            Barrio = barrio,
            CategoriaCodigos = categorias.Count > 0 ? categorias : null,
            NivelPrecioMaximo = MapearNivelPrecioMaximo(viaje.NivelPresupuesto),
            SoloAlAireLibre = MencionaAireLibre(mensaje) ? true : null,
            SoloBajoTecho = MencionaBajoTecho(mensaje) ? true : null,
            AptoNinos = RequiereAptoNinos(viaje) ? true : null,
            AccesibleMovilidadReducida = RequiereAccesibilidad(viaje) ? true : null,
            ExcluirIds = idsExcluidos.ToList(),
            LatitudReferencia = referencia?.Latitud,
            LongitudReferencia = referencia?.Longitud,
            RadioMetrosMaximo = referencia is null ? null : _options.RadioMetrosDefault,
            SimilitudMinima = _options.SimilitudMinima
        };

        var retrieval = Stopwatch.StartNew();
        var falloSemantico = false;
        IReadOnlyCollection<LugarCandidatoDto> lugares = Array.Empty<LugarCandidatoDto>();

        try
        {
            lugares = await _lugaresClient.BuscarSemanticoAsync(busqueda, cancellationToken);
        }
        catch (ServicioExternoNoDisponibleException)
        {
            falloSemantico = true;
        }

        _telemetry.RegistrarEtapa("retrieval", retrieval.Elapsed, viajeId);

        var usoFallback = falloSemantico;

        if (lugares.Count < _options.MinResultados)
        {
            var catalogo = await _lugaresClient.BuscarAsync(
                barrio,
                categorias,
                cancellationToken);

            var filtrados = FiltrarFallback(catalogo, busqueda, idsExcluidos)
                .Take(_options.TopK)
                .ToList();

            if (filtrados.Count > 0)
            {
                lugares = filtrados;
                usoFallback = true;
            }
        }

        var recomendaciones = lugares
            .Select(MapearRecomendacion)
            .ToList();

        var resultado = new AsistenteResponseDto
        {
            Mensaje = recomendaciones.Count == 0
                ? "No encontré lugares que encajen con esos filtros. Probá con otra categoría o ampliá el presupuesto."
                : $"Encontré {recomendaciones.Count} lugares que pueden encajar en tu recorrido.",
            PuedeAplicar = viaje.EsAdministrador(usuarioIdSolicitante),
            Recomendaciones = recomendaciones,
            Advertencia = usoFallback
                ? falloSemantico
                    ? "No se pudo usar la búsqueda semántica; se usó el catálogo filtrado."
                    : "La búsqueda semántica no devolvió suficientes resultados; se usó el catálogo filtrado."
                : null
        };

        _telemetry.RegistrarResultado(
            recomendaciones.Count == 0 ? "sin_resultados" : usoFallback ? "fallback" : "ok",
            recomendaciones.Count,
            viajeId);
        _telemetry.RegistrarEtapa("total", total.Elapsed, viajeId);

        return resultado;
    }

    private static (BarrioCaba Barrio, IReadOnlyCollection<string> Categorias) ObtenerContextoDia(
        Viaje viaje,
        short? numeroDia)
    {
        if (viaje.Dias.Count == 0)
        {
            if (numeroDia.HasValue && numeroDia.Value != 1)
            {
                throw new ReglaDeNegocioException(
                    $"El día {numeroDia.Value} no existe en el viaje.");
            }

            return (
                viaje.Barrio,
                viaje.Etiquetas.Select(e => e.CategoriaCodigo).ToList());
        }

        var dia = numeroDia.HasValue
            ? viaje.Dias.FirstOrDefault(d => d.NumeroDia == numeroDia.Value)
            : viaje.Dias.OrderBy(d => d.NumeroDia).First();

        if (dia is null)
        {
            throw new ReglaDeNegocioException(
                $"El día {numeroDia!.Value} no existe en el viaje.");
        }

        var categorias = dia.Etiquetas.Count > 0
            ? dia.Etiquetas.Select(e => e.CategoriaCodigo).ToList()
            : viaje.Etiquetas.Select(e => e.CategoriaCodigo).ToList();

        return (dia.Barrio, categorias);
    }

    private async Task<IReadOnlyList<LugarCandidatoDto>> ObtenerParadasActualesAsync(
        Viaje viaje,
        short? numeroDia,
        CancellationToken cancellationToken)
    {
        var itinerario = numeroDia.HasValue
            ? viaje.Itinerarios.FirstOrDefault(i => i.NumeroDia == numeroDia.Value)
            : viaje.Itinerarios.OrderBy(i => i.NumeroDia).FirstOrDefault();

        if (itinerario is null)
        {
            return Array.Empty<LugarCandidatoDto>();
        }

        try
        {
            var lugares = await Task.WhenAll(
                itinerario.Paradas
                    .OrderBy(p => p.Turno)
                    .ThenBy(p => p.OrdenParada)
                    .Select(p => _lugaresClient.ObtenerPorIdAsync(p.LugarId, cancellationToken)));

            return lugares
                .Where(l => l is not null)
                .Cast<LugarCandidatoDto>()
                .ToList();
        }
        catch (ServicioExternoNoDisponibleException)
        {
            return Array.Empty<LugarCandidatoDto>();
        }
    }

    private static HashSet<Guid> ObtenerIdsExcluidos(Viaje viaje)
    {
        var ids = viaje.Itinerarios
            .SelectMany(i => i.Paradas)
            .Select(p => p.LugarId)
            .ToHashSet();

        if (viaje.LugarPartidaId.HasValue)
        {
            ids.Add(viaje.LugarPartidaId.Value);
        }

        return ids;
    }

    private static LugarCandidatoDto? ObtenerReferenciaCercania(
        string mensaje,
        IReadOnlyList<LugarCandidatoDto> paradasActuales)
    {
        if (paradasActuales.Count == 0
            || !mensaje.Contains("cerca", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return paradasActuales[0];
    }

    private static bool MencionaAireLibre(string mensaje)
        => mensaje.Contains("aire libre", StringComparison.OrdinalIgnoreCase)
            || mensaje.Contains("parque", StringComparison.OrdinalIgnoreCase);

    private static bool MencionaBajoTecho(string mensaje)
        => mensaje.Contains("bajo techo", StringComparison.OrdinalIgnoreCase)
            || mensaje.Contains("lluvia", StringComparison.OrdinalIgnoreCase);

    private static bool RequiereAptoNinos(Viaje viaje)
        => viaje.TipoViaje == TipoViaje.Familia
            || viaje.ParametrosFamilia?.HayNinos == true;

    private static bool RequiereAccesibilidad(Viaje viaje)
        => viaje.ParametrosFamilia?.MovilidadReducida == true;

    private static IEnumerable<LugarCandidatoDto> FiltrarFallback(
        IEnumerable<LugarCandidatoDto> lugares,
        BusquedaSemanticaLugaresDto busqueda,
        HashSet<Guid> idsExcluidos)
        => lugares
            .Where(l => !idsExcluidos.Contains(l.Id))
            .Where(l => !busqueda.NivelPrecioMaximo.HasValue
                || MapearNivelPrecioMaximo(l.NivelPrecio) <= busqueda.NivelPrecioMaximo.Value)
            .Where(l => busqueda.SoloAlAireLibre != true || l.EsAireLibre)
            .Where(l => busqueda.SoloBajoTecho != true || l.EsBajoTecho)
            .Where(l => busqueda.AptoNinos != true || l.AptoNinos)
            .Where(l => busqueda.AccesibleMovilidadReducida != true || l.AccesibleMovilidadReducida)
            .Where(l => busqueda.CategoriaCodigos is not { Count: > 0 }
                || l.Categorias.Any(c => busqueda.CategoriaCodigos.Contains(c)))
            .Where(l => !busqueda.Barrio.HasValue || l.Barrio == busqueda.Barrio.Value);

    private static RecomendacionLugarDto MapearRecomendacion(LugarCandidatoDto lugar) => new()
    {
        LugarId = lugar.Id,
        Nombre = lugar.Nombre,
        Descripcion = lugar.Descripcion,
        Direccion = lugar.Direccion,
        Barrio = lugar.Barrio,
        Subzona = lugar.Subzona,
        Latitud = lugar.Latitud,
        Longitud = lugar.Longitud,
        Puntuacion = lugar.Puntuacion,
        CantidadResenas = lugar.CantidadResenas,
        NivelPrecio = MapearNivelPrecioMaximo(lugar.NivelPrecio),
        DuracionMinutos = lugar.DuracionSugeridaMinutos,
        EsAireLibre = lugar.EsAireLibre,
        EsBajoTecho = lugar.EsBajoTecho,
        AptoNinos = lugar.AptoNinos,
        Accesible = lugar.AccesibleMovilidadReducida,
        FotoPortadaUrl = lugar.FotoPortadaUrl,
        Categorias = lugar.Categorias,
        OpcionesDietarias = lugar.OpcionesDietarias,
        Similitud = lugar.Similitud ?? 0,
        Motivo = ConstruirMotivo(lugar)
    };

    private static string ConstruirMotivo(LugarCandidatoDto lugar)
        => lugar.Categorias.Count > 0
            ? $"Coincide con {string.Join(", ", lugar.Categorias.Take(2))}."
            : "Coincide con tu búsqueda.";

    private static short MapearNivelPrecioMaximo(NivelPresupuesto nivel) => nivel switch
    {
        NivelPresupuesto.Economico => 1,
        NivelPresupuesto.Equilibrado => 2,
        _ => 3
    };
}
