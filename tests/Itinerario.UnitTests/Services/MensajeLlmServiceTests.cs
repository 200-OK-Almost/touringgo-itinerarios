using System.Text.Json;
using Itinerario.Application.Configuration;
using Itinerario.Application.DTOs;
using Itinerario.Application.Interfaces;
using Itinerario.Application.Services;
using Itinerario.Domain.Enums;
using Itinerario.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace Itinerario.UnitTests.Services;

public class MensajeLlmServiceTests
{
    [Fact]
    public async Task GenerarAsync_Habilitado_DevuelveMensajeYConstruyePrompt()
    {
        var contexto = CrearContexto();
        var cliente = new OllamaClientFalso(new MensajeLlmCrudo
        {
            Mensaje = "  Te recomiendo   este bar. "
        });
        var telemetria = new TelemetriaFalsa();
        var service = CrearServicio(cliente, new AsistenteOptions
        {
            LlmGenerarMensaje = true
        }, telemetria);

        var mensaje = await service.GenerarAsync(contexto);

        Assert.Equal("Te recomiendo este bar.", mensaje);
        Assert.True(cliente.FueLlamado);
        Assert.Contains(contexto.MensajeUsuario, cliente.Prompt);
        Assert.Contains(contexto.Recomendaciones.Single().Nombre, cliente.Prompt);
        Assert.Contains("No inventes lugares", cliente.Prompt);
        Assert.Contains("Busco un cafe en Puerto Madero", cliente.Prompt);
        Assert.Contains("No digas que aplicaste cambios", cliente.Prompt);
        Assert.Contains("generation_llm", telemetria.Etapas);
        Assert.Contains("llm_ok", telemetria.Resultados);
    }

    [Fact]
    public async Task GenerarAsync_Deshabilitado_NoLlamaAlModelo()
    {
        var cliente = new OllamaClientFalso(new MensajeLlmCrudo { Mensaje = "Hola" });
        var telemetria = new TelemetriaFalsa();
        var service = CrearServicio(cliente, new AsistenteOptions
        {
            LlmGenerarMensaje = false
        }, telemetria);

        var mensaje = await service.GenerarAsync(CrearContexto());

        Assert.Null(mensaje);
        Assert.False(cliente.FueLlamado);
        Assert.Empty(telemetria.Etapas);
        Assert.Empty(telemetria.Resultados);
    }

    [Fact]
    public async Task GenerarAsync_SinRecomendaciones_LlamaAlModeloYPideAclaracion()
    {
        var contexto = CrearContexto();
        contexto.Recomendaciones = Array.Empty<RecomendacionLugarDto>();
        var cliente = new OllamaClientFalso(new MensajeLlmCrudo
        {
            Mensaje = "Contame qué tipo de lugar buscás."
        });
        var service = CrearServicio(cliente, new AsistenteOptions
        {
            LlmGenerarMensaje = true
        });

        var mensaje = await service.GenerarAsync(contexto);

        Assert.Equal("Contame qué tipo de lugar buscás.", mensaje);
        Assert.True(cliente.FueLlamado);
        Assert.Contains("No hay recomendaciones disponibles", cliente.Prompt);
        Assert.Contains("No recomiendes lugares específicos", cliente.Prompt);
    }

    [Fact]
    public async Task GenerarAsync_SinSenal_IndicaRedirigirLaConversacion()
    {
        var contexto = CrearContexto();
        contexto.Recomendaciones = Array.Empty<RecomendacionLugarDto>();
        contexto.SinSenal = true;
        var cliente = new OllamaClientFalso(new MensajeLlmCrudo
        {
            Mensaje = "Hola, contame qué lugar querés conocer."
        });
        var service = CrearServicio(cliente, new AsistenteOptions
        {
            LlmGenerarMensaje = true
        });

        var mensaje = await service.GenerarAsync(contexto);

        Assert.Equal("Hola, contame qué lugar querés conocer.", mensaje);
        Assert.Contains("no parece ser una consulta de lugares", cliente.Prompt);
    }

    [Fact]
    public async Task GenerarAsync_SinResultadosEnBarrioConCategoria_NoPideTipo()
    {
        var contexto = CrearContexto();
        contexto.Recomendaciones = Array.Empty<RecomendacionLugarDto>();
        contexto.Barrio = BarrioCaba.PuertoMadero;
        contexto.Filtros = new FiltrosIntencionDto
        {
            CategoriaCodigos = new[] { "cafe_especialidad" }
        };
        contexto.SinResultadosEnBarrio = true;
        var cliente = new OllamaClientFalso(new MensajeLlmCrudo
        {
            Mensaje = "No encontré cafeterías en Puerto Madero. ¿Querés ampliar la búsqueda?"
        });
        var service = CrearServicio(cliente, new AsistenteOptions
        {
            LlmGenerarMensaje = true
        });

        var mensaje = await service.GenerarAsync(contexto);

        Assert.NotNull(mensaje);
        Assert.Contains(
            "No hay recomendaciones disponibles para cafe_especialidad en Puerto Madero",
            cliente.Prompt);
        Assert.Contains("No preguntes el tipo de lugar ni el barrio", cliente.Prompt);
        Assert.Contains("Sin resultados en barrio: sí", cliente.Prompt);
        Assert.DoesNotContain("pedile al usuario que aclare", cliente.Prompt);
    }

    [Fact]
    public async Task GenerarAsync_BusquedaAmpliada_IndicaOtrosBarrios()
    {
        var contexto = CrearContexto();
        contexto.Barrio = BarrioCaba.PuertoMadero;
        contexto.Filtros = new FiltrosIntencionDto
        {
            CategoriaCodigos = new[] { "cafe_especialidad" }
        };
        contexto.BusquedaAmpliada = true;
        var cliente = new OllamaClientFalso(new MensajeLlmCrudo
        {
            Mensaje = "No había cafeterías en Puerto Madero; te muestro opciones cercanas."
        });
        var service = CrearServicio(cliente, new AsistenteOptions
        {
            LlmGenerarMensaje = true
        });

        var mensaje = await service.GenerarAsync(contexto);

        Assert.NotNull(mensaje);
        Assert.Contains("mostrás opciones de otros barrios", cliente.Prompt);
        Assert.Contains("Búsqueda ampliada: sí", cliente.Prompt);
    }

    [Fact]
    public async Task GenerarAsync_SinSenal_NoMencionaCategoriasNiResultados()
    {
        var contexto = CrearContexto();
        contexto.Recomendaciones = Array.Empty<RecomendacionLugarDto>();
        contexto.SinSenal = true;
        contexto.Barrio = BarrioCaba.PuertoMadero;
        contexto.Filtros = new FiltrosIntencionDto
        {
            CategoriaCodigos = new[] { "bares_vermu" }
        };
        var cliente = new OllamaClientFalso(new MensajeLlmCrudo
        {
            Mensaje = "De nada. Si querés, puedo ayudarte a buscar otro lugar."
        });
        var service = CrearServicio(cliente, new AsistenteOptions
        {
            LlmGenerarMensaje = true
        });

        var mensaje = await service.GenerarAsync(contexto);

        Assert.NotNull(mensaje);
        Assert.Contains("El mensaje es un saludo, agradecimiento", cliente.Prompt);
        Assert.Contains("(no aplica)", cliente.Prompt);
        Assert.DoesNotContain("bares_vermu", cliente.Prompt);
        Assert.DoesNotContain("No hay recomendaciones disponibles para", cliente.Prompt);
    }

    [Fact]
    public async Task GenerarAsync_MensajeVacio_DevuelveNullYRegistraError()
    {
        var cliente = new OllamaClientFalso(new MensajeLlmCrudo { Mensaje = "   " });
        var telemetria = new TelemetriaFalsa();
        var service = CrearServicio(cliente, new AsistenteOptions
        {
            LlmGenerarMensaje = true
        }, telemetria);

        var mensaje = await service.GenerarAsync(CrearContexto());

        Assert.Null(mensaje);
        Assert.Contains("llm_error", telemetria.Resultados);
    }

    [Fact]
    public async Task GenerarAsync_MensajeLargo_SeRecorta()
    {
        var cliente = new OllamaClientFalso(new MensajeLlmCrudo
        {
            Mensaje = new string('a', 600)
        });
        var service = CrearServicio(cliente, new AsistenteOptions
        {
            LlmGenerarMensaje = true
        });

        var mensaje = await service.GenerarAsync(CrearContexto());

        Assert.NotNull(mensaje);
        Assert.Equal(500, mensaje!.Length);
    }

    [Fact]
    public async Task GenerarAsync_ServicioCaido_DevuelveNullYRegistraError()
    {
        var cliente = new OllamaClientFalso(
            comportamiento: _ => throw new ServicioExternoNoDisponibleException("Ollama caído."));
        var telemetria = new TelemetriaFalsa();
        var service = CrearServicio(cliente, new AsistenteOptions
        {
            LlmGenerarMensaje = true
        }, telemetria);

        var mensaje = await service.GenerarAsync(CrearContexto());

        Assert.Null(mensaje);
        Assert.Contains("generation_llm", telemetria.Etapas);
        Assert.Contains("llm_error", telemetria.Resultados);
    }

    [Fact]
    public async Task GenerarAsync_JsonInvalido_DevuelveNullYRegistraError()
    {
        var cliente = new OllamaClientFalso(
            comportamiento: _ => throw new JsonException("JSON inválido."));
        var telemetria = new TelemetriaFalsa();
        var service = CrearServicio(cliente, new AsistenteOptions
        {
            LlmGenerarMensaje = true
        }, telemetria);

        var mensaje = await service.GenerarAsync(CrearContexto());

        Assert.Null(mensaje);
        Assert.Contains("llm_error", telemetria.Resultados);
    }

    [Fact]
    public async Task GenerarAsync_Timeout_DevuelveNullYRegistraTimeout()
    {
        var cliente = new OllamaClientFalso(
            comportamiento: cancellationToken => Task.Delay(
                TimeSpan.FromSeconds(30),
                cancellationToken));
        var telemetria = new TelemetriaFalsa();
        var service = CrearServicio(cliente, new AsistenteOptions
        {
            LlmGenerarMensaje = true,
            GenerationTimeoutSeconds = 1
        }, telemetria);

        var mensaje = await service.GenerarAsync(CrearContexto());

        Assert.Null(mensaje);
        Assert.Contains("llm_timeout", telemetria.Resultados);
    }

    [Fact]
    public async Task GenerarAsync_CancelacionExterna_PropagaExcepcion()
    {
        var cliente = new OllamaClientFalso(
            comportamiento: cancellationToken => Task.Delay(
                Timeout.InfiniteTimeSpan,
                cancellationToken));
        var service = CrearServicio(cliente, new AsistenteOptions
        {
            LlmGenerarMensaje = true
        });

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.GenerarAsync(CrearContexto(), cancellationTokenSource.Token));
    }

    private static MensajeLlmService CrearServicio(
        IOllamaClient cliente,
        AsistenteOptions options,
        TelemetriaFalsa? telemetria = null)
        => new(
            cliente,
            telemetria ?? new TelemetriaFalsa(),
            Options.Create(options));

    private static MensajeLlmContextoDto CrearContexto() => new()
    {
        ViajeId = Guid.NewGuid(),
        MensajeUsuario = "Agregá un bar para la tarde",
        Accion = TipoAccionAsistente.Agregar,
        NumeroDia = 1,
        Historial = new[]
        {
            new MensajeHistorialDto
            {
                Rol = "user",
                Contenido = "Busco un cafe en Puerto Madero"
            }
        },
        Recomendaciones = new[]
        {
            new RecomendacionLugarDto
            {
                LugarId = Guid.NewGuid(),
                Nombre = "Bar de la esquina",
                Barrio = BarrioCaba.Palermo,
                Latitud = -34.58m,
                Longitud = -58.42m,
                Categorias = new[] { "bares_vermu" },
                Motivo = "Coincide con bares_vermu."
            }
        }
    };

    private sealed class OllamaClientFalso : IOllamaClient
    {
        private readonly Func<CancellationToken, Task>? _comportamiento;

        public OllamaClientFalso(
            MensajeLlmCrudo? respuesta = null,
            Func<CancellationToken, Task>? comportamiento = null)
        {
            Respuesta = respuesta;
            _comportamiento = comportamiento;
        }

        public MensajeLlmCrudo? Respuesta { get; }

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

            return (T)(object)(Respuesta ?? new MensajeLlmCrudo());
        }

        public Task<string> ChatTextoAsync(string prompt, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
