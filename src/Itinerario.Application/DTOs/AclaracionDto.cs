namespace Itinerario.Application.DTOs;

public class AclaracionDto
{
    public string Pregunta { get; set; } = string.Empty;
    public IReadOnlyCollection<OpcionAclaracionDto> Opciones { get; set; } = new List<OpcionAclaracionDto>();
}
