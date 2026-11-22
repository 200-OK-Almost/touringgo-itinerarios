using System.Security.Claims;
using System.Text;
using Itinerario.API.RateLimiting;
using Itinerario.Application.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Itinerario.UnitTests.RateLimiting;

public class AsistenteRateLimitAttributeTests
{
    [Fact]
    public async Task OnActionExecutionAsync_Permitido_LlamaAlSiguiente()
    {
        var store = new StoreFalso();
        var context = CrearContexto(store, "usuario-1");
        var siguienteLlamado = false;

        await new AsistenteRateLimitAttribute(AsistenteRateLimitPolicies.Usuario)
            .OnActionExecutionAsync(
                context,
                () =>
                {
                    siguienteLlamado = true;
                    return Task.FromResult<ActionExecutedContext>(null!);
                });

        Assert.True(siguienteLlamado);
        Assert.Null(context.Result);
        Assert.Equal("usuario:usuario-1", store.UltimaClave);
    }

    [Fact]
    public async Task OnActionExecutionAsync_Rechazado_Devuelve429YNoLlamaAlSiguiente()
    {
        var store = new StoreFalso
        {
            Decision = new AsistenteRateLimitDecision(false, TimeSpan.FromSeconds(30))
        };
        var context = CrearContexto(store, "usuario-1");
        var siguienteLlamado = false;

        await new AsistenteRateLimitAttribute(AsistenteRateLimitPolicies.Usuario)
            .OnActionExecutionAsync(
                context,
                () =>
                {
                    siguienteLlamado = true;
                    return Task.FromResult<ActionExecutedContext>(null!);
                });

        Assert.False(siguienteLlamado);
        Assert.IsType<EmptyResult>(context.Result);
        Assert.Equal(StatusCodes.Status429TooManyRequests, context.HttpContext.Response.StatusCode);
        Assert.Equal("30", context.HttpContext.Response.Headers["Retry-After"].ToString());

        context.HttpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(context.HttpContext.Response.Body, Encoding.UTF8);
        Assert.Contains("Demasiadas consultas", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task OnActionExecutionAsync_Viaje_UsaUsuarioYViaje()
    {
        var store = new StoreFalso();
        var context = CrearContexto(store, "usuario-1", "viaje-1");

        await new AsistenteRateLimitAttribute(AsistenteRateLimitPolicies.Viaje)
            .OnActionExecutionAsync(
                context,
                () => Task.FromResult<ActionExecutedContext>(null!));

        Assert.Equal("viaje:usuario-1:viaje-1", store.UltimaClave);
    }

    private static ActionExecutingContext CrearContexto(
        StoreFalso store,
        string usuarioId,
        string? viajeId = null)
    {
        var services = new ServiceCollection()
            .AddSingleton<IOptions<AsistenteOptions>>(Options.Create(new AsistenteOptions
            {
                RateLimit = new AsistenteRateLimitOptions
                {
                    PermitLimitPorUsuario = 2,
                    PermitLimitPorViaje = 3,
                    WindowSeconds = 60
                }
            }))
            .AddSingleton<IAsistenteRateLimitStore>(store)
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services
        };
        httpContext.Response.Body = new MemoryStream();
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[] { new Claim("sub", usuarioId) },
                "test"));

        var routeData = new RouteData();
        if (viajeId is not null)
        {
            routeData.Values["id"] = viajeId;
        }

        return new ActionExecutingContext(
            new ActionContext(httpContext, routeData, new ActionDescriptor()),
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
    }

    private sealed class StoreFalso : IAsistenteRateLimitStore
    {
        public AsistenteRateLimitDecision Decision { get; set; } = new(true, TimeSpan.Zero);

        public string? UltimaClave { get; private set; }

        public Task<AsistenteRateLimitDecision> AdquirirAsync(
            string clave,
            int limite,
            TimeSpan ventana,
            CancellationToken cancellationToken = default)
        {
            UltimaClave = clave;
            return Task.FromResult(Decision);
        }
    }
}
