# sgpla-backend

API REST de SGPLa: .NET 10, ASP.NET Core Minimal APIs, EF Core 10 (solo como ORM), SQL Server 2022 y DbUp para las migraciones.

El diseño completo está en `../PLAN_INICIAL.md` y el modelo de datos en `../DATABASE.md`.

Cada módulo se implementa según `../ESTANDAR_MODULOS.md`: capas, nombres, contratos entre módulos, pruebas y lista de verificación.

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

Hay 10 módulos: Institucional, Catalogos, Usuarios, OfertaEducativa, Docentes, Integracion, SolicitudesApertura, Publicacion, Aspirantes y ConsejoTecnico. Las dependencias permitidas entre ellos se declaran en `tests/Sgpla.ArchitectureTests/Modulos.cs`. Las pruebas de arquitectura también verifican las capas, la ubicación de los tipos según su sufijo, la visibilidad, que un módulo solo use el contrato público (`Application.Contracts`) de otro y que los logs no reciban datos personales.

## Requisitos

- .NET SDK 10.0.401 o un parche posterior de la banda 10.0.4xx (`global.json` usa `latestPatch` y excluye previews).
- Docker, para SQL Server local, el entorno completo con `docker compose` y las pruebas de integración.

## Uso con Docker (solo requiere Docker)

Desde la raíz del monorepo:

```bash
cp .env.example .env         # opcional: ajustar contraseña y puertos
docker compose build --pull  # descarga la versión vigente de cada imagen base
docker compose up -d
```

`--pull` evita compilar con imágenes base viejas que hayan quedado en la caché local. La imagen del SDK de compilación está fijada a la versión exacta de `global.json` (`sdk:10.0.401`), porque un cambio de banda puede romper el build con analizadores nuevos (`TreatWarningsAsErrors`). Las imágenes de runtime (`aspnet:10.0`, `runtime:10.0`) quedan flotantes: solo ejecutan código ya compilado, y los parches dentro de 10.0 son compatibles por diseño.

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

## Autenticación

Variables nuevas en `.env` (ver `.env.example`), todas con un valor de desarrollo por omisión salvo el bootstrap:

| Variable | Uso |
|---|---|
| `SGPLA_JWT_CLAVE` | Clave de firma de los JWT (HS256). Obligatoria y de al menos 32 caracteres; nunca uses el valor de ejemplo fuera de desarrollo. |
| `SGPLA_LDAP_SERVIDOR`, `SGPLA_LDAP_PUERTO`, `SGPLA_LDAP_SEGURIDAD` | Directorio LDAP de la UV para el login de DGAA y Entidad Académica. `SGPLA_LDAP_SEGURIDAD=SinTls` (el valor actual de la UV) solo se acepta si `SGPLA_API_ENVIRONMENT=Development`. |
| `SGPLA_BOOTSTRAP_CORREO`, `SGPLA_BOOTSTRAP_NOMBRE` | Correo y nombre del primer Superusuario, usados solo por el bootstrap. |

El primer Superusuario se crea con el subcomando `bootstrap-superusuario` de la propia imagen de la API, no con la CLI de migraciones:

```bash
docker compose run --rm api bootstrap-superusuario
```

Imprime el id y una contraseña temporal una sola vez; cámbiala al iniciar sesión. Ejecutarlo de nuevo con un Superusuario ya activo no crea nada y termina con código 0, así que es seguro incluirlo en cada despliegue.

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
