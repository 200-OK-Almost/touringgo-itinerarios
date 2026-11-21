# Asistente RAG — Fase 0

Esta fase prepara la base del asistente sin implementar todavía retrieval, intención, Ollama ni la aplicación de cambios al itinerario.

## Alcance de la fase

- Contratos del asistente.
- Configuración y feature flag.
- Endpoint global de capacidades.
- Rate limiting.
- Telemetría básica.

Fuera de alcance:

- Búsqueda semántica y filtros duros.
- Backfill de embeddings.
- Detección de intención.
- Integración con Ollama.
- Aplicar acciones al itinerario.
- Frontend.

## Endpoint de capacidades

```http
GET /api/viajes/asistente/capacidades
Authorization: Bearer <jwt>
```

Requiere autenticación. Devuelve si el asistente está habilitado y los límites configurados:

```json
{
  "habilitado": true,
  "limites": {
    "topK": 12,
    "minResultados": 3,
    "maxMensajeCaracteres": 1000,
    "maxHistorial": 6,
    "rateLimitPorUsuario": 20,
    "rateLimitPorViaje": 40,
    "ventanaRateLimitSegundos": 60
  }
}
```

`PuedeAplicar` no es global: se calcula por viaje y usuario, y viajará en cada `AsistenteResponseDto` en las próximas fases.

## Configuración

| Clave | Default | Descripción |
|---|---|---|
| `Asistente:Enabled` | `false` | Feature flag principal. En Development está en `true`. |
| `Asistente:TopK` | `12` | Cantidad máxima de lugares a recuperar. |
| `Asistente:MinResultados` | `3` | Umbral para activar fallback. |
| `Asistente:SimilitudMinima` | `null` | Sin umbral hasta calibrar con datos reales. |
| `Asistente:RadioMetrosDefault` | `800` | Radio por defecto para consultas de cercanía. |
| `Asistente:MaxMensajeCaracteres` | `1000` | Límite del mensaje del usuario. |
| `Asistente:MaxHistorial` | `6` | Máximo de mensajes de historial aceptados. |
| `Asistente:IntentTimeoutSeconds` | `10` | Timeout para detección de intención. |
| `Asistente:GenerationTimeoutSeconds` | `45` | Timeout para generación con LLM. |
| `Asistente:VerboseLogging` | `false` | Habilita logs de detalle. |
| `Asistente:RateLimit:PermitLimitPorUsuario` | `20` | Consultas por ventana y usuario. |
| `Asistente:RateLimit:PermitLimitPorViaje` | `40` | Consultas por ventana, usuario y viaje. |
| `Asistente:RateLimit:WindowSeconds` | `60` | Ventana del rate limiter. |
| `Asistente:RateLimit:QueueLimit` | `0` | Consultas en cola antes de rechazar. |

Los valores se validan al arrancar. Si una opción queda fuera de rango, la API no inicia.

## Contratos

DTOs agregados en `Itinerario.Application/DTOs`:

- `AsistenteRequestDto`
- `AsistenteResponseDto`
- `RecomendacionLugarDto`
- `AccionPropuestaDto`
- `AplicarAccionAsistenteDto`
- `AclaracionDto`
- `OpcionAclaracionDto`
- `MensajeHistorialDto`
- `AsistenteCapacidadesDto`
- `AsistenteLimitesDto`

`Version` se agrega a los contratos de itinerario y acción. Es un string opaco basado en `xmin` del `Itinerario`. Se compara solo por igualdad, nunca como número. Su materialización y el bump de `FechaActualizacion` se implementan en la Fase 1.4.

## Rate limiting

Políticas disponibles:

- `asistente-usuario`: partición por `sub` del JWT.
- `asistente-viaje`: partición por usuario + `id` del viaje.

Cuando se supera el límite, la API responde:

```http
HTTP/1.1 429 Too Many Requests
Retry-After: 60
Content-Type: application/json

{ "mensaje": "Demasiadas consultas al asistente. Intentá de nuevo en unos segundos." }
```

El límite es por instancia. Si el servicio escala horizontalmente, habrá que mover el almacenamiento a un backend distribuido.

## Telemetría

`IAsistenteTelemetry` permite registrar:

- duración de etapas (`embedding`, `retrieval`, `intent`, `llm`, `total`);
- resultado de la consulta y cantidad de recomendaciones;
- detalle de diagnóstico solo si `Asistente:VerboseLogging` está activo.

No se loguean mensajes completos, prompts ni descripciones de lugares por defecto.

## Próximas fases

1. Filtros duros y benchmark vectorial en Lugares.
2. Backfill de embeddings con hash versionado.
3. Asistente determinista sin LLM.
4. Aplicar acciones con concurrencia basada en `xmin`.
5. Chat funcional en el frontend.
6. Intención, Ollama con structured outputs y endurecimiento.
