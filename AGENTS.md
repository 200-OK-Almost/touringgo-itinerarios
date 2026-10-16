# AGENTS.md — Microservicio Itinerarios

Contexto para trabajar con opencode en `turinggo-itinerarios`. Asistente de planificación urbana y salidas grupales en CABA (recorridos a pie de 3-4 paradas por barrio, con cuadras y minutos entre paradas). Modos: Solo, Familia, Amigos. La IA (Gemini) solo traduce elecciones en paradas; no hay chatbot.

## Stack y arquitectura

- .NET 8, ASP.NET Core Web API, EF Core Code-First, PostgreSQL. Frontend y gateway viven en otros repos.
- Solución `Itinerario.sln`, 4 proyectos bajo `src/` (Clean Architecture) + 1 de tests. Namespaces raíz `Itinerario.*` (ojo: la DB y el repo son `itinerarios`, el código `Itinerario`).
  - `Itinerario.API` → entrypoint (`Program.cs`), `Controllers/` (`ViajesController`), `Filters/` (`ValidationFilter`), `Services/` (`CurrentUser`), `Extensions/GlobalExceptionHandler.cs`.
  - `Itinerario.Application` → `DTOs/`, `Interfaces/`, `Services/`, `Validators/` (FluentValidation) y `ResultadoPaginado<T>`; registro en `DependencyInjection.AddApplication`.
  - `Itinerario.Domain` → `Entities/` (incl. `DatosViaje`), `Enums/`, `Exceptions/` (sin dependencias).
  - `Itinerario.Infrastructure` → `Data/ApplicationDbContext.cs`, `Data/Configurations/` (`IEntityTypeConfiguration`), `Repositories/`, `Clients/`, `Data/Migrations/`; registro en `DependencyInjection.AddInfrastructure`.
  - `tests/Itinerario.UnitTests` → xUnit, referencia a `Itinerario.Application`.
- Dirección de dependencias: `API → Application + Infrastructure`; `Infrastructure → Application + Domain`; `Application → Domain`.
- Base de datos: `itinerarios_db` (database-per-service).
- Endpoints: `POST /api/viajes`, `GET /api/viajes?pagina&tamano`, `GET /api/viajes/{id}`, `PUT /api/viajes/{id}`, `DELETE /api/viajes/{id}`, `POST /api/viajes/unirse`.

## Reglas de oro (microservicios)

1. **Nunca FK ni navegación hacia tablas de otro servicio.** Solo se guarda el id (`Guid` / `short`).
2. **Nada de `JOIN`/`Include()` entre servicios.** Datos externos solo por REST (`Infrastructure/Clients/`, hoy `LugaresClient` es un stub sin HTTP).
3. No tocar la base ni el código de otro repo.
4. Nunca commitear secretos reales (`.env` está ignorado).

## Comandos

```powershell
# Build, run y tests (desde la raíz del repo)
dotnet build Itinerario.sln
dotnet test Itinerario.sln
dotnet run --project src/Itinerario.API      # Swagger en /swagger; perfil http en http://localhost:5256

# Infraestructura (usa compose.yaml: servicios api + postgres)
docker compose up -d postgres                # solo la base para desarrollo local
docker compose down -v                       # ojo: -v borra datos

# EF Core — siempre indicar proyecto y startup; migraciones en Data/Migrations
dotnet ef migrations add <Nombre> --project src/Itinerario.Infrastructure --startup-project src/Itinerario.API --output-dir Data/Migrations
dotnet ef database update --project src/Itinerario.Infrastructure --startup-project src/Itinerario.API
```

## Gotchas verificados

- El comando `dotnet ef` requiere la herramienta global `dotnet-ef` (8.x): `dotnet tool install --global dotnet-ef --version "8.*"`. No viene con el build.
- La entidad `Itinerario` **colisiona con el namespace raíz** `Itinerario`: donde se la referencia (Infrastructure/Application) hay que usar alias o nombre completo (p. ej. `using ItinerarioEntity = Itinerario.Domain.Entities.Itinerario;`), como en `ApplicationDbContext`.
- **Identidad sin JWT**: `ICurrentUser` (`API/Services/CurrentUser`) lee el claim `sub`; si no hay, cae al header **`X-Usuario-Id`** (dev); si tampoco, 401. Reemplazar el fallback cuando exista Usuarios. Los endpoints que crean/modifican requieren usuario.
- **Enums en JSON**: `System.Text.Json` no acepta nombres de enum por defecto; se agregó `JsonStringEnumConverter` en `AddJsonOptions`, así que el contrato usa textos (`"Solo"`, `"Palermo"`).
- **Auto-validado**: el `ValidationFilter` global resuelve `IValidator<T>` de DI y devuelve **400** con `ModelState`; los validadores viven en `Application/Validators/` y se registran con `AddValidatorsFromAssembly`.
- La API **no aplica migraciones al arrancar**; correr `dotnet ef database update` a mano. Migraciones: `InitialCreate` (6 tablas, enums `text`) y `ViajeXminConcurrencyToken` (**sin DDL**: `xmin` es columna de sistema, solo sincroniza el snapshot).
- **Concurrencia**: `Viaje` usa `xmin` como rowversion; `DbUpdateConcurrencyException` → `ConflictoDeConcurrenciaException` → **409**. Difícil de reproducir con requests secuenciales.
- **Código de invitación**: el repo traduce la violación Postgres **23505** del constraint `IX_viajes_codigo_invitacion` y `ViajeService.CrearAsync` reintenta la generación.
- Los enums se persisten como `text`. Ver `docs/modelo-datos.md` para el DER completo (tablas, índices, enums, referencias externas).
- `GeoCalculator`, `ItinerarioGenerator` y `Viaje` (dominio) son clases puras/testeables, sin acceso a DB ni HTTP.
- Connection string local: `Host=localhost;Port=5432;Database=itinerarios_db;Username=postgres;Password=postgres` (`docker compose` expone postgres en 5432).

## Git

- `main` es estable. Trabajar en `feature/<tarea>`.
- Commits convencionales: `feat(itinerarios): ...`, `fix(itinerarios): ...`, `test(itinerarios): ...`, `refactor(itinerarios): ...`, `chore: ...`.

## Estado actual

Hecho (fases 1-4): dominio (enums + entidades + comportamiento), `ApplicationDbContext` con mapeo `snake_case` y migraciones, DTOs + validadores, servicios (`GeoCalculator`, `ItinerarioGenerator`, `ViajeService` con permisos y paginación), `ViajeRepository` EF, `LugaresClient` stub, `ViajesController` (CRUD + unirse), validación automática, identidad, 23505 + reintento, concurrencia `xmin` → 409, y tests de `GeoCalculator` (15).

Pendiente: endpoint de **generación de itinerario** (reglas + `ILugaresClient`), refinamientos de un clic (*más económico*, *reducir caminata*, *más aire libre*), sugerencias de turno libre, integración real con Usuarios/Lugares por REST, JWT real (reemplazar `X-Usuario-Id`) y Gemini.

Antes de dar una tarea por terminada: `dotnet build Itinerario.sln` sin errores y `dotnet test` en verde.
