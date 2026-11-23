using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs;

public class IntencionAsistenteDto
{
    public TipoAccionAsistente Accion { get; set; } = TipoAccionAsistente.Recomendar;
    public string QuerySemantica { get; set; } = string.Empty;
    public BarrioCaba? Barrio { get; set; }
    public FiltrosIntencionDto Filtros { get; set; } = new();
    public ParadaObjetivoDto? ParadaObjetivo { get; set; }
    public double? RadioMetros { get; set; }
    public AclaracionDto? Aclaracion { get; set; }
}
