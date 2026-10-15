namespace Itinerario.Domain.Exceptions;

public class ReglaDeNegocioException : Exception
{
    public ReglaDeNegocioException(string message)
        : base(message)
    {
    }
}
