using Itinerario.Application.Configuration;
using Itinerario.Application.DTOs;
using Itinerario.Application.Validators;
using Microsoft.Extensions.Options;

namespace Itinerario.UnitTests.Validators;

public class AsistenteValidatorsTests
{
    private readonly AsistenteRequestDtoValidator _requestValidator = new(
        Options.Create(new AsistenteOptions
        {
            MaxMensajeCaracteres = 20,
            MaxHistorial = 2
        }));

    private readonly AplicarAccionAsistenteDtoValidator _aplicarValidator = new();

    [Fact]
    public void Request_Valido_EsValido()
    {
        var resultado = _requestValidator.Validate(new AsistenteRequestDto
        {
            Mensaje = "café",
            NumeroDia = 1,
            Historial = new[]
            {
                new MensajeHistorialDto { Rol = "user", Contenido = "hola" },
                new MensajeHistorialDto { Rol = "assistant", Contenido = "hola" }
            }
        });

        Assert.True(resultado.IsValid, Mensajes(resultado));
    }

    [Fact]
    public void Request_MensajeVacio_EsInvalido()
    {
        var resultado = _requestValidator.Validate(new AsistenteRequestDto
        {
            Mensaje = "   "
        });

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Request_MensajeLargo_EsInvalido()
    {
        var resultado = _requestValidator.Validate(new AsistenteRequestDto
        {
            Mensaje = new string('a', 21)
        });

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Request_HistorialExcedido_EsInvalido()
    {
        var resultado = _requestValidator.Validate(new AsistenteRequestDto
        {
            Mensaje = "café",
            Historial = Enumerable.Range(0, 3)
                .Select(_ => new MensajeHistorialDto { Rol = "user", Contenido = "hola" })
                .ToList()
        });

        Assert.False(resultado.IsValid);
        Assert.Contains(
            resultado.Errors,
            error => error.ErrorMessage.Contains("historial", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Request_RolInvalido_EsInvalido()
    {
        var resultado = _requestValidator.Validate(new AsistenteRequestDto
        {
            Mensaje = "café",
            Historial = new[]
            {
                new MensajeHistorialDto { Rol = "system", Contenido = "hola" }
            }
        });

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Aplicar_AgregarValido_EsValido()
    {
        var resultado = _aplicarValidator.Validate(new AplicarAccionAsistenteDto
        {
            Tipo = "AgregarParada",
            NumeroDia = 1,
            LugarId = Guid.NewGuid(),
            Version = "123"
        });

        Assert.True(resultado.IsValid, Mensajes(resultado));
    }

    [Fact]
    public void Aplicar_TipoInvalido_EsInvalido()
    {
        var resultado = _aplicarValidator.Validate(new AplicarAccionAsistenteDto
        {
            Tipo = "QuitarParada",
            NumeroDia = 1,
            LugarId = Guid.NewGuid(),
            Version = "123"
        });

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Aplicar_ReemplazoSinParada_EsInvalido()
    {
        var resultado = _aplicarValidator.Validate(new AplicarAccionAsistenteDto
        {
            Tipo = "ReemplazarParada",
            NumeroDia = 1,
            LugarId = Guid.NewGuid(),
            Version = "123"
        });

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Aplicar_AgregarConParada_EsInvalido()
    {
        var resultado = _aplicarValidator.Validate(new AplicarAccionAsistenteDto
        {
            Tipo = "AgregarParada",
            NumeroDia = 1,
            LugarId = Guid.NewGuid(),
            ParadaId = Guid.NewGuid(),
            Version = "123"
        });

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Aplicar_VersionVaciaYTurnoInvalido_EsInvalido()
    {
        var resultado = _aplicarValidator.Validate(new AplicarAccionAsistenteDto
        {
            Tipo = "AgregarParada",
            NumeroDia = 0,
            LugarId = Guid.Empty,
            Version = string.Empty,
            Turno = "Madrugada"
        });

        Assert.False(resultado.IsValid);
        Assert.True(resultado.Errors.Count >= 4);
    }

    private static string Mensajes(FluentValidation.Results.ValidationResult resultado)
        => string.Join(" | ", resultado.Errors.Select(error => error.ErrorMessage));
}
