using System.Text.RegularExpressions;
using Itinerario.Application.Constants;
using Itinerario.Application.DTOs;
using Itinerario.Domain.Enums;

namespace Itinerario.Application.Services;

public static class IntencionLlmSanitizer
{
    private const int MaxQuerySemanticaCaracteres = 200;
    private const int MaxRestriccionDietariaCaracteres = 50;
    private const double RadioMinimoMetros = 100;
    private const double RadioMaximoMetros = 5000;

    public static IntencionAsistenteDto? Sanitizar(
        IntencionLlmCruda? crudo,
        IntencionLlmContextoDto contexto)
    {
        if (crudo is null
            || !Enum.TryParse<TipoAccionAsistente>(
                crudo.Accion,
                ignoreCase: true,
                out var accion))
        {
            return null;
        }

        var paradaObjetivo = ResolverParadaObjetivo(crudo.ParadaObjetivoId, contexto);

        if (accion == TipoAccionAsistente.Reemplazar && paradaObjetivo is null)
        {
            return null;
        }

        var bajoTecho = crudo.SoloBajoTecho == true;
        var aireLibre = !bajoTecho && crudo.SoloAlAireLibre == true;

        return new IntencionAsistenteDto
        {
            Accion = accion,
            QuerySemantica = NormalizarQuery(crudo.QuerySemantica),
            Filtros = new FiltrosIntencionDto
            {
                CategoriaCodigos = NormalizarCategorias(crudo.Categorias),
                SoloAlAireLibre = aireLibre ? true : null,
                SoloBajoTecho = bajoTecho ? true : null,
                AptoNinos = crudo.AptoNinos == true ? true : null,
                AccesibleMovilidadReducida = crudo.AccesibleMovilidadReducida == true
                    ? true
                    : null,
                RestriccionDietaria = NormalizarDieta(crudo.RestriccionDietaria),
                Turno = ParseTurno(crudo.Turno),
                NivelPrecioMaximo = ClampearNivelPrecio(crudo.NivelPrecioMaximo)
            },
            ParadaObjetivo = paradaObjetivo,
            RadioMetros = ClampearRadio(crudo.RadioMetros)
        };
    }

    private static string NormalizarQuery(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return "lugar";
        }

        var limpia = Regex.Replace(query.Trim(), @"\s+", " ");

        return limpia.Length <= MaxQuerySemanticaCaracteres
            ? limpia
            : limpia[..MaxQuerySemanticaCaracteres].Trim();
    }

    private static IReadOnlyCollection<string> NormalizarCategorias(
        IEnumerable<string>? categorias)
        => (categorias ?? Array.Empty<string>())
            .Where(categoria => !string.IsNullOrWhiteSpace(categoria))
            .Select(categoria => CategoriasLugares.Codigos.FirstOrDefault(
                codigo => string.Equals(codigo, categoria.Trim(), StringComparison.OrdinalIgnoreCase)))
            .Where(categoria => categoria is not null)
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();

    private static string? NormalizarDieta(string? dieta)
    {
        if (string.IsNullOrWhiteSpace(dieta))
        {
            return null;
        }

        var limpia = Regex.Replace(dieta.Trim(), @"\s+", " ");

        return limpia.Length <= MaxRestriccionDietariaCaracteres
            ? limpia
            : limpia[..MaxRestriccionDietariaCaracteres].Trim();
    }

    private static Turno? ParseTurno(string? turno)
        => Enum.TryParse<Turno>(turno, ignoreCase: true, out var valor)
            ? valor
            : null;

    private static short? ClampearNivelPrecio(short? nivel)
        => nivel.HasValue ? (short)Math.Clamp(nivel.Value, (short)1, (short)3) : null;

    private static double? ClampearRadio(double? radio)
        => radio is > 0
            ? Math.Clamp(radio.Value, RadioMinimoMetros, RadioMaximoMetros)
            : null;

    private static ParadaObjetivoDto? ResolverParadaObjetivo(
        string? paradaObjetivoId,
        IntencionLlmContextoDto contexto)
    {
        if (!Guid.TryParse(paradaObjetivoId, out var paradaId))
        {
            return null;
        }

        var parada = contexto.Paradas.FirstOrDefault(
            candidata => candidata.ParadaId == paradaId);

        return parada is null
            ? null
            : new ParadaObjetivoDto
            {
                ParadaId = parada.ParadaId,
                Nombre = parada.Nombre,
                Turno = parada.Turno
            };
    }
}
