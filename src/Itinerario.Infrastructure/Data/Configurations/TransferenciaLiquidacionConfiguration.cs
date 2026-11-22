
using Itinerario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class TransferenciaLiquidacionConfiguration : IEntityTypeConfiguration<TransferenciaLiquidacion>
{
    public void Configure(
        EntityTypeBuilder<TransferenciaLiquidacion> builder)
    {
        builder.ToTable("transferencias_liquidacion", t =>
        {
            t.HasCheckConstraint(
                "ck_transferencias_monto",
                """ "Monto" > 0""");

            t.HasCheckConstraint(
                "ck_transferencias_participantes",
                """ "DeParticipanteId" <> "AParticipanteId" """);
        });

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Monto)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.HasOne(t => t.Liquidacion)
            .WithMany(l => l.Transferencias)
            .HasForeignKey(t => t.LiquidacionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.DeParticipante)
            .WithMany()
            .HasForeignKey(t => new {
                t.ViajeId,
                t.DeParticipanteId 
            })
            .HasPrincipalKey(m => new
            {
                m.ViajeId,
                m.UsuarioId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.AParticipante)
            .WithMany()
            .HasForeignKey(t => new {
                t.ViajeId,
                t.DeParticipanteId
            })
            .HasPrincipalKey(m => new
            {
                m.ViajeId,
                m.UsuarioId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.LiquidacionId)
            .HasDatabaseName("ix_transferencias_liquidacion");

        builder.HasIndex(t => t.DeParticipanteId)
            .HasDatabaseName("ix_transferencias_de_participante");

        builder.HasIndex(t => t.AParticipanteId)
            .HasDatabaseName("ix_transferencias_a_participante");
    }
}
