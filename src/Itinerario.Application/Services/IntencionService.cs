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

    private static readonly Dictionary<string, string[]> CategoriasDetectables = new(StringComparer.Ordinal)
    {
        ["cafe_especialidad"] = new[] { "cafe", "cafes", "cafeteria", "cafeterias" },
        ["museos_galerias"] = new[] { "museo", "museos", "galeria", "galerias" },
        ["parques_aire_libre"] = new[] { "parque", "parques", "plaza", "plazas" },
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
        "recomenda", "recomendar", "recomendame", "recomendas", "recomendarias",
        "sugeri", "sugerir", "sugerime", "sugeris",
        "opcion", "opciones", "idea", "ideas", "alternativa", "alternativas",
        "dame", "dime", "decime", "mostrame", "quiero", "queria",
        "podrias", "podes", "puedo", "puede", "hay",
        "algo", "otro", "otra", "tambien", "solo", "aqui", "ahi", "alli", "favor",
        "primera", "primer", "segunda", "segundo", "tercer", "tercera",
        "cuarta", "cuarto", "quinta", "quinto", "ultima", "ultimo"
    };

    private static readonly HashSet<string> SenalesDeViaje = new(StringComparer.Ordinal)
    {
        "cafe", "cafes", "cafeteria", "cafeterias",
        "museo", "museos", "galeria", "galerias",
        "parque", "parques", "plaza", "plazas",
        "aire", "libre", "libreria", "librerias", "libro", "libros",
        "bar", "bares", "vermu", "vermut",
        "restaurante", "restaurantes", "comida", "gastronomia",
        "cocteleria", "coctelerias", "coctel", "cocteles", "trago", "tragos",
        "comer", "cenar", "almorzar", "merendar", "desayunar", "tomar",
        "caminar", "caminando", "paseo", "caminata",
        "romantico", "romantica", "romanticos", "romanticas",
        "tranquilo", "tranquila", "tranquilos", "tranquilas",
        "divertido", "divertida", "lindo", "linda", "lindos", "lindas",
        "naturaleza", "arte", "cultura", "musica",
        "vegano", "vegana", "vegetariano", "vegetariana",
        "celiaco", "celiaca", "kosher", "halal", "gluten", "tacc", "lactosa",
        "ninos", "ninas", "familia", "chicos", "chicas", "infantil",
        "accesible", "silla", "ruedas", "movilidad", "reducida", "discapacidad",
        "economico", "economica","barato", "barata",
        "equilibrado", "equilibrada", "moderado", "moderada", "caro", "cara", "presupuesto",
        "manana", "mediodia", "tarde", "noche",
        "cerca", "cercano", "cercana", "cercanos", "cercanas",
        "proximo", "proxima", "proximos", "proximas",
        "bajo", "techo", "lluvia", "adentro", "cubierto", "outdoor", "afuera"
    };

    private static readonly HashSet<string> PalabrasSinSenal = new(StringComparer.Ordinal)
    {
        "hola", "buenas", "buenos", "dias", "tardes", "noches",
        "gracias", "chau", "adios", "hasta", "luego", "ok", "dale"
    };

    private static readonly (string Termino, BarrioCaba Barrio)[] BarriosDetectables =
    {
        ("puerto madero", BarrioCaba.PuertoMadero),
        ("puertomadero", BarrioCaba.PuertoMadero),
        ("san telmo", BarrioCaba.SanTelmo),
        ("santelmo", BarrioCaba.SanTelmo),
        ("san nicolas", BarrioCaba.SanNicolas),
        ("sannicolas", BarrioCaba.SanNicolas),
        ("palermo", BarrioCaba.Palermo),
        ("recoleta", BarrioCaba.Recoleta)
    };

    private static readonly HashSet<string> PalabrasIgnoradasQuery = new(PalabrasIgnoradas, StringComparer.Ordinal)
    {
        "cerca", "cercano", "cercana", "cercanos", "cercanas",
        "proximo", "proxima", "proximos", "proximas",
        "barrio", "zona", "palermo", "recoleta", "santelmo", "san", "telmo", "sannicolas", "nicolas",
        "puerto", "madero", "puertomadero",
        "economico", "economica", "barato", "barata",
        "equilibrado", "equilibrada", "moderado", "moderada",
        "caro", "cara", "presupuesto",
        "ninos", "ninas", "familia", "chicos", "chicas", "infantil",
        "accesible", "silla", "ruedas", "movilidad", "reducida", "discapacidad",
        "vegano", "vegana", "vegetariano", "vegetariana", "celiaco", "celiaca",
        "kosher", "halal", "gluten", "tacc", "lactosa", "sin"
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

    public bool TieneSenalDeViaje(string? mensaje)
    {
        var normalizado = Normalizar(mensaje);

        if (string.IsNullOrEmpty(normalizado))
        {
            return false;
        }

        var tokens = Regex.Split(normalizado, @"\s+");

        if (tokens.All(token => PalabrasSinSenal.Contains(token)))
        {
            return false;
        }

        if (tokens.Any(token => SenalesDeViaje.Contains(token)))
        {
            return true;
        }

        if (DetectarBarrio(normalizado).HasValue)
        {
            return true;
        }

        var filtros = ExtraerFiltros(normalizado);

        return filtros.CategoriaCodigos.Count > 0
            || filtros.SoloAlAireLibre == true
            || filtros.SoloBajoTecho == true
            || filtros.AptoNinos == true
            || filtros.AccesibleMovilidadReducida == true
            || !string.IsNullOrWhiteSpace(filtros.RestriccionDietaria)
            || filtros.Turno.HasValue
            || filtros.NivelPrecioMaximo.HasValue;
    }

    private static BarrioCaba? DetectarBarrio(string normalizado)
    {
        foreach (var (termino, barrio) in BarriosDetectables)
        {
            if (ContieneAlguna(normalizado, new[] { termino }))
            {
                return barrio;
            }
        }

        return null;
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
            0 => CrearAclaracion(ordenadas, string.Join(" ", tokens)),
            1 => Resolver(coincidencias[0]),
            _ => CrearAclaracion(coincidencias)
        };
    }

    public IntencionAsistenteDto Interpretar(
        string? mensaje,
        IReadOnlyCollection<ParadaResolubleDto>? paradas = null,
        double radioMetrosDefault = 800)
    {
        var normalizado = Normalizar(mensaje);
        var accion = DetectarAccion(mensaje);

        var intencion = new IntencionAsistenteDto
        {
            Accion = accion,
            Barrio = DetectarBarrio(normalizado),
            Filtros = ExtraerFiltros(normalizado),
            RadioMetros = MencionaCercania(normalizado) ? radioMetrosDefault : null
        };

        if (accion == TipoAccionAsistente.Reemplazar && paradas is { Count: > 0 })
        {
            var resolucion = ResolverParadaObjetivo(mensaje, paradas);
            intencion.ParadaObjetivo = resolucion.Parada;
            intencion.Aclaracion = resolucion.Aclaracion;
        }

        intencion.QuerySemantica = ConstruirQuery(normalizado, accion, intencion.ParadaObjetivo);

        return intencion;
    }

    private static FiltrosIntencionDto ExtraerFiltros(string normalizado)
    {
        var bajoTecho = ContieneAlguna(normalizado, new[]
        {
            "bajo techo", "lluvia", "adentro", "cubierto"
        });

        var aireLibre = ContieneAlguna(normalizado, new[]
        {
            "aire libre", "al aire libre", "afuera", "outdoor"
        });

        return new FiltrosIntencionDto
        {
            CategoriaCodigos = DetectarCategorias(normalizado),
            SoloAlAireLibre = !bajoTecho && aireLibre ? true : null,
            SoloBajoTecho = bajoTecho ? true : null,
            AptoNinos = ContieneAlguna(normalizado, new[]
            {
                "ninos", "ninas", "familia", "chicos", "chicas", "infantil"
            })
                ? true
                : null,
            AccesibleMovilidadReducida = ContieneAlguna(normalizado, new[]
            {
                "accesible", "silla de ruedas", "movilidad reducida", "discapacidad"
            })
                ? true
                : null,
            RestriccionDietaria = DetectarDieta(normalizado),
            Turno = DetectarTurno(normalizado),
            NivelPrecioMaximo = DetectarPrecio(normalizado)
        };
    }

    private static IReadOnlyCollection<string> DetectarCategorias(string normalizado)
        => CategoriasDetectables
            .Where(categoria => categoria.Value.Any(alias => ContienePalabra(normalizado, alias)))
            .Select(categoria => categoria.Key)
            .ToList();

    private static string? DetectarDieta(string normalizado)
    {
        var dietas = new (string Termino, string Valor)[]
        {
            ("sin gluten", "sin gluten"),
            ("sin tacc", "sin tacc"),
            ("sin lactosa", "sin lactosa"),
            ("vegetariano", "vegetariano"),
            ("vegetariana", "vegetariano"),
            ("vegano", "vegano"),
            ("vegana", "vegano"),
            ("celiaco", "celiaco"),
            ("celiaca", "celiaco"),
            ("kosher", "kosher"),
            ("halal", "halal")
        };

        foreach (var (termino, valor) in dietas)
        {
            if (ContienePalabra(normalizado, termino))
            {
                return valor;
            }
        }

        return null;
    }

    private static Turno? DetectarTurno(string normalizado)
    {
        var turnos = new (string Palabra, Turno Turno)[]
        {
            ("manana", Turno.Manana),
            ("mediodia", Turno.Mediodia),
            ("tarde", Turno.Tarde),
            ("noche", Turno.Noche)
        };

        return turnos
            .Select(turno => (turno.Turno, Indice: IndicePalabra(normalizado, turno.Palabra)))
            .Where(turno => turno.Indice >= 0)
            .OrderBy(turno => turno.Indice)
            .Select(turno => (Turno?)turno.Turno)
            .FirstOrDefault();
    }

    private static short? DetectarPrecio(string normalizado)
    {
        if (ContieneAlguna(normalizado, new[] { "economico", "economica", "barato", "barata" }))
        {
            return 1;
        }

        if (ContieneAlguna(normalizado, new[] { "equilibrado", "equilibrada", "moderado", "moderada" }))
        {
            return 2;
        }

        return null;
    }

    private static bool MencionaCercania(string normalizado)
        => ContieneAlguna(normalizado, new[]
        {
            "cerca", "cercano", "cercana", "cercanos", "cercanas",
            "proximo", "proxima", "proximos", "proximas",
            "a pocas cuadras", "caminando"
        });

    private string ConstruirQuery(
        string normalizado,
        TipoAccionAsistente accion,
        ParadaObjetivoDto? parada)
    {
        var texto = accion == TipoAccionAsistente.Reemplazar
            ? ExtraerTextoDeseado(normalizado)
            : normalizado;

        if (parada is not null)
        {
            texto = QuitarNombreParada(texto, parada);
        }

        var tokens = Regex.Split(texto, @"\s+")
            .Where(token => token.Length >= 3 && !PalabrasIgnoradasQuery.Contains(token))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return tokens.Count == 0 ? "lugar" : string.Join(" ", tokens);
    }

    private static string ExtraerTextoDeseado(string normalizado)
    {
        var indicePor = normalizado.IndexOf(" por ", StringComparison.Ordinal);
        if (indicePor >= 0)
        {
            return normalizado[(indicePor + " por ".Length)..];
        }

        var indiceEnVezDe = normalizado.IndexOf("en vez de ", StringComparison.Ordinal);
        if (indiceEnVezDe >= 0)
        {
            return normalizado[(indiceEnVezDe + "en vez de ".Length)..];
        }

        var indiceEnLugarDe = normalizado.IndexOf("en lugar de ", StringComparison.Ordinal);
        if (indiceEnLugarDe >= 0)
        {
            return normalizado[(indiceEnLugarDe + "en lugar de ".Length)..];
        }

        return string.Empty;
    }

    private string QuitarNombreParada(string texto, ParadaObjetivoDto parada)
    {
        var tokens = ObtenerTokens(Normalizar(parada.Nombre));

        foreach (var token in tokens)
        {
            texto = Regex.Replace(
                texto,
                $@"\b{Regex.Escape(token)}\b",
                " ",
                RegexOptions.CultureInvariant);
        }

        return texto;
    }

    private static int IndicePalabra(string texto, string palabra)
    {
        var match = Regex.Match(
            texto,
            $@"\b{Regex.Escape(palabra)}\b",
            RegexOptions.CultureInvariant);

        return match.Success ? match.Index : -1;
    }

    private static bool ContienePalabra(string texto, string palabra)
        => Regex.IsMatch(
            texto,
            $@"\b{Regex.Escape(palabra)}\b",
            RegexOptions.CultureInvariant);

    private static string ObtenerTextoObjetivo(string normalizado)
    {
        var indicePor = normalizado.IndexOf(" por ", StringComparison.Ordinal);
        if (indicePor > 0)
        {
            return normalizado[..indicePor].Trim();
        }

        var indiceEnVezDe = normalizado.IndexOf("en vez de ", StringComparison.Ordinal);
        if (indiceEnVezDe >= 0)
        {
            return normalizado[(indiceEnVezDe + "en vez de ".Length)..].Trim();
        }

        var indiceEnLugarDe = normalizado.IndexOf("en lugar de ", StringComparison.Ordinal);
        if (indiceEnLugarDe >= 0)
        {
            return normalizado[(indiceEnLugarDe + "en lugar de ".Length)..].Trim();
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
        IEnumerable<ParadaResolubleDto> paradas,
        string? terminoBuscado = null) => new()
    {
        Aclaracion = new AclaracionDto
        {
            Pregunta = string.IsNullOrWhiteSpace(terminoBuscado)
                ? "¿A cuál parada te referís?"
                : $"No encontré \"{terminoBuscado.Trim()}\". ¿A cuál parada te referís?",
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
        => frases.Any(frase => ContienePalabra(normalizado, frase));
}
