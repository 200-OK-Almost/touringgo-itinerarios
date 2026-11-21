using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
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
