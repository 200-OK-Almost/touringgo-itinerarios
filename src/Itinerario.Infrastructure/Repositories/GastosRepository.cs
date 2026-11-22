using Itinerario.Application;
using Itinerario.Application.DTOs.Gastos;
using Itinerario.Domain.Entities;
using Itinerario.Domain.Enums;
using Itinerario.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public sealed class GastosRepository : IGastosRepository
{
    private readonly ApplicationDbContext _context;

    public GastosRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Gasto?> ObtenerPorIdAsync(
        Guid viajeId,
        Guid gastoId,
        CancellationToken ct = default)
    {
        return _context.Gastos
            .AsTracking()
            .Include(g => g.Divisiones)
            .FirstOrDefaultAsync(
                g => g.Id == gastoId && g.ViajeId == viajeId,
                ct);
    }

    public async Task<ResultadoPaginado<Gasto>> ListarAsync(
        Guid viajeId,
        FiltroGastosDto filtro,
        CancellationToken ct = default)
    {
        IQueryable<Gasto> query = _context.Gastos
            .AsNoTracking()
            .Where(g => g.ViajeId == viajeId);

        if (filtro.Estado == EstadoGastoFiltro.Pendientes)
        {
            query = query.Where(g => g.LiquidacionId == null);
        }
        else if (filtro.Estado == EstadoGastoFiltro.Liquidados)
        {
            query = query.Where(g => g.LiquidacionId != null);
        }

        if (filtro.UsuarioId.HasValue)
        {
            var usuarioId = filtro.UsuarioId.Value;

            query = query.Where(g =>
                g.PagadoPorMiembroId == usuarioId ||
                g.RegistradoPorMiembroId == usuarioId ||
                g.Divisiones.Any(d => d.ParticipanteId == usuarioId));
        }

        if (filtro.Moneda != null)
        {
            query = query.Where(g => g.Moneda == filtro.Moneda);
        }

        if (filtro.FechaDesde.HasValue)
        {
            query = query.Where(
                g => g.FechaGasto >= filtro.FechaDesde.Value);
        }

        if (filtro.FechaHasta.HasValue)
        {
            query = query.Where(
                g => g.FechaGasto <= filtro.FechaHasta.Value);
        }

        var totalItems = await query.CountAsync(ct);

        var items = await query
            .Include(g => g.Divisiones)
            .OrderByDescending(g => g.FechaGasto)
            .ThenByDescending(g => g.Id)
            .Skip((filtro.Pagina - 1) * filtro.Tamano)
            .Take(filtro.Tamano)
            .ToListAsync(ct);

        return new ResultadoPaginado<Gasto>(items, totalItems);
    }

    public async Task<HashSet<Guid>> ObtenerMiembrosExistentesAsync(
        Guid viajeId,
        IEnumerable<Guid> usuarioIds,
        CancellationToken ct = default)
    {
        var ids = usuarioIds.Distinct().ToArray();

        return await ToHashSetAsync(_context.MiembrosViaje
            .AsNoTracking()
            .Where(m =>
                m.ViajeId == viajeId &&
                ids.Contains(m.UsuarioId))
            .Select(m => m.UsuarioId));
            
    }

    public async Task<Dictionary<Guid, decimal>>
        ObtenerTotalPagadoPendienteAsync(
            Guid viajeId,
            TipoMoneda moneda,
            CancellationToken ct = default)
    {
        return await _context.Gastos
            .AsNoTracking()
            .Where(g =>
                g.ViajeId == viajeId &&
                g.LiquidacionId == null &&
                g.Moneda == moneda)
            .GroupBy(g => g.PagadoPorMiembroId)
            .Select(grupo => new
            {
                UsuarioId = grupo.Key,
                Total = grupo.Sum(g => g.Monto)
            })
            .ToDictionaryAsync(
                x => x.UsuarioId,
                x => x.Total,
                ct);
    }

    public async Task<Dictionary<Guid, decimal>>
        ObtenerTotalAsignadoPendienteAsync(
            Guid viajeId,
            TipoMoneda moneda,
            CancellationToken ct = default)
    {
        return await _context.DivisionesGasto
            .AsNoTracking()
            .Where(d =>
                d.ViajeId == viajeId &&
                d.Gasto.LiquidacionId == null &&
                d.Gasto.Moneda == moneda)
            .GroupBy(d => d.ParticipanteId)
            .Select(grupo => new
            {
                UsuarioId = grupo.Key,
                Total = grupo.Sum(d => d.MontoAsignado)
            })
            .ToDictionaryAsync(
                x => x.UsuarioId,
                x => x.Total,
                ct);
    }

    public async Task<List<Gasto>> ObtenerPendientesParaLiquidarAsync(
        Guid viajeId,
        IEnumerable<Guid> gastoIds,
        CancellationToken ct = default)
    {
        var ids = gastoIds.Distinct().ToArray();

        return await _context.Gastos
            .AsTracking()
            .Include(g => g.Divisiones)
            .Where(g =>
                g.ViajeId == viajeId &&
                ids.Contains(g.Id) &&
                g.LiquidacionId == null)
            .ToListAsync(ct);
    }

    public void Agregar(Gasto gasto)
    {
        _context.Gastos.Add(gasto);
    }

    public void AgregarLiquidacion(Liquidacion liquidacion)
    {
        _context.Liquidaciones.Add(liquidacion);
    }

    public void Eliminar(Gasto gasto)
    {
        _context.Gastos.Remove(gasto);
    }

    public async Task GuardarCambiosAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<HashSet<TSource>> ToHashSetAsync<TSource>(
        this IQueryable<TSource> source,
        IEqualityComparer<TSource>? comparer = null,
        CancellationToken cancellationToken = default)
    {
        var set = new HashSet<TSource>(comparer);
        await foreach (var item in source.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            set.Add(item);
        }
        return set;
    }
}