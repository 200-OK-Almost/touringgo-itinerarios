using System.Security.Claims;
using Itinerario.Application.Interfaces;

namespace Itinerario.API.Services;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
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

            // Fallback de desarrollo hasta que el gateway/servicio Usuarios envie el JWT
            var header = contexto?.Request.Headers["X-Usuario-Id"].ToString();
            if (Guid.TryParse(header, out usuarioId))
            {
                return usuarioId;
            }

            throw new UnauthorizedAccessException("No se pudo identificar el usuario (claim sub o header X-Usuario-Id).");
        }
    }
}
