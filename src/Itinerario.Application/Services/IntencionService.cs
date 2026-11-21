using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Itinerario.Application.DTOs;
using Itinerario.Application.Interfaces;
using Itinerario.Domain.Enums;

namespace Itinerario.Application.Services;

public class IntencionService : IIntencionService
{
    private static readonly string[] FrasesReemplazo =
    {
        "reemplaza", "reemplazar", "reemplazo",
        "cambia", "cambiar", "cambio",
        "sustitui", "sustituir",
        "en vez de", "en lugar de"
    };

    private static readonly string[] FrasesAgregar =
    {
        "agrega", "agregar", "agregame",
        "suma", "sumar", "sumame",
        "anade", "anadir", "anadime", "anadi",
        "incluye", "incluir", "inclui",
        "mete", "meter"
    };

    private static readonly string[] FrasesRecomendar =
    {
        "recomenda", "recomendar", "recomendame",
        "sugeri", "sugerir", "sugerime",
        "que lugar", "que lugares",
        "donde", "opcion", "opciones",
        "idea", "ideas",
        "alternativa", "alternativas"
    };

    private static readonly string[] FrasesConsulta =
    {
        "que ", "cual ", "cuales ",
        "donde ", "como ",
        "me recomendas", "me recomendarias",
        "recomendame", "sugerime",
        "opciones", "ideas", "alternativas"
    };

    private static readonly Dictionary<string, string[]> AliasCategorias = new(StringComparer.Ordinal)
    {
        ["cafe_especialidad"] = new[] { "cafe", "cafes", "cafeteria", "cafeterias" },
        ["museos_galerias"] = new[] { "museo", "museos", "galeria", "galerias" },
        ["parques_aire_libre"] = new[] { "parque", "parques", "plaza", "plazas", "aire", "libre" },
        ["librerias_diseno"] = new[] { "libreria", "librerias", "libro", "libros" },
        ["bares_vermu"] = new[] { "bar", "bares", "vermu", "vermut" },
        ["gastronomia_clasica"] = new[] { "restaurante", "restaurantes", "comida", "gastronomia" },
        ["cocteleria_autor"] = new[] { "cocteleria", "coctelerias", "coctel", "cocteles", "trago", "tragos" }
    };

    private static readonly HashSet<string> PalabrasIgnoradas = new(StringComparer.Ordinal)
    {
        "el", "la", "los", "las", "un", "una", "unos", "unas",
        "de", "del", "al", "a", "en", "con", "por", "para",
        "que", "me", "te", "se", "mi", "tu", "su", "lo", "le", "les",
        "y", "o", "u",
        "parada", "paradas", "lugar", "lugares", "sitio", "sitios",
        "actual", "actuales", "dia", "dias",
        "manana", "mediodia", "tarde", "noche",
        "reemplaza", "reemplazar", "reemplazo", "cambia", "cambiar", "cambio",
        "sustitui", "sustituir",
        "agrega", "agregar", "agregame", "suma", "sumar", "sumame",
        "anade", "anadir", "anadime", "anadi", "incluye", "incluir", "inclui",
        "mete", "meter", "pon", "pone", "poner",
        "algo", "otro", "otra",
        "primera", "primer", "segunda", "segundo", "tercer", "tercera",
        "cuarta", "cuarto", "quinta", "quinto", "ultima", "ultimo"
    };

    public string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        var descompuesto = texto.Normalize(NormalizationForm.FormD);
        var constructor = new StringBuilder(descompuesto.Length);

        foreach (var caracter in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
            {
                constructor.Append(char.ToLowerInvariant(caracter));
            }
        }

        var sinTildes = constructor
            .ToString()
            .Normalize(NormalizationForm.FormC);

        var sinSignos = Regex.Replace(sinTildes, @"[^a-z0-9\s]", " ");

        return Regex.Replace(sinSignos, @"\s+", " ").Trim();
    }

    public TipoAccionAsistente DetectarAccion(string? mensaje)
    {
        var normalizado = Normalizar(mensaje);

        if (string.IsNullOrEmpty(normalizado))
        {
            return TipoAccionAsistente.Recomendar;
        }

        if (ContieneAlguna(normalizado, FrasesReemplazo))
        {
            return TipoAccionAsistente.Reemplazar;
        }

        if (EsConsulta(normalizado))
        {
            return TipoAccionAsistente.Recomendar;
        }

        if (ContieneAlguna(normalizado, FrasesAgregar))
        {
            return TipoAccionAsistente.Agregar;
        }

        return TipoAccionAsistente.Recomendar;
    }

    public ResolucionParadaObjetivoDto ResolverParadaObjetivo(
        string? mensaje,
        IReadOnlyCollection<ParadaResolubleDto> paradas)
    {
        var ordenadas = paradas
            .OrderBy(p => p.Turno)
            .ThenBy(p => p.OrdenParada)
            .ToList();

        if (ordenadas.Count == 0)
        {
            return new ResolucionParadaObjetivoDto();
        }

        var normalizado = Normalizar(mensaje);
        var objetivo = ObtenerTextoObjetivo(normalizado);

        var orden = DetectarOrden(objetivo);
        if (orden.HasValue)
        {
            var indice = orden.Value == -1 ? ordenadas.Count : orden.Value;

            return indice >= 1 && indice <= ordenadas.Count
                ? Resolver(ordenadas[indice - 1])
                : CrearAclaracion(ordenadas);
        }

        var tokens = ObtenerTokens(objetivo);
        if (tokens.Count == 0)
        {
            return ordenadas.Count == 1
                ? Resolver(ordenadas[0])
                : CrearAclaracion(ordenadas);
        }

        var coincidencias = ordenadas
            .Where(parada => tokens.Any(token => CoincideConParada(parada, token)))
            .ToList();

        return coincidencias.Count switch
        {
            0 => CrearAclaracion(ordenadas),
            1 => Resolver(coincidencias[0]),
            _ => CrearAclaracion(coincidencias)
        };
    }

    private static string ObtenerTextoObjetivo(string normalizado)
    {
        var indicePor = normalizado.IndexOf(" por ", StringComparison.Ordinal);
        if (indicePor > 0)
        {
            return normalizado[..indicePor].Trim();
        }

        var indiceEnVezDe = normalizado.IndexOf(" en vez de ", StringComparison.Ordinal);
        if (indiceEnVezDe >= 0)
        {
            return normalizado[(indiceEnVezDe + " en vez de ".Length)..].Trim();
        }

        var indiceEnLugarDe = normalizado.IndexOf(" en lugar de ", StringComparison.Ordinal);
        if (indiceEnLugarDe >= 0)
        {
            return normalizado[(indiceEnLugarDe + " en lugar de ".Length)..].Trim();
        }

        return normalizado;
    }

    private static int? DetectarOrden(string objetivo)
    {
        if (Regex.IsMatch(objetivo, @"\bultim[ao]\b"))
        {
            return -1;
        }

        if (Regex.IsMatch(objetivo, @"\bprimer[ao]?\b"))
        {
            return 1;
        }

        if (Regex.IsMatch(objetivo, @"\bsegund[ao]\b"))
        {
            return 2;
        }

        if (Regex.IsMatch(objetivo, @"\btercer[ao]?\b"))
        {
            return 3;
        }

        if (Regex.IsMatch(objetivo, @"\bcuart[ao]\b"))
        {
            return 4;
        }

        return Regex.IsMatch(objetivo, @"\bquint[ao]\b") ? 5 : null;
    }

    private static IReadOnlyList<string> ObtenerTokens(string objetivo)
        => Regex.Split(objetivo, @"\s+")
            .Where(token => token.Length >= 3 && !PalabrasIgnoradas.Contains(token))
            .Distinct(StringComparer.Ordinal)
            .ToList();

    private bool CoincideConParada(ParadaResolubleDto parada, string token)
    {
        var variantes = Variantes(token);
        var nombre = Normalizar(parada.Nombre);

        if (variantes.Any(variante => Regex.IsMatch(
            nombre,
            $@"\b{Regex.Escape(variante)}\b",
            RegexOptions.CultureInvariant)))
        {
            return true;
        }

        return parada.Categorias.Any(categoria =>
            AliasCategorias.TryGetValue(categoria, out var alias)
            && variantes.Any(variante => alias.Contains(variante, StringComparer.Ordinal)));
    }

    private static IEnumerable<string> Variantes(string token)
    {
        yield return token;

        if (token.EndsWith("es", StringComparison.Ordinal) && token.Length > 4)
        {
            yield return token[..^2];
        }

        if (token.EndsWith("s", StringComparison.Ordinal) && token.Length > 3)
        {
            yield return token[..^1];
        }
    }

    private static ResolucionParadaObjetivoDto Resolver(ParadaResolubleDto parada) => new()
    {
        Parada = new ParadaObjetivoDto
        {
            ParadaId = parada.ParadaId,
            Nombre = parada.Nombre,
            Turno = parada.Turno
        }
    };

    private static ResolucionParadaObjetivoDto CrearAclaracion(
        IEnumerable<ParadaResolubleDto> paradas) => new()
    {
        Aclaracion = new AclaracionDto
        {
            Pregunta = "¿A cuál parada te referís?",
            Opciones = paradas
                .Select(parada => new OpcionAclaracionDto
                {
                    Id = parada.ParadaId.ToString(),
                    Etiqueta = $"{parada.Nombre} ({EtiquetaTurno(parada.Turno)})"
                })
                .ToList()
        }
    };

    private static string EtiquetaTurno(Turno turno) => turno switch
    {
        Turno.Manana => "Mañana",
        Turno.Mediodia => "Mediodía",
        Turno.Tarde => "Tarde",
        Turno.Noche => "Noche",
        _ => turno.ToString()
    };

    private static bool EsConsulta(string normalizado)
        => FrasesConsulta.Any(frase => normalizado.StartsWith(frase, StringComparison.Ordinal)
            || normalizado.Contains($" {frase}", StringComparison.Ordinal))
            || ContieneAlguna(normalizado, FrasesRecomendar);

    private static bool ContieneAlguna(string normalizado, IEnumerable<string> frases)
        => frases.Any(frase => Regex.IsMatch(
            normalizado,
            $@"\b{Regex.Escape(frase)}\b",
            RegexOptions.CultureInvariant));
}
