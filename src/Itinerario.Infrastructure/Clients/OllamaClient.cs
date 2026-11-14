using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Itinerario.Application.Interfaces;
using Itinerario.Domain.Exceptions;
using Microsoft.Extensions.Configuration;
using Polly.Timeout;

namespace Itinerario.Infrastructure.Clients;

public class OllamaClient : IOllamaClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly string _model;

    public OllamaClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _model = configuration["Ollama:Model"] ?? "llama3.2";
    }

    public async Task<T> ChatJsonAsync<T>(string prompt, CancellationToken cancellationToken = default)
    {
        var contenido = await CompletarAsync(prompt, jsonMode: true, cancellationToken);

        return JsonSerializer.Deserialize<T>(contenido, JsonOptions)
            ?? throw new InvalidOperationException("El modelo devolvió una respuesta que no es un JSON válido.");
    }

    public Task<string> ChatTextoAsync(string prompt, CancellationToken cancellationToken = default)
        => CompletarAsync(prompt, jsonMode: false, cancellationToken);

    private async Task<string> CompletarAsync(string prompt, bool jsonMode, CancellationToken cancellationToken)
    {
        var request = new OllamaChatRequest
        {
            Model = _model,
            Messages = new List<OllamaMensaje> { new() { Role = "user", Content = prompt } },
            Stream = false,
            ResponseFormat = jsonMode ? new OllamaResponseFormat { Type = "json_object" } : null
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("v1/chat/completions", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(JsonOptions, cancellationToken);
            var contenido = result?.Choices?.FirstOrDefault()?.Message?.Content;

            if (string.IsNullOrWhiteSpace(contenido))
            {
                throw new InvalidOperationException("El modelo devolvió una respuesta vacía.");
            }

            return contenido;
        }
        catch (HttpRequestException ex)
        {
            throw new ServicioExternoNoDisponibleException("No se pudo contactar con el LLM (Ollama).", ex);
        }
        catch (TimeoutRejectedException)
        {
            throw new ServicioExternoNoDisponibleException("El LLM (Ollama) no respondió a tiempo.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ServicioExternoNoDisponibleException("El LLM (Ollama) no respondió a tiempo.");
        }
    }

    private sealed class OllamaChatRequest
    {
        public string Model { get; set; } = string.Empty;

        public List<OllamaMensaje> Messages { get; set; } = new();

        public bool Stream { get; set; }

        [JsonPropertyName("response_format")]
        public OllamaResponseFormat? ResponseFormat { get; set; }
    }

    private sealed class OllamaMensaje
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
    }

    private sealed class OllamaResponseFormat
    {
        public string? Type { get; set; }
    }

    private sealed class OllamaChatResponse
    {
        public List<OllamaChoice>? Choices { get; set; }
    }

    private sealed class OllamaChoice
    {
        public OllamaMensaje? Message { get; set; }
    }
}
