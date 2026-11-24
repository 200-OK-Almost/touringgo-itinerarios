
using Itinerario.Domain.Entities;
using Itinerario.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class LiquidacionConfiguration : IEntityTypeConfiguration<Liquidacion>
{
    public void Configure(EntityTypeBuilder<Liquidacion> builder)
    {
        builder.ToTable("liquidaciones", t =>
        {
            t.HasCheckConstraint(
                "ck_liquidaciones_moneda",
                """length(trim("Moneda"::text)) = 3""");
        });

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Moneda)
            .HasConversion<string>()
            .HasColumnType("char(3)")
            .IsRequired();

        builder.Property(l => l.FechaLiquidacion)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasOne(l => l.Viaje)
            .WithMany(v => v.Liquidaciones)
            .HasForeignKey(l => l.ViajeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new
        {
            l.ViajeId,
            l.FechaLiquidacion
        })
            .HasDatabaseName("ix_liquidaciones_viaje_fecha");

        builder.HasIndex(l => l.RealizadaPorUsuarioId)
            .HasDatabaseName("ix_liquidaciones_realizada_por");
    }
}
