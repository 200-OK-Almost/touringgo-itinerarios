using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs;

public class FiltrosIntencionDto
{
    public IReadOnlyCollection<string> CategoriaCodigos { get; set; } = new List<string>();
    public bool? SoloAlAireLibre { get; set; }
    public bool? SoloBajoTecho { get; set; }
    public bool? AptoNinos { get; set; }
    public bool? AccesibleMovilidadReducida { get; set; }
    public string? RestriccionDietaria { get; set; }
    public Turno? Turno { get; set; }
    public short? NivelPrecioMaximo { get; set; }
}
