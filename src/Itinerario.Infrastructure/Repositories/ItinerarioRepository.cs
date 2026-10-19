using Itinerario.Application.Interfaces;
using Itinerario.Domain.Exceptions;
using Itinerario.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Itinerario.Domain.Entities;
using ItinerarioEntity = Itinerario.Domain.Entities.Itinerario;

namespace Itinerario.Infrastructure.Repositories;

public class ItinerarioRepository : IItinerarioRepository
{
    private readonly ApplicationDbContext _context;

    public ItinerarioRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<ItinerarioEntity>> ObtenerPorViajeAsync(
        Guid viajeId,
        CancellationToken cancellationToken = default)
        => await _context.Itinerarios
            .Include(i => i.Paradas)
            .Where(i => i.ViajeId == viajeId)
            .OrderBy(i => i.NumeroDia)
            .ToListAsync(cancellationToken);

    public async Task ReemplazarAsync(
        Guid viajeId,
        IReadOnlyCollection<ItinerarioEntity> itinerarios,
        CancellationToken cancellationToken = default)
    {
        await using var transaccion = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var existentes = await _context.Itinerarios
                .Where(i => i.ViajeId == viajeId)
                .ToListAsync(cancellationToken);

            _context.Itinerarios.RemoveRange(existentes);
            await _context.SaveChangesAsync(cancellationToken);

            await _context.Itinerarios.AddRangeAsync(itinerarios, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            await transaccion.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaccion.RollbackAsync(cancellationToken);
            throw new ConflictoDeConcurrenciaException("El viaje fue modificado por otro usuario.");
        }
    }

    public Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public void AgregarParada(ParadaItinerario parada)
        => throw new NotImplementedException();
}
