# AGENTS.md — Microservicio Itinerarios

Contexto para trabajar con opencode en `turinggo-itinerarios`. Asistente de planificación urbana y salidas grupales en CABA (recorridos a pie de 3-4 paradas por barrio, con cuadras y minutos entre paradas). Modos: Solo, Familia, Amigos. La IA (Gemini) solo traduce elecciones en paradas; no hay chatbot.

## Stack y arquitectura

- .NET 8, ASP.NET Core Web API, EF Core Code-First, PostgreSQL. Frontend y gateway viven en otros repos.
- Solución `Itinerario.sln`, 4 proyectos bajo `src/` (Clean Architecture) + 1 de tests. Namespaces raíz `Itinerario.*` (ojo: la DB y el repo son `itinerarios`, el código `Itinerario`).
  - `Itinerario.API` → entrypoint (`Program.cs`, `Controllers/`, `Extensions/GlobalExceptionHandler.cs`).
  - `Itinerario.Application` → `DTOs/`, `Interfaces/`, `Services/`, `Validators/` (FluentValidation) y `ResultadoPaginado<T>`; registro en `DependencyInjection.AddApplication`.
  - `Itinerario.Domain` → `Entities/` (incl. `DatosViaje`), `Enums/`, `Exceptions/` (sin dependencias).
  - `Itinerario.Infrastructure` → `Data/ApplicationDbContext.cs`, `Data/Configurations/` (`IEntityTypeConfiguration`), `Repositories/`, `Data/Migrations/`; registro en `DependencyInjection.AddInfrastructure`.
  - `tests/Itinerario.UnitTests` → xUnit, referencia a `Itinerario.Application`.
- Dirección de dependencias: `API → Application + Infrastructure`; `Infrastructure → Application + Domain`; `Application → Domain`.
- Base de datos: `itinerarios_db` (database-per-service).

## Reglas de oro (microservicios)

1. **Nunca FK ni navegación hacia tablas de otro servicio.** Solo se guarda el id (`Guid` / `short`).
2. **Nada de `JOIN`/`Include()` entre servicios.** Datos externos solo por REST (`Clients/`, aún no creado).
3. No tocar la base ni el código de otro repo.
4. Nunca commitear secretos reales (`.env` está ignorado).

## Comandos

```powershell
# Build, run y tests (desde la raíz del repo)
dotnet build Itinerario.sln
dotnet test Itinerario.sln
dotnet run --project src/Itinerario.API      # Swagger en /swagger; perfil http en http://localhost:5256

# Infraestructura (usa compose.yaml: servicios api + postgres)
docker compose up -d
docker compose down -v

# EF Core — siempre indicar proyecto y startup; migraciones en Data/Migrations
dotnet ef migrations add <Nombre> --project src/Itinerario.Infrastructure --startup-project src/Itinerario.API --output-dir Data/Migrations
dotnet ef database update --project src/Itinerario.Infrastructure --startup-project src/Itinerario.API
```

## Gotchas verificados

- El comando `dotnet ef` requiere la herramienta global `dotnet-ef` (8.x): `dotnet tool install --global dotnet-ef --version "8.*"`. No viene con el build.
- La entidad `Itinerario` **colisiona con el namespace raíz** `Itinerario`: donde se la referencia (Infrastructure/Application) hay que usar alias o nombre completo (p. ej. `using ItinerarioEntity = Itinerario.Domain.Entities.Itinerario;`), como en `ApplicationDbContext`.
- `appsettings.Development.json` **está versionado** y apunta a `itinerarios_db` (alineado con `appsettings.json`). El perfil http corre con `ASPNETCORE_ENVIRONMENT=Development`.
- La API **no aplica migraciones al arrancar**; hay que correr `dotnet ef database update` a mano. Ya existe `InitialCreate` (6 tablas, sin FK a usuarios/lugares, enums `text`).
- Los enums se persisten como `text`. Ver `docs/modelo-datos.md` para el DER completo (tablas, índices, enums, referencias externas) y sus advertencias.
- Los validadores de FluentValidation existen (`Application/Validators/`) y se registran con `AddValidatorsFromAssembly`, pero el **auto-validado en ASP.NET (400 con `ModelState`) todavía no está cableado** (pendiente Fase 4).
- `GeoCalculator`, `ItinerarioGenerator` y `Viaje` (dominio) son clases puras/testeables, sin acceso a DB ni HTTP.
- Connection string local: `Host=localhost;Port=5432;Database=itinerarios_db;Username=postgres;Password=postgres` (`docker compose` expone postgres en 5432).

## Git

- `main` es estable. Trabajar en `feature/<tarea>`.
- Commits convencionales: `feat(itinerarios): ...`, `fix(itinerarios): ...`, `test(itinerarios): ...`, `chore: ...`.

## Estado actual

Hecho: dominio (enums + entidades + comportamiento), `ApplicationDbContext` con mapeo `snake_case`, migración `InitialCreate`, DTOs + validadores, servicios (`GeoCalculator`, `ItinerarioGenerator`, `ViajeService` con permisos y paginación), DI y tests de `GeoCalculator`.

Pendiente (Fase 4, rama `feature/crud-viajes`): implementación EF de `IViajeRepository` e `ILugaresClient`, controllers/CRUD de viajes y `/api/viajes/unirse`, auto-validado de FluentValidation, traducción de código de invitación duplicado (Postgres 23505) + reintento, token de concurrencia (`UseXminAsConcurrencyToken()` → 409) y tomar `CreadoPorUsuarioId`/`usuarioIdSolicitante` del claim `sub`.

Antes de dar una tarea por terminada: `dotnet build Itinerario.sln` sin errores y `dotnet test` en verde.
