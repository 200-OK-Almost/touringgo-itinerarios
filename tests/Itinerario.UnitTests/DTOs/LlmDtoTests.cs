using System.Text.Json;
using Itinerario.Application.DTOs;

namespace Itinerario.UnitTests.DTOs;

public class LlmDtoTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void IntencionLlmCruda_DeserializaJsonDelModelo()
    {
        const string json = """
        {
          "accion": "Reemplazar",
          "querySemantica": "aire libre",
          "paradaObjetivoId": "11111111-1111-1111-1111-111111111111",
          "categorias": ["parques_aire_libre", "cafe_especialidad"],
          "soloAlAireLibre": true,
          "soloBajoTecho": null,
          "aptoNinos": false,
          "accesibleMovilidadReducida": true,
          "restriccionDietaria": "vegano",
          "turno": "Tarde",
          "nivelPrecioMaximo": 2,
          "radioMetros": 800
        }
        """;

        var crudo = JsonSerializer.Deserialize<IntencionLlmCruda>(json, JsonOptions);

        Assert.NotNull(crudo);
        Assert.Equal("Reemplazar", crudo.Accion);
        Assert.Equal("aire libre", crudo.QuerySemantica);
        Assert.Equal("11111111-1111-1111-1111-111111111111", crudo.ParadaObjetivoId);
        Assert.Equal(new[] { "parques_aire_libre", "cafe_especialidad" }, crudo.Categorias!.ToArray());
        Assert.True(crudo.SoloAlAireLibre);
        Assert.Null(crudo.SoloBajoTecho);
        Assert.False(crudo.AptoNinos);
        Assert.True(crudo.AccesibleMovilidadReducida);
        Assert.Equal("vegano", crudo.RestriccionDietaria);
        Assert.Equal("Tarde", crudo.Turno);
        Assert.Equal((short?)2, crudo.NivelPrecioMaximo);
        Assert.Equal(800d, crudo.RadioMetros);
    }

    [Fact]
    public void IntencionLlmCruda_PermiteCamposAusentes()
    {
        var crudo = JsonSerializer.Deserialize<IntencionLlmCruda>(
            """{ "accion": "Recomendar" }""",
            JsonOptions);

        Assert.NotNull(crudo);
        Assert.Equal("Recomendar", crudo.Accion);
        Assert.Null(crudo.QuerySemantica);
        Assert.Null(crudo.ParadaObjetivoId);
        Assert.Null(crudo.Categorias);
        Assert.Null(crudo.RadioMetros);
    }

    [Fact]
    public void MensajeLlmCrudo_DeserializaMensaje()
    {
        var crudo = JsonSerializer.Deserialize<MensajeLlmCrudo>(
            """{ "mensaje": "Te propongo estos lugares." }""",
            JsonOptions);

        Assert.NotNull(crudo);
        Assert.Equal("Te propongo estos lugares.", crudo.Mensaje);
    }
}
