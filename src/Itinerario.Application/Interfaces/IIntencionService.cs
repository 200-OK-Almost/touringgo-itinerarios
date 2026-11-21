using Itinerario.Domain.Enums;

namespace Itinerario.Application.Interfaces;

public interface IIntencionService
{
    string Normalizar(string? texto);

    TipoAccionAsistente DetectarAccion(string? mensaje);
}
