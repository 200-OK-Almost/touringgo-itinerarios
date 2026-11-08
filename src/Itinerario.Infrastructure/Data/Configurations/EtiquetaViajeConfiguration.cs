using Itinerario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itinerario.Infrastructure.Data.Configurations;

public class EtiquetaViajeConfiguration : IEntityTypeConfiguration<EtiquetaViaje>
{
    public void Configure(EntityTypeBuilder<EtiquetaViaje> builder)
    {
        builder.ToTable("etiquetas_viaje");

        builder.HasKey(e => new { e.ViajeId, e.CategoriaCodigo });

        builder.Property(e => e.ViajeId)
            .HasColumnName("viaje_id");

        builder.Property(e => e.CategoriaCodigo)
            .HasColumnName("categoria_codigo");

        builder.HasOne(e => e.Viaje)
            .WithMany(v => v.Etiquetas)
            .HasForeignKey(e => e.ViajeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
