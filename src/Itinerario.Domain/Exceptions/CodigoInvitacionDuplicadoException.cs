namespace Itinerario.Domain.Exceptions;

public class CodigoInvitacionDuplicadoException : Exception
{
    public CodigoInvitacionDuplicadoException(string message)
        : base(message)
    {
    }
}
