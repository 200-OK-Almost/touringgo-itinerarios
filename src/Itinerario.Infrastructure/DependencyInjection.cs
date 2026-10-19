using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Itinerario.Application.Interfaces;
using Itinerario.Infrastructure.Clients;
using Itinerario.Infrastructure.Data;
using Itinerario.Infrastructure.Repositories;

namespace Itinerario.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IViajeRepository, ViajeRepository>();
        services.AddScoped<IItinerarioRepository, ItinerarioRepository>();
        services.AddScoped<ILugaresClient, LugaresClient>();

        return services;
    }
}