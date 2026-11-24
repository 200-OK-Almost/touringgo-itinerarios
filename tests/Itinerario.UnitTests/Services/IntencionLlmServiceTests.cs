using System.Text.Json;
using Itinerario.Application.Configuration;
using Itinerario.Application.DTOs;
using Itinerario.Application.Interfaces;
using Itinerario.Application.Services;
using Itinerario.Domain.Enums;
using Itinerario.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace Itinerario.UnitTests.Services;

public class IntencionLlmServiceTests
{
    [Fact]
    public async Task InterpretarAsync_Habilitado_DevuelveSalidaYConstruyePrompt()
    {
        var contexto = CrearContexto();
        var cliente = new OllamaClientFalso(new IntencionLlmCruda
        {
            Accion = "Reemplazar",
            QuerySemantica = "aire libre"
        });
        var telemetria = new TelemetriaFalsa();
        var service = CrearServicio(cliente, new AsistenteOptions
        {
            LlmEnabled = true
        }, telemetria);

        var resultado = await service.InterpretarAsync(contexto);

        Assert.NotNull(resultado);
        Assert.Equal("Reemplazar", resultado!.Accion);
        Assert.Equal("aire libre", resultado.QuerySemantica);
        Assert.Contains(contexto.Mensaje, cliente.Prompt);
        Assert.Contains(contexto.Paradas.Single().ParadaId.ToString(), cliente.Prompt);
        Assert.Contains("parques_aire_libre", cliente.Prompt);
        Assert.Contains("No inventes lugares", cliente.Prompt);
        Assert.Contains("Quiero algo en Palermo", cliente.Prompt);
        Assert.Contains("intent_llm", telemetria.Etapas);
        Assert.Contains("llm_ok", telemetria.Resultados);
    }

    [Fact]
    public async Task InterpretarAsync_Deshabilitado_NoLlamaAlModelo()
    {
        var cliente = new OllamaClientFalso(new IntencionLlmCruda());
        var telemetria = new TelemetriaFalsa();
        var service = CrearServicio(cliente, new AsistenteOptions
        {
            LlmEnabled = false
        }, telemetria);

        var resultado = await service.InterpretarAsync(CrearContexto());

        Assert.Null(resultado);
        Assert.False(cliente.FueLlamado);
        Assert.Empty(telemetria.Etapas);
        Assert.Empty(telemetria.Resultados);
    }

    [Fact]
    public async Task InterpretarAsync_MensajeVacio_NoLlamaAlModelo()
    {
        var contexto = CrearContexto();
        contexto.Mensaje = "   ";
        var cliente = new OllamaClientFalso(new IntencionLlmCruda());
        var service = CrearServicio(cliente, new AsistenteOptions
        {
            LlmEnabled = true
        });

        var resultado = await service.InterpretarAsync(contexto);

        Assert.Null(resultado);
        Assert.False(cliente.FueLlamado);
    }

    [Fact]
    public async Task InterpretarAsync_ServicioCaido_DevuelveNullYRegistraError()
    {
        var cliente = new OllamaClientFalso(
            comportamiento: _ => throw new ServicioExternoNoDisponibleException("Ollama caído."));
        var telemetria = new TelemetriaFalsa();
        var service = CrearServicio(cliente, new AsistenteOptions
        {
            LlmEnabled = true
        }, telemetria);

        var resultado = await service.InterpretarAsync(CrearContexto());

        Assert.Null(resultado);
        Assert.Contains("intent_llm", telemetria.Etapas);
        Assert.Contains("llm_error", telemetria.Resultados);
    }

    [Fact]
    public async Task InterpretarAsync_JsonInvalido_DevuelveNullYRegistraError()
    {
        var cliente = new OllamaClientFalso(
            comportamiento: _ => throw new JsonException("JSON inválido."));
        var telemetria = new TelemetriaFalsa();
        var service = CrearServicio(cliente, new AsistenteOptions
        {
            LlmEnabled = true
        }, telemetria);

        var resultado = await service.InterpretarAsync(CrearContexto());

        Assert.Null(resultado);
        Assert.Contains("llm_error", telemetria.Resultados);
    }

    [Fact]
    public async Task InterpretarAsync_Timeout_DevuelveNullYRegistraTimeout()
    {
        var cliente = new OllamaClientFalso(
            comportamiento: cancellationToken => Task.Delay(
                TimeSpan.FromSeconds(30),
                cancellationToken));
        var telemetria = new TelemetriaFalsa();
        var service = CrearServicio(cliente, new AsistenteOptions
        {
            LlmEnabled = true,
            IntentTimeoutSeconds = 1
        }, telemetria);

        var resultado = await service.InterpretarAsync(CrearContexto());

        Assert.Null(resultado);
        Assert.Contains("llm_timeout", telemetria.Resultados);
    }

    [Fact]
    public async Task InterpretarAsync_CancelacionExterna_PropagaExcepcion()
    {
        var cliente = new OllamaClientFalso(
            comportamiento: cancellationToken => Task.Delay(
                Timeout.InfiniteTimeSpan,
                cancellationToken));
        var service = CrearServicio(cliente, new AsistenteOptions
        {
            LlmEnabled = true
        });

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.InterpretarAsync(CrearContexto(), cancellationTokenSource.Token));
    }

    private static IntencionLlmService CrearServicio(
        IOllamaClient cliente,
        AsistenteOptions options,
        TelemetriaFalsa? telemetria = null)
        => new(
            cliente,
            telemetria ?? new TelemetriaFalsa(),
            Options.Create(options));

    private static IntencionLlmContextoDto CrearContexto() => new()
    {
        ViajeId = Guid.NewGuid(),
        Mensaje = "Reemplazá el museo por algo al aire libre",
        NumeroDia = 1,
        Barrio = BarrioCaba.Palermo,
        NivelPresupuesto = NivelPresupuesto.Equilibrado,
        CategoriasViaje = new[] { "museos_galerias" },
        Historial = new[]
        {
            new MensajeHistorialDto
            {
                Rol = "user",
                Contenido = "Quiero algo en Palermo"
            }
        },
        AptoNinos = true,
        AccesibleMovilidadReducida = false,
        Paradas = new[]
        {
            new ParadaResolubleDto
            {
                ParadaId = Guid.NewGuid(),
                LugarId = Guid.NewGuid(),
                Nombre = "Museo de Arte",
                Turno = Turno.Manana,
                OrdenParada = 1,
                Categorias = new[] { "museos_galerias" }
            }
        }
    };

    private sealed class OllamaClientFalso : IOllamaClient
    {
        private readonly Func<CancellationToken, Task>? _comportamiento;

        public OllamaClientFalso(
            IntencionLlmCruda? respuesta = null,
            Func<CancellationToken, Task>? comportamiento = null)
        {
            Respuesta = respuesta;
            _comportamiento = comportamiento;
        }

        public IntencionLlmCruda? Respuesta { get; }

        public bool FueLlamado { get; private set; }

        public string? Prompt { get; private set; }

        public async Task<T> ChatJsonAsync<T>(string prompt, CancellationToken cancellationToken = default)
        {
            FueLlamado = true;
            Prompt = prompt;

            if (_comportamiento is not null)
            {
                await _comportamiento(cancellationToken);
            }

            return (T)(object)(Respuesta ?? new IntencionLlmCruda());
        }

        public Task<string> ChatTextoAsync(string prompt, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
