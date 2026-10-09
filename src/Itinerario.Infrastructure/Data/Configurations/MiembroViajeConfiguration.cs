using Itinerario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itinerario.Infrastructure.Data.Configurations;

public class MiembroViajeConfiguration : IEntityTypeConfiguration<MiembroViaje>
{
    public void Configure(EntityTypeBuilder<MiembroViaje> builder)
    {
        builder.ToTable("miembros_viaje");

        builder.HasKey(m => new { m.ViajeId, m.UsuarioId });

        builder.Property(m => m.ViajeId)
            .HasColumnName("viaje_id");

        builder.Property(m => m.UsuarioId)
            .HasColumnName("usuario_id");

        builder.Property(m => m.Rol)
            .HasColumnName("rol")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(m => m.FechaIncorporacion)
            .HasColumnName("fecha_incorporacion")
            .IsRequired();

        builder.HasIndex(m => m.UsuarioId);

        builder.HasOne(m => m.Viaje)
            .WithMany(v => v.Miembros)
            .HasForeignKey(m => m.ViajeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
