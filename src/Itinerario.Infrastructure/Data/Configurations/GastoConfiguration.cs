
using Itinerario.Domain.Entities;
using Itinerario.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class GastoConfiguration : IEntityTypeConfiguration<Gasto>
{
    public void Configure(EntityTypeBuilder<Gasto> builder)
    {
        builder.ToTable("gastos", t =>
        {
            t.HasCheckConstraint(
                "ck_gastos_monto_total",
                """ "Monto" > 0 """);

        });

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Concepto)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(g => g.Monto)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(g => g.Moneda)
            .HasConversion<string>()
            .HasColumnType("char(3)")
            .IsRequired();

        builder.Property(g => g.FechaGasto)
            .IsRequired();

        builder.Property(g => g.FechaCreacion)
            .HasDefaultValueSql("now()");

        builder.Property(g => g.FechaActualizacion)
            .HasDefaultValueSql("now()");

        builder.Property(g => g.Version)
            .IsConcurrencyToken()
            .HasDefaultValue(1);

        // Relaciones
        builder.HasOne(g => g.Viaje)
            .WithMany(v => v.Gastos)
            .HasForeignKey(g => g.ViajeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.PagadoPorMiembro)
            .WithMany()
            .HasForeignKey(g => new
            {
                g.ViajeId,
                g.PagadoPorMiembroId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.Liquidacion)
            .WithMany(l => l.Gastos)
            .HasForeignKey(g => new
            {
                g.LiquidacionId,
                g.ViajeId
            })
            .HasPrincipalKey(l => new
            {
                l.Id,
                l.ViajeId
            })
            .OnDelete(DeleteBehavior.Restrict);

        // Índices
        builder.HasIndex(g => new { g.ViajeId, g.FechaGasto })
            .HasDatabaseName("ix_gastos_viaje_fecha");

        builder.HasIndex(g => new { g.ViajeId, g.LiquidacionId })
            .HasDatabaseName("ix_gastos_viaje_liquidacion");

        builder.HasIndex(g => g.PagadoPorMiembroId)
            .HasDatabaseName("ix_gastos_pagado_por");

        builder.HasIndex(g => g.RegistradoPorMiembroId)
            .HasDatabaseName("ix_gastos_registrado_por");
    }
}
