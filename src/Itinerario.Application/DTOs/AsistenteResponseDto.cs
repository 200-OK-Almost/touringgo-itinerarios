namespace Itinerario.Application.DTOs;

public class AsistenteResponseDto
{
    public string Mensaje { get; set; } = string.Empty;
    public bool PuedeAplicar { get; set; }
    public IReadOnlyCollection<RecomendacionLugarDto> Recomendaciones { get; set; } = new List<RecomendacionLugarDto>();
    public AccionPropuestaDto? Accion { get; set; }
    public AclaracionDto? Aclaracion { get; set; }
    public string? Advertencia { get; set; }
}
