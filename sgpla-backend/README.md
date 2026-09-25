# sgpla-backend

API REST de SGPLa: .NET 10, ASP.NET Core Minimal APIs, EF Core 10 (solo como ORM), SQL Server 2022 y DbUp para las migraciones.

El diseño completo está en `../PLAN_INICIAL.md` y el modelo de datos en `../DATABASE.md`.

## Estructura

```
src/
  Sgpla.Api/                   host: Program.cs, OpenAPI/Scalar, /health, CORS, registro de módulos
  Sgpla.Database/              DbUp: Baseline/{baseline,seed}.sql + Scripts/####__descripcion.sql (recursos embebidos) + CLI
  BuildingBlocks/
    Sgpla.SharedKernel/        tipos compartidos del dominio (vacío por ahora)
    Sgpla.BuildingBlocks.Infrastructure/  SgplaDbContext único y registro de persistencia
  Modules/<Modulo>/Sgpla.Modules.<Modulo>/
    Domain/  Application/  Infrastructure/  Endpoints/  <Modulo>Module.cs
tests/
  Sgpla.ArchitectureTests/     reglas de capas y grafo de dependencias entre módulos
  Sgpla.UnitTests/             reglas de dominio (sin pruebas todavía)
  Sgpla.IntegrationTests/      SQL Server con Testcontainers + WebApplicationFactory
```

Hay 10 módulos: Institucional, Catalogos, Usuarios, OfertaEducativa, Docentes, Integracion, SolicitudesApertura, Publicacion, Aspirantes y ConsejoTecnico. Las dependencias permitidas entre ellos se declaran en `tests/Sgpla.ArchitectureTests/Modulos.cs`.

## Requisitos

- .NET SDK 10.0.112 o una versión 10.0 posterior estable (`global.json` usa `latestFeature` y excluye previews).
- Docker, para SQL Server local, el entorno completo con `docker compose` y las pruebas de integración.

## Uso con Docker (solo requiere Docker)

Desde la raíz del monorepo:

```bash
cp .env.example .env         # opcional: ajustar contraseña y puertos
docker compose up -d --build
```

Se levantan tres servicios:

| Servicio | Qué hace |
|---|---|
| `sqlserver` | SQL Server 2022, con los datos en el volumen `sgpla_sqlserver-data`. |
| `migraciones` | Aplica los scripts de DbUp cuando SQL Server está listo y termina. Si no hay scripts nuevos, no hace nada. |
| `api` | Arranca solo si las migraciones terminaron bien. |

Con la API en marcha:
- **Documentación interactiva:** `http://localhost:8180/scalar/v1`
- **Estado:** `http://localhost:8180/health`

Comandos útiles:
- `docker compose logs -f api` para ver los logs de la API.
- `docker compose down` para detener todo; `docker compose down -v` borra también la base.

Las imágenes se construyen con `src/Sgpla.Api/Dockerfile` y `src/Sgpla.Database/Dockerfile`; en ambos casos el contexto es `sgpla-backend/`.

## Uso local con el SDK

Todos los comandos se ejecutan desde `sgpla-backend/`, salvo el de `docker compose`.

```bash
# 1. Solo SQL Server (desde la raíz del monorepo)
docker compose up -d sqlserver

# 2. Migraciones (crea la base sgpla-bd si no existe)
dotnet run --project src/Sgpla.Database -- --ensure-database \
  --connection "Server=localhost,1433;Database=sgpla-bd;User Id=sa;Password=Sgpla_Dev_2026!;TrustServerCertificate=True"

# 3. API
dotnet run --project src/Sgpla.Api
```

Con la API en marcha:
- **Documentación interactiva:** `http://localhost:5180/scalar/v1`
- **Estado:** `http://localhost:5180/health`

## Pruebas

```bash
dotnet test --solution Sgpla.slnx                     # todas (requiere Docker)
dotnet test --project tests/Sgpla.ArchitectureTests  # solo arquitectura
```

Las pruebas usan Microsoft.Testing.Platform, habilitado en `global.json`.

## Migraciones

La base se construye en dos pasos, ambos registrados por DbUp en `dbo.schema_versions`:

1. **Baseline** (`src/Sgpla.Database/Baseline/`): `baseline.sql` crea los esquemas y todas las tablas de `DATABASE.md`, y `seed.sql` carga los datos iniciales de los catálogos. Ambos se ejecutan en una sola transacción, solo sobre una base vacía, y quedan registrados como `baseline` y `baseline-seed`. Si la base ya tiene tablas pero no esos registros, la migración falla sin tocarla.
2. **Migraciones** (`src/Sgpla.Database/Scripts/####__descripcion.sql`): cada cambio posterior, incluidos los datos de catálogos, es un script nuevo que empieza en `0001`. Se aplican en orden de nombre.

Reglas:
- Un cambio de esquema siempre es una migración nueva. No se editan `baseline.sql` ni `seed.sql`: las bases existentes ya no los vuelven a leer.
- Las migraciones ya aplicadas no se modifican.
- La API no migra al arrancar.

**Compactar migraciones.** Cuando se acumulen muchas, se pueden fusionar en el baseline:
1. Confirmar que **todas** las bases desplegadas ya aplicaron las migraciones que se van a fusionar.
2. Regenerar `baseline.sql` y `seed.sql` para que incluyan su efecto.
3. Borrar esas migraciones de `Scripts/`.

Las bases existentes no notan el cambio (ya tienen `baseline` y esas migraciones registradas) y las nuevas obtienen lo mismo desde el baseline.
