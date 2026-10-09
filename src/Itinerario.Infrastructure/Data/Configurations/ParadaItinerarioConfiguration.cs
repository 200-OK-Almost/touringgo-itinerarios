using Itinerario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itinerario.Infrastructure.Data.Configurations;

public class ParadaItinerarioConfiguration : IEntityTypeConfiguration<ParadaItinerario>
{
    public void Configure(EntityTypeBuilder<ParadaItinerario> builder)
    {
        builder.ToTable("paradas_itinerario");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id");

        builder.Property(p => p.ItinerarioId)
            .HasColumnName("itinerario_id");

        builder.Property(p => p.LugarId)
            .HasColumnName("lugar_id");

        builder.Property(p => p.Turno)
            .HasColumnName("turno")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(p => p.OrdenParada)
            .HasColumnName("orden_parada")
            .IsRequired();

        builder.Property(p => p.HoraInicio)
            .HasColumnName("hora_inicio");

        builder.Property(p => p.DuracionMinutos)
            .HasColumnName("duracion_minutos");

        builder.Property(p => p.DistanciaMetrosDesdeAnterior)
            .HasColumnName("distancia_metros_desde_anterior");

        builder.Property(p => p.CuadrasDesdeAnterior)
            .HasColumnName("cuadras_desde_anterior");

        builder.Property(p => p.MinutosAPieDesdeAnterior)
            .HasColumnName("minutos_a_pie_desde_anterior");

        builder.Property(p => p.OrigenParada)
            .HasColumnName("origen_parada")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(p => p.SugerenciaIa)
            .HasColumnName("sugerencia_ia")
            .HasMaxLength(255);

        builder.Property(p => p.NotasPersonales)
            .HasColumnName("notas_personales")
            .HasMaxLength(500);

        builder.Property(p => p.FechaCreacion)
            .HasColumnName("fecha_creacion")
            .IsRequired();

        builder.HasIndex(p => new { p.ItinerarioId, p.Turno, p.OrdenParada });

        builder.HasOne(p => p.Itinerario)
            .WithMany(i => i.Paradas)
            .HasForeignKey(p => p.ItinerarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
