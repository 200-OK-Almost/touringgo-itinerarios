using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs;

public class CrearViajeRecomendadoDto
{
    public string Titulo { get; set; } = string.Empty;
    public BarrioCaba Barrio { get; set; }
    public string? CategoriaCodigo { get; set; }
    public IReadOnlyCollection<Guid> LugarIds { get; set; } = new List<Guid>();
    public DateOnly? FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
}
