using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ItinerarioEntity = Itinerario.Domain.Entities.Itinerario;

namespace Itinerario.Infrastructure.Data.Configurations;

public class ItinerarioConfiguration : IEntityTypeConfiguration<ItinerarioEntity>
{
    public void Configure(EntityTypeBuilder<ItinerarioEntity> builder)
    {
        builder.ToTable("itinerarios");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id");

        builder.Property(i => i.ViajeId)
            .HasColumnName("viaje_id");

        builder.Property(i => i.NumeroDia)
            .HasColumnName("numero_dia")
            .IsRequired();

        builder.Property(i => i.Fecha)
            .HasColumnName("fecha");

        builder.Property(i => i.OrigenTipo)
            .HasColumnName("origen_tipo")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(i => i.OrigenDescripcion)
            .HasColumnName("origen_descripcion")
            .HasMaxLength(150);

        builder.Property(i => i.OrigenLatitud)
            .HasColumnName("origen_latitud")
            .HasPrecision(10, 8);

        builder.Property(i => i.OrigenLongitud)
            .HasColumnName("origen_longitud")
            .HasPrecision(11, 8);

        builder.Property(i => i.FechaCreacion)
            .HasColumnName("fecha_creacion")
            .IsRequired();

        builder.Property(i => i.FechaActualizacion)
            .HasColumnName("fecha_actualizacion")
            .IsRequired();

        builder.HasIndex(i => new { i.ViajeId, i.NumeroDia })
            .IsUnique();

        builder.HasOne(i => i.Viaje)
            .WithMany(v => v.Itinerarios)
            .HasForeignKey(i => i.ViajeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
