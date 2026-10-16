namespace Itinerario.Domain.Exceptions;

public class ConflictoDeConcurrenciaException : Exception
{
    public ConflictoDeConcurrenciaException(string message)
        : base(message)
    {
    }
}
