
using Itinerario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class DivisionGastoConfiguration
    : IEntityTypeConfiguration<DivisionGasto>
{
    public void Configure(EntityTypeBuilder<DivisionGasto> builder)
    {
        builder.ToTable("division_gastos", t =>
        {
            t.HasCheckConstraint(
                "ck_division_gastos_monto_asignado",
                """ "MontoAsignado" > 0""");
        });

        builder.HasKey(d => d.Id);
        
        // para solucionar error de ActualizarAsync
        builder.Property(d => d.Id)
            .ValueGeneratedNever();

        builder.Property(d => d.MontoAsignado)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.HasOne(d => d.Gasto)
            .WithMany(g => g.Divisiones)
            .HasForeignKey(d => new
            {
                d.GastoId,
                d.ViajeId
            })
            .HasPrincipalKey(g => new
            {
                g.Id,
                g.ViajeId
            })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Participante)
            .WithMany()
            .HasForeignKey(d => new
            {
                d.ViajeId,
                d.ParticipanteId
            })
            .HasPrincipalKey(m => new
            {
                m.ViajeId,
                m.UsuarioId
            })
            .OnDelete(DeleteBehavior.Restrict);
        // Un participante solo puede aparecer una vez
        // dentro de la división de un mismo gasto.
        builder.HasIndex(d => new
        {
            d.GastoId,
            d.ParticipanteId
        })
            .IsUnique()
            .HasDatabaseName("uq_division_gasto_participante");

        builder.HasIndex(d => d.ParticipanteId)
            .HasDatabaseName("ix_division_gastos_participante");
    }
}
