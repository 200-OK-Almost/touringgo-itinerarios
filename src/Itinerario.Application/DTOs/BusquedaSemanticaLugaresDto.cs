using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs;

public class BusquedaSemanticaLugaresDto
{
    public string Query { get; set; } = string.Empty;
    public int Cantidad { get; set; } = 12;
    public BarrioCaba? Barrio { get; set; }
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
