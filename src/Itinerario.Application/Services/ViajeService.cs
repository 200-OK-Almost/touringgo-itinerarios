using System.Security.Cryptography;
using Itinerario.Application.DTOs;
using Itinerario.Application.Interfaces;
using Itinerario.Domain.Entities;
using Itinerario.Domain.Enums;
using Itinerario.Domain.Exceptions;

namespace Itinerario.Application.Services;

public class ViajeService : IViajeService
{
    private const int LongitudCodigoInvitacion = 10;
    private const string CaracteresCodigoInvitacion = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int MaxIntentosCodigo = 10;
    private const int PaginaPorDefecto = 1;
    private const int TamanoMinimo = 1;
    private const int TamanoMaximo = 100;

    private readonly IViajeRepository _repository;
    private readonly ILugaresClient _lugaresClient;

    public ViajeService(IViajeRepository repository, ILugaresClient lugaresClient)
    {
        _repository = repository;
        _lugaresClient = lugaresClient;
    }

    public async Task<ViajeResponseDto> CrearAsync(ViajeCreateDto dto, Guid creadoPorUsuarioId, CancellationToken cancellationToken = default)
    {
        await CompletarCoordenadasHospedajeSiAplicaAsync(dto, cancellationToken);

        for (var intento = 0; intento < MaxIntentosCodigo; intento++)
        {
            var codigoInvitacion = await GenerarCodigoInvitacionUnicoAsync(cancellationToken);

            var viaje = Viaje.Crear(
                Guid.NewGuid(),
                MapearDatos(dto),
                creadoPorUsuarioId,
                codigoInvitacion,
                dto.CategoriaCodigos,
                MapearParametrosFamilia(dto.ParametrosFamilia));

            AgregarDias(viaje, dto);

            try
            {
                await _repository.AgregarAsync(viaje, cancellationToken);
                await _repository.GuardarCambiosAsync(cancellationToken);

                return MapearRespuesta(viaje);
            }
            catch (CodigoInvitacionDuplicadoException)
            {
                _repository.Descartar(viaje);
            }
        }

        throw new InvalidOperationException("No se pudo generar un código de invitación único.");
    }

    public async Task<ViajeResponseDto> ObtenerPorIdAsync(Guid id, Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var viaje = await ObtenerEntidadAsync(id, cancellationToken);

        if (!viaje.EsMiembro(usuarioId))
        {
            throw new NotFoundException($"No se encontró el viaje con id {id}.");
        }

        return MapearRespuesta(viaje);
    }

    public async Task<ResultadoPaginadoDto<ViajeResponseDto>> ObtenerTodosAsync(
        Guid usuarioId,
        int pagina = 1,
        int tamano = 20,
        CancellationToken cancellationToken = default)
    {
        var paginaNormalizada = pagina < 1 ? PaginaPorDefecto : pagina;
        var tamanoNormalizado = Math.Clamp(tamano, TamanoMinimo, TamanoMaximo);

        var resultado = await _repository.ObtenerPaginadoPorUsuarioAsync(
            usuarioId, paginaNormalizada, tamanoNormalizado, cancellationToken);

        return new ResultadoPaginadoDto<ViajeResponseDto>
        {
            Items = resultado.Items.Select(MapearRespuesta).ToList(),
            Total = resultado.Total,
            Pagina = paginaNormalizada,
            Tamano = tamanoNormalizado
        };
    }

    public async Task<ViajeResponseDto> ActualizarAsync(
        Guid id,
        ViajeUpdateDto dto,
        Guid usuarioIdSolicitante,
        CancellationToken cancellationToken = default)
    {
        var viaje = await ObtenerEntidadAsync(id, cancellationToken);

        if (!viaje.EsAdministrador(usuarioIdSolicitante))
        {
            throw new AccesoDenegadoException("Solo un administrador del viaje puede modificarlo.");
        }

        await CompletarCoordenadasHospedajeSiAplicaAsync(dto, cancellationToken);

        viaje.ActualizarDatos(MapearDatos(dto));
        viaje.CambiarEstado(dto.Estado);

        _repository.Actualizar(viaje);
        _repository.SincronizarEtiquetas(viaje, dto.CategoriaCodigos);
        SincronizarDias(viaje, dto);
        _repository.EstablecerParametrosFamilia(viaje, MapearParametrosFamilia(dto.ParametrosFamilia));
        await _repository.GuardarCambiosAsync(cancellationToken);

        return MapearRespuesta(viaje);
    }

    public async Task EliminarAsync(Guid id, Guid usuarioIdSolicitante, CancellationToken cancellationToken = default)
    {
        var viaje = await ObtenerEntidadAsync(id, cancellationToken);

        if (!viaje.EsAdministrador(usuarioIdSolicitante))
        {
            throw new AccesoDenegadoException("Solo un administrador del viaje puede eliminarlo.");
        }

        _repository.Eliminar(viaje);
        await _repository.GuardarCambiosAsync(cancellationToken);
    }

    public async Task<ViajeResponseDto> UnirseAsync(string codigoInvitacion, Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var viaje = await _repository.ObtenerPorCodigoInvitacionAsync(codigoInvitacion, cancellationToken)
            ?? throw new NotFoundException($"No se encontró un viaje con el código {codigoInvitacion}.");

        if (viaje.Miembros.All(m => m.UsuarioId != usuarioId))
        {
            var miembro = new MiembroViaje
            {
                ViajeId = viaje.Id,
                UsuarioId = usuarioId,
                Rol = RolMiembro.Miembro,
                FechaIncorporacion = DateTimeOffset.UtcNow
            };

            viaje.Miembros.Add(miembro);
            _repository.AgregarMiembro(miembro);

            await _repository.GuardarCambiosAsync(cancellationToken);
        }

        return MapearRespuesta(viaje);
    }

    private async Task<Viaje> ObtenerEntidadAsync(Guid id, CancellationToken cancellationToken)
        => await _repository.ObtenerPorIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el viaje con id {id}.");

    private async Task<string> GenerarCodigoInvitacionUnicoAsync(CancellationToken cancellationToken)
    {
        for (var intento = 0; intento < MaxIntentosCodigo; intento++)
        {
            var bytes = RandomNumberGenerator.GetBytes(LongitudCodigoInvitacion);
            var codigo = new string(bytes
                .Select(b => CaracteresCodigoInvitacion[b % CaracteresCodigoInvitacion.Length])
                .ToArray());

            if (!await _repository.ExisteCodigoInvitacionAsync(codigo, cancellationToken))
            {
                return codigo;
            }
        }

        throw new InvalidOperationException("No se pudo generar un código de invitación único.");
    }

    private async Task CompletarCoordenadasHospedajeSiAplicaAsync(ViajeCreateDto dto, CancellationToken cancellationToken)
    {
        if (EsTipoHospedaje(dto.TipoPuntoPartida)
            && dto.LugarPartidaId.HasValue
            && (!dto.LatitudPartida.HasValue || !dto.LongitudPartida.HasValue))
        {
            var lugar = await _lugaresClient.ObtenerPorIdAsync(dto.LugarPartidaId.Value, cancellationToken);
            if (lugar is not null)
            {
                dto.LatitudPartida = lugar.Latitud;
                dto.LongitudPartida = lugar.Longitud;
                dto.NombrePuntoPartida ??= lugar.Nombre;
            }
        }

        if (dto.Dias is not null)
        {
            foreach (var dia in dto.Dias)
            {
                await CompletarCoordenadasHospedajeDiaAsync(dia, cancellationToken);
            }
        }
    }

    private async Task CompletarCoordenadasHospedajeSiAplicaAsync(ViajeUpdateDto dto, CancellationToken cancellationToken)
    {
        if (EsTipoHospedaje(dto.TipoPuntoPartida)
            && dto.LugarPartidaId.HasValue
            && (!dto.LatitudPartida.HasValue || !dto.LongitudPartida.HasValue))
        {
            var lugar = await _lugaresClient.ObtenerPorIdAsync(dto.LugarPartidaId.Value, cancellationToken);
            if (lugar is not null)
            {
                dto.LatitudPartida = lugar.Latitud;
                dto.LongitudPartida = lugar.Longitud;
                dto.NombrePuntoPartida ??= lugar.Nombre;
            }
        }

        if (dto.Dias is not null)
        {
            foreach (var dia in dto.Dias)
            {
                await CompletarCoordenadasHospedajeDiaAsync(dia, cancellationToken);
            }
        }
    }

    private async Task CompletarCoordenadasHospedajeDiaAsync(
        ViajeDiaCreateDto dia,
        CancellationToken cancellationToken)
    {
        if (EsTipoHospedaje(dia.TipoPuntoPartida)
            && dia.LugarPartidaId.HasValue
            && (!dia.LatitudPartida.HasValue || !dia.LongitudPartida.HasValue))
        {
            var lugar = await _lugaresClient.ObtenerPorIdAsync(dia.LugarPartidaId.Value, cancellationToken);
            if (lugar is not null)
            {
                dia.LatitudPartida = lugar.Latitud;
                dia.LongitudPartida = lugar.Longitud;
                dia.NombrePuntoPartida ??= lugar.Nombre;
            }
        }
    }

    private static bool EsTipoHospedaje(TipoPuntoPartida tipo)
        => tipo is TipoPuntoPartida.HospedajePropio or TipoPuntoPartida.HospedajeSugerido;

    private static void AgregarDias(Viaje viaje, ViajeCreateDto dto)
    {
        var dias = dto.Dias is { Count: > 0 }
            ? MapearDias(viaje, dto.Dias)
            : ConstruirDiasPorDefecto(
                viaje.Id,
                dto.FechaInicio,
                dto.FechaFin,
                dto.Barrio,
                dto.TipoPuntoPartida,
                dto.LugarPartidaId,
                dto.NombrePuntoPartida,
                dto.DireccionPartida,
                dto.LatitudPartida,
                dto.LongitudPartida,
                dto.TipoViaje,
                dto.CategoriaCodigos);

        foreach (var dia in dias)
        {
            viaje.Dias.Add(dia);
        }
    }

    private void SincronizarDias(Viaje viaje, ViajeUpdateDto dto)
    {
        if (dto.Dias is not null)
        {
            _repository.SincronizarDias(viaje, MapearDias(viaje, dto.Dias));
            return;
        }

        if (viaje.Dias.Count == 0)
        {
            _repository.SincronizarDias(
                viaje,
                ConstruirDiasPorDefecto(
                    viaje.Id,
                    viaje.FechaInicio,
                    viaje.FechaFin,
                    viaje.Barrio,
                    viaje.TipoPuntoPartida,
                    viaje.LugarPartidaId,
                    viaje.NombrePuntoPartida,
                    viaje.DireccionPartida,
                    viaje.LatitudPartida,
                    viaje.LongitudPartida,
                    viaje.TipoViaje,
                    viaje.Etiquetas.Select(e => e.CategoriaCodigo).ToList()));
        }
    }

    private static IReadOnlyCollection<ViajeDia> MapearDias(
        Viaje viaje,
        IReadOnlyCollection<ViajeDiaCreateDto> dias)
        => dias
            .OrderBy(d => d.NumeroDia)
            .Select(d => MapearDia(viaje, d))
            .ToList();

    private static ViajeDia MapearDia(Viaje viaje, ViajeDiaCreateDto dto)
    {
        var id = Guid.NewGuid();
        var categorias = dto.CategoriaCodigos is { Count: > 0 }
            ? dto.CategoriaCodigos
            : viaje.Etiquetas.Select(e => e.CategoriaCodigo).ToList();

        return new ViajeDia
        {
            Id = id,
            ViajeId = viaje.Id,
            NumeroDia = dto.NumeroDia,
            Fecha = dto.Fecha,
            Barrio = dto.Barrio,
            TipoPuntoPartida = dto.TipoPuntoPartida,
            LugarPartidaId = dto.LugarPartidaId,
            NombrePuntoPartida = dto.NombrePuntoPartida,
            DireccionPartida = dto.DireccionPartida,
            LatitudPartida = dto.LatitudPartida,
            LongitudPartida = dto.LongitudPartida,
            TipoViaje = dto.TipoViaje ?? viaje.TipoViaje,
            Etiquetas = categorias
                .Select(codigo => new EtiquetaViajeDia
                {
                    ViajeDiaId = id,
                    CategoriaCodigo = codigo
                })
                .ToList()
        };
    }

    private static IReadOnlyCollection<ViajeDia> ConstruirDiasPorDefecto(
        Guid viajeId,
        DateOnly? fechaInicio,
        DateOnly? fechaFin,
        BarrioCaba barrio,
        TipoPuntoPartida tipoPuntoPartida,
        Guid? lugarPartidaId,
        string? nombrePuntoPartida,
        string? direccionPartida,
        decimal? latitudPartida,
        decimal? longitudPartida,
        TipoViaje tipoViaje,
        IReadOnlyCollection<string> categoriaCodigos)
        => PlanificadorDias.Calcular(fechaInicio, fechaFin)
            .Select(dia =>
            {
                var id = Guid.NewGuid();

                return new ViajeDia
                {
                    Id = id,
                    ViajeId = viajeId,
                    NumeroDia = dia.NumeroDia,
                    Fecha = dia.Fecha,
                    Barrio = barrio,
                    TipoPuntoPartida = tipoPuntoPartida,
                    LugarPartidaId = lugarPartidaId,
                    NombrePuntoPartida = nombrePuntoPartida,
                    DireccionPartida = direccionPartida,
                    LatitudPartida = latitudPartida,
                    LongitudPartida = longitudPartida,
                    TipoViaje = tipoViaje,
                    Etiquetas = categoriaCodigos
                        .Select(codigo => new EtiquetaViajeDia
                        {
                            ViajeDiaId = id,
                            CategoriaCodigo = codigo
                        })
                        .ToList()
                };
            })
            .ToList();

    private static DatosViaje MapearDatos(ViajeCreateDto dto) => new(
        dto.Titulo,
        dto.TipoViaje,
        dto.Barrio,
        dto.AlcanceTemporal,
        dto.FechaInicio,
        dto.FechaFin,
        dto.NivelPresupuesto,
        dto.RitmoCaminata,
        dto.LimiteCuadrasEntreParadas,
        dto.TipoPuntoPartida,
        dto.LugarPartidaId,
        dto.NombrePuntoPartida,
        dto.DireccionPartida,
        dto.LatitudPartida,
        dto.LongitudPartida);

    private static DatosViaje MapearDatos(ViajeUpdateDto dto) => new(
        dto.Titulo,
        dto.TipoViaje,
        dto.Barrio,
        dto.AlcanceTemporal,
        dto.FechaInicio,
        dto.FechaFin,
        dto.NivelPresupuesto,
        dto.RitmoCaminata,
        dto.LimiteCuadrasEntreParadas,
        dto.TipoPuntoPartida,
        dto.LugarPartidaId,
        dto.NombrePuntoPartida,
        dto.DireccionPartida,
        dto.LatitudPartida,
        dto.LongitudPartida);

    private static ParametrosFamilia? MapearParametrosFamilia(ParametrosFamiliaDto? dto)
        => dto is null
            ? null
            : new ParametrosFamilia
            {
                CantidadAcompanantes = dto.CantidadAcompanantes,
                HayNinos = dto.HayNinos,
                CantidadNinos = dto.CantidadNinos,
                MovilidadReducida = dto.MovilidadReducida,
                RequiereRitmoBajo = dto.RequiereRitmoBajo,
                RequiereMenuInfantil = dto.RequiereMenuInfantil
            };

    private static ViajeResponseDto MapearRespuesta(Viaje viaje) => new()
    {
        Id = viaje.Id,
        Titulo = viaje.Titulo,
        TipoViaje = viaje.TipoViaje,
        Estado = viaje.Estado,
        Barrio = viaje.Barrio,
        AlcanceTemporal = viaje.AlcanceTemporal,
        FechaInicio = viaje.FechaInicio,
        FechaFin = viaje.FechaFin,
        NivelPresupuesto = viaje.NivelPresupuesto,
        RitmoCaminata = viaje.RitmoCaminata,
        LimiteCuadrasEntreParadas = viaje.LimiteCuadrasEntreParadas,
        TipoPuntoPartida = viaje.TipoPuntoPartida,
        LugarPartidaId = viaje.LugarPartidaId,
        NombrePuntoPartida = viaje.NombrePuntoPartida,
        DireccionPartida = viaje.DireccionPartida,
        LatitudPartida = viaje.LatitudPartida,
        LongitudPartida = viaje.LongitudPartida,
        CodigoInvitacion = viaje.CodigoInvitacion,
        CreadoPorUsuarioId = viaje.CreadoPorUsuarioId,
        FechaCreacion = viaje.FechaCreacion,
        FechaActualizacion = viaje.FechaActualizacion,
        CategoriaCodigos = viaje.Etiquetas.Select(e => e.CategoriaCodigo).ToList(),
        Dias = viaje.Dias.Count > 0
            ? viaje.Dias
                .OrderBy(d => d.NumeroDia)
                .Select(dia => MapearDiaRespuesta(dia, viaje))
                .ToList()
            : null,
        ParametrosFamilia = viaje.ParametrosFamilia is null
            ? null
            : new ParametrosFamiliaDto
            {
                CantidadAcompanantes = viaje.ParametrosFamilia.CantidadAcompanantes,
                HayNinos = viaje.ParametrosFamilia.HayNinos,
                CantidadNinos = viaje.ParametrosFamilia.CantidadNinos,
                MovilidadReducida = viaje.ParametrosFamilia.MovilidadReducida,
                RequiereRitmoBajo = viaje.ParametrosFamilia.RequiereRitmoBajo,
                RequiereMenuInfantil = viaje.ParametrosFamilia.RequiereMenuInfantil
            },
        Itinerarios = viaje.Itinerarios?.Count > 0
            ? viaje.Itinerarios
                .OrderBy(i => i.NumeroDia)
                .Select(MapearItinerario)
                .ToList()
            : null
    };

    private static ViajeDiaResponseDto MapearDiaRespuesta(ViajeDia dia, Viaje viaje) => new()
    {
        Id = dia.Id,
        ViajeId = dia.ViajeId,
        NumeroDia = dia.NumeroDia,
        Fecha = dia.Fecha,
        Barrio = dia.Barrio,
        TipoPuntoPartida = dia.TipoPuntoPartida,
        LugarPartidaId = dia.LugarPartidaId,
        NombrePuntoPartida = dia.NombrePuntoPartida,
        DireccionPartida = dia.DireccionPartida,
        LatitudPartida = dia.LatitudPartida,
        LongitudPartida = dia.LongitudPartida,
        TipoViaje = dia.TipoViaje ?? viaje.TipoViaje,
        CategoriaCodigos = dia.Etiquetas.Count > 0
            ? dia.Etiquetas.Select(e => e.CategoriaCodigo).ToList()
            : viaje.Etiquetas.Select(e => e.CategoriaCodigo).ToList()
    };

    private static ItinerarioResponseDto MapearItinerario(Domain.Entities.Itinerario itinerario) => new()
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
