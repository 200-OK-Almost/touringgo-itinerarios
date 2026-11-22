using Itinerario.Application.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Itinerario.API.RateLimiting;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class AsistenteRateLimitAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _policy;

    public AsistenteRateLimitAttribute(string policy)
    {
        _policy = policy;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var options = httpContext.RequestServices
            .GetRequiredService<IOptions<AsistenteOptions>>()
            .Value;
        var store = httpContext.RequestServices
            .GetRequiredService<IAsistenteRateLimitStore>();

        var clave = ConstruirClave(_policy, context);
        var limite = _policy == AsistenteRateLimitPolicies.Usuario
            ? options.RateLimit.PermitLimitPorUsuario
            : options.RateLimit.PermitLimitPorViaje;
        var ventana = TimeSpan.FromSeconds(options.RateLimit.WindowSeconds);

        var decision = await store.AdquirirAsync(
            clave,
            limite,
            ventana,
            httpContext.RequestAborted);

        if (!decision.Permitido)
        {
            await AsistenteRateLimitResponse.EscribirAsync(
                httpContext,
                decision.RetryAfter,
                httpContext.RequestAborted);

            context.Result = new EmptyResult();
            return;
        }

        await next();
    }

    private static string ConstruirClave(
        string policy,
        ActionExecutingContext context)
    {
        var usuarioId = context.HttpContext.User.FindFirst("sub")?.Value;

        return policy switch
        {
            AsistenteRateLimitPolicies.Usuario =>
                $"usuario:{AsistenteRateLimitKeys.Usuario(usuarioId)}",
            AsistenteRateLimitPolicies.Viaje =>
                $"viaje:{AsistenteRateLimitKeys.Viaje(usuarioId ?? "anonimo", ObtenerViajeId(context))}",
            _ => throw new InvalidOperationException(
                $"No existe una política de rate limiting llamada '{policy}'.")
        };
    }

    private static string? ObtenerViajeId(ActionExecutingContext context)
    {
        if (context.RouteData.Values.TryGetValue("id", out var routeValue))
        {
            return routeValue?.ToString();
        }

        return context.HttpContext.Request.RouteValues.TryGetValue("id", out var value)
            ? value?.ToString()
            : null;
    }
}
