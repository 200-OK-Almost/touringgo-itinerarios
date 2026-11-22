# Asistente RAG — Fases 0 a 3

El asistente tiene implementado el corte vertical completo sin streaming:

- contratos, configuración y endpoint de capacidades;
- rate limiting y telemetría;
- recuperación de lugares desde Lugares;
- recomendaciones deterministas;
- detección de intención por reglas;
- resolución de parada objetivo;
- query limpia, filtros duros y radio;
- aclaraciones;
- propuesta de acción;
- aplicación de acciones con concurrencia por `xmin`;
- Ollama opcional para intención y generación de mensaje;
- fallback total al flujo determinista.

El frontend consume los mismos contratos de Fase 2. No requiere cambios por la Fase 3.

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
  "mensaje": "Reemplazá el museo por algo al aire libre",
  "numeroDia": 1,
  "historial": []
}
```

Response:

```json
{
  "mensaje": "Encontré 2 lugares para reemplazar “Museo de Arte”.",
  "puedeAplicar": true,
  "recomendaciones": [
    {
      "lugarId": "uuid",
      "nombre": "Parque",
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
      "esAireLibre": true,
      "esBajoTecho": false,
      "aptoNinos": true,
      "accesible": true,
      "fotoPortadaUrl": "https://...",
      "categorias": ["parques_aire_libre"],
      "opcionesDietarias": [],
      "similitud": 0.82,
      "motivo": "Coincide con parques_aire_libre."
    }
  ],
  "accion": {
    "tipo": "ReemplazarParada",
    "numeroDia": 1,
    "lugarId": "uuid-del-primero",
    "paradaId": "uuid-del-museo",
    "turno": null,
    "version": "xmin-opaco"
  },
  "aclaracion": null,
  "advertencia": null
}
```

Reglas:

- cualquier miembro del viaje puede consultar;
- si el usuario no es miembro, responde `404`;
- si el asistente está deshabilitado, responde `404`;
- si la búsqueda semántica falla o devuelve pocos resultados, usa el catálogo filtrado y devuelve `advertencia`;
- no aplica cambios en esta llamada;
- `accion` solo se completa si el usuario es administrador, hay una recomendación, el itinerario tiene `version` y la intención es `Agregar` o `Reemplazar`.

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

## Flujo de consulta

1. Validar miembro y día.
2. Cargar barrio, categorías, presupuesto, parámetros familiares e itinerario.
3. Hidratar las paradas actuales con `ObtenerPorIdAsync`.
4. Interpretar la intención con reglas.
5. Detectar si la intención es débil.
6. Si `Asistente:LlmEnabled=true` y la intención es débil, consultar Ollama.
7. Sanees la salida del LLM y usarla solo si es válida.
8. Si hay aclaración, responder sin consultar Lugares.
9. Construir la búsqueda semántica con query limpia y filtros combinados.
10. Consultar Lugares.
11. Si hay pocos resultados, usar `BuscarAsync` como fallback.
12. Mapear recomendaciones y construir `Accion` si corresponde.
13. Si `Asistente:LlmGenerarMensaje=true`, generar el mensaje.
14. Registrar telemetría y devolver.

## Intención determinista

La interpretación base vive en `IIntencionService`.

### Acciones

| Acción | Ejemplos |
|---|---|
| `Recomendar` | “¿Qué lugar me recomendás?”, “Sugerime opciones”. |
| `Agregar` | “Agregá un café”, “Sumá un parque”. |
| `Reemplazar` | “Reemplazá el museo”, “Cambiá la primera parada”. |

Prioridad:

1. reemplazo;
2. consulta o recomendación;
3. agregar;
4. recomendar por defecto.

### Normalización

El texto se pasa a minúsculas, sin tildes, sin puntuación y con espacios colapsados.

### Parada objetivo

Solo se resuelve para `Reemplazar`.

- Por nombre: “el museo”.
- Por categoría: museo, café, bar, parque, librería, restaurante, coctelería.
- Por orden: “la primera”, “la segunda”, “la última”.
- Por frase:
  - `reemplazá X por Y` → objetivo = `X`;
  - `en vez de X` / `en lugar de X` → objetivo = `X`.

Si hay ambigüedad, se devuelve `Aclaracion`:

```json
{
  "mensaje": "¿A cuál parada te referís?",
  "puedeAplicar": true,
  "recomendaciones": [],
  "accion": null,
  "aclaracion": {
    "pregunta": "¿A cuál parada te referís?",
    "opciones": [
      { "id": "uuid-parada-1", "etiqueta": "Café Uno (Mañana)" },
      { "id": "uuid-parada-2", "etiqueta": "Café Dos (Tarde)" }
    ]
  },
  "advertencia": null
}
```

Reglas:

- no se consulta Lugares;
- no se devuelven recomendaciones;
- no se propone acción;
- si no se encuentra el objetivo, se ofrecen todas las paradas del día;
- si no hay paradas, no se devuelve aclaración.

### Query limpia y filtros

La query del embedding no incluye:

- la acción;
- el lugar que se quiere reemplazar;
- el barrio;
- el presupuesto;
- IDs;
- filtros ya expresados como parámetros.

Ejemplo:

```text
"Reemplazá el museo por algo al aire libre"
→ querySemantica = "aire libre"
```

Filtros extraídos:

| Filtro | Ejemplos |
|---|---|
| Categorías | café, museo, parque, librería, bar, restaurante, coctelería. |
| Aire libre / bajo techo | “al aire libre”, “parque”, “bajo techo”, “lluvia”. |
| Apto niños | “niños”, “familia”, “chicos”, “infantil”. |
| Accesibilidad | “accesible”, “silla de ruedas”, “movilidad reducida”. |
| Dieta | “vegano”, “vegetariano”, “sin gluten”, “sin tacc”, “sin lactosa”, “kosher”, “halal”. |
| Turno | mañana, mediodía, tarde, noche. |
| Precio | “económico”, “barato” → 1; “moderado”, “equilibrado” → 2. |
| Radio | “cerca”, “cercano”, “próximo”, “a pocas cuadras”, “caminando”. |

Combinación con el viaje:

- si la intención trae categorías, reemplazan a las del viaje;
- si no trae, se usan las del día o del viaje;
- el precio máximo es el mínimo entre la intención y el presupuesto del viaje;
- niños y accesibilidad se activan si vienen en la intención o en los parámetros familiares;
- el radio usa como referencia la parada objetivo; si no hay, la primera parada.

## Ollama (Fase 3)

Ollama es opcional. El flujo determinista sigue funcionando sin él.

### Flags

| Clave | Default | Descripción |
|---|---|---|
| `Asistente:LlmEnabled` | `false` | Activa el fallback de intención con Ollama. |
| `Asistente:LlmGenerarMensaje` | `false` | Activa la generación del mensaje. |

En Docker:

```yaml
Asistente__LlmEnabled: "true"
Asistente__LlmGenerarMensaje: "true"
```

Solo habilitar si Ollama está disponible. Si no responde, se usa el fallback.

### Intención con LLM

`IIntencionLlmService` se llama solo cuando:

- `LlmEnabled=true`;
- no hay aclaración pendiente;
- la intención de reglas es débil.

Casos débiles:

- query `"lugar"`;
- sin acción, objetivo ni filtros;
- `Reemplazar` sin parada objetivo.

Contrato esperado:

```json
{
  "accion": "Recomendar|Agregar|Reemplazar",
  "querySemantica": "aire libre",
  "paradaObjetivoId": "uuid-o-null",
  "categorias": ["parques_aire_libre"],
  "soloAlAireLibre": true,
  "soloBajoTecho": null,
  "aptoNinos": null,
  "accesibleMovilidadReducida": null,
  "restriccionDietaria": null,
  "turno": "Manana|Mediodia|Tarde|Noche|null",
  "nivelPrecioMaximo": null,
  "radioMetros": 800
}
```

El prompt:

- incluye mensaje, día, barrio, presupuesto, familia y paradas actuales;
- incluye la whitelist de categorías;
- prohíbe inventar IDs o lugares;
- trata el mensaje del usuario como dato, no como instrucción;
- pide JSON sin markdown.

Se usa `json_object` y validación posterior. No se depende de `json_schema` estricto.

### Saneamiento

`IntencionLlmSanitizer` valida y normaliza:

- acción válida;
- categorías contra whitelist;
- `paradaObjetivoId` solo si pertenece a las paradas actuales;
- query recortada a 200 caracteres; vacía → `"lugar"`;
- dieta recortada a 50 caracteres;
- turno válido;
- precio entre 1 y 3;
- radio entre 100 y 5000 metros;
- conflicto aire libre/bajo techo → prioriza bajo techo;
- `Reemplazar` sin parada válida → salida inválida.

Si la salida no es usable, se conserva la intención determinista.

### Generación de mensaje

`IMensajeLlmService` se llama solo cuando:

- `LlmGenerarMensaje=true`;
- hay recomendaciones;
- no hay aclaración.

Contrato esperado:

```json
{ "mensaje": "texto de respuesta" }
```

Reglas:

- español rioplatense;
- máximo 400 caracteres en el prompt y 500 al sanear;
- sin markdown;
- sin inventar lugares ni datos;
- sin decir que aplicó cambios.

Si falla, se usa el mensaje determinista actual.

### Fallback

| Situación | Resultado |
|---|---|
| Flag deshabilitado | No se llama al modelo. |
| Ollama no disponible | Se usa la intención o el mensaje determinista. |
| Timeout de intención | Se usan las reglas. |
| Timeout de generación | Se usa el mensaje determinista. |
| JSON inválido | Se usa el fallback. |
| IDs o categorías inválidas | Se descartan o se usa el fallback. |
| Cancelación del request | Se propaga; no se oculta. |

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
| `Asistente:Enabled` | `false` | Feature flag general. En Development está en `true`. |
| `Asistente:LlmEnabled` | `false` | Habilita intención con Ollama. |
| `Asistente:LlmGenerarMensaje` | `false` | Habilita generación del mensaje. |
| `Asistente:TopK` | `12` | Cantidad máxima de lugares a recuperar. |
| `Asistente:MinResultados` | `3` | Umbral para activar fallback. |
| `Asistente:SimilitudMinima` | `null` | Sin umbral hasta calibrar. |
| `Asistente:RadioMetrosDefault` | `800` | Radio para consultas de cercanía. |
| `Asistente:MaxMensajeCaracteres` | `1000` | Límite del mensaje de entrada. |
| `Asistente:MaxHistorial` | `6` | Máximo de mensajes de historial. |
| `Asistente:IntentTimeoutSeconds` | `10` | Timeout de intención con LLM. |
| `Asistente:GenerationTimeoutSeconds` | `45` | Timeout de generación de mensaje. |
| `Asistente:VerboseLogging` | `false` | Logs de detalle. |
| `Asistente:RateLimit:*` | ver appsettings | Límites por usuario/viaje. |
| `Ollama:BaseUrl` | `http://localhost:11434` | Endpoint compatible con OpenAI. |
| `Ollama:Model` | `llama3.2` | Modelo a usar. |

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

- etapas: `retrieval`, `intent_llm`, `generation_llm`, `total`;
- resultados finales: `ok`, `fallback`, `sin_resultados`, `aclaracion`;
- resultados de LLM: `llm_ok`, `llm_error`, `llm_timeout`;
- cantidad de recomendaciones;
- detalle solo con `Asistente:VerboseLogging`.

No se loguean mensajes, prompts ni descripciones completas por defecto.

## Tests

```powershell
dotnet build Itinerario.sln
dotnet test Itinerario.sln
```

Cobertura principal:

- normalización y detección de acciones;
- resolución por nombre, categoría, orden y frases;
- ambigüedad y aclaraciones;
- query limpia, filtros y radio;
- integración con `AsistenteService`;
- propuesta de acción, permisos y versión;
- fallback con filtros;
- opciones y contratos del LLM;
- prompt de intención y saneamiento;
- timeout, JSON inválido y servicio caído;
- reglas fuertes vs intención débil;
- generación de mensaje y fallback determinista.

## Limitaciones actuales

- La intención determinista es por reglas; no interpreta lenguaje libre complejo.
- Ollama es opcional y está deshabilitado por defecto.
- No se usa el historial para resolver seguimientos de aclaraciones.
- Si dos paradas tienen el mismo nombre y solo cambia el turno, la resolución puede seguir siendo ambigua.
- No hay `json_schema` estricto, streaming ni tool calling.
- El historial no se persiste.

## Próxima fase

Fase 4: endurecimiento.

- rate limiting distribuido;
- pruebas de concurrencia con Postgres real;
- métricas y trazas;
- calibración de similitud;
- validaciones y límites de payload;
- smoke y carga E2E.
