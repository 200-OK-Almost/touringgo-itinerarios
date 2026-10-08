# Template de Microservicio .NET

Template base para crear microservicios de TouringGO utilizando .NET 8, ASP.NET Core, Entity Framework Core y PostgreSQL.

El template proporciona una estructura inicial con separación por capas, configuración de acceso a datos y soporte para ejecución mediante Docker.

## Tecnologías

* .NET 8
* ASP.NET Core Web API
* Entity Framework Core 8
* PostgreSQL
* Npgsql
* Docker
* Docker Compose
* Swagger / OpenAPI

## Estructura

```text
src/
├── Service.API/
│   ├── Controllers/
│   ├── Extensions/
│   └── Program.cs
│
├── Service.Application/
│   ├── DTOs/
│   ├── Interfaces/
│   ├── Services/
│   └── DependencyInjection.cs
│
├── Service.Domain/
│   ├── Entities/
│   ├── Enums/
│   └── Exceptions/
│
└── Service.Infrastructure/
    ├── Data/
    │   ├── Configurations/
    │   └── ApplicationDbContext.cs
    ├── Repositories/
    └── DependencyInjection.cs
```

La solución sigue las siguientes dependencias:

```text
Service.API
├── Service.Application
└── Service.Infrastructure

Service.Application
└── Service.Domain

Service.Infrastructure
├── Service.Application
└── Service.Domain

Service.Domain
└── ninguna
```

La capa `Domain` no depende de ninguna otra capa.

`Application` contiene la lógica de aplicación, DTOs e interfaces.

`Infrastructure` contiene el acceso a datos y las implementaciones de infraestructura.

`API` expone los endpoints HTTP y configura la aplicación.

---

## Crear un nuevo microservicio

Este repositorio se utiliza como GitHub Template.

Desde GitHub:

1. Seleccionar **Use this template**.
2. Crear un nuevo repositorio para el microservicio.
3. Clonar el nuevo repositorio localmente.

Por ejemplo:

```text
touringgo-usuarios
```

El template utiliza `Service` como nombre genérico. Al crear un microservicio real, se debe reemplazar por el nombre correspondiente.

Por ejemplo:

```text
Service.API
        ↓
Usuarios.API

Service.Application
        ↓
Usuarios.Application

Service.Domain
        ↓
Usuarios.Domain

Service.Infrastructure
        ↓
Usuarios.Infrastructure
```

También deben actualizarse:

* nombres de los proyectos
* namespaces
* referencias entre proyectos
* nombre de la solución
* nombre de la DLL en el `Dockerfile`
* cualquier referencia restante a `Service`

No es necesario modificar la arquitectura de capas.

## Dependencias entre proyectos

Las referencias esperadas son:

```text
Usuarios.API
 ├── Usuarios.Application
 └── Usuarios.Infrastructure

Usuarios.Application
 └── Usuarios.Domain

Usuarios.Infrastructure
 ├── Usuarios.Application
 └── Usuarios.Domain
```

No se deben agregar referencias desde `Domain` hacia otras capas.

Tampoco se debe hacer que `Application` dependa de `Infrastructure`.

---

## Base de datos

Cada microservicio debe tener su propia base de datos.

Por ejemplo:

```text
Usuarios       → usuarios_db
Lugares        → lugares_db
Itinerarios    → itinerarios_db
Preferencias   → preferencias_db
```

Esto permite mantener el límite de cada microservicio y evitar que los servicios dependan directamente de las tablas de otros servicios.

La clave de configuración puede mantenerse como:

```text
DefaultConnection
```

pero el valor debe cambiar según el microservicio.

Ejemplo:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=usuarios_db;Username=postgres;Password=postgres"
  }
}
```

La configuración utilizada dentro de Docker se encuentra en `compose.yaml` y utiliza el nombre del servicio de PostgreSQL como hostname:

```text
Host=postgres
```

Dentro de un contenedor, `localhost` hace referencia al propio contenedor, no al contenedor de PostgreSQL.

---

## Crear las entidades

Las entidades propias del microservicio deben agregarse en:

```text
src/Usuarios.Domain/Entities/
```

Por ejemplo:

```text
Entities/
└── Usuario.cs
```

Luego deben agregarse al `ApplicationDbContext`:

```csharp
public DbSet<Usuario> Usuarios => Set<Usuario>();
```

Las configuraciones específicas de Entity Framework pueden colocarse en:

```text
src/Usuarios.Infrastructure/Data/Configurations/
```

Por ejemplo:

```text
UsuarioConfiguration.cs
```

y registrarse desde `OnModelCreating`.

---

## Migraciones de Entity Framework Core

El template no contiene migraciones iniciales.

Una vez creadas las entidades del microservicio, generar la primera migración:

```powershell
dotnet ef migrations add InitialCreate `
    --project src/Usuarios.Infrastructure `
    --startup-project src/Usuarios.API `
    --output-dir Data/Migrations
```

Esto creará:

```text
Usuarios.Infrastructure/
└── Data/
    └── Migrations/
        ├── ..._InitialCreate.cs
        ├── ..._InitialCreate.Designer.cs
        └── ApplicationDbContextModelSnapshot.cs
```

Para aplicar las migraciones:

```powershell
dotnet ef database update `
    --project src/Usuarios.Infrastructure `
    --startup-project src/Usuarios.API
```

Las migraciones pertenecen al microservicio y deben versionarse junto con su código.

No deben compartirse entre microservicios.

---

## Ejecución local

Para ejecutar el microservicio directamente desde .NET:

```powershell
dotnet run --project src/Usuarios.API
```

La cadena de conexión utilizada en este caso puede apuntar a:

```text
Host=localhost
```

si PostgreSQL está ejecutándose en la máquina local.

---

## Ejecución con Docker

Para construir la imagen:

```powershell
docker build -t usuarios-service .
```

Para ejecutar el contenedor:

```powershell
docker run --rm -p 8080:8080 usuarios-service
```

También se puede ejecutar el microservicio junto con PostgreSQL mediante Docker Compose:

```powershell
docker compose up --build
```

Para detener los contenedores:

```powershell
docker compose down
```

Para detenerlos y eliminar los volúmenes asociados:

```powershell
docker compose down -v
```

El puerto HTTP del contenedor es:

```text
8080
```

Por lo tanto, desde la máquina host se accede mediante:

```text
http://localhost:8080
```

---

## Agregar un nuevo endpoint

Los controllers pertenecen a:

```text
src/Usuarios.API/Controllers/
```

La lógica de aplicación debe mantenerse fuera del controller.

Un flujo típico es:

```text
Controller
    ↓
Application Service
    ↓
Repository / Interface
    ↓
Infrastructure
    ↓
PostgreSQL
```

Las interfaces que necesita la aplicación deben declararse en:

```text
Usuarios.Application/Interfaces/
```

y sus implementaciones concretas en:

```text
Usuarios.Infrastructure/Repositories/
```

Los servicios de aplicación deben ubicarse en:

```text
Usuarios.Application/Services/
```

---

## Inyección de dependencias

Cada capa posee su propia clase `DependencyInjection` para registrar sus dependencias.

En `Program.cs`:

```csharp
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
```

Los servicios de aplicación se registran en:

```text
Usuarios.Application/DependencyInjection.cs
```

Las dependencias de infraestructura, como `DbContext` y repositorios, se registran en:

```text
Usuarios.Infrastructure/DependencyInjection.cs
```

---

## Manejo de errores

La API cuenta con un manejador global de excepciones.

Los errores inesperados se procesan de forma centralizada y se devuelve una respuesta HTTP apropiada al cliente.

Los casos esperados de la aplicación, como que una entidad no exista, no necesariamente deben tratarse como excepciones.

Por ejemplo, un servicio puede devolver `null` cuando no encuentra una entidad y el controller puede responder con:

```http
404 Not Found
```

Las excepciones deben reservarse principalmente para situaciones realmente excepcionales.

---

## Configuración y secretos

No se deben almacenar credenciales reales, API keys u otros secretos en el repositorio.

Para desarrollo local pueden utilizarse valores de prueba.

Para entornos donde existan credenciales reales, utilizar variables de entorno, configuración externa o mecanismos como User Secrets.

Ejemplo mediante variables de entorno:

```text
ConnectionStrings__DefaultConnection
```

Docker Compose puede sobrescribir la configuración de `appsettings.json` mediante variables de entorno.

---

## Agregar funcionalidades al microservicio

El template solamente proporciona la estructura inicial.

Cada nuevo microservicio debe agregar sus propios:

* entidades
* enums
* DTOs
* interfaces
* servicios
* repositorios
* configuraciones de Entity Framework
* migrations
* controllers

No se deben agregar entidades ficticias o de prueba al template solamente para demostrar que funciona.

El template debe permanecer genérico.

---

## Flujo recomendado

Al crear un nuevo microservicio:

```text
1. Crear repositorio desde el template
        ↓
2. Renombrar Service → NombreDelServicio
        ↓
3. Actualizar namespaces y referencias
        ↓
4. Configurar la base de datos
        ↓
5. Crear entidades
        ↓
6. Configurar Entity Framework
        ↓
7. Crear migración inicial
        ↓
8. Crear repositorios y servicios
        ↓
9. Crear controllers
        ↓
10. Probar localmente
        ↓
11. Probar con Docker Compose
```

Cada microservicio debe poder ejecutarse y evolucionar de forma independiente.
