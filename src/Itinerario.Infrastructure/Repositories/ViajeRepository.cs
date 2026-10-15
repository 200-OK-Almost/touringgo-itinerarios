using Itinerario.Application;
using Itinerario.Application.Interfaces;
using Itinerario.Domain.Entities;
using Itinerario.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Itinerario.Infrastructure.Repositories;

public class ViajeRepository : IViajeRepository
{
    private readonly ApplicationDbContext _context;

    public ViajeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Viaje?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Viajes
            .Include(v => v.Miembros)
            .Include(v => v.Etiquetas)
            .Include(v => v.ParametrosFamilia)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<ResultadoPaginado<Viaje>> ObtenerPaginadoAsync(
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Viajes
            .Include(v => v.Etiquetas)
            .Include(v => v.ParametrosFamilia)
            .AsNoTracking();

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(v => v.FechaCreacion)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(cancellationToken);

        return new ResultadoPaginado<Viaje>(items, total);
    }

    public Task<Viaje?> ObtenerPorCodigoInvitacionAsync(
        string codigoInvitacion,
        CancellationToken cancellationToken = default)
        => _context.Viajes
            .Include(v => v.Miembros)
            .FirstOrDefaultAsync(v => v.CodigoInvitacion == codigoInvitacion, cancellationToken);

    public Task<bool> ExisteCodigoInvitacionAsync(
        string codigoInvitacion,
        CancellationToken cancellationToken = default)
        => _context.Viajes.AnyAsync(v => v.CodigoInvitacion == codigoInvitacion, cancellationToken);

    public async Task AgregarAsync(Viaje viaje, CancellationToken cancellationToken = default)
        => await _context.Viajes.AddAsync(viaje, cancellationToken);

    public void Actualizar(Viaje viaje)
        => _context.Viajes.Update(viaje);

    public void Eliminar(Viaje viaje)
        => _context.Viajes.Remove(viaje);

    public Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
