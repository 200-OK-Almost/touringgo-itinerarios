using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ItinerarioEntity = Itinerario.Domain.Entities.Itinerario;

namespace Itinerario.Infrastructure.Data;

public static class ItinerarioVersionLoader
{
    public static void Cargar(
        ApplicationDbContext context,
        IEnumerable<ItinerarioEntity> itinerarios)
    {
        foreach (var itinerario in itinerarios)
        {
            var entry = context.Entry(itinerario);

            if (entry.State == EntityState.Detached)
            {
                continue;
            }

            var version = entry.Property<uint>("xmin").CurrentValue;
            itinerario.Version = version.ToString(CultureInfo.InvariantCulture);
        }
    }
}
