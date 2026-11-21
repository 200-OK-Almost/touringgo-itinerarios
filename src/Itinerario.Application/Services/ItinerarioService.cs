using Itinerario.Application.DTOs;
using Itinerario.Application.Interfaces;
using Itinerario.Domain.Entities;
using Itinerario.Domain.Enums;
using Itinerario.Domain.Exceptions;
using ItinerarioEntity = Itinerario.Domain.Entities.Itinerario;

namespace Itinerario.Application.Services;

public class ItinerarioService : IItinerarioService
{
    private readonly IViajeRepository _viajeRepository;
    private readonly IItinerarioRepository _itinerarioRepository;
    private readonly ILugaresClient _lugaresClient;
    private readonly IItinerarioGenerator _generator;
    private readonly IItinerarioRefiner _refiner;
    private readonly IUsuariosClient _usuariosClient;
    private readonly IOllamaClient _ollamaClient;
    private readonly IInterpretacionService _interpretacionService;

    public ItinerarioService(
        IViajeRepository viajeRepository,
        IItinerarioRepository itinerarioRepository,
        ILugaresClient lugaresClient,
        IItinerarioGenerator generator,
        IItinerarioRefiner refiner,
        IUsuariosClient usuariosClient,
        IOllamaClient ollamaClient,
        IInterpretacionService interpretacionService)
    {
        _viajeRepository = viajeRepository;
        _itinerarioRepository = itinerarioRepository;
        _lugaresClient = lugaresClient;
        _generator = generator;
        _refiner = refiner;
        _usuariosClient = usuariosClient;
        _ollamaClient = ollamaClient;
        _interpretacionService = interpretacionService;
    }

    public async Task<IReadOnlyCollection<ItinerarioResponseDto>> GenerarAsync(
        Guid viajeId,
        Guid usuarioIdSolicitante,
        CancellationToken cancellationToken = default)
    {
        var viaje = await _viajeRepository.ObtenerPorIdAsync(viajeId, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el viaje con id {viajeId}.");

        if (!viaje.EsAdministrador(usuarioIdSolicitante))
        {
            throw new AccesoDenegadoException("Solo un administrador del viaje puede generar el itinerario.");
        }

        if (viaje.Estado == EstadoViaje.EnPlanificacion)
        {
            throw new ConflictoDeConcurrenciaException("Ya hay una generación de itinerario en curso para este viaje.");
        }

        var estadoAnterior = viaje.Estado;
        viaje.CambiarEstado(EstadoViaje.EnPlanificacion);
        _viajeRepository.Actualizar(viaje);
        await _viajeRepository.GuardarCambiosAsync(cancellationToken);

        try
        {
            await CompletarCoordenadasHospedajeSiAplicaAsync(viaje, cancellationToken);

            var perfil = await ObtenerPerfilSeguroAsync(cancellationToken);
            var nombres = new Dictionary<Guid, string>();
            var usados = new HashSet<Guid>();
            var itinerarios = new List<ItinerarioEntity>();

            foreach (var dia in ObtenerDias(viaje))
            {
                var categoriaCodigos = ObtenerCategoriasDia(viaje, dia);
                var candidatos = await BuscarCandidatosAsync(
                    viaje,
                    perfil,
                    dia.Barrio,
                    dia.LugarPartidaId,
                    categoriaCodigos,
                    cancellationToken);

                foreach (var candidato in candidatos)
                {
                    nombres[candidato.Id] = candidato.Nombre;
                }

                var disponibles = candidatos
                    .Where(l => !usados.Contains(l.Id))
                    .ToList();

                var paradas = _generator.Generar(
                    viaje,
                    disponibles,
                    dia.LatitudPartida,
                    dia.LongitudPartida).ToList();

                var itinerario = new ItinerarioEntity
                {
                    Id = Guid.NewGuid(),
                    ViajeId = viaje.Id,
                    NumeroDia = dia.NumeroDia,
                    Fecha = dia.Fecha,
                    OrigenTipo = dia.TipoPuntoPartida,
                    OrigenDescripcion = dia.NombrePuntoPartida ?? dia.DireccionPartida,
                    OrigenLatitud = dia.LatitudPartida,
                    OrigenLongitud = dia.LongitudPartida,
                    FechaCreacion = DateTimeOffset.UtcNow,
                    FechaActualizacion = DateTimeOffset.UtcNow,
                    Paradas = paradas
                };

                foreach (var parada in paradas)
                {
                    parada.ItinerarioId = itinerario.Id;
                    usados.Add(parada.LugarId);
                }

                itinerarios.Add(itinerario);
            }

            await RellenarSugerenciasAsync(
                itinerarios.SelectMany(i => i.Paradas),
                nombres,
                cancellationToken);

            viaje.CambiarEstado(EstadoViaje.Planificado);

            await _itinerarioRepository.ReemplazarAsync(viaje.Id, itinerarios, cancellationToken);

            return itinerarios.Select(MapearRespuesta).ToList();
        }
        catch
        {
            try
            {
                viaje.CambiarEstado(estadoAnterior);
                _viajeRepository.Actualizar(viaje);
                await _viajeRepository.GuardarCambiosAsync(CancellationToken.None);
            }
            catch
            {
                // Ignorar error al revertir para no tapar la excepción original
            }

            throw;
        }
    }

    private async Task CompletarCoordenadasHospedajeSiAplicaAsync(Viaje viaje, CancellationToken cancellationToken)
    {
        if (EsTipoHospedaje(viaje.TipoPuntoPartida)
            && viaje.LugarPartidaId.HasValue
            && (!viaje.LatitudPartida.HasValue || !viaje.LongitudPartida.HasValue))
        {
            var lugar = await _lugaresClient.ObtenerPorIdAsync(viaje.LugarPartidaId.Value, cancellationToken);
            if (lugar is not null)
            {
                viaje.LatitudPartida = lugar.Latitud;
                viaje.LongitudPartida = lugar.Longitud;
                viaje.NombrePuntoPartida ??= lugar.Nombre;
                _viajeRepository.Actualizar(viaje);
                await _viajeRepository.GuardarCambiosAsync(cancellationToken);
            }
        }

        foreach (var dia in viaje.Dias)
        {
            if (!EsTipoHospedaje(dia.TipoPuntoPartida)
                || !dia.LugarPartidaId.HasValue
                || (dia.LatitudPartida.HasValue && dia.LongitudPartida.HasValue))
            {
                continue;
            }

            var lugar = await _lugaresClient.ObtenerPorIdAsync(dia.LugarPartidaId.Value, cancellationToken);
            if (lugar is null)
            {
                continue;
            }

            dia.LatitudPartida = lugar.Latitud;
            dia.LongitudPartida = lugar.Longitud;
            dia.NombrePuntoPartida ??= lugar.Nombre;
            _viajeRepository.Actualizar(viaje);
            await _viajeRepository.GuardarCambiosAsync(cancellationToken);
        }
    }

    private static bool EsTipoHospedaje(TipoPuntoPartida tipo)
        => tipo is TipoPuntoPartida.HospedajePropio or TipoPuntoPartida.HospedajeSugerido;

    private async Task<PerfilViajeroDto?> ObtenerPerfilSeguroAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _usuariosClient.ObtenerPerfilAsync(cancellationToken);
        }
        catch (ServicioExternoNoDisponibleException)
        {
            return null;
        }
    }

    private static IReadOnlyList<ViajeDia> ObtenerDias(Viaje viaje)
    {
        if (viaje.Dias.Count > 0)
        {
            return viaje.Dias
                .OrderBy(d => d.NumeroDia)
                .ToList();
        }

        return PlanificadorDias.Calcular(viaje.FechaInicio, viaje.FechaFin)
            .Select(dia => new ViajeDia
            {
                ViajeId = viaje.Id,
                NumeroDia = dia.NumeroDia,
                Fecha = dia.Fecha,
                Barrio = viaje.Barrio,
                TipoPuntoPartida = viaje.TipoPuntoPartida,
                LugarPartidaId = viaje.LugarPartidaId,
                NombrePuntoPartida = viaje.NombrePuntoPartida,
                DireccionPartida = viaje.DireccionPartida,
                LatitudPartida = viaje.LatitudPartida,
                LongitudPartida = viaje.LongitudPartida,
                TipoViaje = viaje.TipoViaje
            })
            .ToList();
    }

    private static IReadOnlyCollection<string> ObtenerCategoriasDia(Viaje viaje, ViajeDia dia)
        => dia.Etiquetas.Count > 0
            ? dia.Etiquetas.Select(e => e.CategoriaCodigo).ToList()
            : viaje.Etiquetas.Select(e => e.CategoriaCodigo).ToList();

    private static string ClaveCategorias(IReadOnlyCollection<string> categorias)
        => string.Join("|", categorias.OrderBy(codigo => codigo));

    private async Task<List<LugarCandidatoDto>> BuscarCandidatosAsync(
        Viaje viaje,
        PerfilViajeroDto? perfil,
        BarrioCaba barrio,
        Guid? lugarPartidaId,
        IReadOnlyCollection<string> categoriaCodigos,
        CancellationToken cancellationToken)
    {
        try
        {
            var intencion = ConstruirIntencion(viaje, perfil, barrio, categoriaCodigos);
            var busqueda = new BusquedaSemanticaLugaresDto
            {
                Query = intencion,
                Cantidad = 20,
                Barrio = barrio,
                CategoriaCodigos = categoriaCodigos.ToList()
            };

            var semanticos = await _lugaresClient.BuscarSemanticoAsync(
                busqueda,
                cancellationToken);

            var delBarrio = semanticos
                .Where(l => l.Barrio == barrio)
                .Where(l => l.Id != lugarPartidaId) // filtrar el lugar de partida
                .ToList();
            if (delBarrio.Count >= 2)
            {
                return delBarrio;
            }
        }
        catch (ServicioExternoNoDisponibleException)
        {
            // degrada al filtro determinista por barrio y categorías
        }

        var categoricos = await _lugaresClient.BuscarAsync(barrio, categoriaCodigos, cancellationToken);
        return categoricos
            .Where(l => l.Id != lugarPartidaId)
            .ToList();
    }

    private async Task RellenarSugerenciasAsync(
        IEnumerable<ParadaItinerario> paradas,
        IReadOnlyDictionary<Guid, string> nombres,
        CancellationToken cancellationToken)
    {
        foreach (var parada in paradas)
        {
            var nombre = nombres.TryGetValue(parada.LugarId, out var n) ? n : "este lugar";
            var prompt = $"Escribí una frase corta (máximo 25 palabras) sugiriendo qué hacer en \"{nombre}\" durante el turno {parada.Turno}. Respondé solo la frase, sin comillas ni markdown.";

            try
            {
                var texto = await _ollamaClient.ChatTextoAsync(prompt, cancellationToken);
                parada.SugerenciaIa = Truncar(texto, 255);
            }
            catch (ServicioExternoNoDisponibleException)
            {
                // sin sugerencia; no rompe la generación
            }
        }
    }

    private static string ConstruirIntencion(
        Viaje viaje,
        PerfilViajeroDto? perfil,
        BarrioCaba barrio,
        IReadOnlyCollection<string> categoriaCodigos)
    {
        var categorias = categoriaCodigos
            .Select(NombreCategoria)
            .Where(n => n is not null)
            .Cast<string>()
            .ToList();

        var partes = new List<string> { $"Lugares en {barrio}" };
        if (categorias.Count > 0)
        {
            partes.Add($"de tipo {string.Join(", ", categorias)}");
        }

        partes.Add($"presupuesto {viaje.NivelPresupuesto}");

        if (perfil?.RequiereRecorridoSinEscalones == true)
        {
            partes.Add("accesibles, sin escalones");
        }

        if (perfil?.RequiereBanoAccesible == true)
        {
            partes.Add("con baño accesible");
        }

        return string.Join(", ", partes);
    }

    private static string? NombreCategoria(string codigo) => codigo switch
    {
        "cafe_especialidad" => "café de especialidad",
        "museos_galerias" => "museos y galerías",
        "parques_aire_libre" => "parques y aire libre",
        "librerias_diseno" => "librerías y diseño",
        "bares_vermu" => "bares y vermuterías",
        "gastronomia_clasica" => "gastronomía clásica",
        "cocteleria_autor" => "coctelería de autor",
        _ => null
    };

    private static string Truncar(string texto, int max)
        => texto.Length <= max ? texto : texto[..max];

    public async Task<IReadOnlyCollection<ItinerarioResponseDto>> ObtenerAsync(
        Guid viajeId,
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var viaje = await _viajeRepository.ObtenerPorIdAsync(viajeId, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el viaje con id {viajeId}.");

        if (!viaje.EsMiembro(usuarioId))
        {
            throw new NotFoundException($"No se encontró el viaje con id {viajeId}.");
        }

        var itinerarios = await _itinerarioRepository.ObtenerPorViajeAsync(viajeId, cancellationToken);

        if (itinerarios.Count == 0)
        {
            throw new NotFoundException($"El viaje {viajeId} no tiene un itinerario generado.");
        }

        return itinerarios.Select(MapearRespuesta).ToList();
    }

    public async Task<IReadOnlyCollection<ItinerarioResponseDto>> RefinarAsync(
        Guid viajeId,
        TipoRefinamiento tipo,
        short? numeroDia,
        Guid usuarioIdSolicitante,
        CancellationToken cancellationToken = default)
    {
        var (viaje, itinerarios) = await PrepararModificacionAsync(
            viajeId, numeroDia, usuarioIdSolicitante, cancellationToken);

        var diasPorNumero = ObtenerDias(viaje).ToDictionary(d => d.NumeroDia);
        var lugaresPorDia = new Dictionary<
            (BarrioCaba Barrio, string Categorias),
            IReadOnlyCollection<LugarCandidatoDto>>();

        foreach (var itinerario in itinerarios)
        {
            var dia = ObtenerDiaConfig(viaje, diasPorNumero, itinerario.NumeroDia);
            var categorias = ObtenerCategoriasDia(viaje, dia);
            var clave = (dia.Barrio, ClaveCategorias(categorias));

            if (!lugaresPorDia.ContainsKey(clave))
            {
                lugaresPorDia[clave] = await BuscarLugaresAsync(
                    dia.Barrio,
                    categorias,
                    cancellationToken);
            }
        }

        var lugaresActuales = ConstruirLugaresActuales(
            itinerarios,
            lugaresPorDia.Values.SelectMany(lugares => lugares).ToList());

        var puntosPartida = diasPorNumero.Values
            .Where(dia => dia.LugarPartidaId.HasValue)
            .Select(dia => dia.LugarPartidaId!.Value)
            .ToHashSet();

        foreach (var itinerario in itinerarios)
        {
            var dia = ObtenerDiaConfig(viaje, diasPorNumero, itinerario.NumeroDia);
            var categorias = ObtenerCategoriasDia(viaje, dia);
            var lugaresDia = lugaresPorDia[(dia.Barrio, ClaveCategorias(categorias))];
            var disponibles = ConstruirDisponibles(itinerarios, lugaresDia, viaje)
                .Where(lugar => !puntosPartida.Contains(lugar.Id))
                .ToList();

            var modificado = _refiner.Refinar(tipo, itinerario, lugaresActuales, disponibles, viaje);

            if (modificado)
            {
                _itinerarioRepository.MarcarModificado(itinerario);
            }
        }

        await _itinerarioRepository.GuardarCambiosAsync(cancellationToken);

        return itinerarios.Select(MapearRespuesta).ToList();
    }

    public async Task<IReadOnlyCollection<ItinerarioResponseDto>> RefinarPorTextoAsync(
        Guid viajeId,
        string texto,
        short? numeroDia,
        Guid usuarioIdSolicitante,
        CancellationToken cancellationToken = default)
    {
        var tipo = await _interpretacionService.InterpretarRefinamientoAsync(texto, cancellationToken);

        return await RefinarAsync(viajeId, tipo, numeroDia, usuarioIdSolicitante, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ItinerarioResponseDto>> SugerirTurnosLibresAsync(
        Guid viajeId,
        short? numeroDia,
        Guid usuarioIdSolicitante,
        CancellationToken cancellationToken = default)
    {
        var (viaje, itinerarios) = await PrepararModificacionAsync(
            viajeId, numeroDia, usuarioIdSolicitante, cancellationToken);

        var perfil = await ObtenerPerfilSeguroAsync(cancellationToken);
        var diasPorNumero = ObtenerDias(viaje).ToDictionary(d => d.NumeroDia);
        var candidatosPorDia = new Dictionary<
            (BarrioCaba Barrio, Guid? LugarPartidaId, string Categorias),
            List<LugarCandidatoDto>>();

        foreach (var itinerario in itinerarios)
        {
            var dia = ObtenerDiaConfig(viaje, diasPorNumero, itinerario.NumeroDia);
            var categorias = ObtenerCategoriasDia(viaje, dia);
            var clave = (dia.Barrio, dia.LugarPartidaId, ClaveCategorias(categorias));

            if (!candidatosPorDia.TryGetValue(clave, out var lugares))
            {
                lugares = await BuscarCandidatosAsync(
                    viaje,
                    perfil,
                    dia.Barrio,
                    dia.LugarPartidaId,
                    categorias,
                    cancellationToken);
                candidatosPorDia[clave] = lugares;
            }

            var disponibles = ConstruirDisponibles(itinerarios, lugares, viaje);
            var sugerencias = _refiner.SugerirTurnosLibres(itinerario, disponibles, viaje);

            foreach (var sugerencia in sugerencias)
            {
                _itinerarioRepository.AgregarParada(sugerencia);
            }

            if (sugerencias.Count > 0)
            {
                _itinerarioRepository.MarcarModificado(itinerario);
            }
        }

        await _itinerarioRepository.GuardarCambiosAsync(cancellationToken);

        return itinerarios.Select(MapearRespuesta).ToList();
    }

    private async Task<(Viaje Viaje, IReadOnlyCollection<ItinerarioEntity> Itinerarios)> PrepararModificacionAsync(
        Guid viajeId,
        short? numeroDia,
        Guid usuarioIdSolicitante,
        CancellationToken cancellationToken)
    {
        var viaje = await _viajeRepository.ObtenerPorIdAsync(viajeId, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el viaje con id {viajeId}.");

        if (!viaje.EsAdministrador(usuarioIdSolicitante))
        {
            throw new AccesoDenegadoException("Solo un administrador del viaje puede modificar el itinerario.");
        }

        var itinerarios = await _itinerarioRepository.ObtenerPorViajeAsync(viajeId, cancellationToken);

        if (itinerarios.Count == 0)
        {
            throw new NotFoundException($"El viaje {viajeId} no tiene un itinerario generado.");
        }

        if (numeroDia.HasValue && !itinerarios.Any(i => i.NumeroDia == numeroDia.Value))
        {
            throw new ReglaDeNegocioException($"El día {numeroDia.Value} no existe en el itinerario.");
        }

        var destino = numeroDia.HasValue
            ? itinerarios.Where(i => i.NumeroDia == numeroDia.Value).ToList()
            : itinerarios;

        return (viaje, destino);
    }

    private Task<IReadOnlyCollection<LugarCandidatoDto>> BuscarLugaresAsync(
        BarrioCaba barrio,
        IReadOnlyCollection<string> categoriaCodigos,
        CancellationToken cancellationToken)
        => _lugaresClient.BuscarAsync(barrio, categoriaCodigos, cancellationToken);

    private static ViajeDia ObtenerDiaConfig(
        Viaje viaje,
        IReadOnlyDictionary<short, ViajeDia> diasPorNumero,
        short numeroDia)
    {
        if (diasPorNumero.TryGetValue(numeroDia, out var dia))
        {
            return dia;
        }

        return new ViajeDia
        {
            ViajeId = viaje.Id,
            NumeroDia = numeroDia,
            Barrio = viaje.Barrio,
            TipoPuntoPartida = viaje.TipoPuntoPartida,
            LugarPartidaId = viaje.LugarPartidaId,
            NombrePuntoPartida = viaje.NombrePuntoPartida,
            DireccionPartida = viaje.DireccionPartida,
            LatitudPartida = viaje.LatitudPartida,
            LongitudPartida = viaje.LongitudPartida,
            TipoViaje = viaje.TipoViaje
        };
    }

    private static IReadOnlyDictionary<Guid, LugarCandidatoDto> ConstruirLugaresActuales(
        IReadOnlyCollection<ItinerarioEntity> itinerarios,
        IReadOnlyCollection<LugarCandidatoDto> lugares)
    {
        var porId = lugares.ToDictionary(l => l.Id);
        var actuales = new Dictionary<Guid, LugarCandidatoDto>();

        foreach (var parada in itinerarios.SelectMany(i => i.Paradas))
        {
            if (porId.TryGetValue(parada.LugarId, out var lugar))
            {
                actuales[parada.LugarId] = lugar;
            }
        }

        return actuales;
    }

    private static List<LugarCandidatoDto> ConstruirDisponibles(
        IReadOnlyCollection<ItinerarioEntity> itinerarios,
        IReadOnlyCollection<LugarCandidatoDto> lugares,
        Viaje viaje)
    {
        var usados = new HashSet<Guid>(itinerarios.SelectMany(i => i.Paradas).Select(p => p.LugarId));
        return lugares
            .Where(l => !usados.Contains(l.Id))
            .Where(l => DentroDePresupuesto(l.NivelPrecio, viaje.NivelPresupuesto))
            .ToList();
    }

    private static bool DentroDePresupuesto(NivelPresupuesto candidato, NivelPresupuesto tope)
        => candidato <= tope;

    private static ItinerarioResponseDto MapearRespuesta(ItinerarioEntity itinerario) => new()
    {
        Id = itinerario.Id,
        ViajeId = itinerario.ViajeId,
        NumeroDia = itinerario.NumeroDia,
        Fecha = itinerario.Fecha,
        OrigenTipo = itinerario.OrigenTipo,
        OrigenDescripcion = itinerario.OrigenDescripcion,
        OrigenLatitud = itinerario.OrigenLatitud,
        OrigenLongitud = itinerario.OrigenLongitud,
        Version = itinerario.Version,
        Paradas = itinerario.Paradas
            .OrderBy(p => p.Turno)
            .ThenBy(p => p.OrdenParada)
            .Select(MapearParada)
            .ToList()
    };

    private static ParadaItinerarioDto MapearParada(ParadaItinerario parada) => new()
    {
        Id = parada.Id,
        LugarId = parada.LugarId,
        Turno = parada.Turno,
        OrdenParada = parada.OrdenParada,
        HoraInicio = parada.HoraInicio,
        DuracionMinutos = parada.DuracionMinutos,
        DistanciaMetrosDesdeAnterior = parada.DistanciaMetrosDesdeAnterior,
        CuadrasDesdeAnterior = parada.CuadrasDesdeAnterior,
        MinutosAPieDesdeAnterior = parada.MinutosAPieDesdeAnterior,
        OrigenParada = parada.OrigenParada,
        SugerenciaIa = parada.SugerenciaIa,
        NotasPersonales = parada.NotasPersonales
    };
}
