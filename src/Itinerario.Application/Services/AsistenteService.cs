using System.Diagnostics;
using Itinerario.Application.Configuration;
using Itinerario.Application.DTOs;
using Itinerario.Application.Interfaces;
using Itinerario.Domain.Entities;
using Itinerario.Domain.Enums;
using Itinerario.Domain.Exceptions;
using Microsoft.Extensions.Options;
using ItinerarioEntity = Itinerario.Domain.Entities.Itinerario;

namespace Itinerario.Application.Services;

public class AsistenteService : IAsistenteService
{
    private readonly IViajeRepository _viajeRepository;
    private readonly ILugaresClient _lugaresClient;
    private readonly IGeoCalculator _geo;
    private readonly IIntencionService _intencionService;
    private readonly IIntencionLlmService _intencionLlmService;
    private readonly IMensajeLlmService _mensajeLlmService;
    private readonly IAsistenteTelemetry _telemetry;
    private readonly AsistenteOptions _options;

    public AsistenteService(
        IViajeRepository viajeRepository,
        ILugaresClient lugaresClient,
        IGeoCalculator geo,
        IIntencionService intencionService,
        IIntencionLlmService intencionLlmService,
        IMensajeLlmService mensajeLlmService,
        IAsistenteTelemetry telemetry,
        IOptions<AsistenteOptions> options)
    {
        _viajeRepository = viajeRepository;
        _lugaresClient = lugaresClient;
        _geo = geo;
        _intencionService = intencionService;
        _intencionLlmService = intencionLlmService;
        _mensajeLlmService = mensajeLlmService;
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
        using var actividad = _telemetry.IniciarActividad("asistente.consulta", viajeId);

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

        var (barrio, categoriasViaje, itinerario) = ObtenerContextoDia(
            viaje,
            request.NumeroDia);

        var paradasActuales = await ObtenerParadasActualesAsync(
            itinerario,
            cancellationToken);

        var referenciaSolicitada = await ObtenerReferenciaSolicitadaAsync(
            request.ReferenciaLugarId,
            cancellationToken);

        var paradasResolubles = paradasActuales
            .Select(parada => new ParadaResolubleDto
            {
                ParadaId = parada.Parada.Id,
                LugarId = parada.Lugar.Id,
                Nombre = parada.Lugar.Nombre,
                Turno = parada.Parada.Turno,
                OrdenParada = parada.Parada.OrdenParada,
                Categorias = parada.Lugar.Categorias
            })
            .ToList();

        var intencion = InterpretarConHistorial(
            mensaje,
            request.Historial,
            paradasResolubles);
        var sinSenal = !_intencionService.TieneSenalDeViaje(mensaje);
        var barrioBusqueda = intencion.Barrio ?? barrio;

        var puedeAplicar = viaje.EsAdministrador(usuarioIdSolicitante);

        if (!sinSenal
            && _options.LlmEnabled
            && intencion.Aclaracion is null
            && EsIntencionDebil(intencion))
        {
            var contexto = ConstruirContextoLlm(
                viajeId,
                mensaje,
                itinerario?.NumeroDia ?? request.NumeroDia,
                barrioBusqueda,
                categoriasViaje,
                viaje,
                paradasResolubles,
                request.Historial,
                intencion);

            var crudo = await _intencionLlmService.InterpretarAsync(
                contexto,
                cancellationToken);

            var intencionLlm = IntencionLlmSanitizer.Sanitizar(crudo, contexto);

            if (intencionLlm is not null)
            {
                intencionLlm.Barrio ??= intencion.Barrio;
                intencion = intencionLlm;
            }
        }

        barrioBusqueda = intencion.Barrio ?? barrio;

        if (intencion.Aclaracion is not null)
        {
            var aclaracion = new AsistenteResponseDto
            {
                Mensaje = intencion.Aclaracion.Pregunta,
                PuedeAplicar = puedeAplicar,
                Aclaracion = intencion.Aclaracion
            };

            actividad?.SetTag("asistente.resultado", "aclaracion");
            _telemetry.RegistrarResultado("aclaracion", 0, viajeId);
            _telemetry.RegistrarEtapa("total", total.Elapsed, viajeId);

            return aclaracion;
        }

        var categoriasBusqueda = intencion.Filtros.CategoriaCodigos.Count > 0
            ? intencion.Filtros.CategoriaCodigos
            : categoriasViaje;

        var presupuestoViaje = MapearNivelPrecioMaximo(viaje.NivelPresupuesto);
        var presupuestoMaximo = intencion.Filtros.NivelPrecioMaximo.HasValue
            ? Math.Min(intencion.Filtros.NivelPrecioMaximo.Value, presupuestoViaje)
            : presupuestoViaje;

        var idsExcluidos = ObtenerIdsExcluidos(viaje);
        var referencia = ObtenerReferenciaCercania(
            intencion,
            paradasActuales,
            referenciaSolicitada,
            request.ReferenciaLugarId.HasValue);
        var radioMetros = referencia is null
            ? null
            : intencion.RadioMetros
                ?? (request.ReferenciaLugarId.HasValue
                    ? _options.RadioMetrosDefault
                    : null);
        var puntoOrden = referencia is not null
            ? ((double)referencia.Latitud, (double)referencia.Longitud)
            : ObtenerPuntoReferencia(itinerario, viaje);

        var busqueda = new BusquedaSemanticaLugaresDto
        {
            Query = intencion.QuerySemantica,
            Cantidad = _options.TopK,
            Barrio = barrioBusqueda,
            CategoriaCodigos = categoriasBusqueda.Count > 0 ? categoriasBusqueda : null,
            NivelPrecioMaximo = presupuestoMaximo,
            SoloAlAireLibre = intencion.Filtros.SoloAlAireLibre,
            SoloBajoTecho = intencion.Filtros.SoloBajoTecho,
            AptoNinos = intencion.Filtros.AptoNinos == true || RequiereAptoNinos(viaje)
                ? true
                : null,
            AccesibleMovilidadReducida =
                intencion.Filtros.AccesibleMovilidadReducida == true || RequiereAccesibilidad(viaje)
                    ? true
                    : null,
            RestriccionDietaria = intencion.Filtros.RestriccionDietaria,
            ExcluirIds = idsExcluidos.ToList(),
            LatitudReferencia = referencia?.Latitud,
            LongitudReferencia = referencia?.Longitud,
            RadioMetrosMaximo = radioMetros,
            SimilitudMinima = _options.SimilitudMinima
        };

        var falloSemantico = false;
        var usoFallback = false;
        var busquedaAmpliada = false;
        var sinResultadosEnBarrio = false;
        IReadOnlyCollection<LugarCandidatoDto> lugares = Array.Empty<LugarCandidatoDto>();

        if (!sinSenal)
        {
            var retrieval = Stopwatch.StartNew();

            using (var actividadRetrieval = _telemetry.IniciarActividad("asistente.retrieval", viajeId))
            {
                try
                {
                    lugares = await _lugaresClient.BuscarSemanticoAsync(busqueda, cancellationToken);
                }
                catch (ServicioExternoNoDisponibleException)
                {
                    falloSemantico = true;
                }

                actividadRetrieval?.SetTag("asistente.fallo_semantico", falloSemantico);
            }

            _telemetry.RegistrarEtapa("retrieval", retrieval.Elapsed, viajeId);

            usoFallback = falloSemantico;

            if (lugares.Count < _options.MinResultados)
            {
                var catalogo = await _lugaresClient.BuscarAsync(
                    barrioBusqueda,
                    categoriasBusqueda,
                    cancellationToken);

                var filtrados = FiltrarFallback(catalogo, busqueda, idsExcluidos)
                    .Where(lugar => DentroDelRadio(lugar, busqueda))
                    .Take(_options.TopK)
                    .ToList();

                if (filtrados.Count > 0)
                {
                    lugares = filtrados;
                    usoFallback = true;
                }
            }

            if (lugares.Count == 0)
            {
                sinResultadosEnBarrio = true;

                var busquedaAmplia = new BusquedaSemanticaLugaresDto
                {
                    Query = busqueda.Query,
                    Cantidad = Math.Max(_options.TopK, 20),
                    Barrio = null,
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
                    RadioMetrosMaximo = null,
                    SimilitudMinima = busqueda.SimilitudMinima
                };

                try
                {
                    var ampliados = await _lugaresClient.BuscarSemanticoAsync(
                        busquedaAmplia,
                        cancellationToken);

                    if (ampliados.Count > 0)
                    {
                        lugares = OrdenarPorDistancia(
                                ampliados,
                                puntoOrden)
                            .Take(_options.TopK)
                            .ToList();
                        busquedaAmpliada = true;
                        usoFallback = false;
                    }
                }
                catch (ServicioExternoNoDisponibleException)
                {
                    falloSemantico = true;
                }
            }
        }

        var recomendaciones = lugares
            .Take(_options.TopK)
            .Select(MapearRecomendacion)
            .ToList();

        var accion = ConstruirAccion(intencion, recomendaciones, itinerario, puedeAplicar);

        var mensajeRespuesta = sinSenal
            ? "No pude interpretar tu pedido. Contame qué tipo de lugar buscás: café, museo, bar, parque o restaurante."
            : ConstruirMensaje(
                intencion,
                recomendaciones.Count,
                itinerario?.NumeroDia ?? request.NumeroDia);

        if (_options.LlmGenerarMensaje)
        {
            var contextoMensaje = new MensajeLlmContextoDto
            {
                ViajeId = viajeId,
                MensajeUsuario = mensaje,
                Accion = intencion.Accion,
                ParadaObjetivo = intencion.ParadaObjetivo?.Nombre,
                NumeroDia = itinerario?.NumeroDia ?? request.NumeroDia,
                Barrio = barrioBusqueda,
                Filtros = intencion.Filtros,
                BusquedaAmpliada = busquedaAmpliada,
                SinResultadosEnBarrio = sinResultadosEnBarrio,
                Historial = request.Historial ?? Array.Empty<MensajeHistorialDto>(),
                Recomendaciones = recomendaciones,
                SinSenal = sinSenal
            };

            var mensajeLlm = await _mensajeLlmService.GenerarAsync(
                contextoMensaje,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(mensajeLlm))
            {
                mensajeRespuesta = mensajeLlm;
            }
        }

        var resultado = new AsistenteResponseDto
        {
            Mensaje = mensajeRespuesta,
            PuedeAplicar = puedeAplicar,
            Recomendaciones = recomendaciones,
            Accion = accion,
            Advertencia = busquedaAmpliada
                ? ConstruirAdvertenciaAmpliada(categoriasBusqueda, barrioBusqueda)
                : usoFallback
                    ? falloSemantico
                    ? "No se pudo usar la búsqueda semántica; se usó el catálogo filtrado."
                    : "La búsqueda semántica no devolvió suficientes resultados; se usó el catálogo filtrado."
                : null
        };

        var estado = sinSenal
            ? "sin_senal"
            : busquedaAmpliada
                ? "ampliado"
                : recomendaciones.Count == 0
                    ? "sin_resultados"
                    : usoFallback
                        ? "fallback"
                        : "ok";

        actividad?.SetTag("asistente.resultado", estado);
        actividad?.SetTag("asistente.recomendaciones", recomendaciones.Count);

        _telemetry.RegistrarResultado(estado, recomendaciones.Count, viajeId);
        _telemetry.RegistrarEtapa("total", total.Elapsed, viajeId);

        return resultado;
    }

    private IntencionAsistenteDto InterpretarConHistorial(
        string mensaje,
        IReadOnlyCollection<MensajeHistorialDto>? historial,
        IReadOnlyCollection<ParadaResolubleDto> paradas)
    {
        var intencion = _intencionService.Interpretar(
            mensaje,
            paradas,
            _options.RadioMetrosDefault);

        if (historial is not { Count: > 0 })
        {
            return intencion;
        }

        var necesitaCategoria = intencion.Filtros.CategoriaCodigos.Count == 0;
        var necesitaBarrio = intencion.Barrio is null;

        if (!necesitaCategoria && !necesitaBarrio)
        {
            return intencion;
        }

        foreach (var mensajeHistorial in historial.Reverse())
        {
            if (!string.Equals(
                    mensajeHistorial.Rol,
                    "user",
                    StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(mensajeHistorial.Contenido))
            {
                continue;
            }

            var historica = _intencionService.Interpretar(
                mensajeHistorial.Contenido,
                paradas,
                _options.RadioMetrosDefault);

            if (necesitaCategoria && historica.Filtros.CategoriaCodigos.Count > 0)
            {
                intencion.Filtros.CategoriaCodigos = historica.Filtros.CategoriaCodigos;
                necesitaCategoria = false;
            }

            if (necesitaBarrio && historica.Barrio is not null)
            {
                intencion.Barrio = historica.Barrio;
                necesitaBarrio = false;
            }

            if (!necesitaCategoria && !necesitaBarrio)
            {
                break;
            }
        }

        return intencion;
    }

    private static bool EsIntencionDebil(IntencionAsistenteDto intencion)
    {
        if (intencion.Aclaracion is not null)
        {
            return false;
        }

        if (intencion.Accion == TipoAccionAsistente.Reemplazar
            && intencion.ParadaObjetivo is null)
        {
            return true;
        }

        return intencion.Filtros.CategoriaCodigos.Count == 0
            && intencion.Filtros.SoloAlAireLibre is null
            && intencion.Filtros.SoloBajoTecho is null
            && intencion.Filtros.AptoNinos is null
            && intencion.Filtros.AccesibleMovilidadReducida is null
            && string.IsNullOrWhiteSpace(intencion.Filtros.RestriccionDietaria)
            && intencion.Filtros.Turno is null
            && intencion.Filtros.NivelPrecioMaximo is null
            && intencion.RadioMetros is null
            && intencion.ParadaObjetivo is null;
    }

    private static IntencionLlmContextoDto ConstruirContextoLlm(
        Guid viajeId,
        string mensaje,
        short? numeroDia,
        BarrioCaba barrio,
        IReadOnlyCollection<string> categoriasViaje,
        Viaje viaje,
        IReadOnlyCollection<ParadaResolubleDto> paradas,
        IReadOnlyCollection<MensajeHistorialDto>? historial,
        IntencionAsistenteDto intencion)
        => new()
        {
            ViajeId = viajeId,
            Mensaje = mensaje,
            NumeroDia = numeroDia,
            Barrio = barrio,
            NivelPresupuesto = viaje.NivelPresupuesto,
            CategoriasViaje = categoriasViaje,
            Historial = historial ?? Array.Empty<MensajeHistorialDto>(),
            AptoNinos = intencion.Filtros.AptoNinos == true || RequiereAptoNinos(viaje),
            AccesibleMovilidadReducida =
                intencion.Filtros.AccesibleMovilidadReducida == true
                || RequiereAccesibilidad(viaje),
            Paradas = paradas
        };

    private static (
        BarrioCaba Barrio,
        IReadOnlyCollection<string> Categorias,
        ItinerarioEntity? Itinerario) ObtenerContextoDia(
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
                viaje.Etiquetas.Select(e => e.CategoriaCodigo).ToList(),
                viaje.Itinerarios.FirstOrDefault(i => i.NumeroDia == 1));
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

        var itinerario = viaje.Itinerarios
            .FirstOrDefault(i => i.NumeroDia == dia.NumeroDia);

        return (dia.Barrio, categorias, itinerario);
    }

    private async Task<IReadOnlyList<ParadaActual>> ObtenerParadasActualesAsync(
        ItinerarioEntity? itinerario,
        CancellationToken cancellationToken)
    {
        if (itinerario is null)
        {
            return Array.Empty<ParadaActual>();
        }

        try
        {
            var paradas = itinerario.Paradas
                .OrderBy(p => p.Turno)
                .ThenBy(p => p.OrdenParada)
                .ToList();

            var lugares = await Task.WhenAll(
                paradas.Select(p => _lugaresClient.ObtenerPorIdAsync(p.LugarId, cancellationToken)));

            return paradas
                .Zip(lugares, (parada, lugar) => lugar is null
                    ? null
                    : new ParadaActual(parada, lugar))
                .Where(parada => parada is not null)
                .Cast<ParadaActual>()
                .ToList();
        }
        catch (ServicioExternoNoDisponibleException)
        {
            return Array.Empty<ParadaActual>();
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

    private async Task<LugarCandidatoDto?> ObtenerReferenciaSolicitadaAsync(
        Guid? referenciaLugarId,
        CancellationToken cancellationToken)
    {
        if (!referenciaLugarId.HasValue)
        {
            return null;
        }

        try
        {
            return await _lugaresClient.ObtenerPorIdAsync(
                referenciaLugarId.Value,
                cancellationToken);
        }
        catch (ServicioExternoNoDisponibleException)
        {
            return null;
        }
    }

    private bool DentroDelRadio(
        LugarCandidatoDto lugar,
        BusquedaSemanticaLugaresDto busqueda)
    {
        if (!busqueda.LatitudReferencia.HasValue
            || !busqueda.LongitudReferencia.HasValue
            || !busqueda.RadioMetrosMaximo.HasValue
            || busqueda.RadioMetrosMaximo.Value <= 0)
        {
            return true;
        }

        var metros = _geo.DistanciaMetros(
            (double)busqueda.LatitudReferencia.Value,
            (double)busqueda.LongitudReferencia.Value,
            (double)lugar.Latitud,
            (double)lugar.Longitud);

        return metros <= busqueda.RadioMetrosMaximo.Value;
    }

    private IEnumerable<LugarCandidatoDto> OrdenarPorDistancia(
        IEnumerable<LugarCandidatoDto> lugares,
        (double Lat, double Lon)? referencia)
    {
        if (referencia is null)
        {
            return lugares;
        }

        return lugares.OrderBy(lugar => _geo.DistanciaMetros(
            referencia.Value.Lat,
            referencia.Value.Lon,
            (double)lugar.Latitud,
            (double)lugar.Longitud));
    }

    private static (double Lat, double Lon)? ObtenerPuntoReferencia(
        ItinerarioEntity? itinerario,
        Viaje viaje)
    {
        if (itinerario?.OrigenLatitud is { } origenLat
            && itinerario.OrigenLongitud is { } origenLon)
        {
            return ((double)origenLat, (double)origenLon);
        }

        if (viaje.LatitudPartida is { } partidaLat
            && viaje.LongitudPartida is { } partidaLon)
        {
            return ((double)partidaLat, (double)partidaLon);
        }

        return null;
    }

    private static string ConstruirAdvertenciaAmpliada(
        IReadOnlyCollection<string> categorias,
        BarrioCaba barrio)
    {
        var categoria = categorias
            .Select(NombreCategoria)
            .FirstOrDefault(nombre => nombre is not null)
            ?? "lugares";

        return $"No encontré {categoria} en {FormatearBarrio(barrio)}. " +
            "Te muestro opciones cercanas en otros barrios.";
    }

    private static string? NombreCategoria(string codigo) => codigo switch
    {
        "cafe_especialidad" => "cafeterías",
        "museos_galerias" => "museos",
        "parques_aire_libre" => "parques",
        "librerias_diseno" => "librerías",
        "bares_vermu" => "bares",
        "gastronomia_clasica" => "restaurantes",
        "cocteleria_autor" => "coctelerías",
        _ => null
    };

    private static string FormatearBarrio(BarrioCaba barrio) => barrio switch
    {
        BarrioCaba.SanTelmo => "San Telmo",
        BarrioCaba.SanNicolas => "San Nicolás",
        BarrioCaba.PuertoMadero => "Puerto Madero",
        _ => barrio.ToString()
    };

    private static LugarCandidatoDto? ObtenerReferenciaCercania(
        IntencionAsistenteDto intencion,
        IReadOnlyList<ParadaActual> paradasActuales,
        LugarCandidatoDto? referenciaSolicitada,
        bool buscarReferenciaSolicitada)
    {
        if (referenciaSolicitada is not null)
        {
            return referenciaSolicitada;
        }

        if ((!buscarReferenciaSolicitada && !intencion.RadioMetros.HasValue)
            || paradasActuales.Count == 0)
        {
            return null;
        }

        if (intencion.ParadaObjetivo is not null)
        {
            var objetivo = paradasActuales.FirstOrDefault(
                parada => parada.Parada.Id == intencion.ParadaObjetivo.ParadaId);

            if (objetivo is not null)
            {
                return objetivo.Lugar;
            }
        }

        return paradasActuales[0].Lugar;
    }

    private static AccionPropuestaDto? ConstruirAccion(
        IntencionAsistenteDto intencion,
        IReadOnlyList<RecomendacionLugarDto> recomendaciones,
        ItinerarioEntity? itinerario,
        bool puedeAplicar)
    {
        if (!puedeAplicar
            || intencion.Accion == TipoAccionAsistente.Recomendar
            || recomendaciones.Count == 0
            || itinerario is null
            || string.IsNullOrWhiteSpace(itinerario.Version))
        {
            return null;
        }

        if (intencion.Accion == TipoAccionAsistente.Reemplazar
            && intencion.ParadaObjetivo is null)
        {
            return null;
        }

        if (intencion.Accion == TipoAccionAsistente.Agregar
            && intencion.Filtros.Turno is { } turno
            && itinerario.Paradas.Any(parada => parada.Turno == turno))
        {
            return null;
        }

        return new AccionPropuestaDto
        {
            Tipo = intencion.Accion == TipoAccionAsistente.Agregar
                ? "AgregarParada"
                : "ReemplazarParada",
            NumeroDia = itinerario.NumeroDia,
            LugarId = recomendaciones[0].LugarId,
            ParadaId = intencion.Accion == TipoAccionAsistente.Reemplazar
                ? intencion.ParadaObjetivo!.ParadaId
                : null,
            Turno = intencion.Accion == TipoAccionAsistente.Agregar
                ? intencion.Filtros.Turno?.ToString()
                : null,
            Version = itinerario.Version
        };
    }

    private static string ConstruirMensaje(
        IntencionAsistenteDto intencion,
        int cantidad,
        short? numeroDia)
    {
        if (cantidad == 0)
        {
            return "No encontré lugares que encajen con esos filtros. Probá con otra categoría o ampliá el presupuesto.";
        }

        return intencion.Accion switch
        {
            TipoAccionAsistente.Reemplazar when intencion.ParadaObjetivo is not null
                => $"Encontré {cantidad} lugares para reemplazar “{intencion.ParadaObjetivo.Nombre}”.",
            TipoAccionAsistente.Agregar
                => $"Encontré {cantidad} lugares para agregar al día {numeroDia ?? 1}.",
            _ => $"Encontré {cantidad} lugares que pueden encajar en tu recorrido."
        };
    }

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
            .Where(l => string.IsNullOrWhiteSpace(busqueda.RestriccionDietaria)
                || l.OpcionesDietarias.Any(opcion => CoincideDieta(opcion, busqueda.RestriccionDietaria)))
            .Where(l => busqueda.CategoriaCodigos is not { Count: > 0 }
                || l.Categorias.Any(c => busqueda.CategoriaCodigos.Contains(c)))
            .Where(l => !busqueda.Barrio.HasValue || l.Barrio == busqueda.Barrio.Value);

    private static bool CoincideDieta(string opcion, string restriccion)
        => opcion.Contains(restriccion, StringComparison.OrdinalIgnoreCase)
            || restriccion.Contains(opcion, StringComparison.OrdinalIgnoreCase);

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

    private sealed record ParadaActual(
        ParadaItinerario Parada,
        LugarCandidatoDto Lugar);
}
