namespace Itinerario.Infrastructure.Clients;

internal sealed class LugaresPaginadosDto
{
    public List<LugarListadoItemDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int Tamano { get; set; }
}

internal sealed class LugarListadoItemDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Barrio { get; set; }
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public short DuracionEstimadaMinutos { get; set; }
    public short NivelPrecio { get; set; }
    public bool EsAlAireLibre { get; set; }
    public List<string> Categorias { get; set; } = new();
}

internal sealed class LugarSemanticoItemDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Barrio { get; set; }
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public short DuracionEstimadaMinutos { get; set; }
    public short NivelPrecio { get; set; }
    public bool EsAlAireLibre { get; set; }
    public List<CategoriaItemDto> Categorias { get; set; } = new();
}

internal sealed class CategoriaItemDto
{
    public short Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
}
