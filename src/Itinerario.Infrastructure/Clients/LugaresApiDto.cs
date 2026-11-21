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
    public string? Subzona { get; set; }
    public string? Direccion { get; set; }
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public short DuracionEstimadaMinutos { get; set; }
    public short NivelPrecio { get; set; }
    public decimal? Puntuacion { get; set; }
    public int CantidadResenas { get; set; }
    public bool EsAlAireLibre { get; set; }
    public bool EsBajoTecho { get; set; }
    public bool AptoNinos { get; set; }
    public bool AccesibleMovilidadReducida { get; set; }
    public bool OfreceMenuInfantil { get; set; }
    public string? FotoPortadaUrl { get; set; }
    public List<string> Categorias { get; set; } = new();
}

internal sealed class LugarSemanticoItemDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Barrio { get; set; }
    public string? Subzona { get; set; }
    public string? Direccion { get; set; }
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public string? Descripcion { get; set; }
    public short DuracionEstimadaMinutos { get; set; }
    public short NivelPrecio { get; set; }
    public decimal? Puntuacion { get; set; }
    public int CantidadResenas { get; set; }
    public bool EsAlAireLibre { get; set; }
    public bool EsBajoTecho { get; set; }
    public bool AptoNinos { get; set; }
    public bool AccesibleMovilidadReducida { get; set; }
    public bool OfreceMenuInfantil { get; set; }
    public List<string> OpcionesDietarias { get; set; } = new();
    public List<FotoItemDto> Fotos { get; set; } = new();
    public List<CategoriaItemDto> Categorias { get; set; } = new();
    public double Similitud { get; set; }
}

internal sealed class CategoriaItemDto
{
    public short Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
}

internal sealed class FotoItemDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public int Orden { get; set; }
    public bool EsPortada { get; set; }
}
