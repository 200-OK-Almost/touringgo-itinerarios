namespace Itinerario.Domain.Entities;

public class EtiquetaViajeDia
{
    public Guid ViajeDiaId { get; set; }
    public string CategoriaCodigo { get; set; } = string.Empty;

    public ViajeDia? Dia { get; set; }
}
