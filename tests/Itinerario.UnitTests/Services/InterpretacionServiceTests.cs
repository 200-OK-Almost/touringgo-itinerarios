using System.Text.Json;
using Itinerario.Application.Interfaces;
using Itinerario.Application.Services;
using Itinerario.Domain.Enums;
using Itinerario.Domain.Exceptions;

namespace Itinerario.UnitTests.Services;

public class InterpretacionServiceTests
{
    [Fact]
    public async Task InterpretarAsync_MapeaRespuestaValida()
    {
        const string json = """{"titulo":"Cafés y parques","tipoViaje":"Solo","barrio":"Palermo","alcanceTemporal":"TardeNoche","nivelPresupuesto":"Economico","ritmoCaminata":"Activo","categoriaCodigos":["cafe_especialidad","parques_aire_libre"]}""";
        var servicio = new InterpretacionService(new OllamaClientFalso(json));

        var resultado = await servicio.InterpretarAsync("una tarde de cafés");

        Assert.Equal("Cafés y parques", resultado.Titulo);
        Assert.Equal(TipoViaje.Solo, resultado.TipoViaje);
        Assert.Equal(BarrioCaba.Palermo, resultado.Barrio);
        Assert.Equal(AlcanceTemporal.TardeNoche, resultado.AlcanceTemporal);
        Assert.Equal(NivelPresupuesto.Economico, resultado.NivelPresupuesto);
        Assert.Equal(RitmoCaminata.Activo, resultado.RitmoCaminata);
        Assert.Equal(new[] { "cafe_especialidad", "parques_aire_libre" }, resultado.CategoriaCodigos);
    }

    [Fact]
    public async Task InterpretarAsync_MenosDeDosCategorias_LanzaReglaDeNegocio()
    {
        const string json = """{"titulo":"x","tipoViaje":"Solo","barrio":"Palermo","alcanceTemporal":"DiaCompleto","nivelPresupuesto":"Equilibrado","ritmoCaminata":"Tranquilo","categoriaCodigos":["cafe_especialidad"]}""";
        var servicio = new InterpretacionService(new OllamaClientFalso(json));

        await Assert.ThrowsAsync<ReglaDeNegocioException>(() => servicio.InterpretarAsync("pedido"));
    }

    [Fact]
    public async Task InterpretarAsync_NormalizaCategoriasDesconocidasYCaseInsensitive()
    {
        const string json = """{"titulo":"x","tipoViaje":"Familia","barrio":"Recoleta","alcanceTemporal":"DiaCompleto","nivelPresupuesto":"SinRestricciones","ritmoCaminata":"Tranquilo","categoriaCodigos":["CAFE_ESPECIALIDAD","algo_inexistente","MUSEOS_GALERIAS"]}""";
        var servicio = new InterpretacionService(new OllamaClientFalso(json));

        var resultado = await servicio.InterpretarAsync("pedido");

        Assert.Equal(new[] { "cafe_especialidad", "museos_galerias" }, resultado.CategoriaCodigos);
    }

    [Fact]
    public async Task InterpretarRefinamientoAsync_MapeaTipo()
    {
        const string json = """{"tipo":"ReducirCaminata"}""";
        var servicio = new InterpretacionService(new OllamaClientFalso(json));

        var tipo = await servicio.InterpretarRefinamientoAsync("menos caminata");

        Assert.Equal(TipoRefinamiento.ReducirCaminata, tipo);
    }

    private sealed class OllamaClientFalso : IOllamaClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly string _json;

        public OllamaClientFalso(string json)
        {
            _json = json;
        }

        public Task<T> ChatJsonAsync<T>(string prompt, CancellationToken cancellationToken = default)
            => Task.FromResult(JsonSerializer.Deserialize<T>(_json, JsonOptions)!);

        public Task<string> ChatTextoAsync(string prompt, CancellationToken cancellationToken = default)
            => Task.FromResult("Sugerencia de prueba.");
    }
}
