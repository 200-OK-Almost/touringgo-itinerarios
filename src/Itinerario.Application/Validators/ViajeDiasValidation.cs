using Itinerario.Application.DTOs;

namespace Itinerario.Application.Validators;

public static class ViajeDiasValidation
{
    public static bool CantidadValida(IReadOnlyCollection<ViajeDiaCreateDto>? dias)
        => dias is { Count: >= 1 and <= 7 };

    public static bool NumeracionValida(IReadOnlyCollection<ViajeDiaCreateDto>? dias)
    {
        if (dias is null)
        {
            return true;
        }

        var numeros = dias
            .OrderBy(d => d.NumeroDia)
            .Select(d => d.NumeroDia)
            .ToList();

        return numeros.Distinct().Count() == numeros.Count
            && numeros.Select((numero, indice) => numero == indice + 1).All(valido => valido);
    }

    public static bool CoincidenConRango(
        DateOnly? fechaInicio,
        DateOnly? fechaFin,
        IReadOnlyCollection<ViajeDiaCreateDto>? dias)
    {
        if (dias is null || dias.Count == 0 || fechaInicio is null || fechaFin is null || fechaFin < fechaInicio)
        {
            return true;
        }

        var fechasEsperadas = new List<DateOnly>();

        for (var fecha = fechaInicio.Value; fecha <= fechaFin.Value; fecha = fecha.AddDays(1))
        {
            fechasEsperadas.Add(fecha);
        }

        var fechasRecibidas = dias
            .OrderBy(d => d.NumeroDia)
            .Select(d => d.Fecha)
            .ToList();

        return fechasRecibidas.Count == fechasEsperadas.Count
            && fechasRecibidas.Select((fecha, indice) => fecha == fechasEsperadas[indice]).All(valido => valido);
    }
}
