using Itinerario.Application.Interfaces;

namespace Itinerario.Application.Services;

public class GeoCalculator : IGeoCalculator
{
    private const double RadioTierraMetros = 6_371_000;
    private const double MetrosPorCuadra = 100;
    private const double MetrosPorMinutoAPie = 80;

    public double DistanciaMetros(double latitud1, double longitud1, double latitud2, double longitud2)
    {
        var deltaLatitud = AGrados(latitud2 - latitud1);
        var deltaLongitud = AGrados(longitud2 - longitud1);

        var a = Math.Sin(deltaLatitud / 2) * Math.Sin(deltaLatitud / 2)
                + Math.Cos(AGrados(latitud1)) * Math.Cos(AGrados(latitud2))
                * Math.Sin(deltaLongitud / 2) * Math.Sin(deltaLongitud / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return RadioTierraMetros * c;
    }

    public int Cuadras(double metros)
        => (int)Math.Round(metros / MetrosPorCuadra, MidpointRounding.AwayFromZero);

    public int MinutosAPie(double metros)
        => (int)Math.Round(metros / MetrosPorMinutoAPie, MidpointRounding.AwayFromZero);

    private static double AGrados(double radianes) => radianes * Math.PI / 180;
}
