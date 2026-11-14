using System.Net.Http.Headers;
using System.Net.Http.Json;
using Itinerario.Application.DTOs;
using Itinerario.Application.Interfaces;
using Itinerario.Domain.Enums;
using Itinerario.Domain.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Itinerario.Infrastructure.Clients;

public class UsuariosClient : IUsuariosClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UsuariosClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PerfilViajeroDto> ObtenerPerfilAsync(CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/me/perfil");

        var token = ObtenerToken();
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var raw = await response.Content.ReadFromJsonAsync<PerfilUsuarioRawDto>(cancellationToken: cancellationToken);
            if (raw?.Preferencias is null || raw.Restricciones is null)
            {
                throw new InvalidOperationException("El perfil del usuario no está disponible.");
            }

            return Mapear(raw);
        }
        catch (HttpRequestException ex)
        {
            throw new ServicioExternoNoDisponibleException("No se pudo obtener el perfil desde Usuarios.", ex);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ServicioExternoNoDisponibleException("Usuarios no respondió a tiempo.");
        }
    }

    private string? ObtenerToken()
    {
        var contexto = _httpContextAccessor.HttpContext;
        if (contexto is null)
        {
            return null;
        }

        var auth = contexto.Request.Headers["Authorization"].ToString();
        if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return auth["Bearer ".Length..].Trim();
        }

        if (contexto.Request.Cookies.TryGetValue("access_token", out var cookie) && !string.IsNullOrWhiteSpace(cookie))
        {
            return cookie;
        }

        return null;
    }

    private static PerfilViajeroDto Mapear(PerfilUsuarioRawDto raw)
    {
        var preferencias = raw.Preferencias!;
        var restricciones = raw.Restricciones!;

        return new PerfilViajeroDto
        {
            RitmoCaminata = MapearRitmo(preferencias.RitmoVisitaPreferido),
            Categorias = (preferencias.Categorias ?? new List<CategoriaRawDto>())
                .Where(c => !string.IsNullOrWhiteSpace(c.CategoriaCodigo))
                .Select(c => new CategoriaAfinidadDto { Codigo = c.CategoriaCodigo!, Afinidad = c.Afinidad })
                .ToList(),
            Dietas = (restricciones.Dieta ?? new List<string>())
                .Select(d => d.ToLowerInvariant())
                .ToList(),
            RequiereRecorridoSinEscalones = restricciones.RequiereRecorridoSinEscalones,
            RequiereBanoAccesible = restricciones.RequiereBanoAccesible
        };
    }

    private static RitmoCaminata MapearRitmo(string? ritmo)
        => ritmo?.Trim().Equals("Activo", StringComparison.OrdinalIgnoreCase) == true
            ? RitmoCaminata.Activo
            : RitmoCaminata.Tranquilo;

    private sealed class PerfilUsuarioRawDto
    {
        public PreferenciasRawDto? Preferencias { get; set; }
        public RestriccionesRawDto? Restricciones { get; set; }
    }

    private sealed class PreferenciasRawDto
    {
        public string? RitmoVisitaPreferido { get; set; }
        public List<CategoriaRawDto>? Categorias { get; set; }
    }

    private sealed class CategoriaRawDto
    {
        public string? CategoriaCodigo { get; set; }
        public int Afinidad { get; set; }
    }

    private sealed class RestriccionesRawDto
    {
        public List<string>? Dieta { get; set; }
        public bool RequiereRecorridoSinEscalones { get; set; }
        public bool RequiereBanoAccesible { get; set; }
    }
}
