namespace Itinerario.Domain.Exceptions;

public class AccesoDenegadoException : Exception
{
    public AccesoDenegadoException(string message)
        : base(message)
    {
    }
}
