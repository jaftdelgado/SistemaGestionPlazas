# Migración de SQL Server a PostgreSQL

Especificación para el agente que implemente el cambio de motor de base de datos del backend de SGPLa. Las decisiones de la tabla de abajo ya están aprobadas por el responsable del proyecto; no se reabren. Si algo que no está escrito aquí te obliga a decidir, DETENTE y pregunta (regla 2 de `AGENTS.md`).

## Contexto

El backend (`sgpla-backend/`, .NET 10, EF Core 10 y DbUp) usa SQL Server 2022 y pasa a PostgreSQL 18. **El modelo de datos no cambia**: mismas 54 tablas, columnas, relaciones, restricciones e índices, con los mismos nombres. La única excepción aprobada es la columna `version` de `academico.solicitud_apertura` (MIG-D3). El frontend (`sgpla-web/`) queda fuera del alcance.

No existe ninguna base con datos reales: la base se recrea desde cero y no hay que exportar ni cargar datos.

## Decisiones aprobadas

| # | Decisión |
|---|---|
| MIG-D1 | `baseline.sql`, `seed.sql` y `development.sql` se **reescriben en su lugar** en SQL de PostgreSQL. Es una excepción explícita a la regla 13 de `AGENTS.md` (los baseline no se editan) y se registra en `DECISIONES.md`, igual que SOL-D1 y SOL-D2. `Scripts/` sigue vacío |
| MIG-D2 | Toda columna `datetime2(0)` pasa a `timestamptz(0)`. Npgsql lee esos valores como `DateTimeKind.Utc`, así que se elimina `ConversionesUtc` y sus usos. No se activa `Npgsql.EnableLegacyTimestampBehavior` |
| MIG-D3 | La concurrencia optimista de `solicitud_apertura` usa la columna de sistema `xmin`. Se quita `version rowversion` del baseline; `SolicitudApertura.Version` pasa de `byte[]` a `uint` y sigue con `IsRowVersion()`, que Npgsql mapea a `xmin`. SOL-D2 queda modificada por esta decisión |
| MIG-D4 | PostgreSQL 18, con la etiqueta exacta de la imagen fijada (por ejemplo `postgres:18.x-alpine`; usa la última 18.x estable y la misma en compose y Testcontainers) |
| MIG-D5 | La bitácora de DbUp queda en `public.schema_versions` |
| MIG-D6 | Sin datos que migrar: `docker compose down -v` y se recrea |

## Reglas que aplican en especial a este trabajo

- **Regla 10 (licencias): DETENTE antes de agregar paquetes.** Revisa la licencia de cada paquete nuevo y de sus dependencias, y presenta la lista con sus versiones para que se apruebe antes de fijarla en `Directory.Packages.props`.
- **Regla 4:** todo build, prueba e inspección se hace con Docker. Nada de `dotnet` local; para consultar la base, `psql` dentro de un contenedor.
- **Regla 8:** sin atajos. Si una prueba deja de tener sentido con PostgreSQL, se reformula para que verifique la misma regla; no se borra ni se salta.
- **Regla 11:** lo que no esté en este documento se reporta como nota y no se corrige.
- **Regla 12:** los comentarios del código enuncian la regla, sin remitir a documentos.
- Usa las skills `backend-resource`, `backend-testing`, `backend-verify`, `git-workflow` y `skill-authoring` de `.agents/skills/`.

## Rama y commits

- Rama `refactor/postgresql-migration`, creada desde `origin/develop` recién sincronizado.
- Commits por área con la convención de `git-workflow`, `git add` por ruta explícita, sin `amend` ni `rebase`, sin atribución de IA.
- Sin push ni PR hasta que se pidan.

## Paso 0. Prueba rápida (antes de reescribir el baseline)

En un contenedor `postgres:18.x` con una tabla mínima que tenga una columna `varchar COLLATE es_ci_ai` (ver el paso 4), comprueba con Npgsql y EF Core:

1. que `=` y la restricción `UNIQUE` ignoran mayúsculas y acentos (`'Xalapa'` = `'XALAPÁ'`);
2. qué SQL genera `.Contains(parametro)` sobre esa columna (`LIKE` o `strpos`) y si PostgreSQL 18 lo ejecuta sin el error de intercalaciones no deterministas;
3. que `OrderBy` sobre esa columna ordena en español.

Si el punto 2 falla, DETENTE y reporta qué SQL se generó y qué error dio, con las alternativas posibles: por ejemplo, `EF.Functions.Like` explícito, o `unaccent` con `ILIKE`. No elijas tú.

## Paso 1. Paquetes

`sgpla-backend/Directory.Packages.props`, bloque de persistencia y de pruebas:

- Se quitan `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.Data.SqlClient`, `dbup-sqlserver` y `Testcontainers.MsSql`.
- Entran, tras la aprobación de licencias: `Npgsql.EntityFrameworkCore.PostgreSQL` (10.x, compatible con EF Core 10.0.12), `dbup-postgresql` (de la misma familia 7.x que `dbup-sqlserver`) y `Testcontainers.PostgreSql` (la misma versión que el resto de Testcontainers, hoy 4.15.0). Si el código usa `Npgsql` directamente, se agrega `Npgsql` como paquete explícito.
- `EFCore.NamingConventions` se queda.

Se actualizan las referencias en:

- `src/BuildingBlocks/Sgpla.BuildingBlocks.Infrastructure/Sgpla.BuildingBlocks.Infrastructure.csproj`
- `src/Sgpla.Database/Sgpla.Database.csproj`
- `tests/Sgpla.IntegrationTests/Sgpla.IntegrationTests.csproj`

## Paso 2. Persistencia y API

| Archivo | Cambio |
|---|---|
| `src/BuildingBlocks/Sgpla.BuildingBlocks.Infrastructure/Persistence/PersistenciaExtensions.cs` | `UseSqlServer` pasa a ser `UseNpgsql`. Se mantienen `UseSnakeCaseNamingConvention` y `Ignore(CoreEventId.SaveChangesFailed)`. El comentario de 2601/2627 se reescribe para 23505 |
| `src/BuildingBlocks/Sgpla.BuildingBlocks.Infrastructure/Http/ViolacionUnicidadExceptionHandler.cs` | Detecta `DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg }` y registra `pg.ConstraintName` (o `"desconocida"` si viene nulo). Se elimina la expresión regular. Nunca se registran `Detail` ni `MessageText`, y la cadena de conexión no lleva `Include Error Detail`. El resto (409, código `Persistencia.ValorDuplicado`, texto) queda igual |
| `src/Sgpla.Api/Program.cs:39` | `AddDbContextCheck<SgplaDbContext>("sqlserver")` pasa a `"postgresql"` |
| `src/Modules/Institucional/.../EntidadesAcademicas/EntidadAcademicaConsultas.cs:77` y `src/Modules/Usuarios/.../Cuentas/CuentaConsultas.cs:70` | `EF.Functions.Collate(x, "Modern_Spanish_100_CI_AI")` pasa a `EF.Functions.Collate(x, "es_ci_ai")`. La llamada se mantiene porque `entidad_academica.nombre` y `usuario.nombre` no tienen intercalación de columna; la búsqueda depende del resultado del paso 0 |
| `src/Modules/SolicitudesApertura/.../Domain/SolicitudesApertura/SolicitudApertura.cs:52` | `public uint Version { get; private set; }` (MIG-D3) |
| `src/Modules/SolicitudesApertura/.../Infrastructure/SolicitudesApertura/SolicitudAperturaConfiguration.cs` | Se mantiene `IsRowVersion()` y se quitan los usos de `ConversionesUtc` |
| `src/Modules/SolicitudesApertura/.../Infrastructure/SolicitudesApertura/ArchivoSolicitudAperturaConfiguration.cs` | Se quitan los usos de `ConversionesUtc` |
| `src/Modules/SolicitudesApertura/.../Infrastructure/SolicitudesApertura/ConversionesUtc.cs` | Se elimina (MIG-D2) |
| `src/BuildingBlocks/Sgpla.BuildingBlocks.Application/Archivos.cs`, `src/Modules/Catalogos/.../ArticuloConfiguration.cs`, `src/Modules/Institucional/.../CampusConfiguration.cs`, `src/BuildingBlocks/Sgpla.SharedKernel/Entity.cs` | Comentarios: "SQL Server" pasa a "la base de datos" o "PostgreSQL". El comentario de `IsUnicode(false)` sobre la conversión a `nvarchar` ya no aplica; se quita el comentario y se puede conservar `IsUnicode(false)`, que no tiene efecto en PostgreSQL. `Entity.cs` dice `int IDENTITY` y pasa a decir `int` de identidad |

Antes de cerrar, busca con `git grep -n -iE 'sqlserver|sql server|SqlClient|SqlException|rowversion|datetime2|Modern_Spanish|Latin1_General'` en `sgpla-backend/src` y `sgpla-backend/tests` y deja todo resuelto.

## Paso 3. Migrador (`src/Sgpla.Database/DatabaseMigrator.cs`)

- `EsquemaBitacora` pasa a `"public"`. `TablaBitacora`, `NombreBaseline` y `NombreSeed` no cambian.
- `EnsureDatabase.For.SqlDatabase` pasa a `EnsureDatabase.For.PostgresqlDatabase`.
- `DeployChanges.To.SqlDatabase(...).JournalToSqlTable(...)` pasa a `DeployChanges.To.PostgresqlDatabase(...).JournalToPostgresqlTable(EsquemaBitacora, TablaBitacora)`.
- `catch (SqlException)` pasa a `catch (NpgsqlException)`.
- `ConsultarEstadoBaseline` se reescribe con `NpgsqlConnection`/`NpgsqlCommand`, con el mismo resultado de tres estados:
  - `Aplicado` si existe `public.schema_versions` (usa `to_regclass('public.schema_versions')`) y tiene la fila con `scriptname = 'baseline'`. Comprueba el nombre real de la columna en la versión de `dbup-postgresql` que se instale.
  - `BaseConDatosSinBaseline` si existe alguna tabla en `pg_tables` fuera de `pg_catalog` e `information_schema`, sin contar la bitácora.
  - `BaseVacia` en cualquier otro caso.
  - PostgreSQL no admite `IF` fuera de un bloque `DO`. Resuelve la consulta en una sola sentencia `SELECT CASE …`, o en dos consultas desde C#. El comentario sobre el cortocircuito de T-SQL desaparece.
- Se conserva la lógica: baseline y seed en una sola transacción solo sobre una base vacía, la salvaguarda y las migraciones de `Scripts/` con una transacción por script. Se actualiza el XML doc (`dbo.schema_versions`, "Cadena de conexión a SQL Server").

## Paso 4. Scripts SQL (`src/Sgpla.Database/Baseline/`)

### `baseline.sql`

Traducción uno a uno, en el mismo orden y con todos los nombres de restricciones e índices intactos (el más largo tiene 61 caracteres; el límite de PostgreSQL es 63). Corrige el encabezado: dice 55 tablas y son 54 (`academico` 23, `plazas` 25, `usuarios` 5, `integracion` 1).

Al inicio:

```sql
CREATE SCHEMA IF NOT EXISTS academico;
CREATE SCHEMA IF NOT EXISTS plazas;
CREATE SCHEMA IF NOT EXISTS usuarios;
CREATE SCHEMA IF NOT EXISTS integracion;

-- Ignora mayúsculas y acentos en comparaciones, unicidad y búsquedas.
CREATE COLLATION IF NOT EXISTS public.es_ci_ai (provider = icu, locale = 'es-u-ks-level1', deterministic = false);
```

| T-SQL actual | PostgreSQL |
|---|---|
| `SET ANSI_NULLS`, `SET QUOTED_IDENTIFIER`, `GO`, `IF SCHEMA_ID(...) EXEC(...)` | Se eliminan; los esquemas se crean como arriba |
| `int IDENTITY(1,1)` | `int GENERATED BY DEFAULT AS IDENTITY` |
| `tinyint` | `smallint` |
| `bit` | `boolean` |
| `nvarchar(n)`, `varchar(n)` | `varchar(n)` |
| `char(n)`, `date`, `time(0)`, `int` | Sin cambio |
| `datetime2(0)` | `timestamptz(0)` (MIG-D2) |
| `rowversion` (columna `version` de `solicitud_apertura`) | Se elimina la columna (MIG-D3) |
| `PRIMARY KEY CLUSTERED (id)` | `PRIMARY KEY (id)` |
| `CONSTRAINT df_… DEFAULT (0)` | `DEFAULT 0` (PostgreSQL no le pone nombre a un default) |
| `COLLATE Modern_Spanish_100_CI_AI` y `COLLATE Latin1_General_100_CI_AI` en una columna | `COLLATE public.es_ci_ai` |
| `CREATE UNIQUE INDEX ux_… ON … WHERE x IS NULL` | Igual (índice parcial) |
| `N'texto'` | `'texto'` |
| `LEN(TRIM(x)) > 0` | `length(btrim(x)) > 0` |
| `DATALENGTH(x) = LEN(x)` (sin espacios al final) | `x NOT LIKE '% '` |
| `LEN(x) > 0` / `LEN(x) = n` sobre `varchar` | `length(x) > 0` / `length(x) = n` |
| `x COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z0-9]%'` y variantes (`[^0-9]`, `[^A-Z0-9-]`, `[^A-Z0-9/-]`) | `x COLLATE "C" ~ '^[A-Z0-9]*$'` con la misma clase. Si la regla original exige longitud mayor que 0 aparte, se conserva aparte |
| `x COLLATE Latin1_General_100_BIN2 = LOWER(x)` / `UPPER(x)` | `x COLLATE "C" = lower(x) COLLATE "C"` / `upper(...)` |
| `nombre COLLATE Latin1_General_100_BIN2 = N'Superusuario'` | `nombre COLLATE "C" = 'Superusuario'` |
| `CHARINDEX(' ', x) = 0` | `strpos(x, ' ') = 0` |
| `x LIKE '_%@_%._%'` y demás `LIKE` sin clases | Igual. Si la columna tiene `es_ci_ai`, se agrega `COLLATE "C"` en la expresión solo si el paso 0 mostró que PostgreSQL no acepta ese `LIKE` |

Cuidado con:

- Toda comparación dentro de un `CHECK` sobre una columna con `es_ci_ai` que hoy fuerza `BIN2` debe llevar `COLLATE "C"`; si no, se vuelve insensible a mayúsculas y la regla cambia.
- Los `CHECK` sobre columnas `char(n)`: en PostgreSQL `length()` sobre `char(n)` ignora los espacios de relleno, igual que `LEN`.

### `seed.sql`

- Se quitan `SET ANSI_NULLS`, `SET QUOTED_IDENTIFIER`, `GO`, `N'` y todos los `SET IDENTITY_INSERT … ON/OFF`. Con `GENERATED BY DEFAULT`, el `id` explícito se acepta tal cual.
- Al final se reajusta la secuencia de cada tabla que recibió ids explícitos. Hoy son `academico.municipio`, `academico.sistema_educativo`, `academico.nivel_formacion`, `academico.area_formacion`, `academico.area_academica`, `academico.region`, `academico.campus`, `academico.grado_academico` y `plazas.tratamiento_academico`:

  ```sql
  SELECT setval(pg_get_serial_sequence('academico.municipio', 'id'), (SELECT max(id) FROM academico.municipio));
  ```

  Sin esto, el primer alta por la API choca con un id del seed. `usuarios.rol` tiene id sin identidad y no lo necesita.
- Los valores booleanos (`1`/`0` en columnas `bit`) pasan a `true`/`false`.

### `development.sql`

La misma limpieza de sintaxis. No se agrega al migrador, como hasta ahora.

## Paso 5. Pruebas de integración (`tests/Sgpla.IntegrationTests/`)

| Archivo | Cambio |
|---|---|
| `Infraestructura/SqlServerFixture.cs` | Se renombra a `PostgreSqlFixture.cs` / `PostgreSqlFixture`. Usa `new PostgreSqlBuilder("postgres:18.x-alpine").Build()` y `NpgsqlConnectionStringBuilder { Database = "sgpla-bd" }`. La siembra de periodos se hace con `INSERT … VALUES (…) ON CONFLICT (clave) DO NOTHING` (comprueba que `clave` tiene restricción única en el baseline). Las constantes `ClavePeriodoActual`/`ClavePeriodoSiguiente` se conservan. El atributo `[assembly: AssemblyFixture]` apunta al tipo nuevo |
| Todas las clases de prueba que reciben `SqlServerFixture sqlServer` | Reciben `PostgreSqlFixture` (el nombre del parámetro, por ejemplo `baseDatos`, a tu criterio, igual en todas) |
| `Infraestructura/SgplaApiFactory.cs` | El tipo del fixture y sus constantes |
| `EsqueletoTests.cs` | Las consultas a `sys.schemas`, `sys.tables` y `sys.columns` pasan a `information_schema.schemata`/`tables`/`columns`, con los mismos conteos esperados (23/1/25/5). `Migraciones_SolicitudAperturaSinVinculacionYConVersion` pasa a comprobar que no existen `programacion_academica_id`, `vinculada_en`, `vinculada_por_usuario_id` **ni** `version`, y se renombra según el nuevo sentido (la concurrencia ahora usa `xmin`) |
| `MigracionesTests.cs` | La bitácora se consulta como `public.schema_versions` con la columna real de dbup-postgresql. La prueba de la base sin baseline crea `"sgpla-sin-baseline"` conectada a la base `postgres` (`CREATE DATABASE "sgpla-sin-baseline"`), crea `public.existente` y cuenta con `SELECT count(*) FROM pg_tables WHERE schemaname NOT IN ('pg_catalog','information_schema')` (el resultado es `long`: ajusta el cast) |
| `ViolacionUnicidadTests.cs` | `ProvocarViolacionAsync` espera `PostgresException` con `SqlState == PostgresErrorCodes.UniqueViolation` y `ConstraintName == "uq_grado_academico__nombre"`. `ErrorDeComandoEnEfCore_NoRegistraElValorDuplicado` deja de afirmar que el mensaje trae el valor: afirma que la excepción es una violación de unicidad, que los logs no contienen el valor y que `MessageText` tampoco. La regla que protege, no registrar el valor duplicado, queda igual |
| `ConcurrenciaTests.cs` | `NpgsqlConnection`; el `UPDATE … SET justificacion = justificacion WHERE id = @id` renueva `xmin`. El comentario de `rowversion` pasa a `xmin` |
| `OfertaEducativa/DatosAcademicosSql.cs`, `SolicitudesApertura/DatosSolicitudesSql.cs`, `Usuarios/SesionEndpointsTests.cs` | `OUTPUT INSERTED.id` pasa a `RETURNING id`. Los lotes con variables (`DECLARE @x … SET @x = SCOPE_IDENTITY()`) se reescriben como inserciones encadenadas con `RETURNING`, CTE (`WITH p AS (INSERT … RETURNING id) …`) o varias órdenes desde C#. `SqlConnection`/`SqlCommand`/`AddWithValue` pasan a sus equivalentes de Npgsql (parámetros `@nombre`). Revisa los tipos devueltos (`int`/`long`) |
| `SeedTests.cs`, `ExperienciasEducativasContratoTests.cs`, `SolicitudAperturaEndpointsTests.cs`, `Catalogos/*`, `Institucional/*` y demás con SQL a mano | La misma traducción: `N'`, `TOP n` a `LIMIT n`, booleanos y funciones (`LEN` a `length`, `GETUTCDATE` a `now()`) |

Las aserciones sobre listados siguen acotadas a los datos que crea cada prueba (regla 9).

## Paso 6. Docker, configuración, CI y scripts

- `docker-compose.yml`:
  - El ancla de la cadena de conexión pasa a `Host=postgres;Port=5432;Database=sgpla-bd;Username=sgpla;Password=${SGPLA_DB_PASSWORD:-Sgpla_Dev_2026!}`.
  - El servicio `sqlserver` pasa a `postgres`: imagen `postgres:18.x-alpine`, `POSTGRES_DB: sgpla-bd`, `POSTGRES_USER: sgpla`, `POSTGRES_PASSWORD`, puerto `${SGPLA_DB_PORT:-5432}:5432`, volumen `postgres-data:/var/lib/postgresql` (comprueba la ruta de datos que espera la imagen 18) y health check `pg_isready -U sgpla -d sgpla-bd`.
  - `migraciones` depende de `postgres`.
  - El volumen `sqlserver-data` pasa a `postgres-data`, y el comentario inicial se actualiza.
- `.env.example`: `SGPLA_SA_PASSWORD` pasa a `SGPLA_DB_PASSWORD` y `SGPLA_SQL_PORT=1433` a `SGPLA_DB_PORT=5432`, con los comentarios correspondientes.
- `src/Sgpla.Api/appsettings.Development.json`: `Host=localhost;Port=5432;Database=sgpla-bd;Username=sgpla;Password=Sgpla_Dev_2026!`.
- `.github/workflows/backend-ci.yml`: el paso pasa a llamarse "Pruebas de integración (PostgreSQL en contenedor)".
- `sgpla-backend/scripts/verify.sh`: el comentario de la línea 35 menciona SQL Server. `smoke.sh` no cambia, salvo que algo dependa del motor.

## Paso 7. Documentos y skills

Una contradicción entre documentos se corrige en este mismo PR.

- `DATABASE.md` y `DATABASE_DIAGRAM.md`:
  - el motor (`DATABASE.md:13`), la cabecera "Tipo SQL Server" y los tipos de cada columna (según la tabla del paso 4);
  - las intercalaciones (`es_ci_ai`) e `IDENTITY`;
  - la fila `version rowversion` (`DATABASE.md` ~1716) se quita y se anota que la concurrencia usa `xmin`;
  - las menciones a "SQL Server" como almacén pasan a "la base de datos".

  Sin cambiar reglas, entidades ni relaciones.
- `ESTANDAR_MODULOS.md`: las comparaciones por intercalación (~652), la unicidad 23505 (~940), las pruebas contra PostgreSQL (~964), el ejemplo del fixture (~1005) y la tabla de anti-patrones (~1084).
- `PLAN_INICIAL.md`, `README.md` y `sgpla-backend/README.md`: el motor, el compose, las variables de `.env` y cómo consultar la base con `psql` (`docker compose exec postgres psql -U sgpla -d sgpla-bd`).
- `DECISIONES.md`: una sección nueva con MIG-D1 a MIG-D6 (redactadas como las existentes) y una anotación en SOL-D2 que diga que MIG-D3 la modifica.
- Skills `.agents/skills/backend-testing`, `backend-verify` (`sqlcmd` pasa a `psql`) y `backend-resource`, siguiendo `skill-authoring`, incluido el CI `agents-ci`. Los envoltorios de `.claude/skills/` solo cambian si su descripción menciona SQL Server.
- Este archivo, `Migracion_PostgreSQL.md`, se elimina al cerrar el PR, una vez que su contenido está en `DECISIONES.md`.

## Verificación (al final, una sola vez)

1. `sgpla-backend/scripts/verify.sh`: build, format y las tres suites. Reporta los conteos del resumen tal como salgan, incluidos los avisos de atajos.
2. `sgpla-backend/scripts/smoke.sh`: entorno limpio, migraciones, bootstrap del Superusuario, inicio de sesión y token. El login LDAP real se omite (no hay red hacia la UV) y se reporta como omitido.
3. Con la API levantada y el token del smoke:
   - dar de alta un catálogo, por ejemplo un grado académico, para confirmar que la secuencia no choca con el seed;
   - repetir el alta con otras mayúsculas o acentos y confirmar el 409;
   - una búsqueda de entidades académicas sin acentos que encuentre un nombre con acento.
4. `psql` en un contenedor contra la base del compose:
   - `\dn` muestra los 4 esquemas;
   - hay 54 tablas;
   - `\dOs` muestra `es_ci_ai`;
   - los 7 índices parciales existen;
   - `public.schema_versions` tiene `baseline` y `baseline-seed`.

## Entrega

Reporta:

- la lista de commits;
- el resultado literal de los resúmenes de `verify.sh` y `smoke.sh`;
- el resultado del paso 0;
- los paquetes con versión y licencia aprobadas;
- cualquier nota fuera de alcance.
