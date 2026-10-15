namespace Itinerario.Application;

public record ResultadoPaginado<T>(
    IReadOnlyCollection<T> Items,
    int Total);
