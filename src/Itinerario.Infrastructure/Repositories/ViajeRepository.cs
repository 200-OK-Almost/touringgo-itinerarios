using Itinerario.Application;
using Itinerario.Application.Interfaces;
using Itinerario.Domain.Entities;
using Itinerario.Domain.Exceptions;
using Itinerario.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
        => _context.Entry(viaje).State = EntityState.Modified;

    public void Eliminar(Viaje viaje)
        => _context.Viajes.Remove(viaje);

    public void AgregarMiembro(MiembroViaje miembro)
        => _context.MiembrosViaje.Add(miembro);

    public void SincronizarEtiquetas(
        Viaje viaje,
        IReadOnlyCollection<short> categoriaIds)
    {
        var actuales = viaje.Etiquetas.ToList();
        var deseadas = categoriaIds.ToHashSet();

        foreach (var etiqueta in actuales.Where(e => !deseadas.Contains(e.CategoriaId)))
        {
            viaje.Etiquetas.Remove(etiqueta);
            _context.EtiquetasViaje.Remove(etiqueta);
        }

        var existentes = actuales.Select(e => e.CategoriaId).ToHashSet();

        foreach (var categoriaId in deseadas.Where(c => !existentes.Contains(c)))
        {
            var nueva = new EtiquetaViaje { ViajeId = viaje.Id, CategoriaId = categoriaId };
            viaje.Etiquetas.Add(nueva);
            _context.EtiquetasViaje.Add(nueva);
        }
    }

    public void EstablecerParametrosFamilia(
        Viaje viaje,
        ParametrosFamilia? parametros)
    {
        if (parametros is null)
        {
            if (viaje.ParametrosFamilia is not null)
            {
                _context.ParametrosFamilia.Remove(viaje.ParametrosFamilia);
                viaje.ParametrosFamilia = null;
            }

            return;
        }

        if (viaje.ParametrosFamilia is null)
        {
            parametros.ViajeId = viaje.Id;
            viaje.ParametrosFamilia = parametros;
            _context.ParametrosFamilia.Add(parametros);
            return;
        }

        var existente = viaje.ParametrosFamilia;
        existente.CantidadAcompanantes = parametros.CantidadAcompanantes;
        existente.HayNinos = parametros.HayNinos;
        existente.CantidadNinos = parametros.CantidadNinos;
        existente.MovilidadReducida = parametros.MovilidadReducida;
        existente.RequiereRitmoBajo = parametros.RequiereRitmoBajo;
        existente.RequiereMenuInfantil = parametros.RequiereMenuInfantil;
    }

    public void Descartar(Viaje viaje)
        => _context.Entry(viaje).State = EntityState.Detached;

    public async Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (EsCodigoInvitacionDuplicado(ex))
        {
            throw new CodigoInvitacionDuplicadoException("El código de invitación ya existe.");
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictoDeConcurrenciaException("El viaje fue modificado por otro usuario.");
        }
    }

    private static bool EsCodigoInvitacionDuplicado(DbUpdateException ex)
        => ex.InnerException is PostgresException postgres
            && postgres.SqlState == PostgresErrorCodes.UniqueViolation
            && postgres.ConstraintName == "IX_viajes_codigo_invitacion";
}
