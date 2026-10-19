using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs;

public class RefinamientoDto
{
    public TipoRefinamiento Tipo { get; set; }
    public short? NumeroDia { get; set; }
}
