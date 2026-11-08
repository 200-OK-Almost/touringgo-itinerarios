namespace Itinerario.Domain.Exceptions;

public class ServicioExternoNoDisponibleException : Exception
{
    public ServicioExternoNoDisponibleException(string message)
        : base(message)
    {
    }

    public ServicioExternoNoDisponibleException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
