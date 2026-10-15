using Itinerario.Domain.Enums;

namespace Itinerario.Domain.Entities;

public record DatosViaje(
    string Titulo,
    TipoViaje TipoViaje,
    BarrioCaba Barrio,
    AlcanceTemporal AlcanceTemporal,
    DateOnly? FechaInicio,
    DateOnly? FechaFin,
    NivelPresupuesto NivelPresupuesto,
    RitmoCaminata RitmoCaminata,
    short? LimiteCuadrasEntreParadas,
    TipoPuntoPartida TipoPuntoPartida,
    Guid? LugarPartidaId,
    string? NombrePuntoPartida,
    string? DireccionPartida,
    decimal? LatitudPartida,
    decimal? LongitudPartida);
