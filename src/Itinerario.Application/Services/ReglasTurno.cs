using Itinerario.Application.DTOs;
using Itinerario.Domain.Enums;

namespace Itinerario.Application.Services;

public static class ReglasTurno
{
    private static readonly IReadOnlyDictionary<Turno, HashSet<string>> CategoriasPorTurno =
        new Dictionary<Turno, HashSet<string>>
        {
            [Turno.Manana] = new(StringComparer.OrdinalIgnoreCase)
            {
                "cafe_especialidad",
                "parques_aire_libre",
                "museos_galerias",
                "librerias_diseno"
            },
            [Turno.Mediodia] = new(StringComparer.OrdinalIgnoreCase)
            {
                "gastronomia_clasica"
            },
            [Turno.Tarde] = new(StringComparer.OrdinalIgnoreCase)
            {
                "cafe_especialidad",
                "parques_aire_libre",
                "bares_vermu",
                "museos_galerias",
                "librerias_diseno"
            },
            [Turno.Noche] = new(StringComparer.OrdinalIgnoreCase)
            {
                "gastronomia_clasica",
                "bares_vermu",
                "cocteleria_autor"
            }
        };

    public static IReadOnlyCollection<string> CategoriasPermitidas(Turno turno)
        => CategoriasPorTurno.TryGetValue(turno, out var categorias)
            ? categorias
            : Array.Empty<string>();

    public static bool EsCompatible(Turno turno, LugarCandidatoDto lugar)
    {
        if (!CategoriasPorTurno.TryGetValue(turno, out var categorias))
        {
            return true;
        }

        return lugar.Categorias.Any(categoria => categorias.Contains(categoria));
    }
}
