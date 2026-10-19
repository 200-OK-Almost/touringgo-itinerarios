using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs;

public class ParadaItinerarioDto
{
    public Guid Id { get; set; }
    public Guid LugarId { get; set; }
    public Turno Turno { get; set; }
    public short OrdenParada { get; set; }
    public TimeOnly? HoraInicio { get; set; }
    public short? DuracionMinutos { get; set; }
    public int? DistanciaMetrosDesdeAnterior { get; set; }
    public short? CuadrasDesdeAnterior { get; set; }
    public short? MinutosAPieDesdeAnterior { get; set; }
    public OrigenParada OrigenParada { get; set; }
    public string? SugerenciaIa { get; set; }
    public string? NotasPersonales { get; set; }
}
