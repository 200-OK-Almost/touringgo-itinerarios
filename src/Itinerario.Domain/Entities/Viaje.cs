using Itinerario.Domain.Enums;
using Itinerario.Domain.Exceptions;

namespace Itinerario.Domain.Entities;

public class Viaje
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public TipoViaje TipoViaje { get; set; }
    public EstadoViaje Estado { get; set; }
    public BarrioCaba Barrio { get; set; }
    public AlcanceTemporal AlcanceTemporal { get; set; }
    public DateOnly? FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public NivelPresupuesto NivelPresupuesto { get; set; }
    public RitmoCaminata RitmoCaminata { get; set; }
    public short? LimiteCuadrasEntreParadas { get; set; }
    public TipoPuntoPartida TipoPuntoPartida { get; set; }
    public Guid? LugarPartidaId { get; set; }
    public string? NombrePuntoPartida { get; set; }
    public string? DireccionPartida { get; set; }
    public decimal? LatitudPartida { get; set; }
    public decimal? LongitudPartida { get; set; }
    public string CodigoInvitacion { get; set; } = string.Empty;
    public Guid CreadoPorUsuarioId { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
    public DateTimeOffset FechaActualizacion { get; set; }

    public ParametrosFamilia? ParametrosFamilia { get; set; }
    public ICollection<MiembroViaje> Miembros { get; set; } = new List<MiembroViaje>();
    public ICollection<EtiquetaViaje> Etiquetas { get; set; } = new List<EtiquetaViaje>();
    public ICollection<Itinerario> Itinerarios { get; set; } = new List<Itinerario>();

    public static Viaje Crear(
        Guid id,
        DatosViaje datos,
        Guid creadoPorUsuarioId,
        string codigoInvitacion,
        IEnumerable<short> categoriaIds,
        ParametrosFamilia? parametros)
    {
        var viaje = new Viaje
        {
            Id = id,
            CodigoInvitacion = codigoInvitacion,
            CreadoPorUsuarioId = creadoPorUsuarioId,
            Estado = EstadoViaje.Borrador,
            FechaCreacion = DateTimeOffset.UtcNow
        };

        viaje.ActualizarDatos(datos, categoriaIds, parametros);
        viaje.AgregarMiembro(creadoPorUsuarioId, RolMiembro.Administrador);

        return viaje;
    }

    public void ActualizarDatos(DatosViaje datos, IEnumerable<short> categoriaIds, ParametrosFamilia? parametros)
    {
        Titulo = datos.Titulo;
        TipoViaje = datos.TipoViaje;
        Barrio = datos.Barrio;
        AlcanceTemporal = datos.AlcanceTemporal;
        FechaInicio = datos.FechaInicio;
        FechaFin = datos.FechaFin;
        NivelPresupuesto = datos.NivelPresupuesto;
        RitmoCaminata = datos.RitmoCaminata;
        LimiteCuadrasEntreParadas = datos.LimiteCuadrasEntreParadas;
        TipoPuntoPartida = datos.TipoPuntoPartida;
        LugarPartidaId = datos.LugarPartidaId;
        NombrePuntoPartida = datos.NombrePuntoPartida;
        DireccionPartida = datos.DireccionPartida;
        LatitudPartida = datos.LatitudPartida;
        LongitudPartida = datos.LongitudPartida;
        FechaActualizacion = DateTimeOffset.UtcNow;

        Etiquetas = categoriaIds
            .Select(categoriaId => new EtiquetaViaje { ViajeId = Id, CategoriaId = categoriaId })
            .ToList();

        ParametrosFamilia = parametros;

        if (ParametrosFamilia is not null)
        {
            ParametrosFamilia.ViajeId = Id;
        }
    }

    public void CambiarEstado(EstadoViaje nuevoEstado)
    {
        if (nuevoEstado == Estado)
        {
            return;
        }

        var transicionValida = (Estado, nuevoEstado) switch
        {
            (EstadoViaje.Borrador, EstadoViaje.Planificado) => true,
            (EstadoViaje.Planificado, EstadoViaje.Finalizado) => true,
            _ => false
        };

        if (!transicionValida)
        {
            throw new ReglaDeNegocioException($"No se puede pasar del estado {Estado} a {nuevoEstado}.");
        }

        Estado = nuevoEstado;
        FechaActualizacion = DateTimeOffset.UtcNow;
    }

    public bool EsAdministrador(Guid usuarioId)
        => Miembros.Any(m => m.UsuarioId == usuarioId && m.Rol == RolMiembro.Administrador);

    public void AgregarMiembro(Guid usuarioId, RolMiembro rol)
    {
        if (Miembros.All(m => m.UsuarioId != usuarioId))
        {
            Miembros.Add(new MiembroViaje
            {
                ViajeId = Id,
                UsuarioId = usuarioId,
                Rol = rol,
                FechaIncorporacion = DateTimeOffset.UtcNow
            });
        }
    }
}
