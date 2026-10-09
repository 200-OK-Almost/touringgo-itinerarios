# TuringGO — Microservicio Itinerarios

Asistente de planificación urbana y salidas grupales en CABA. Este README describe el **microservicio Itinerarios**, que vive en su propio repositorio (`turinggo-itinerarios`), y sirve de contexto para trabajar desde la terminal con un agente de IA (opencode).

> Tip: opencode lee automáticamente un archivo `AGENTS.md` en la raíz. Copiá este contenido a `AGENTS.md` (o corré `/init` en opencode y pegá este contenido encima) para que el agente lo tenga siempre presente.

---

## 1. Contexto del proyecto

* Web app responsiva que arma recorridos a pie de 3 o 4 paradas por barrio, mostrando cuadras y minutos de caminata entre paradas.
* **No hay chatbot**: el usuario usa solo botones, tarjetas y etiquetas. La IA (Gemini) traduce esas elecciones en paradas de itinerario.
* Barrios: Palermo, Recoleta, San Telmo, San Nicolás, Puerto Madero.
* Modos: Solo, Familia (organizador representante) y Amigos (colaborativo + gastos).
* Stack: C# / .NET 8, ASP.NET Core Web API, Entity Framework Core (Code-First), PostgreSQL, YARP (gateway), Docker. Frontend en HTML5/CSS3/JS vanilla + Bootstrap 5 + Leaflet.

## 2. Arquitectura

Cada microservicio tiene **su propio repositorio**. Este repo (`turinggo-itinerarios`) contiene únicamente el servicio Itinerarios:

| Repo / componente | Responsabilidad |
|---|---|
| `turinggo-usuarios` | Google Auth, JWT, perfil, preferencias |
| **`turinggo-itinerarios`** (este repo) | Viajes, itinerarios, paradas, Haversine, generación con IA |
| `turinggo-lugares` | Catálogo, Google Places con caché |
| Gateway (YARP) | Punto único de entrada, CORS y ruteo |
| Frontend | HTML5/CSS3/JS + Bootstrap 5 + Leaflet |

Los demás servicios **no están en este repo**: se consumen por REST. Los contratos entre ellos están en la sección 9.

Cada servicio tiene **su propia base de datos** (database per service):

|Servicio|Base|
|-|-|
|Usuarios|`usuarios_db`|
|Itinerarios|`itinerarios_db`|
|Lugares|`lugares_db`|

### Reglas de oro

1. **Nunca hay FK ni propiedad de navegación hacia tablas de otro servicio.** Solo se guarda el id (`Guid` / `short`).
2. **Nada de `JOIN` ni `Include()` entre servicios.** Si hace falta un dato de otro servicio, se pide por REST.
3. Los servicios no tocan la base de otro ni el código de otro repo.
4. Secretos fuera del repo (`.env`, `appsettings.Development.json` ignorado). Nunca commitear claves.

## 3. Responsabilidades de Itinerarios

* CRUD de viajes, parámetros de familia, etiquetas ("vibras") y miembros.
* Código de invitación y unirse a un viaje (Modo Amigos).
* Configuración del punto de partida (hospedaje propio, sugerido, nodo urbano, primera parada).
* Cálculo con **Haversine**: distancia, cuadras y minutos a pie entre paradas consecutivas.

  * 1 cuadra ≈ 100 m. Velocidad de caminata ≈ 80 m/min.
* Generación del itinerario (primero por reglas, después con Gemini sobre la misma interfaz).
* Refinamientos de un clic: *más económico*, *reducir caminata*, *más aire libre*.
* Sugerencias para turnos libres (3 opciones a menos de 5 cuadras).

## 4. Modelo de datos (tablas de este servicio)

`viajes`, `parametros_familia`, `miembros_viaje`, `etiquetas_viaje`, `itinerarios`, `paradas_itinerario`.

### Detalle de las 6 tablas

Fuente: DER del proyecto. Convenciones: tipos de PostgreSQL, columnas en `snake_case`, enums guardados como `text`. **PK** = clave primaria, **FK** = clave foránea (solo dentro de este servicio), **EXT** = id que vive en otro servicio (sin FK).

> La nulabilidad (`NOT NULL` / `NULL`) no figura en el DER: es una propuesta razonable. Ajustala si el equipo decide otra cosa.

#### 4.1 `viajes`

Tabla central. Un viaje es una salida planificada por un usuario.

| Columna | Tipo | Nulo | Clave / Notas |
|---|---|---|---|
| `id` | uuid | NO | **PK** |
| `titulo` | varchar(100) | NO | Nombre de la salida |
| `tipo_viaje` | enum `tipo_viaje` (text) | NO | `solo`, `familia`, `amigos` |
| `estado` | enum `estado_viaje` (text) | NO | `borrador` (default), `planificado`, `finalizado` |
| `barrio` | enum `barrio_caba` (text) | NO | `palermo`, `recoleta`, `san_telmo`, `san_nicolas`, `puerto_madero` |
| `alcance_temporal` | enum `alcance_temporal` (text) | NO | `dia_completo`, `tarde_noche`, `manana_almuerzo` |
| `fecha_inicio` | date | SÍ | |
| `fecha_fin` | date | SÍ | |
| `nivel_presupuesto` | enum `nivel_presupuesto` (text) | NO | `economico`, `equilibrado`, `sin_restricciones` |
| `ritmo_caminata` | enum `ritmo_caminata` (text) | NO | En un viaje solo `tranquilo` o `activo` |
| `limite_cuadras_entre_paradas` | smallint | SÍ | Tope de cuadras entre paradas (depende del ritmo) |
| `tipo_punto_partida` | enum `tipo_punto_partida` (text) | NO | `hospedaje_propio`, `hospedaje_sugerido`, `nodo_urbano`, `primera_parada` |
| `lugar_partida_id` | uuid | SÍ | **EXT** → Lugares. Usado en hospedaje sugerido / nodo urbano |
| `nombre_punto_partida` | varchar(150) | SÍ | Nombre del hotel o nodo |
| `direccion_partida` | varchar(200) | SÍ | Dirección ingresada (hospedaje propio) |
| `latitud_partida` | decimal(10,8) | SÍ | |
| `longitud_partida` | decimal(11,8) | SÍ | |
| `codigo_invitacion` | varchar(12) | NO | **UNIQUE**. Para unirse en Modo Amigos |
| `creado_por_usuario_id` | uuid | NO | **EXT** → Usuarios. Índice recomendado |
| `fecha_creacion` | timestamptz | NO | |
| `fecha_actualizacion` | timestamptz | NO | |

Relaciones: 1 viaje → 0..1 `parametros_familia`, → N `miembros_viaje`, → N `etiquetas_viaje`, → N `itinerarios`.

#### 4.2 `parametros_familia`

Restricciones de los acompañantes en Modo Familia. Relación 1 a 1 con `viajes`.

| Columna | Tipo | Nulo | Clave / Notas |
|---|---|---|---|
| `viaje_id` | uuid | NO | **PK** y **FK** → `viajes.id` (CASCADE) |
| `cantidad_acompanantes` | smallint | NO | |
| `hay_ninos` | boolean | NO | |
| `cantidad_ninos` | smallint | NO | 0 si `hay_ninos` es falso |
| `movilidad_reducida` | boolean | NO | |
| `requiere_ritmo_bajo` | boolean | NO | |
| `requiere_menu_infantil` | boolean | NO | |

Solo existe para viajes de tipo `familia`.

#### 4.3 `miembros_viaje`

Quién participa de cada viaje (tabla de unión viaje ↔ usuario).

| Columna | Tipo | Nulo | Clave / Notas |
|---|---|---|---|
| `viaje_id` | uuid | NO | **PK (compuesta)** y **FK** → `viajes.id` (CASCADE) |
| `usuario_id` | uuid | NO | **PK (compuesta)**. **EXT** → Usuarios. Índice recomendado |
| `rol` | enum `rol_miembro` (text) | NO | `administrador` (creador), `miembro` (default) |
| `fecha_incorporacion` | timestamptz | NO | |

Al crear un viaje, el creador se inserta como `administrador`.

#### 4.4 `etiquetas_viaje`

Las "vibras" elegidas para el viaje (entre 2 y 4 por viaje). Tabla de unión viaje ↔ categoría de interés.

| Columna | Tipo | Nulo | Clave / Notas |
|---|---|---|---|
| `viaje_id` | uuid | NO | **PK (compuesta)** y **FK** → `viajes.id` (CASCADE) |
| `categoria_id` | smallint | NO | **PK (compuesta)**. **EXT** → Usuarios (`categorias_interes.id`) |

La regla de 2 a 4 etiquetas se valida en el servicio/DTO, no en la base.

#### 4.5 `itinerarios`

Un itinerario por día del viaje.

| Columna | Tipo | Nulo | Clave / Notas |
|---|---|---|---|
| `id` | uuid | NO | **PK** |
| `viaje_id` | uuid | NO | **FK** → `viajes.id` (CASCADE) |
| `numero_dia` | smallint | NO | **UNIQUE** junto con `viaje_id` |
| `fecha` | date | SÍ | |
| `origen_tipo` | enum `tipo_punto_partida` (text) | NO | Origen de ese día |
| `origen_descripcion` | varchar(150) | SÍ | |
| `origen_latitud` | decimal(10,8) | SÍ | |
| `origen_longitud` | decimal(11,8) | SÍ | |
| `fecha_creacion` | timestamptz | NO | |
| `fecha_actualizacion` | timestamptz | NO | |

Relaciones: 1 itinerario → N `paradas_itinerario`.

#### 4.6 `paradas_itinerario`

Cada parada del recorrido, con su distancia a la parada anterior (calculada con Haversine).

| Columna | Tipo | Nulo | Clave / Notas |
|---|---|---|---|
| `id` | uuid | NO | **PK** |
| `itinerario_id` | uuid | NO | **FK** → `itinerarios.id` (CASCADE) |
| `lugar_id` | uuid | NO | **EXT** → Lugares |
| `turno` | enum `turno` (text) | NO | `manana`, `tarde`, `noche` |
| `orden_parada` | smallint | NO | Orden dentro del turno. Índice `(itinerario_id, turno, orden_parada)` |
| `hora_inicio` | time | SÍ | |
| `duracion_minutos` | smallint | SÍ | |
| `distancia_metros_desde_anterior` | int | SÍ | Haversine. Nulo en la primera parada si parte del origen |
| `cuadras_desde_anterior` | smallint | SÍ | 1 cuadra ≈ 100 m |
| `minutos_a_pie_desde_anterior` | smallint | SÍ | ≈ 80 m/min |
| `origen_parada` | enum `origen_parada` (text) | NO | `ia`, `refinamiento`, `sugerencia_hueco`, `manual` |
| `sugerencia_ia` | varchar(255) | SÍ | Texto corto generado por Gemini |
| `notas_personales` | varchar(500) | SÍ | Notas del usuario |
| `fecha_creacion` | timestamptz | NO | |

#### 4.7 Diagrama de relaciones del servicio

```
viajes 1 ──── 0..1 parametros_familia
viajes 1 ──── N    miembros_viaje        (usuario_id → EXT Usuarios)
viajes 1 ──── N    etiquetas_viaje       (categoria_id → EXT Usuarios)
viajes 1 ──── N    itinerarios
itinerarios 1 ── N paradas_itinerario    (lugar_id → EXT Lugares)
```

Todas las FK son internas a la base `itinerarios_db`. Las columnas **EXT** son solo ids, sin FK.

#### 4.8 Resumen de índices y restricciones

| Tabla | Restricción / índice |
|---|---|
| `viajes` | UNIQUE `codigo_invitacion`; índice en `creado_por_usuario_id` |
| `parametros_familia` | PK = FK `viaje_id` |
| `miembros_viaje` | PK `(viaje_id, usuario_id)`; índice en `usuario_id` |
| `etiquetas_viaje` | PK `(viaje_id, categoria_id)` |
| `itinerarios` | UNIQUE `(viaje_id, numero_dia)` |
| `paradas_itinerario` | índice `(itinerario_id, turno, orden_parada)` |

> Valores de enum: el DER los define en `snake_case` (`san_telmo`) y el código C# los nombra en PascalCase (`SanTelmo`). Con la conversión a `text` por defecto se guarda el nombre C#. Si preferís `snake_case` en la base, hay que agregar un `ValueConverter`. Definanlo una vez y apliquenlo igual en los tres servicios.

### Referencias externas (solo id, sin FK)

|Campo|Vive en|
|-|-|
|`Viaje.CreadoPorUsuarioId`|Usuarios|
|`MiembroViaje.UsuarioId`|Usuarios|
|`EtiquetaViaje.CategoriaId`|Usuarios (`categorias_interes`)|
|`Viaje.LugarPartidaId`|Lugares|
|`ParadaItinerario.LugarId`|Lugares|

### Enums

```csharp
TipoViaje        { Solo, Familia, Amigos }
EstadoViaje      { Borrador, Planificado, Finalizado }
BarrioCaba       { Palermo, Recoleta, SanTelmo, SanNicolas, PuertoMadero }
AlcanceTemporal  { DiaCompleto, TardeNoche, MananaAlmuerzo }
NivelPresupuesto { Economico, Equilibrado, SinRestricciones }
RitmoCaminata    { Tranquilo, Moderado, Activo }   // en un viaje solo Tranquilo o Activo
TipoPuntoPartida { HospedajePropio, HospedajeSugerido, NodoUrbano, PrimeraParada }
RolMiembro       { Administrador, Miembro }
Turno            { Manana, Tarde, Noche }
OrigenParada     { Ia, Refinamiento, SugerenciaHueco, Manual }
```

En la base los enums se guardan como `text`.

## 5. Estructura del servicio

```
<carpeta-del-proyecto>/    # donde está el .csproj (raíz del repo o subcarpeta)
  Domain/
    Enums/
    Entities/
  Data/               ItinerariosDbContext
  Dtos/
  Controllers/
  Services/           GeoCalculator (Haversine), generador de itinerarios
  Clients/            Clientes REST hacia Usuarios y Lugares
  Program.cs
```

* Namespace raíz: `TuringGO.Itinerarios` (confirmar que coincide con el del `.csproj`; si difiere, usar el del `.csproj`).
* Una clase por archivo. Nombres de tablas y columnas en `snake_case` según el DER (se mapean en el `DbContext`).

## 6. Comandos útiles

```bash
# Infraestructura (desde la raíz del repo)
docker compose up -d              # levanta PostgreSQL (base itinerarios_db)
docker compose down -v            # borra volúmenes (re-ejecuta el script de init de la base)

# Desde la carpeta del proyecto (donde está el .csproj)
dotnet build
dotnet run
dotnet test

# Migraciones EF Core
dotnet ef migrations add <Nombre>
dotnet ef database update
dotnet ef migrations remove       # deshace la última migración (si no se aplicó)
```

Connection string local (en `appsettings.json`, sin secretos reales):

```
Host=localhost;Port=5432;Database=itinerarios_db;Username=turinggo;Password=turinggo
```

## 7. Flujo de trabajo con Git

* `main`: estable y protegida. `develop`: integración.
* Ramas: `feature/<tarea>`. Todo entra por Pull Request a `develop` con 1 revisión.
* Commits en formato convencional: `feat(itinerarios): ...`, `fix(itinerarios): ...`, `chore: ...`.
* Rama actual de trabajo: `feature/entidades-itinerario`.

## 8. Estado y próximos pasos

* [x] Repo `turinggo-itinerarios` y proyecto creados
* [ ] Enums y entidades según el DER
* [ ] `ItinerariosDbContext` con relaciones, largos e índices
* [ ] Migración `InitialCreate` (verificar: 6 tablas, **sin FK a usuarios/lugares**, enums como `text`)
* [ ] CRUD de viajes + DTOs + validaciones (ritmo solo `Tranquilo`/`Activo`)
* [ ] Parámetros de familia y etiquetas
* [ ] Unirse con código de invitación (`POST /api/viajes/unirse`)
* [ ] `GeoCalculator` (Haversine) con tests unitarios
* [ ] `LugaresClient` (stub con 5 lugares hardcodeados hasta que exista Lugares)
* [ ] Generador de itinerarios por reglas
* [ ] Refinamientos y sugerencias de turno libre
* [ ] Integración con Gemini
* [ ] Pedir al responsable del Gateway la ruta `/api/itinerarios/{**catch-all}` (el gateway no está en este repo)

## 9. Contratos a cerrar con el equipo

* **Usuarios:** claims del JWT (`sub`, `email`, `name`) y clave compartida de validación. Mientras tanto, `CreadoPorUsuarioId` llega por body con `// TODO: tomar del claim "sub"`.
* **Lugares:** `POST /api/lugares/batch` (lista de ids → datos de lugares) y búsqueda por barrio y categoría.
* **Códigos de categoría compartidos** entre `categorias_interes` (Usuarios) y `categorias_lugar` (Lugares), por ejemplo `cafe_especialidad`.
* **Valores de ritmo:** `Tranquilo` ⇒ máximo 4 cuadras entre paradas; definir el valor para `Activo`.

## 10. Instrucciones para el agente 

* Respetá las **reglas de oro** de la sección 2, especialmente: sin FK ni navegación entre servicios.
* Seguí el DER: nombres de tablas y columnas, largos (`varchar`) y precisiones (`decimal(10,8)` latitud, `decimal(11,8)` longitud).
* Usá .NET 8 y C# con nullable habilitado. Evitá agregar paquetes sin avisar.
* Este repo contiene solo Itinerarios: no asumas código de Usuarios, Lugares ni del gateway. Si necesitás datos de otro servicio, usá un cliente REST (`Clients/`) con stub mientras no exista. No toques `docker-compose.yml` sin pedirlo.
* Antes de dar una tarea por terminada: `dotnet build` sin errores y, si cambió el modelo, revisar la migración generada.
* Los cálculos de Haversine y el generador deben ser **clases puras y testeables**, sin acceso a base de datos ni HTTP.
* Nunca escribas secretos ni claves reales en archivos versionados.

