using Itinerario.Domain.Enums;

namespace Itinerario.Application.DTOs;

public class RecomendacionLugarDto
{
    public Guid LugarId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Direccion { get; set; }
    public BarrioCaba Barrio { get; set; }
    public string? Subzona { get; set; }
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public decimal? Puntuacion { get; set; }
    public int CantidadResenas { get; set; }
    public short NivelPrecio { get; set; }
    public short DuracionMinutos { get; set; }
    public bool EsAireLibre { get; set; }
    public bool EsBajoTecho { get; set; }
    public bool AptoNinos { get; set; }
    public bool Accesible { get; set; }
    public string? FotoPortadaUrl { get; set; }
    public IReadOnlyCollection<string> Categorias { get; set; } = new List<string>();
    public IReadOnlyCollection<string> OpcionesDietarias { get; set; } = new List<string>();
    public double Similitud { get; set; }
    public string Motivo { get; set; } = string.Empty;
}
