using Itinerario.Application.DTOs;
using Itinerario.Application.Interfaces;
using Itinerario.Domain.Enums;

namespace Itinerario.Infrastructure.Clients;

public class LugaresClient : ILugaresClient
{
    private static readonly IReadOnlyList<LugarStub> Lugares = new List<LugarStub>
    {
        new(Guid.Parse("11111111-1111-1111-1111-111111111101"), "Plaza Serrano", BarrioCaba.Palermo, -34.5883m, -58.4306m, Turno.Manana, 1, NivelPresupuesto.Economico, true, 45),
        new(Guid.Parse("11111111-1111-1111-1111-111111111102"), "MALBA", BarrioCaba.Palermo, -34.5772m, -58.4028m, Turno.Tarde, 2, NivelPresupuesto.Equilibrado, false, 90),
        new(Guid.Parse("11111111-1111-1111-1111-111111111103"), "Café de Recoleta", BarrioCaba.Recoleta, -34.5889m, -58.3934m, Turno.Manana, 3, NivelPresupuesto.Economico, false, 60),
        new(Guid.Parse("11111111-1111-1111-1111-111111111104"), "Feria de San Telmo", BarrioCaba.SanTelmo, -34.6215m, -58.3733m, Turno.Tarde, 1, NivelPresupuesto.Economico, true, 60),
        new(Guid.Parse("11111111-1111-1111-1111-111111111105"), "Cena en Puerto Madero", BarrioCaba.PuertoMadero, -34.6118m, -58.3630m, Turno.Noche, 4, NivelPresupuesto.SinRestricciones, true, 120)
    };

    public Task<LugarCandidatoDto?> ObtenerPorIdAsync(Guid lugarId, CancellationToken cancellationToken = default)
    {
        var lugar = Lugares.FirstOrDefault(l => l.Id == lugarId);

        return Task.FromResult(lugar is null ? null : Mapear(lugar));
    }

    public Task<IReadOnlyCollection<LugarCandidatoDto>> BuscarAsync(
        BarrioCaba barrio,
        IReadOnlyCollection<short> categoriaIds,
        CancellationToken cancellationToken = default)
    {
        var resultado = Lugares
            .Where(l => l.Barrio == barrio)
            .Where(l => categoriaIds.Count == 0 || categoriaIds.Contains(l.CategoriaId))
            .Select(Mapear)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<LugarCandidatoDto>>(resultado);
    }

    private static LugarCandidatoDto Mapear(LugarStub lugar) => new()
    {
        Id = lugar.Id,
        Nombre = lugar.Nombre,
        Latitud = lugar.Latitud,
        Longitud = lugar.Longitud,
        Turno = lugar.Turno,
        CategoriaId = lugar.CategoriaId,
        NivelPrecio = lugar.NivelPrecio,
        EsAireLibre = lugar.EsAireLibre,
        DuracionSugeridaMinutos = lugar.DuracionSugeridaMinutos
    };

    private sealed record LugarStub(
        Guid Id,
        string Nombre,
        BarrioCaba Barrio,
        decimal Latitud,
        decimal Longitud,
        Turno Turno,
        short CategoriaId,
        NivelPresupuesto NivelPrecio,
        bool EsAireLibre,
        short DuracionSugeridaMinutos);
}
