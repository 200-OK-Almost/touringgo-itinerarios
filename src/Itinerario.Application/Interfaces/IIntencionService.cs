using Itinerario.Application.DTOs;
using Itinerario.Domain.Enums;

namespace Itinerario.Application.Interfaces;

public interface IIntencionService
{
    string Normalizar(string? texto);

    TipoAccionAsistente DetectarAccion(string? mensaje);

    ResolucionParadaObjetivoDto ResolverParadaObjetivo(
        string? mensaje,
        IReadOnlyCollection<ParadaResolubleDto> paradas);

    IntencionAsistenteDto Interpretar(
        string? mensaje,
        IReadOnlyCollection<ParadaResolubleDto>? paradas = null,
        double radioMetrosDefault = 800);
}
