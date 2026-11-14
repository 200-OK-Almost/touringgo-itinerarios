using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Itinerario.Domain.Enums;
using Itinerario.Domain.Exceptions;
using Itinerario.Infrastructure.Clients;

namespace Itinerario.UnitTests.Services;

public class LugaresClientTests
{
    [Fact]
    public async Task BuscarAsync_ConstruyeQueryYMapeaRespuesta()
    {
        string query = "";
        var handler = new FakeHandler(req =>
        {
            query = req.RequestUri!.Query;
            const string json = """
            {
              "items": [
                { "id": "11111111-1111-1111-1111-111111111101", "nombre": "Plaza Serrano", "latitud": -34.5883, "longitud": -58.4306, "duracionEstimadaMinutos": 45, "nivelPrecio": 1, "esAlAireLibre": true }
              ],
              "total": 1,
              "pagina": 1,
              "tamano": 100
            }
            """;
            return JsonResponse(json);
        });

        var client = new LugaresClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });

        var resultado = await client.BuscarAsync(
            BarrioCaba.Palermo,
            new[] { "cafe_especialidad", "parques_aire_libre" });

        var unico = Assert.Single(resultado);
        Assert.Equal("Plaza Serrano", unico.Nombre);
        Assert.Equal(NivelPresupuesto.Economico, unico.NivelPrecio);
        Assert.True(unico.EsAireLibre);
        Assert.Equal(45, unico.DuracionSugeridaMinutos);

        Assert.Contains("barrio=Palermo", query);
        Assert.Contains("categoriaCodigo=cafe_especialidad", query);
        Assert.Contains("categoriaCodigo=parques_aire_libre", query);
        Assert.Contains("pagina=1", query);
        Assert.Contains("tamano=100", query);
    }

    [Fact]
    public async Task BuscarAsync_IteraPaginasHastaCompletar()
    {
        var paginas = new List<int>();
        var handler = new FakeHandler(req =>
        {
            var query = req.RequestUri!.Query;
            var pagina = int.Parse(QueryParam(query, "pagina"));
            paginas.Add(pagina);

            var tamano = pagina == 1 ? 100 : 2;
            var items = Enumerable.Range(0, tamano)
                .Select(i => $"{{\"id\":\"{Guid.NewGuid()}\",\"nombre\":\"Lugar {i}\",\"latitud\":0,\"longitud\":0,\"duracionEstimadaMinutos\":30,\"nivelPrecio\":2,\"esAlAireLibre\":false}}");

            return JsonResponse(
                $"{{\"items\":[{string.Join(',', items)}],\"total\":102,\"pagina\":{pagina},\"tamano\":100}}");
        });

        var client = new LugaresClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });

        var resultado = await client.BuscarAsync(BarrioCaba.Palermo, new[] { "museos_galerias" });

        Assert.Equal(102, resultado.Count);
        Assert.Equal(new[] { 1, 2 }, paginas);
    }

    [Fact]
    public async Task BuscarAsync_LugaresCaido_LanzaServicioExternoNoDisponible()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var client = new LugaresClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });

        await Assert.ThrowsAsync<ServicioExternoNoDisponibleException>(
            () => client.BuscarAsync(BarrioCaba.Palermo, new[] { "museos_galerias" }));
    }

    [Fact]
    public async Task BuscarAsync_MapeaNivelPrecioFueraDeRango()
    {
        var handler = new FakeHandler(_ => JsonResponse(
            """
            {
              "items": [
                { "id": "11111111-1111-1111-1111-111111111101", "nombre": "Lugar", "latitud": 0, "longitud": 0, "duracionEstimadaMinutos": 30, "nivelPrecio": 99, "esAlAireLibre": false }
              ],
              "total": 1,
              "pagina": 1,
              "tamano": 100
            }
            """));

        var client = new LugaresClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });

        var resultado = await client.BuscarAsync(BarrioCaba.Palermo, new[] { "museos_galerias" });

        Assert.Equal(NivelPresupuesto.SinRestricciones, Assert.Single(resultado).NivelPrecio);
    }

    [Fact]
    public async Task BuscarSemanticoAsync_MapeaRespuesta()
    {
        var handler = new FakeHandler(_ => JsonResponse(
            """
            [
              { "id": "11111111-1111-1111-1111-111111111101", "nombre": "Plaza Serrano", "barrio": "Palermo", "latitud": -34.5883, "longitud": -58.4306, "duracionEstimadaMinutos": 45, "nivelPrecio": 2, "esAlAireLibre": true }
            ]
            """));

        var client = new LugaresClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });

        var resultado = await client.BuscarSemanticoAsync("cafe tranquilo", 10);

        var unico = Assert.Single(resultado);
        Assert.Equal("Plaza Serrano", unico.Nombre);
        Assert.Equal(BarrioCaba.Palermo, unico.Barrio);
        Assert.Equal(NivelPresupuesto.Equilibrado, unico.NivelPrecio);
        Assert.True(unico.EsAireLibre);
    }

    private static HttpResponseMessage JsonResponse(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private static string QueryParam(string query, string key)
        => Regex.Match(query, $@"(?<={key}=)[^&]*").Value;

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(_handler(request));
    }
}
