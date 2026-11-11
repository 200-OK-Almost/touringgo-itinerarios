using Itinerario.Application.DTOs;
using Itinerario.Application.Interfaces;
using Itinerario.Domain.Enums;
using Itinerario.Domain.Exceptions;

namespace Itinerario.Application.Services;

public class InterpretacionService : IInterpretacionService
{
    private static readonly string[] CodigosValidos =
    {
        "cafe_especialidad",
        "museos_galerias",
        "parques_aire_libre",
        "librerias_diseno",
        "bares_vermu",
        "gastronomia_clasica",
        "cocteleria_autor"
    };

    private readonly IOllamaClient _ollamaClient;

    public InterpretacionService(IOllamaClient ollamaClient)
    {
        _ollamaClient = ollamaClient;
    }

    public async Task<BosquejoViajeDto> InterpretarAsync(string texto, CancellationToken cancellationToken = default)
    {
        var prompt = $$"""
            Sos un asistente que interpreta pedidos de salidas por la Ciudad de Buenos Aires.
            Responde ÚNICAMENTE con un JSON válido (sin texto adicional, sin markdown) con esta forma:
            {
              "titulo": "texto corto",
              "tipoViaje": "Solo" | "Familia" | "Amigos",
              "barrio": "Palermo" | "Recoleta" | "SanTelmo" | "SanNicolas" | "PuertoMadero",
              "alcanceTemporal": "DiaCompleto" | "TardeNoche" | "MananaAlmuerzo",
              "nivelPresupuesto": "Economico" | "Equilibrado" | "SinRestricciones",
              "ritmoCaminata": "Tranquilo" | "Activo",
              "categoriaCodigos": [2 a 4 códigos de esta lista: cafe_especialidad, museos_galerias, parques_aire_libre, librerias_diseno, bares_vermu, gastronomia_clasica, cocteleria_autor]
            }

            Pedido del usuario: {{texto}}
            """;

        var crudo = await _ollamaClient.ChatJsonAsync<InterpretacionCruda>(prompt, cancellationToken);

        var categorias = NormalizarCategorias(crudo.CategoriaCodigos);
        if (categorias.Count < 2)
        {
            throw new ReglaDeNegocioException("No se pudieron interpretar al menos 2 categorías de interés.");
        }

        return new BosquejoViajeDto
        {
            Titulo = string.IsNullOrWhiteSpace(crudo.Titulo) ? "Salida por CABA" : crudo.Titulo.Trim(),
            TipoViaje = ParseEnum(crudo.TipoViaje, TipoViaje.Solo),
            Barrio = ParseEnum(crudo.Barrio, BarrioCaba.Palermo),
            AlcanceTemporal = ParseEnum(crudo.AlcanceTemporal, AlcanceTemporal.DiaCompleto),
            NivelPresupuesto = ParseEnum(crudo.NivelPresupuesto, NivelPresupuesto.Equilibrado),
            RitmoCaminata = ParseEnum(crudo.RitmoCaminata, RitmoCaminata.Tranquilo),
            CategoriaCodigos = categorias
        };
    }

    public async Task<TipoRefinamiento> InterpretarRefinamientoAsync(string texto, CancellationToken cancellationToken = default)
    {
        var prompt = $$"""
            Clasificá el siguiente pedido de refinamiento de un recorrido en UNA de estas opciones:
            - "MasEconomico" (más barato o económico)
            - "ReducirCaminata" (menos caminata, lugares más cercanos entre sí)
            - "MasAireLibre" (más lugares al aire libre)

            Respondé ÚNICAMENTE con un JSON válido: { "tipo": "MasEconomico" | "ReducirCaminata" | "MasAireLibre" }

            Pedido: {{texto}}
            """;

        var crudo = await _ollamaClient.ChatJsonAsync<RefinamientoCrudo>(prompt, cancellationToken);
        return ParseEnum(crudo.Tipo, TipoRefinamiento.MasEconomico);
    }

    private static List<string> NormalizarCategorias(List<string>? categorias)
        => (categorias ?? new List<string>())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Select(c => CodigosValidos.FirstOrDefault(v => string.Equals(v, c, StringComparison.OrdinalIgnoreCase)))
            .Where(c => c is not null)
            .Cast<string>()
            .Distinct()
            .Take(4)
            .ToList();

    private static T ParseEnum<T>(string? value, T fallback) where T : struct, Enum
        => Enum.TryParse<T>(value, ignoreCase: true, out var result) ? result : fallback;

    private sealed class InterpretacionCruda
    {
        public string? Titulo { get; set; }
        public string? TipoViaje { get; set; }
        public string? Barrio { get; set; }
        public string? AlcanceTemporal { get; set; }
        public string? NivelPresupuesto { get; set; }
        public string? RitmoCaminata { get; set; }
        public List<string>? CategoriaCodigos { get; set; }
    }

    private sealed class RefinamientoCrudo
    {
        public string? Tipo { get; set; }
    }
}
