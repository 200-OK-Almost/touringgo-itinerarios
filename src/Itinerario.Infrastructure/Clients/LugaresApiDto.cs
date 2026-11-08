namespace Itinerario.Infrastructure.Clients;

internal sealed class LugaresPaginadosDto
{
    public List<LugarItemDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int Tamano { get; set; }
}

internal sealed class LugarItemDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public short DuracionEstimadaMinutos { get; set; }
    public short NivelPrecio { get; set; }
    public bool EsAlAireLibre { get; set; }
}
