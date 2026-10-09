using Itinerario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itinerario.Infrastructure.Data.Configurations;

public class ParametrosFamiliaConfiguration : IEntityTypeConfiguration<ParametrosFamilia>
{
    public void Configure(EntityTypeBuilder<ParametrosFamilia> builder)
    {
        builder.ToTable("parametros_familia");

        builder.HasKey(p => p.ViajeId);

        builder.Property(p => p.ViajeId)
            .HasColumnName("viaje_id");

        builder.Property(p => p.CantidadAcompanantes)
            .HasColumnName("cantidad_acompanantes")
            .IsRequired();

        builder.Property(p => p.HayNinos)
            .HasColumnName("hay_ninos")
            .IsRequired();

        builder.Property(p => p.CantidadNinos)
            .HasColumnName("cantidad_ninos")
            .IsRequired();

        builder.Property(p => p.MovilidadReducida)
            .HasColumnName("movilidad_reducida")
            .IsRequired();

        builder.Property(p => p.RequiereRitmoBajo)
            .HasColumnName("requiere_ritmo_bajo")
            .IsRequired();

        builder.Property(p => p.RequiereMenuInfantil)
            .HasColumnName("requiere_menu_infantil")
            .IsRequired();

        builder.HasOne(p => p.Viaje)
            .WithOne(v => v.ParametrosFamilia)
            .HasForeignKey<ParametrosFamilia>(p => p.ViajeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
