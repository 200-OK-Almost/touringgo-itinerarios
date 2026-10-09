using Itinerario.Domain.Entities;
using Itinerario.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Itinerario.Infrastructure.Data.Configurations;

public class ViajeConfiguration : IEntityTypeConfiguration<Viaje>
{
    public void Configure(EntityTypeBuilder<Viaje> builder)
    {
        builder.ToTable("viajes");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .HasColumnName("id");

        builder.Property(v => v.Titulo)
            .HasColumnName("titulo")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(v => v.TipoViaje)
            .HasColumnName("tipo_viaje")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(v => v.Estado)
            .HasColumnName("estado")
            .HasConversion<string>()
            .HasDefaultValue(EstadoViaje.Borrador)
            .IsRequired();

        builder.Property(v => v.Barrio)
            .HasColumnName("barrio")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(v => v.AlcanceTemporal)
            .HasColumnName("alcance_temporal")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(v => v.FechaInicio)
            .HasColumnName("fecha_inicio");

        builder.Property(v => v.FechaFin)
            .HasColumnName("fecha_fin");

        builder.Property(v => v.NivelPresupuesto)
            .HasColumnName("nivel_presupuesto")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(v => v.RitmoCaminata)
            .HasColumnName("ritmo_caminata")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(v => v.LimiteCuadrasEntreParadas)
            .HasColumnName("limite_cuadras_entre_paradas");

        builder.Property(v => v.TipoPuntoPartida)
            .HasColumnName("tipo_punto_partida")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(v => v.LugarPartidaId)
            .HasColumnName("lugar_partida_id");

        builder.Property(v => v.NombrePuntoPartida)
            .HasColumnName("nombre_punto_partida")
            .HasMaxLength(150);

        builder.Property(v => v.DireccionPartida)
            .HasColumnName("direccion_partida")
            .HasMaxLength(200);

        builder.Property(v => v.LatitudPartida)
            .HasColumnName("latitud_partida")
            .HasPrecision(10, 8);

        builder.Property(v => v.LongitudPartida)
            .HasColumnName("longitud_partida")
            .HasPrecision(11, 8);

        builder.Property(v => v.CodigoInvitacion)
            .HasColumnName("codigo_invitacion")
            .HasMaxLength(12)
            .IsRequired();

        builder.Property(v => v.CreadoPorUsuarioId)
            .HasColumnName("creado_por_usuario_id")
            .IsRequired();

        builder.Property(v => v.FechaCreacion)
            .HasColumnName("fecha_creacion")
            .IsRequired();

        builder.Property(v => v.FechaActualizacion)
            .HasColumnName("fecha_actualizacion")
            .IsRequired();

        builder.HasIndex(v => v.CodigoInvitacion)
            .IsUnique();

        builder.HasIndex(v => v.CreadoPorUsuarioId);
    }
}
