using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs;

public class PerfilViajeroDto
{
    public RitmoCaminata RitmoCaminata { get; set; }
    public IReadOnlyCollection<CategoriaAfinidadDto> Categorias { get; set; } = new List<CategoriaAfinidadDto>();
    public IReadOnlyCollection<string> Dietas { get; set; } = new List<string>();
    public bool RequiereRecorridoSinEscalones { get; set; }
    public bool RequiereBanoAccesible { get; set; }
}
