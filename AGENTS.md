# AGENTS.md — Microservicio Itinerarios

Contexto para trabajar con opencode en `turinggo-itinerarios`. Asistente de planificación urbana y salidas grupales en CABA (recorridos a pie de 3-4 paradas por barrio, con cuadras y minutos entre paradas). Modos: Solo, Familia, Amigos. La IA (Gemini) solo traduce elecciones en paradas; no hay chatbot.

## Stack y arquitectura

- .NET 8, ASP.NET Core Web API, EF Core Code-First, PostgreSQL. Frontend y gateway viven en otros repos.
- Solución `Itinerario.sln`, 4 proyectos bajo `src/` (Clean Architecture). Namespaces raíz `Itinerario.*` (ojo: la DB y el repo son `itinerarios`, el código `Itinerario`).
  - `Itinerario.API` → entrypoint (`Program.cs`, `Controllers/`, `Extensions/GlobalExceptionHandler.cs`).
  - `Itinerario.Application` → `DTOs/`, `Interfaces/`, `Services/` (registro en `DependencyInjection.AddApplication`).
  - `Itinerario.Domain` → `Entities/`, `Enums/`, `Exceptions/` (sin dependencias).
  - `Itinerario.Infrastructure` → `Data/ApplicationDbContext.cs`, `Repositories/`, `Migrations/` (registro en `DependencyInjection.AddInfrastructure`).
- Dirección de dependencias: `API → Application + Infrastructure`; `Infrastructure → Application + Domain`; `Application → Domain`.
- Base de datos: `itinerarios_db` (database-per-service).

## Reglas de oro (microservicios)

1. **Nunca FK ni navegación hacia tablas de otro servicio.** Solo se guarda el id (`Guid` / `short`).
2. **Nada de `JOIN`/`Include()` entre servicios.** Datos externos solo por REST (`Clients/`, aún no creado).
3. No tocar la base ni el código de otro repo.
4. Nunca commitear secretos reales (`.env` está ignorado).

## Comandos

```powershell
# Build y run (desde la raíz del repo)
dotnet build Itinerario.sln
dotnet run --project src/Itinerario.API      # Swagger en /swagger; perfil http en http://localhost:5256

# Infraestructura (usa compose.yaml: servicios api + postgres)
docker compose up -d
docker compose down -v

# EF Core — no hay migraciones todavía; siempre indicar proyecto y startup
dotnet ef migrations add <Nombre> --project src/Itinerario.Infrastructure --startup-project src/Itinerario.API
dotnet ef database update --project src/Itinerario.Infrastructure --startup-project src/Itinerario.API
```

## Gotchas verificados

- `appsettings.Development.json` **está versionado** (no ignorado) y apunta a `Database=service_db`, mientras `appsettings.json` usa `itinerarios_db`. Corregir el nombre al desarrollar: el perfil http corre con `ASPNETCORE_ENVIRONMENT=Development`.
- La API **no aplica migraciones al arrancar**; hay que correr `dotnet ef database update` a mano.
- **No existe proyecto de tests**: `dotnet test` no ejecuta nada. Haversine (`GeoCalculator`) y el generador de itinerarios deben ser clases puras y testeables, sin acceso a DB ni HTTP.
- Los enums se persisten como `text`. Ver `docs/modelo-datos.md` para el DER completo (tablas, índices, enums, referencias externas) y sus advertencias.
- Connection string local: `Host=localhost;Port=5432;Database=itinerarios_db;Username=postgres;Password=postgres` (`docker compose` expone postgres en 5432).

## Git

- `main` es estable. Trabajar en `feature/<tarea>`.
- Commits convencionales: `feat(itinerarios): ...`, `fix(itinerarios): ...`, `chore: ...`.

## Estado actual

Solo el scaffold: entidades, configuración del `ApplicationDbContext`, migración `InitialCreate`, CRUD de viajes, servicios y clientes REST están **pendientes**. Al implementar el modelo, seguir `docs/modelo-datos.md` y verificar la migración (6 tablas, sin FK a usuarios/lugares, enums como `text`). Antes de dar una tarea por terminada: `dotnet build Itinerario.sln` sin errores.
