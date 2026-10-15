namespace Itinerario.Application.Interfaces;

public interface IGeoCalculator
{
    double DistanciaMetros(double latitud1, double longitud1, double latitud2, double longitud2);
    int Cuadras(double metros);
    int MinutosAPie(double metros);
}
