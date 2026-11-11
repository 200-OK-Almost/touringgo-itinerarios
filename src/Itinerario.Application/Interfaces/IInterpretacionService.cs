using Itinerario.Application.DTOs;

namespace Itinerario.Application.Interfaces;

public interface IInterpretacionService
{
    Task<BosquejoViajeDto> InterpretarAsync(string texto, CancellationToken cancellationToken = default);
}
