using Itinerario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itinerario.Infrastructure.Data.Configurations;

public class EtiquetaViajeDiaConfiguration : IEntityTypeConfiguration<EtiquetaViajeDia>
{
    public void Configure(EntityTypeBuilder<EtiquetaViajeDia> builder)
    {
        builder.ToTable("etiquetas_viaje_dia");

        builder.HasKey(e => new { e.ViajeDiaId, e.CategoriaCodigo });

        builder.Property(e => e.ViajeDiaId)
            .HasColumnName("viaje_dia_id");

        builder.Property(e => e.CategoriaCodigo)
            .HasColumnName("categoria_codigo");

        builder.HasOne(e => e.Dia)
            .WithMany(d => d.Etiquetas)
            .HasForeignKey(e => e.ViajeDiaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
