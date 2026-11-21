using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Itinerario.Application.DTOs;
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
                { "id": "11111111-1111-1111-1111-111111111101", "nombre": "Plaza Serrano", "latitud": -34.5883, "longitud": -58.4306, "duracionEstimadaMinutos": 45, "nivelPrecio": 1, "esAlAireLibre": true, "categorias": ["parques_aire_libre", "cafe_especialidad"] }
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
        Assert.Equal(
            new[] { "parques_aire_libre", "cafe_especialidad" },
            unico.Categorias);

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
    public async Task BuscarSemanticoAsync_MapeaRespuestaEnriquecida()
    {
        var handler = new FakeHandler(_ => JsonResponse(
            """
            [
              {
                "id": "11111111-1111-1111-1111-111111111101",
                "nombre": "Plaza Serrano",
                "barrio": "Palermo",
                "subzona": "Soho",
                "direccion": "Thames 1800",
                "latitud": -34.5883,
                "longitud": -58.4306,
                "descripcion": "Plaza con ferias y bares",
                "duracionEstimadaMinutos": 45,
                "nivelPrecio": 2,
                "puntuacion": 4.7,
                "cantidadResenas": 1234,
                "esAlAireLibre": true,
                "esBajoTecho": false,
                "aptoNinos": true,
                "accesibleMovilidadReducida": true,
                "ofreceMenuInfantil": false,
                "opcionesDietarias": ["vegano", "celiaco"],
                "fotos": [
                  { "id": "22222222-2222-2222-2222-222222222201", "url": "http://foto", "orden": 1, "esPortada": true }
                ],
                "categorias": [{ "id": 3, "codigo": "parques_aire_libre", "nombre": "Parques y aire libre" }],
                "similitud": 0.87
              }
            ]
            """));

        var client = new LugaresClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });

        var resultado = await client.BuscarSemanticoAsync(
            new BusquedaSemanticaLugaresDto { Query = "cafe tranquilo", Cantidad = 10 });

        var unico = Assert.Single(resultado);
        Assert.Equal("Plaza Serrano", unico.Nombre);
        Assert.Equal("Plaza con ferias y bares", unico.Descripcion);
        Assert.Equal("Thames 1800", unico.Direccion);
        Assert.Equal("Soho", unico.Subzona);
        Assert.Equal(BarrioCaba.Palermo, unico.Barrio);
        Assert.Equal(NivelPresupuesto.Equilibrado, unico.NivelPrecio);
        Assert.Equal(4.7m, unico.Puntuacion);
        Assert.Equal(1234, unico.CantidadResenas);
        Assert.True(unico.EsAireLibre);
        Assert.False(unico.EsBajoTecho);
        Assert.True(unico.AptoNinos);
        Assert.True(unico.AccesibleMovilidadReducida);
        Assert.False(unico.OfreceMenuInfantil);
        Assert.Equal("http://foto", unico.FotoPortadaUrl);
        Assert.Equal(new[] { "vegano", "celiaco" }, unico.OpcionesDietarias);
        Assert.Equal(0.87, unico.Similitud);
        Assert.Equal(new[] { "parques_aire_libre" }, unico.Categorias);
    }

    [Fact]
    public async Task BuscarSemanticoAsync_EnviaFiltrosDuros()
    {
        string? body = null;
        var handler = new FakeHandler(async (req, cancellationToken) =>
        {
            body = await req.Content!.ReadAsStringAsync(cancellationToken);
            return JsonResponse("[]");
        });

        var client = new LugaresClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var excluido = Guid.NewGuid();

        await client.BuscarSemanticoAsync(new BusquedaSemanticaLugaresDto
        {
            Query = "lugar al aire libre",
            Cantidad = 8,
            Barrio = BarrioCaba.Palermo,
            CategoriaCodigos = new[] { "cafe_especialidad" },
            NivelPrecioMaximo = 2,
            SoloAlAireLibre = true,
            SoloBajoTecho = false,
            AptoNinos = true,
            AccesibleMovilidadReducida = true,
            RestriccionDietaria = "Celiaco",
            ExcluirIds = new[] { excluido },
            LatitudReferencia = -34.6037m,
            LongitudReferencia = -58.3816m,
            RadioMetrosMaximo = 800,
            SimilitudMinima = 0.6
        });

        Assert.NotNull(body);
        Assert.Contains("\"barrio\":\"Palermo\"", body);
        Assert.Contains("\"categoriaCodigos\":[\"cafe_especialidad\"]", body);
        Assert.Contains("\"nivelPrecioMaximo\":2", body);
        Assert.Contains("\"soloAlAireLibre\":true", body);
        Assert.Contains("\"soloBajoTecho\":false", body);
        Assert.Contains("\"aptoNinos\":true", body);
        Assert.Contains("\"accesibleMovilidadReducida\":true", body);
        Assert.Contains("\"restriccionDietaria\":\"Celiaco\"", body);
        Assert.Contains(excluido.ToString(), body);
        Assert.Contains("\"radioMetrosMaximo\":800", body);
        Assert.Contains("\"similitudMinima\":0.6", body);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_MapeaCategoriasDelDetalle()
    {
        var handler = new FakeHandler(_ => JsonResponse(
            """
            {
              "id": "11111111-1111-1111-1111-111111111101",
              "nombre": "Café Lattente",
              "barrio": "Palermo",
              "latitud": -34.5885,
              "longitud": -58.4282,
              "duracionEstimadaMinutos": 45,
              "nivelPrecio": 1,
              "esAlAireLibre": true,
              "categorias": [
                { "id": 1, "codigo": "cafe_especialidad", "nombre": "Café de especialidad" }
              ]
            }
            """));

        var client = new LugaresClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });

        var lugar = await client.ObtenerPorIdAsync(Guid.NewGuid());

        Assert.NotNull(lugar);
        Assert.Equal("Café Lattente", lugar!.Nombre);
        Assert.Equal(new[] { "cafe_especialidad" }, lugar.Categorias);
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
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            : this((request, _) => Task.FromResult(handler(request)))
        {
        }

        public FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => _handler(request, cancellationToken);
    }
}
