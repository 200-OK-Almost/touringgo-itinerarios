using Itinerario.Application.DTOs;
using Itinerario.Domain.Enums;

namespace Itinerario.Application.Interfaces;

public interface IInterpretacionService
{
    Task<BosquejoViajeDto> InterpretarAsync(string texto, CancellationToken cancellationToken = default);

    Task<TipoRefinamiento> InterpretarRefinamientoAsync(string texto, CancellationToken cancellationToken = default);
}
