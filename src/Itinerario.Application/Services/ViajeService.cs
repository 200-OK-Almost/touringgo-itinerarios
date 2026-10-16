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

    public ViajeService(IViajeRepository repository)
    {
        _repository = repository;
    }

    public async Task<ViajeResponseDto> CrearAsync(ViajeCreateDto dto, Guid creadoPorUsuarioId, CancellationToken cancellationToken = default)
    {
        for (var intento = 0; intento < MaxIntentosCodigo; intento++)
        {
            var codigoInvitacion = await GenerarCodigoInvitacionUnicoAsync(cancellationToken);

            var viaje = Viaje.Crear(
                Guid.NewGuid(),
                MapearDatos(dto),
                creadoPorUsuarioId,
                codigoInvitacion,
                dto.CategoriaIds,
                MapearParametrosFamilia(dto.ParametrosFamilia));

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

    public async Task<ViajeResponseDto> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => MapearRespuesta(await ObtenerEntidadAsync(id, cancellationToken));

    public async Task<ResultadoPaginadoDto<ViajeResponseDto>> ObtenerTodosAsync(
        int pagina = 1,
        int tamano = 20,
        CancellationToken cancellationToken = default)
    {
        var paginaNormalizada = pagina < 1 ? PaginaPorDefecto : pagina;
        var tamanoNormalizado = Math.Clamp(tamano, TamanoMinimo, TamanoMaximo);

        var resultado = await _repository.ObtenerPaginadoAsync(paginaNormalizada, tamanoNormalizado, cancellationToken);

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

        viaje.ActualizarDatos(MapearDatos(dto), dto.CategoriaIds, MapearParametrosFamilia(dto.ParametrosFamilia));
        viaje.CambiarEstado(dto.Estado);

        _repository.Actualizar(viaje);
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
            viaje.AgregarMiembro(usuarioId, RolMiembro.Miembro);

            _repository.Actualizar(viaje);
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
        CategoriaIds = viaje.Etiquetas.Select(e => e.CategoriaId).ToList(),
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
            }
    };
}
