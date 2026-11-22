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
    public DbSet<EtiquetaViajeDia> EtiquetasViajeDia => Set<EtiquetaViajeDia>();
    public DbSet<ItinerarioEntity> Itinerarios => Set<ItinerarioEntity>();
    public DbSet<ParadaItinerario> ParadasItinerario => Set<ParadaItinerario>();
    public DbSet<ViajeDia> ViajeDias => Set<ViajeDia>();
    public DbSet<Gasto> Gastos => Set<Gasto>();
    public DbSet<DivisionGasto> DivisionesGasto => Set<DivisionGasto>();
    public DbSet<Liquidacion> Liquidaciones => Set<Liquidacion>();
    public DbSet<TransferenciaLiquidacion> TransferenciasLiquidacion => Set<TransferenciaLiquidacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<Viaje>()
            .Property<uint>("xmin")
            .IsRowVersion();

        modelBuilder.Entity<ItinerarioEntity>()
            .Property<uint>("xmin")
            .IsRowVersion();

        modelBuilder.Entity<Gasto>()
            .Property<uint>(g => g.Version)
            .IsRowVersion();

        base.OnModelCreating(modelBuilder);
    }
}
