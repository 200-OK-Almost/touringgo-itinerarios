using Itinerario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ItinerarioEntity = Itinerario.Domain.Entities.Itinerario;

namespace Itinerario.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {

    }

    public DbSet<Viaje> Viajes => Set<Viaje>();
    public DbSet<ParametrosFamilia> ParametrosFamilia => Set<ParametrosFamilia>();
    public DbSet<MiembroViaje> MiembrosViaje => Set<MiembroViaje>();
    public DbSet<EtiquetaViaje> EtiquetasViaje => Set<EtiquetaViaje>();
    public DbSet<ItinerarioEntity> Itinerarios => Set<ItinerarioEntity>();
    public DbSet<ParadaItinerario> ParadasItinerario => Set<ParadaItinerario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
