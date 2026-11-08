using System.Security.Claims;
using Itinerario.Application.Interfaces;

namespace Itinerario.API.Services;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWebHostEnvironment _environment;

    public CurrentUser(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment)
    {
        _httpContextAccessor = httpContextAccessor;
        _environment = environment;
    }

    public Guid UsuarioId
    {
        get
        {
            var contexto = _httpContextAccessor.HttpContext;

            var claimSub = contexto?.User.FindFirst("sub")?.Value;
            if (Guid.TryParse(claimSub, out var usuarioId))
            {
                return usuarioId;
            }

            // Fallback de desarrollo/testing hasta que el gateway/servicio Usuarios envíe el JWT.
            if (_environment.IsDevelopment() || _environment.IsEnvironment("Testing"))
            {
                var header = contexto?.Request.Headers["X-Usuario-Id"].ToString();
                if (Guid.TryParse(header, out usuarioId))
                {
                    return usuarioId;
                }
            }

            throw new UnauthorizedAccessException("No se pudo identificar el usuario (claim sub).");
        }
    }
}
