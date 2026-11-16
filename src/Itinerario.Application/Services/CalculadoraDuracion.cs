namespace Itinerario.Application.Services;

public static class CalculadoraDuracion
{
    public const int MinimoDias = 1;
    public const int MaximoDias = 7;
    public const string MensajeRangoInvalido = "El viaje puede durar entre 1 y 7 días.";

    public static int ContarDiasInclusivos(DateOnly fechaInicio, DateOnly fechaFin)
        => fechaFin.DayNumber - fechaInicio.DayNumber + 1;

    public static bool EsDuracionValida(int dias)
        => dias is >= MinimoDias and <= MaximoDias;

    public static bool EsRangoValido(DateOnly? fechaInicio, DateOnly? fechaFin)
    {
        if (fechaInicio is null || fechaFin is null)
        {
            return false;
        }

        return EsDuracionValida(ContarDiasInclusivos(fechaInicio.Value, fechaFin.Value));
    }
}
