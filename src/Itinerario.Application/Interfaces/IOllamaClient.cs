namespace Itinerario.Application.Interfaces;

public interface IOllamaClient
{
    Task<T> ChatJsonAsync<T>(string prompt, CancellationToken cancellationToken = default);

    Task<string> ChatTextoAsync(string prompt, CancellationToken cancellationToken = default);
}
