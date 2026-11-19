using Itinerario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itinerario.Infrastructure.Data.Configurations;

public class ViajeDiaConfiguration : IEntityTypeConfiguration<ViajeDia>
{
    public void Configure(EntityTypeBuilder<ViajeDia> builder)
    {
        builder.ToTable("dias_viaje");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id");

        builder.Property(d => d.ViajeId)
            .HasColumnName("viaje_id")
            .IsRequired();

        builder.Property(d => d.NumeroDia)
            .HasColumnName("numero_dia")
            .IsRequired();

        builder.Property(d => d.Fecha)
            .HasColumnName("fecha");

        builder.Property(d => d.Barrio)
            .HasColumnName("barrio")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(d => d.TipoPuntoPartida)
            .HasColumnName("tipo_punto_partida")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(d => d.LugarPartidaId)
            .HasColumnName("lugar_partida_id");

        builder.Property(d => d.NombrePuntoPartida)
            .HasColumnName("nombre_punto_partida")
            .HasMaxLength(150);

        builder.Property(d => d.DireccionPartida)
            .HasColumnName("direccion_partida")
            .HasMaxLength(200);

        builder.Property(d => d.LatitudPartida)
            .HasColumnName("latitud_partida")
            .HasPrecision(10, 8);

        builder.Property(d => d.LongitudPartida)
            .HasColumnName("longitud_partida")
            .HasPrecision(11, 8);

        builder.Property(d => d.TipoViaje)
            .HasColumnName("tipo_viaje")
            .HasConversion<string>();

        builder.HasIndex(d => new { d.ViajeId, d.NumeroDia })
            .IsUnique();

        builder.HasOne(d => d.Viaje)
            .WithMany(v => v.Dias)
            .HasForeignKey(d => d.ViajeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
