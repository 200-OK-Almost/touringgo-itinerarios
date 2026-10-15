namespace Itinerario.Application.DTOs;

public class ResultadoPaginadoDto<T>
{
    public IReadOnlyCollection<T> Items { get; set; } = new List<T>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int Tamano { get; set; }
}
