namespace Itinerario.Application.DTOs;

public class IntencionLlmCruda
{
    public string? Accion { get; set; }
    public string? QuerySemantica { get; set; }
    public string? ParadaObjetivoId { get; set; }
    public List<string>? Categorias { get; set; }
    public bool? SoloAlAireLibre { get; set; }
    public bool? SoloBajoTecho { get; set; }
    public bool? AptoNinos { get; set; }
    public bool? AccesibleMovilidadReducida { get; set; }
    public string? RestriccionDietaria { get; set; }
    public string? Turno { get; set; }
    public short? NivelPrecioMaximo { get; set; }
    public double? RadioMetros { get; set; }
}
