# Asistente RAG — Fase 0 + Fase 1 determinista

El asistente tiene implementado el corte vertical sin LLM:

- contratos y configuración;
- endpoint de capacidades;
- rate limiting y telemetría;
- recuperación de lugares desde Lugares;
- recomendaciones deterministas;
- aplicación de acciones al itinerario con concurrencia por `xmin`.

Todavía no incluye detección de intención, Ollama ni frontend.

## Endpoints

### Capacidades

```http
GET /api/viajes/asistente/capacidades
Authorization: Bearer <jwt>
```

Devuelve si el asistente está habilitado y los límites configurados.

### Consulta

```http
POST /api/viajes/{id}/asistente
Authorization: Bearer <jwt>
```

Request:

```json
{
  "mensaje": "¿Qué lugares recomendás para comer cerca de la primera parada?",
  "numeroDia": 1,
  "historial": []
}
```

Response:

```json
{
  "mensaje": "Encontré 3 lugares que pueden encajar en tu recorrido.",
  "puedeAplicar": true,
  "recomendaciones": [
    {
      "lugarId": "uuid",
      "nombre": "Café de especialidad",
      "descripcion": "Texto del lugar",
      "direccion": "Dirección",
      "barrio": "Palermo",
      "subzona": "Soho",
      "latitud": -34.58,
      "longitud": -58.42,
      "puntuacion": 4.7,
      "cantidadResenas": 1200,
      "nivelPrecio": 2,
      "duracionMinutos": 60,
      "esAireLibre": false,
      "esBajoTecho": true,
      "aptoNinos": true,
      "accesible": true,
      "fotoPortadaUrl": "https://...",
      "categorias": ["cafe_especialidad"],
      "opcionesDietarias": ["vegano"],
      "similitud": 0.82,
      "motivo": "Coincide con cafe_especialidad."
    }
  ],
  "accion": null,
  "aclaracion": null,
  "advertencia": null
}
```

Reglas:

- cualquier miembro del viaje puede consultar;
- si el usuario no es miembro, responde `404`;
- si el asistente está deshabilitado, responde `404`;
- si la búsqueda semántica falla o devuelve pocos resultados, usa el catálogo filtrado y devuelve `advertencia`;
- no aplica cambios en esta llamada.

### Aplicar acción

```http
POST /api/viajes/{id}/asistente/aplicar
Authorization: Bearer <jwt>
```

Request:

```json
{
  "tipo": "AgregarParada",
  "numeroDia": 1,
  "lugarId": "uuid",
  "paradaId": null,
  "turno": "Tarde",
  "version": "xmin-opaco"
}
```

Tipos soportados:

- `AgregarParada`;
- `ReemplazarParada`.

Reglas:

- solo administrador del viaje;
- `version` es obligatoria y se compara por igualdad;
- si `version` no coincide → `409`;
- si `ParadaId` no existe → `404`;
- si el lugar ya está en el itinerario → `409`;
- si el día no existe → `400`;
- si el turno está ocupado o el lugar no es compatible → `409`/`400`;
- `AgregarParada` calcula turno libre, orden, hora, distancia, cuadras y minutos;
- `ReemplazarParada` conserva turno y orden, y recalcula métricas;
- el bump de `FechaActualizacion` y la mutación se guardan en el mismo `SaveChanges`.

## Flujo determinista de consulta

1. Validar miembro y día.
2. Cargar barrio, categorías, presupuesto y parámetros familiares.
3. Hidratar paradas actuales con `ObtenerPorIdAsync`.
4. Construir la búsqueda semántica:
   - query = mensaje;
   - filtros duros: barrio, categorías, precio, accesibilidad, dietas, IDs excluidos;
   - radio si el mensaje menciona “cerca”;
   - umbral opcional.
5. Consultar Lugares.
6. Si hay pocos resultados, usar `BuscarAsync` como fallback.
7. Mapear recomendaciones con motivo y `PuedeAplicar`.

No se concatena el barrio ni el presupuesto en el texto del embedding: esos filtros van por SQL.

## Versión e itinerarios

`Version` se obtiene de `xmin` del `Itinerario` y se expone en:

- `ItinerarioResponseDto`;
- `AccionPropuestaDto`;
- `AplicarAccionAsistenteDto`.

Se trata como string opaco y se compara solo por igualdad. Un cambio en el día 1 no invalida una propuesta del día 2.

Todas las rutas que mutan paradas fuerzan el UPDATE del padre:

- `RefinarAsync`;
- `RefinarPorTextoAsync`;
- `SugerirTurnosLibresAsync`;
- `AplicarAccionAsistenteAsync`.

## Configuración

| Clave | Default | Descripción |
|---|---|---|
| `Asistente:Enabled` | `false` | Feature flag. En Development está en `true`. |
| `Asistente:TopK` | `12` | Cantidad máxima de lugares a recuperar. |
| `Asistente:MinResultados` | `3` | Umbral para activar fallback. |
| `Asistente:SimilitudMinima` | `null` | Sin umbral hasta calibrar. |
| `Asistente:RadioMetrosDefault` | `800` | Radio para consultas de cercanía. |
| `Asistente:MaxMensajeCaracteres` | `1000` | Límite del mensaje. |
| `Asistente:MaxHistorial` | `6` | Máximo de mensajes de historial. |
| `Asistente:VerboseLogging` | `false` | Logs de detalle. |
| `Asistente:RateLimit:*` | ver appsettings | Límites por usuario/viaje. |
| `Asistente:IntentTimeoutSeconds` | `10` | Reservado para intención. |
| `Asistente:GenerationTimeoutSeconds` | `45` | Reservado para LLM. |

## Rate limiting

- `asistente-usuario`: usado por el endpoint de capacidades.
- `asistente-viaje`: usado por consulta y aplicar.

Al superar el límite:

```http
HTTP/1.1 429 Too Many Requests
Retry-After: 60
Content-Type: application/json

{ "mensaje": "Demasiadas consultas al asistente. Intentá de nuevo en unos segundos." }
```

El límite es por instancia. Para escalado horizontal habrá que usar un almacenamiento distribuido.

## Telemetría

`IAsistenteTelemetry` registra:

- duración de etapas (`retrieval`, `total`);
- resultado (`ok`, `fallback`, `sin_resultados`);
- cantidad de recomendaciones;
- detalle solo con `Asistente:VerboseLogging`.

No se loguean mensajes, prompts ni descripciones completas por defecto.

## Próximas fases

1. Detección de intención y parada objetivo.
2. Ollama con structured outputs.
3. Frontend del chat.
4. Endurecimiento, rate limiting distribuido y pruebas de carrera con Postgres real.
