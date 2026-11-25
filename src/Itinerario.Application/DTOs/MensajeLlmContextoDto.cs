using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs;

public class MensajeLlmContextoDto
{
    public Guid ViajeId { get; set; }
    public string MensajeUsuario { get; set; } = string.Empty;
    public TipoAccionAsistente Accion { get; set; }
    public string? ParadaObjetivo { get; set; }
    public short? NumeroDia { get; set; }
    public BarrioCaba Barrio { get; set; }
    public FiltrosIntencionDto Filtros { get; set; } = new();
    public bool BusquedaAmpliada { get; set; }
    public bool SinResultadosEnBarrio { get; set; }
    public IReadOnlyCollection<MensajeHistorialDto> Historial { get; set; } = new List<MensajeHistorialDto>();
    public bool SinSenal { get; set; }
    public IReadOnlyCollection<RecomendacionLugarDto> Recomendaciones { get; set; } = new List<RecomendacionLugarDto>();
}
