# Plan: Esqueleto de SGPLa (API REST .NET + SQL Server)

## Contexto

`C:\repos\sgpla` solo contiene `DATABASE.md` (modelo normativo, ~55 tablas en los esquemas `academico`, `usuarios`, `integracion` y `plazas`) y `DATABASE_DIAGRAM.md` (ER Mermaid). No hay código ni repositorio git. El objetivo de esta fase es **generar únicamente el esqueleto** del sistema: solución, módulos, infraestructura transversal, sistema de migraciones, adaptadores externos (LDAP UV y PLANEA), pruebas de integración y CI. Un solo slice de referencia (`Region`) se implementará de punta a punta para validar la cadena completa (DbUp → EF Core → Minimal API → test de integración → CI). El DDL completo y los flujos de negocio son trabajo posterior (ver sección 17 de `DATABASE.md`).

Decisiones tomadas con el usuario:
- **Monorepo**: `C:\repos\sgpla` alojará el backend (API REST, `sgpla-backend/`) y un frontend React (`sgpla-web/`). En esta fase solo se construye el backend; `sgpla-web/` queda reservada con un README. `DATABASE.md`, `DATABASE_DIAGRAM.md` y `PLAN_INICIAL.md` permanecen en la raíz.
- **Arquitectura**: Clean Architecture organizada primero por módulo: `src/Modules/<Modulo>/Sgpla.Modules.<Modulo>/` con carpetas `Domain/`, `Application/`, `Infrastructure/` y `Endpoints/`. Las reglas entre capas se imponen con tests de arquitectura.
- **Migraciones**: DbUp con scripts `.sql` escritos a mano. EF Core se usa solo como ORM; no hay EF Migrations.
- **CI**: GitHub Actions.
- **Endpoints**: Minimal APIs con `MapGroup` por módulo.
- **Runtime**: .NET 10 (LTS vigente), SQL Server 2022.

## Fase 1 (esta ejecución): API REST en estado inicial, sin lógica

Alcance acotado a pedido del usuario. Se crea solo `sgpla-backend/` y nada de lógica de negocio.

**Entorno verificado al iniciar:** .NET SDK 10.0.112, junto con una preview 10.0.200 que no se usará; Docker 29.7 y git 2.52. La carpeta no es un repositorio git. Después, el proyecto se fijó en el SDK 10.0.401, la banda que ya usaban la imagen `dotnet/sdk` y el CI (ver `global.json` abajo).

**Qué se crea:**
- **Configuración de .NET:**
  - `sgpla-backend/global.json`: SDK `10.0.401` con `rollForward: latestPatch` y `allowPrerelease: false`: solo acepta parches de la banda 10.0.4xx y nunca una preview, para que local, CI y Docker compilen con el mismo SDK.
  - `Directory.Build.props`, `Directory.Packages.props` y `Sgpla.slnx`.
- **`src/Sgpla.Api`:**
  - `Program.cs` mínimo con OpenAPI y Scalar, ProblemDetails, health check `/health` (incluye SQL Server), CORS por configuración y el registro de módulos.
  - `appsettings*.json` con la cadena de conexión local.
  - Sin autenticación todavía.
- **`src/Sgpla.Database`:** runner de DbUp (`DatabaseMigrator` + CLI) con un baseline, `Baseline/baseline.sql` (los cuatro esquemas y las 55 tablas de `DATABASE.md`) y `Baseline/seed.sql` (datos iniciales de los catálogos). `Scripts/` queda vacía para las migraciones.
- **`src/BuildingBlocks`:**
  - `Sgpla.SharedKernel` vacío, con solo lo mínimo para compilar.
  - `Sgpla.BuildingBlocks.Infrastructure` con un `SgplaDbContext` sin entidades, que aplica las configuraciones de los módulos registrados.
- **Los 10 módulos** (`src/Modules/<Modulo>/Sgpla.Modules.<Modulo>/`):
  - Carpetas `Domain/`, `Application/`, `Infrastructure/` y `Endpoints/`, cada una con un `.gitkeep`.
  - `<Modulo>Module.cs` con `Add<Modulo>Module` y `Map<Modulo>Endpoints`, que abre un `MapGroup` vacío.
  - Referencias entre proyectos según el grafo de dependencias.
- **`tests/`:**
  - `Sgpla.ArchitectureTests`: capas y grafo de módulos.
  - `Sgpla.IntegrationTests`: Testcontainers, DbUp y WebApplicationFactory; un solo test que verifica que `/health` responda 200 y que existan los cuatro esquemas.
  - `Sgpla.UnitTests`: vacío.
- **En la raíz:** `.gitignore`, `.editorconfig`, `.gitattributes`, `docker-compose.yml`, `.github/workflows/backend-ci.yml`, `README.md` y `sgpla-web/README.md`.

**Qué se pospone:** el slice `Region`, JWT, LDAP, Argon2, PLANEA, el almacenamiento de archivos y las migraciones de datos de catálogos. También la revisión de las etiquetas flotantes de imágenes Docker (`mssql/server:2022-latest`, `dotnet/aspnet:10.0` y `dotnet/runtime:10.0`), que se hará al preparar el primer despliegue real. Lo descrito en las secciones de abajo sigue siendo el diseño objetivo.

**Supuestos por defecto** (el usuario puede cambiarlos):
- Identificadores de dominio en español sin acentos; términos técnicos en inglés.
- xUnit v3 + Shouldly.
- Se ejecuta `git init` (rama `main`), sin commits.
- Las dudas sobre la ubicación de `horario_programacion` y `asignacion_docente` no afectan a esta fase.

## Módulos identificados

Refinamiento 1 (acordado con el usuario):
- El proceso de plazas se divide en tres módulos: Publicacion, Aspirantes y ConsejoTecnico.
- Institucional y OfertaEducativa siguen separados.
- Catalogos es un módulo propio.
- Docentes es un módulo propio.
- Al aprobarse, este plan se copia sobre `C:\repos\sgpla\PLAN_INICIAL.md`.

| Módulo | Tablas (esquema) | Operaciones |
|---|---|---|
| **Institucional** | region, campus, area_academica, entidad_academica (`academico`) | CRUD y baja lógica, sin restauración; `region` y `campus` son de solo lectura (semilla) |
| **OfertaEducativa** | sistema_educativo, nivel_formacion, programa_educativo, plan_estudios, archivo_plan_estudios, area_formacion, experiencia_educativa, periodo_escolar, programacion_academica, horario_programacion (`academico`) | CRUD + baja en cascada; `horario_programacion` solo lectura; el archivo del plan se reemplaza, no se edita |
| **Catalogos** | grado_academico, tipo_documento_expediente, municipio (`academico`); tratamiento_academico, articulo, modalidad_recepcion, tipo_plaza, tipo_contratacion (`plazas`) | Catálogos fijos, precargados en la semilla y de solo lectura (listar/obtener): grado_academico, tipo_documento_expediente, municipio, tratamiento_academico, modalidad_recepcion, tipo_plaza, tipo_contratacion. `articulo` es administrable por el Superusuario: alta, consulta y corrección (el número solo mientras ningún Aviso lo use; la descripción siempre); sin baja |
| **Docentes** | docente, formacion_docente, documento_docente, version_documento_docente, asignacion_docente (`academico`) | CRUD de docente y formaciones; documentos versionados; asignaciones sin CRUD directo (se derivan de PLANEA o del aval de un Acta) |
| **Usuarios** | rol, usuario, usuario_dgaa, usuario_entidad_academica, credencial_superusuario (`usuarios`) | Alta/consulta/edición de nombre/baja/restauración; `rol` es fijo; login LDAP y local, cambio/restablecimiento de contraseña, comando de bootstrap |
| **Integracion** | sincronizacion_planea (`integracion`) | Sin CRUD: disparar sincronización y consultar la bitácora |
| **SolicitudesApertura** | solicitud_apertura, archivo_solicitud_apertura (`academico`) | Crear/editar en PENDIENTE; comandos aceptar/rechazar/cancelar/vincular; sin eliminación |
| **Publicacion** | oferta, aviso, horario_recepcion_requisito, aviso_oferta, documento_aviso, revision_aviso (`plazas`) | CRUD de Oferta y de Aviso en borrador; alta y retiro de Ofertas en un Aviso; comandos enviar/avalar/devolver/firmar/publicar/cancelar/archivar; documentos versionados |
| **Aspirantes** | aspirante, perfil_aspirante, formacion_aspirante, documento_aspirante, version_documento_aspirante, solicitud, solicitud_documento (`plazas`) | Alta de Aspirante junto con su primera Solicitud; nuevas versiones de perfil y documentos; comandos admitir/no admitir/retirar la Solicitud; búsqueda exacta por correo |
| **ConsejoTecnico** | integrante_consejo_tecnico, acta_consejo_tecnico, acta_oferta, votacion_solicitud, acta_asistencia, documento_acta, revision_acta (`plazas`) | CRUD de integrantes (vigencias); Acta en borrador con resultados, asistencia y votación; comandos enviar/avalar/devolver/firmar/archivar; baja lógica en CREADA |

Los tres módulos del proceso de plazas comparten el esquema SQL `plazas`.

**Grafo de dependencias entre módulos** (acíclico, validado por tests de arquitectura):
- Institucional → Catalogos; Catalogos → (ninguno)
- Usuarios → Institucional
- OfertaEducativa → Institucional
- Docentes → OfertaEducativa, Catalogos
- Integracion → OfertaEducativa, Docentes
- SolicitudesApertura → OfertaEducativa
- Publicacion → Institucional, OfertaEducativa, Catalogos
- Aspirantes → Publicacion, Catalogos
- ConsejoTecnico → Publicacion, Aspirantes, Docentes, Catalogos, Institucional

Coordinación dentro del proceso de plazas:
- **Aval de un Acta**: es una sola transacción orquestada por ConsejoTecnico. Invoca servicios de aplicación públicos de Publicacion (cerrar el AvisoOferta y cubrir o liberar la Oferta) y de Docentes (crear o reutilizar el Docente, copiar el perfil del Aspirante, cerrar la asignación anterior y abrir la nueva). Comparten el mismo `SgplaDbContext` y la misma transacción.
- **Validaciones hacia atrás**: Publicacion necesita saber si existen Solicitudes o Actas antes de eliminar un Aviso CREADO o retirar un AvisoOferta, y no puede depender de Aspirantes ni de ConsejoTecnico. Se resuelve por inversión de dependencia: Publicacion define el puerto `IReferenciasAvisoOferta` y Aspirantes y ConsejoTecnico lo implementan. El host registra todas las implementaciones y Publicacion las consulta.
- **Referencias a los catálogos**: de los catálogos de Catalogos solo `articulo` admite correcciones, y su número solo mientras no tenga referencias; los demás son fijos. Catalogos define el contrato `IReferenciasArticulo` en su `Application.Contracts` y Publicacion lo implementa por sus Avisos al construirse.
- **Cancelación de un Aviso**: pasa lo mismo al comprobar que no haya revisión abierta. Esa comprobación es interna de Publicacion, porque revision_aviso le pertenece.

Reglas para mantenerlo acíclico:
- Las referencias a actores (`cargado_por_usuario_id`, `resuelto_por_usuario_id`, etc.) se modelan como `int` sin navegación, así que ningún módulo depende de Usuarios.
- `asignacion_docente.sincronizacion_planea_id` y `acta_oferta_id` también son `int` sin navegación.
- Las FK físicas existen igual en SQL porque las crea DbUp.
- El usuario autenticado y su ámbito (DGAA → área; EA → entidad) se exponen mediante `ICurrentUser` en SharedKernel. Los implementa el host.

## Estructura de la solución

### Raíz del monorepo

```
sgpla/
  README.md                      descripción del monorepo, cómo levantar cada app
  DATABASE.md  DATABASE_DIAGRAM.md  PLAN_INICIAL.md   (se conservan en la raíz)
  .gitignore                     común (.NET: bin/ obj/ *.user; Node: node_modules/ dist/; .env*)
  .editorconfig                  común (C# y TS/JS)
  .gitattributes                 finales de línea normalizados (LF en .sh/.yml, CRLF indiferente)
  docker-compose.yml             infraestructura local compartida: SQL Server 2022
  .github/workflows/
    backend-ci.yml               se dispara solo con cambios en sgpla-backend/** o en el propio workflow
  sgpla-backend/                 solución .NET (detalle abajo)
  sgpla-web/                     reservado para el frontend React; en esta fase solo contiene README.md
```

- La configuración específica de .NET (`global.json`, `Directory.Build.props`, `Directory.Packages.props`) vive dentro de `sgpla-backend/`, para que no afecte al frontend.
- `frontend-ci.yml` se agregará cuando se cree el esqueleto de `sgpla-web/`, con filtro `sgpla-web/**`.
- CORS en la API se configura desde settings (`Cors:OrigenesPermitidos`). En Development admite el origen del servidor de desarrollo del frontend.

### `sgpla-backend/`

```
sgpla-backend/
  Sgpla.slnx
  global.json                    (SDK 10.0.x)
  Directory.Build.props          (net10.0, Nullable, ImplicitUsings, TreatWarningsAsErrors, AnalysisLevel)
  Directory.Packages.props       (Central Package Management)
  src/
    Sgpla.Api/                   host: Program.cs, auth JWT, políticas, ProblemDetails, OpenAPI+Scalar, health checks, OpenTelemetry
    Sgpla.Database/              DbUp: Scripts/*.sql embebidos, DatabaseMigrator (librería) + CLI (Program.cs)
    BuildingBlocks/
      Sgpla.SharedKernel/        Entity base, ISoftDeletable, Result/Error, IClock, ICurrentUser, AmbitoUsuario, RolId (constantes tipadas 1/2/3), paginación
      Sgpla.BuildingBlocks.Infrastructure/  SgplaDbContext único, IAlmacenamientoArchivos (+ impl. en sistema de archivos local), SHA-256, SystemClock, helpers de endpoints/validación
    Modules/
      Institucional/Sgpla.Modules.Institucional/
        Domain/ Application/ Infrastructure/ Endpoints/  InstitucionalModule.cs
      OfertaEducativa/  Catalogos/  Docentes/  Usuarios/  Integracion/  SolicitudesApertura/
      Publicacion/  Aspirantes/  ConsejoTecnico/   (misma forma)
  tests/
    Sgpla.ArchitectureTests/     NetArchTest: capas por namespace + grafo de módulos
    Sgpla.UnitTests/             reglas de dominio puras
    Sgpla.IntegrationTests/      Testcontainers.MsSql + WebApplicationFactory + Respawn + WireMock.Net
```

### Convenciones por módulo

El detalle normativo está en `ESTANDAR_MODULOS.md`, que prevalece sobre este resumen.

- `Domain/`: entidades, enums de estado y reglas invariantes. Sin EF ni ASP.NET.
- `Application/`: un handler por caso de uso (`CrearRegion`, `DarDeBajaRegion`...), DTOs y validadores con FluentValidation. No se usa MediatR (ahora tiene licencia comercial): los handlers son clases que se registran en DI.
- `Infrastructure/`: `IEntityTypeConfiguration<T>` con `ToTable("region", "academico")` y adaptadores externos del módulo.
- `Endpoints/`: `Map<Modulo>Endpoints(IEndpointRouteBuilder)` con `MapGroup("/api/v1/<modulo>")`.
- `<Modulo>Module.cs`: `Add<Modulo>Module(IServiceCollection, IConfiguration)` y `Map<Modulo>Endpoints(...)`. Ambos se invocan explícitamente en `Program.cs`.

### Persistencia (EF Core 10)
- Hay un solo `SgplaDbContext`, necesario por las transacciones que cruzan módulos (p. ej. el aval de un Acta en ConsejoTecnico toca oferta y aviso_oferta de Publicacion, además de docente y asignacion_docente de Docentes). Aplica las configuraciones de los ensamblados de módulo que se registran al arrancar.
- Nombres: `EFCore.NamingConventions` (`UseSnakeCaseNamingConvention`) más esquema explícito por tabla. Los estados se guardan como texto (`HasConversion<string>()`).
- Filtro global de baja lógica (`fecha_eliminacion IS NULL`) con *named query filters* de EF 10, para poder desactivarlo al restaurar.
- Proveedor: `Microsoft.EntityFrameworkCore.SqlServer`. No hay EF Migrations; el esquema pertenece a DbUp.

### Migraciones (DbUp)
- `src/Sgpla.Database/Baseline/baseline.sql` construye la base completa y `src/Sgpla.Database/Scripts/####__descripcion.sql` contiene las migraciones posteriores; ambos son recursos embebidos. La bitácora va en `dbo.schema_versions`.
- `DatabaseMigrator.Migrate(connectionString)` es reutilizable: la CLI (`dotnet run --project src/Sgpla.Database -- --connection "..."`) y el fixture de integración lo usan. `EnsureDatabase` solo se aplica en Development/tests.
- La API **no** migra al arrancar.
- `baseline.sql` no es una migración: crea los esquemas `academico`, `usuarios`, `integracion` y `plazas` y las 55 tablas de `DATABASE.md` con sus PK, FK, UNIQUE, CHECK e índices, sin datos. `DatabaseMigrator` solo lo ejecuta sobre una base vacía y lo registra como `baseline`; si la base ya tiene tablas sin ese registro, falla sin tocarla.
- `seed.sql` se ejecuta junto con el baseline, en la misma transacción, y queda registrado como `baseline-seed`. Carga `usuarios.rol`, `municipio`, `sistema_educativo`, `nivel_formacion`, `area_formacion`, `region` y `campus`. `area_academica`, `entidad_academica` y `periodo_escolar` los registra el Superusuario.
- Cualquier cambio posterior de esquema o de datos va en migraciones de `Scripts/`, a partir de `0001`.

### Seguridad y servicios externos (adaptadores + interfaces; los flujos completos quedan para fases posteriores)
- **JWT**: `Microsoft.AspNetCore.Authentication.JwtBearer`. La API emite tokens de acceso de vida corta (refresh tokens fuera de alcance). Políticas `Superusuario`, `Dgaa`, `EntidadAcademica` y `CambioContrasenaPendiente`.
- **LDAP UV** (módulo Usuarios/Infrastructure): `ILdapAutenticador` implementado con `System.DirectoryServices.Protocols` sobre TLS/LDAPS, sin fallback local. `LdapOptions` (host, puerto, base DN, formato de bind) se configura desde settings/secretos. La contraseña nunca se registra.
- **Hash local**: `IPasswordHasher` con Argon2id en formato PHC (`Isopoh.Cryptography.Argon2`), con parámetros configurables y detección de rehash.
- **PLANEA** (módulo Integracion/Infrastructure): `IPlaneaClient` como typed `HttpClient`, con `Microsoft.Extensions.Http.Resilience`, `PlaneaOptions.BaseUrl` y validación de status/Content-Type/arreglo no vacío. El proceso de sincronización atómica queda como caso de uso por implementar.
- **Almacenamiento de binarios**: la interfaz `IAlmacenamientoArchivos` se implementa en el sistema de archivos local para dev y test. El proveedor definitivo está pendiente, según `DATABASE.md` §17.
- **Configuración de periodos** (actual/siguiente): `PeriodosOptions` en SolicitudesApertura.
- **Bootstrap del primer Superusuario**: se deja reservado el subcomando `bootstrap-superusuario` en la CLI; se implementa junto con el módulo Usuarios.

### Slice de referencia: `Institucional/Region`
- Endpoints en `/api/v1/institucional/regiones`: `GET` (paginado, `incluirEliminados`), `GET /{id}`, `POST`, `PUT /{id}` (solo `nombre`; `clave` es inmutable), `DELETE /{id}` (baja lógica UTC idempotente), `POST /{id}/restaurar`. Requieren la política `Superusuario`.
- Errores con ProblemDetails: 400 por validación, 404, 409 por clave duplicada (incluye filas dadas de baja).
- Este slice sirve de patrón a copiar en el resto de entidades.

## Pruebas y CI

- **Sgpla.IntegrationTests**:
  - `SqlServerFixture` levanta `mcr.microsoft.com/mssql/server:2022-latest` con Testcontainers una sola vez por colección, ejecuta `DatabaseMigrator` y crea `SgplaApiFactory : WebApplicationFactory<Program>` con la cadena de conexión del contenedor.
  - Respawn limpia los datos entre tests y respeta las semillas de `usuarios.rol` y `dbo.schema_versions`.
  - Un `TestAuthHandler` emite claims por rol. `ILdapAutenticador` se sustituye por un fake y PLANEA se simula con WireMock.Net.
  - Tests iniciales: CRUD de Region (crear, duplicado → 409, editar nombre, clave inmutable, baja, restauración) y semilla de roles presente.
- **Sgpla.ArchitectureTests**: Domain no depende de Application/Infrastructure/Endpoints/EF Core; Application no depende de Infrastructure/Endpoints; los módulos respetan el grafo anterior.
- **`.github/workflows/backend-ci.yml`** (push y pull_request a `main`/`develop`):
  - Filtro de rutas: `paths: ['sgpla-backend/**', '.github/workflows/backend-ci.yml']`.
  - `defaults.run.working-directory: sgpla-backend`.
  - Corre en `ubuntu-latest`, que trae Docker para Testcontainers.
  1. `actions/checkout`, `actions/setup-dotnet` con `global-json-file: sgpla-backend/global.json` y caché de NuGet.
  2. `dotnet restore` y `dotnet build -c Release --no-restore`.
  3. `dotnet format --verify-no-changes`.
  4. `dotnet test` de UnitTests y ArchitectureTests.
  5. `dotnet test` de IntegrationTests (SQL Server en contenedor), con el reporte TRX/cobertura subido como artifact.

## Archivos críticos a crear

**En la raíz:**
- `README.md`, `.gitignore`, `.editorconfig`, `.gitattributes`, `docker-compose.yml`
- `.github/workflows/backend-ci.yml`
- `sgpla-web/README.md`, que indica que la carpeta está reservada para el frontend React

**En `sgpla-backend/`** (rutas relativas a esa carpeta):
- `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `Sgpla.slnx`
- `src/Sgpla.Api/Program.cs`
- `src/Sgpla.Database/DatabaseMigrator.cs`, `src/Sgpla.Database/Baseline/baseline.sql`
- `src/BuildingBlocks/Sgpla.BuildingBlocks.Infrastructure/Persistence/SgplaDbContext.cs`
- `src/Modules/Institucional/Sgpla.Modules.Institucional/{Domain/Region.cs, Application/Regiones/*, Infrastructure/RegionConfiguration.cs, Endpoints/RegionEndpoints.cs, InstitucionalModule.cs}`
- `src/Modules/Usuarios/.../Infrastructure/Ldap/LdapAutenticador.cs`, `.../Security/Argon2PasswordHasher.cs`
- `src/Modules/Integracion/.../Infrastructure/Planea/PlaneaClient.cs`
- Los otros módulos se crean con el `.csproj`, las carpetas de capa, `<Modulo>Module.cs` y un `MapGroup` vacío.
- `tests/Sgpla.IntegrationTests/{SqlServerFixture.cs, SgplaApiFactory.cs, Institucional/RegionEndpointsTests.cs}`

## Verificación
Todos los comandos `dotnet` se ejecutan desde `sgpla-backend/`.
1. `dotnet build Sgpla.slnx` sin warnings.
2. `dotnet test tests/Sgpla.ArchitectureTests` y `tests/Sgpla.UnitTests` en verde.
3. Con Docker Desktop activo: `dotnet test tests/Sgpla.IntegrationTests`. Levanta SQL Server, aplica los scripts y pasa el CRUD de Region.
4. Manual:
   - `docker compose up -d`, desde la raíz.
   - `dotnet run --project src/Sgpla.Database -- --connection "<local>"`.
   - `dotnet run --project src/Sgpla.Api`.
   - Abrir `/scalar/v1` y `/health`, y probar los endpoints de regiones con un token de desarrollo.
5. CI: requiere `git init` y un remoto en GitHub (no se hará commit ni push sin que lo pidas). Al subir la rama, `backend-ci.yml` debe pasar completo. Un cambio que solo toque `sgpla-web/` no debe dispararlo.
