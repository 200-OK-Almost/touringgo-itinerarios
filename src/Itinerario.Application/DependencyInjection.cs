using FluentValidation;
using Itinerario.Application.Interfaces;
using Itinerario.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Itinerario.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IGeoCalculator, GeoCalculator>();
        services.AddScoped<IItinerarioGenerator, ItinerarioGenerator>();
        services.AddScoped<IItinerarioRefiner, ItinerarioRefiner>();
        services.AddScoped<IViajeService, ViajeService>();
        services.AddScoped<IItinerarioService, ItinerarioService>();

        return services;
    }
}
