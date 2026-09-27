# Módulo Institucional

Especificación completa del módulo Institucional. Es normativa: quien lo implemente no debe decidir nada que no esté escrito aquí. Si algo falta o se contradice, se detiene la implementación y se aclara en este documento antes de programar.

Documentos relacionados:
- `ESTANDAR_MODULOS.md`: cómo se implementa cualquier módulo (capas, nombres, pruebas). Todo lo que este documento no dice explícitamente se hace como indica el estándar.
- `DATABASE.md`: §6.1 a §6.5, §9 y §14 describen las tablas de este módulo. Este documento registra dónde se desvía de ellas.
- `pendientes.md`: reglas de Institucional que dependen de módulos que aún no existen.
- Referencia de implementación: el módulo Catalogos (`src/Modules/Catalogos`), sobre todo `Articulo` para recursos administrables y `CatalogosFijos` para los de solo lectura.

## Contenido

1. [Alcance](#1-alcance)
2. [Decisiones y desviaciones](#2-decisiones-y-desviaciones)
3. [Piezas compartidas nuevas](#3-piezas-compartidas-nuevas)
4. [Normalización y validación comunes](#4-normalización-y-validación-comunes)
5. [Municipio (en Catalogos)](#5-municipio-en-catalogos)
6. [Región y campus](#6-región-y-campus)
7. [Área académica](#7-área-académica)
8. [Entidad académica](#8-entidad-académica)
9. [Composición del módulo](#9-composición-del-módulo)
10. [Entregas (PR)](#10-entregas-pr)
11. [Criterios de terminado](#11-criterios-de-terminado)

## 1. Alcance

| Recurso | Tabla | Operaciones | Módulo |
|---|---|---|---|
| Municipio | `academico.municipio` | Listar y obtener (catálogo fijo) | **Catalogos** |
| Región | `academico.region` | Listar y obtener (solo lectura) | Institucional |
| Campus | `academico.campus` | Listar (filtro por región) y obtener (solo lectura) | Institucional |
| Área académica | `academico.area_academica` | Listar, obtener, crear, modificar y dar de baja | Institucional |
| Entidad académica | `academico.entidad_academica` | Listar paginado con filtros, obtener, crear, modificar y dar de baja | Institucional |

Fuera de alcance: restauración, autorización (llega con Usuarios) y las reglas que cruzan hacia OfertaEducativa y Usuarios. Todo eso está registrado en `pendientes.md`.

## 2. Decisiones y desviaciones

| # | Decisión | Desviación respecto a |
|---|---|---|
| D1 | Región y campus son de **solo lectura**: sus valores son los de la semilla (5 regiones y 24 campus) y no hay altas, modificaciones ni bajas. Se quita `fecha_eliminacion` de ambas tablas **editando `baseline.sql`**. Es una excepción puntual: la regla general del estándar ("el baseline no se edita, los cambios van en una migración") sigue vigente | `PLAN_INICIAL.md` (preveía CRUD), `DATABASE.md` §5, §6.1, §6.2 y §9.1, `ESTANDAR_MODULOS.md` §15 |
| D2 | Municipio pasa a Catalogos como catálogo fijo. Institucional depende de Catalogos (grafo nuevo: `Institucional → Catalogos`) y usa el contrato `IMunicipios` | `PLAN_INICIAL.md` (tabla de módulos y grafo) |
| D3 | Área y entidad académica **no se restauran**. Un registro dado de baja no existe para la API: `GET`, `PUT` y `DELETE` sobre él responden 404. No hay parámetro `incluirEliminados`. La excepción aplica **solo a Institucional**; el resto del modelo conserva la restauración | `DATABASE.md` §9.3 y §14 ("Baja y restauración"), `ESTANDAR_MODULOS.md` §9 (operaciones Restaurar y `incluirEliminados`) |
| D4 | `DELETE` sobre un registro ya dado de baja responde **404**, no 204 | `ESTANDAR_MODULOS.md` §6 (baja idempotente) |
| D5 | Una referencia a un área académica dada de baja se trata como inexistente: **400** con su campo, no 409 | `ESTANDAR_MODULOS.md` §9 (409 para "padres inactivos") |
| D6 | Se posponen las reglas que dependen de OfertaEducativa y Usuarios (ver `pendientes.md`). Mientras tanto, el área académica de una entidad **siempre** puede cambiarse, y las bajas solo se bloquean por las reglas internas del módulo | `DATABASE.md` §9.1, §9.2 y §10 |
| D7 | La lectura de entidades se filtrará por ámbito cuando exista Usuarios (DGAA: las entidades de su área; Entidad Académica: solo la suya). Regiones, campus y áreas serán visibles para cualquier usuario autenticado. La escritura será solo del Superusuario. Hoy no se implementa nada: cada grupo lleva el comentario de autorización del estándar | — |

## 3. Piezas compartidas nuevas

Se agregan en el PR 3 (sección 10). Son las piezas que `ESTANDAR_MODULOS.md` §2 marca como "pendiente, con Institucional".

### `IEliminable` (SharedKernel)

`src/BuildingBlocks/Sgpla.SharedKernel/IEliminable.cs`:

```csharp
namespace Sgpla.SharedKernel;

/// <summary>Entidad con baja lógica: <see cref="FechaEliminacion"/> en UTC; <c>null</c> significa activa.</summary>
public interface IEliminable
{
    DateTime? FechaEliminacion { get; }
}
```

Cada entidad eliminable declara la propiedad con `private set` y un método propio:

```csharp
/// <summary>Idempotente: si ya tiene fecha, no la reemplaza.</summary>
public void DarDeBaja(DateTime utc) => FechaEliminacion ??= utc;
```

### `FiltrosConsulta` (BuildingBlocks.Infrastructure)

`src/BuildingBlocks/Sgpla.BuildingBlocks.Infrastructure/Persistence/FiltrosConsulta.cs`:

```csharp
namespace Sgpla.BuildingBlocks.Infrastructure.Persistence;

/// <summary>Nombres de los filtros de consulta globales de EF Core.</summary>
public static class FiltrosConsulta
{
    /// <summary>Oculta las filas con <c>fecha_eliminacion</c>. Se omite con <c>IgnoreQueryFilters([BajaLogica])</c>.</summary>
    public const string BajaLogica = nameof(BajaLogica);
}
```

Uso en la configuración de una entidad eliminable:

```csharp
builder.HasQueryFilter(FiltrosConsulta.BajaLogica, a => a.FechaEliminacion == null);
```

Con el filtro activo, todas las consultas y los repositorios ven solo filas activas, y eso implementa D3 sin código adicional. **Solo** las comprobaciones de unicidad de clave usan `IgnoreQueryFilters([FiltrosConsulta.BajaLogica])`, porque la unicidad incluye las filas dadas de baja (`DATABASE.md` §5).

### `TimeProvider`

Hoy el host no lo registra. En `src/Sgpla.Api/Program.cs`, junto a los demás servicios, se agrega `builder.Services.AddSingleton(TimeProvider.System);`.

El handler que da de baja obtiene el instante así, truncado a segundos porque la columna es `datetime2(0)`:

```csharp
var ahora = reloj.GetUtcNow().UtcDateTime;
var instante = ahora.AddTicks(-(ahora.Ticks % TimeSpan.TicksPerSecond));
```

### Normalización de textos (dominio del módulo)

`src/Modules/Institucional/Sgpla.Modules.Institucional/Domain/Comun/Normalizacion.cs`:

```csharp
namespace Sgpla.Modules.Institucional.Domain.Comun;

internal static partial class Normalizacion
{
    /// <summary>"  Facultad   de  Letras " → "Facultad de Letras". <c>null</c> → "".</summary>
    public static string Texto(string? valor) => EspaciosRepetidos().Replace(valor?.Trim() ?? string.Empty, " ");

    /// <summary>Recorta; <c>null</c> → "". No quita separadores internos.</summary>
    public static string Recortar(string? valor) => valor?.Trim() ?? string.Empty;

    /// <summary>Solo caracteres ASCII '0' a '9' (no acepta dígitos de otros alfabetos).</summary>
    public static bool SonDigitos(string valor) => valor.All(c => c is >= '0' and <= '9');

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspaciosRepetidos();
}
```

## 4. Normalización y validación comunes

Todas estas reglas las aplica el **dominio** (fábrica `Crear` y método `Modificar`) y devuelven `Error.Validation(código, mensaje, campo)`, con el campo tomado con `nameof` de la propiedad de la entidad. No se escriben validators de FluentValidation para los datos de una entidad (`ESTANDAR_MODULOS.md` §7). Las fábricas devuelven el **primer** error que encuentran, en el orden de la tabla de cada recurso.

| Tipo de dato | Normalización | Regla | Errores (sufijo del código) |
|---|---|---|---|
| Texto libre obligatorio (nombre, calle, colonia) | `Normalizacion.Texto` | No vacío después de normalizar; longitud máxima de su columna | `<Campo>Vacio`, `<Campo>DemasiadoLargo` |
| Número exterior (opcional) | `null` se queda `null`; si llega un valor, `Normalizacion.Texto` | Si llegó un valor, no puede quedar vacío (`DATABASE.md` §14: se rechaza un número exterior solo con espacios). Máximo 20 | `NumeroExteriorVacio`, `NumeroExteriorDemasiadoLargo` |
| Clave alfanumérica (entidad) | `Normalizacion.Recortar` y `ToUpperInvariant()` | No vacía; máximo 50; solo `A`-`Z` y `0`-`9` (una `Ñ` o un guion se rechazan) | `ClaveVacia`, `ClaveDemasiadoLarga`, `ClaveFormatoInvalido` |
| Clave numérica (área) | Ninguna (es `int` en el JSON) | Mayor que cero | `ClaveNoPositiva` |
| Teléfono | `Normalizacion.Recortar` | No vacío; exactamente 10 caracteres y todos dígitos. No se quitan espacios, guiones, paréntesis ni `+52`: `"228 842 1700"` es un error | `TelefonoVacio`, `TelefonoFormatoInvalido` |
| Extensión (opcional) | `Normalizacion.Recortar`; si queda vacía (o llega `null`), es `null` | Si no es `null`: de 1 a 10 caracteres y todos dígitos. Conserva ceros iniciales | `ExtensionFormatoInvalido` |
| Código postal | `Normalizacion.Recortar` | No vacío; exactamente 5 dígitos. Conserva ceros iniciales | `CodigoPostalVacio`, `CodigoPostalFormatoInvalido` |

Un JSON mal formado o de tipo incorrecto (por ejemplo, `"clave": "abc"` en un campo `int`) lo rechaza ASP.NET Core con 400 antes de llegar al handler. Ese caso no se prueba ni se personaliza.

## 5. Municipio (en Catalogos)

Catálogo fijo de 212 municipios de Veracruz (`DATABASE.md` §6.4). El `id` es la clave municipal del INEGI y la semilla ya lo carga.

### Implementación

El frontend usa el listado en un Select con búsqueda, así que Municipio no usa `AddCatalogoFijo`/`MapCatalogoFijo`: tiene consultas propias, con el caso de `ESTANDAR_MODULOS.md` §9 "si tiene columnas adicionales o filtros" (como `TratamientoAcademico`), pero además paginado.

| Pieza | Archivo | Contenido |
|---|---|---|
| Entidad | `Catalogos/.../Domain/CatalogosFijos/Municipio.cs` | `internal sealed class Municipio : CatalogoFijo`, `LongitudMaximaNombre = 150`, constructor privado. Resumen: `Municipios de Veracruz (DATABASE.md §6.4). El id es la clave municipal del INEGI.` |
| Configuración | `Catalogos/.../Infrastructure/CatalogosFijos/CatalogoFijoConfiguration.cs` | `internal sealed class MunicipioConfiguration() : CatalogoFijoConfiguration<Municipio>("academico", "municipio", Municipio.LongitudMaximaNombre);` (sin cambios) |
| Consultas (Application) | `Catalogos/.../Application/CatalogosFijos/MunicipioConsultas.cs` | `ListarMunicipiosQuery(Paginacion Paginacion, string? Busqueda)` con `ListarMunicipiosValidator` (`PaginacionValidator` más `Busqueda` con `MaximumLength(150)`, reportado en el campo `busqueda`), y `ObtenerMunicipioQuery(int Id)`. La respuesta reutiliza el `CatalogoFijoResponse` genérico: Municipio no tiene columnas adicionales |
| Consultas (Infrastructure) | `Catalogos/.../Infrastructure/CatalogosFijos/MunicipioConsultas.cs` | `ListarMunicipiosHandler` (`IQueryHandler<ListarMunicipiosQuery, Pagina<CatalogoFijoResponse>>`) y `ObtenerMunicipioHandler`. Ambos se registran por escaneo (`AddHandlersModulo`), no con `AddCatalogoFijo<T>` |
| Endpoints | `Catalogos/.../Endpoints/CatalogosFijos/MunicipioEndpoints.cs` | `MapMunicipioEndpoints`, con `ListarMunicipiosRequest([FromQuery] pagina, tamanoPagina, busqueda)` enlazado con `[AsParameters]` |
| Rutas | `CatalogosModule.MapCatalogosEndpoints` | `grupo.MapMunicipioEndpoints();` |

`GET /api/v1/catalogos/municipios` acepta `pagina`, `tamanoPagina` y `busqueda`, todos opcionales:
- Sin parámetros, devuelve la primera página (20 elementos) en orden alfabético por nombre, con el id como desempate: el primero es `Acajete`. El orden es alfabético y no de id, porque el frontend lo usa en un Select.
- `busqueda` se recorta; si queda vacía (o llega solo con espacios), se ignora; admite hasta 150 caracteres (400 con el error en el campo `busqueda`). Filtra con `m.Nombre.Contains(busqueda)`, que se traduce a SQL y usa la colación `Modern_Spanish_100_CI_AI` de la columna, sin comparar en memoria ni usar `ToLower`: `"xalapa"`, `"XALAPA"` y `"xálapa"` encuentran igual a `"Xalapa"` (id 87). Es "contiene", no "empieza con": `"rica"` encuentra `"Poza Rica De Hidalgo"`.
- La paginación usa `PaginacionValidator` y `Pagina<CatalogoFijoResponse>`, como cualquier listado paginado del estándar: `pagina` desde 1, `tamanoPagina` de 1 a 100 (20 por omisión).

Respuesta: `{ "elementos": [{ "id": 87, "nombre": "Xalapa" }, ...], "pagina": 1, "tamanoPagina": 20, "total": 212 }`.

`GET /api/v1/catalogos/municipios/{id}` no cambia: 200 con `{id, nombre}`, o 404 con `codigo` `Municipio.NoEncontrado`.

### Contrato `IMunicipios`

`Catalogos/.../Application/Contracts/IMunicipios.cs` (público, como todo `Contracts`):

```csharp
namespace Sgpla.Modules.Catalogos.Application.Contracts;

/// <summary>Consulta de municipios para otros módulos (Institucional, por el domicilio de una entidad académica).</summary>
public interface IMunicipios
{
    Task<bool> ExisteAsync(int id, CancellationToken cancellationToken);

    /// <summary>Nombre de cada id que existe; los que no existen no aparecen en el diccionario.</summary>
    Task<IReadOnlyDictionary<int, string>> ObtenerNombresAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken);
}
```

Implementación en `Catalogos/.../Infrastructure/CatalogosFijos/Municipios.cs`: `internal sealed class Municipios(SgplaDbContext contexto) : IMunicipios`.
- `ExisteAsync` usa `AnyAsync(m => m.Id == id)`.
- `ObtenerNombresAsync` hace un `AsNoTracking().Where(m => ids.Contains(m.Id)).ToDictionaryAsync(m => m.Id, m => m.Nombre)`. Si `ids` está vacío, devuelve un diccionario vacío sin consultar.

Se registra en `AddCatalogosModule` con `services.AddScoped<IMunicipios, Municipios>();`.

### Dependencia Institucional → Catalogos

En el mismo PR se hacen los tres cambios que exige `ESTANDAR_MODULOS.md` §10:
1. `Sgpla.Modules.Institucional.csproj`: `ProjectReference` a `..\..\Catalogos\Sgpla.Modules.Catalogos\Sgpla.Modules.Catalogos.csproj`.
2. `tests/Sgpla.ArchitectureTests/Modulos.cs`: `["Institucional"] = ["Catalogos"]`. El grafo sigue siendo acíclico porque Catalogos no depende de nadie.
3. `PLAN_INICIAL.md`: el grafo pasa a decir "Institucional → Catalogos", y en la tabla de módulos `municipio` sale de Institucional y entra en la lista de catálogos fijos de Catalogos.

## 6. Región y campus

Catálogos de solo lectura con los valores de la semilla (D1). No tienen fábrica, comportamiento ni baja.

### Esquema (PR 2)

En `src/Sgpla.Database/Baseline/baseline.sql` se elimina la línea `fecha_eliminacion datetime2(0) NULL,` de `academico.region` y de `academico.campus`. No cambia nada más.

### Dominio

`Domain/Ubicaciones/` (la carpeta no se llama `Campus` para no chocar con el nombre de la clase):

```csharp
/// <summary>Regiones universitarias de la UV (DATABASE.md §6.1). Solo lectura: los valores los carga la semilla.</summary>
internal sealed class Region : Entity
{
    public const int LongitudMaximaNombre = 200;

    private Region()
    {
    }

    public int Clave { get; private set; }

    public string Nombre { get; private set; } = string.Empty;
}

/// <summary>Campus de la UV (DATABASE.md §6.2). Solo lectura: los valores los carga la semilla.</summary>
internal sealed class Campus : Entity
{
    public const int LongitudMaximaClave = 50;
    public const int LongitudMaximaNombre = 200;

    private Campus()
    {
    }

    public string Clave { get; private set; } = string.Empty;

    public string Nombre { get; private set; } = string.Empty;

    public int RegionId { get; private set; }
}
```

`Domain/Ubicaciones/UbicacionErrors.cs`:

| Método | Código | Tipo | Mensaje |
|---|---|---|---|
| `RegionNoEncontrada(int id)` | `Region.NoEncontrado` | NotFound | `No existe la región {id}.` |
| `CampusNoEncontrado(int id)` | `Campus.NoEncontrado` | NotFound | `No existe el campus {id}.` |

### Configuración EF

`Infrastructure/Ubicaciones/RegionConfiguration.cs` y `CampusConfiguration.cs`:
- `ToTable("region", "academico")` y `ToTable("campus", "academico")`, con `HasKey(x => x.Id)`.
- `Nombre` con `HasMaxLength(LongitudMaximaNombre)`.
- `Campus.Clave` con `HasMaxLength(Campus.LongitudMaximaClave).IsUnicode(false)`, porque la columna es `varchar`.
- Sin navegaciones.

### Consultas

`Application/Ubicaciones/UbicacionConsultas.cs`:

```csharp
internal sealed record RegionResponse(int Id, int Clave, string Nombre);

internal sealed record CampusResponse(int Id, string Clave, string Nombre, RegionResponse Region);

/// <summary>Todas las regiones, en orden de clave.</summary>
internal sealed record ListarRegionesQuery;

internal sealed record ObtenerRegionQuery(int Id);

/// <summary>Los campus en orden de clave; con <paramref name="RegionId"/>, solo los de esa región.</summary>
/// <param name="RegionId">Filtro opcional. Una región inexistente no es error: el listado queda vacío.</param>
internal sealed record ListarCampusQuery(int? RegionId);

internal sealed class ListarCampusValidator : AbstractValidator<ListarCampusQuery>
{
    public ListarCampusValidator()
    {
        RuleFor(q => q.RegionId).GreaterThan(0).WithName("región").When(q => q.RegionId is not null);
    }
}

internal sealed record ObtenerCampusQuery(int Id);
```

`Infrastructure/Ubicaciones/UbicacionConsultas.cs` contiene los cuatro handlers (`ListarRegionesHandler`, `ObtenerRegionHandler`, `ListarCampusHandler` y `ObtenerCampusHandler`). Todos usan `AsNoTracking` y proyectan en SQL. Los de campus hacen `join` con `Region` para anidarla, igual que `TratamientoAcademicoConsultas.cs` en Catalogos. Orden: regiones por `Clave`; campus por `Clave` y luego `Id`.

### Endpoints

`Endpoints/Ubicaciones/RegionEndpoints.cs` y `CampusEndpoints.cs`:

| Método y ruta | `WithName` | `WithSummary` | Éxito | Errores |
|---|---|---|---|---|
| `GET /api/v1/institucional/regiones` | `ListarRegiones` | Lista todas las regiones, en orden de clave. | 200 arreglo | — |
| `GET /api/v1/institucional/regiones/{id:int}` | `ObtenerRegion` | Obtiene una región. | 200 | 404 `Region.NoEncontrado` |
| `GET /api/v1/institucional/campus?regionId=` | `ListarCampus` | Lista los campus, opcionalmente de una sola región. | 200 arreglo | 400 si `regionId <= 0` (error en `regionId`) |
| `GET /api/v1/institucional/campus/{id:int}` | `ObtenerCampus` | Obtiene un campus. | 200 | 404 `Campus.NoEncontrado` |

Tags: `Regiones` y `Campus`. El parámetro `regionId` llega en `ListarCampusRequest([FromQuery(Name = "regionId")] int? RegionId)`, declarado en el mismo archivo de endpoints y enlazado con `[AsParameters]`. Un `POST`, `PUT` o `DELETE` responde 405.

Respuesta de campus:

```json
{ "id": 1, "clave": "X", "nombre": "Xalapa", "region": { "id": 1, "clave": 1, "nombre": "Xalapa" } }
```

## 7. Área académica

Catálogo global administrable (`DATABASE.md` §6.3). Tiene baja lógica y no se restaura (D3).

### Dominio

`Domain/AreasAcademicas/AreaAcademica.cs`: `internal sealed class AreaAcademica : Entity, IEliminable`.

| Propiedad | Tipo | Constante | Mutable |
|---|---|---|---|
| `Clave` | `int` | — | No |
| `Nombre` | `string` | `LongitudMaximaNombre = 200` | Sí |
| `Telefono` | `string` | `LongitudTelefono = 10` | Sí |
| `Extension` | `string?` | `LongitudMaximaExtension = 10` | Sí |
| `FechaEliminacion` | `DateTime?` | — | Solo con `DarDeBaja` |

Métodos:
- `static Result<AreaAcademica> Crear(int clave, string nombre, string telefono, string? extension)`. Valida en este orden: clave, nombre, teléfono y extensión.
- `Result Modificar(string nombre, string telefono, string? extension)`. Valida igual, en el mismo orden, y solo asigna si todo es válido: si falla, la entidad no cambia. Es un reemplazo completo: una `extension` `null` o vacía **quita** la extensión.
- `void DarDeBaja(DateTime utc)`, como en la sección 3.

`Domain/AreasAcademicas/AreaAcademicaErrors.cs`:

| Código | Tipo | Campo | Mensaje |
|---|---|---|---|
| `AreaAcademica.ClaveNoPositiva` | Validation | `Clave` | La clave debe ser mayor que cero. |
| `AreaAcademica.NombreVacio` | Validation | `Nombre` | El nombre es obligatorio. |
| `AreaAcademica.NombreDemasiadoLargo` | Validation | `Nombre` | El nombre admite hasta 200 caracteres. |
| `AreaAcademica.TelefonoVacio` | Validation | `Telefono` | El teléfono es obligatorio. |
| `AreaAcademica.TelefonoFormatoInvalido` | Validation | `Telefono` | El teléfono debe tener exactamente diez dígitos, sin espacios ni separadores. |
| `AreaAcademica.ExtensionFormatoInvalido` | Validation | `Extension` | La extensión admite de uno a diez dígitos. |
| `AreaAcademica.ClaveDuplicada` | Conflict | — | Ya existe un área académica con esa clave. |
| `AreaAcademica.TieneEntidadesActivas` | Conflict | — | El área académica tiene entidades académicas activas. |
| `AreaAcademica.NoEncontrado(int id)` | NotFound | — | No existe el área académica {id}. |

Los mensajes con longitudes se construyen con las constantes, no con literales.

### Application

`Application/AreasAcademicas/IAreaAcademicaRepository.cs`:

```csharp
internal interface IAreaAcademicaRepository
{
    /// <summary>Solo activas (filtro de baja lógica).</summary>
    Task<AreaAcademica?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Incluye las dadas de baja: la clave no se reutiliza.</summary>
    Task<bool> ExisteClaveAsync(int clave, CancellationToken cancellationToken);

    Task<bool> TieneEntidadesActivasAsync(int areaAcademicaId, CancellationToken cancellationToken);

    void Agregar(AreaAcademica areaAcademica);
}
```

`Application/AreasAcademicas/AreaAcademicaConsultas.cs`:

```csharp
internal sealed record AreaAcademicaResponse(int Id, int Clave, string Nombre, string Telefono, string? Extension)
{
    public static AreaAcademicaResponse Desde(AreaAcademica area) =>
        new(area.Id, area.Clave, area.Nombre, area.Telefono, area.Extension);
}

/// <summary>Todas las áreas activas, en orden de clave.</summary>
internal sealed record ListarAreasAcademicasQuery;

internal sealed record ObtenerAreaAcademicaQuery(int Id);
```

Comandos. Cada uno tiene su archivo, con el Command y el Handler y sin validator:

| Archivo | Command | Handler (dependencias) | Pasos |
|---|---|---|---|
| `CrearAreaAcademica.cs` | `CrearAreaAcademicaCommand(int Clave, string Nombre, string Telefono, string? Extension)` | `ICommandHandler<CrearAreaAcademicaCommand, AreaAcademicaResponse>` (`IAreaAcademicaRepository`, `IUnitOfWork`) | 1. `AreaAcademica.Crear`; si falla, devuelve su error. 2. `ExisteClaveAsync` → `ClaveDuplicada`. 3. `Agregar` y `SaveChangesAsync`. 4. Devuelve `AreaAcademicaResponse.Desde` |
| `ModificarAreaAcademica.cs` | `ModificarAreaAcademicaCommand(int Id, string Nombre, string Telefono, string? Extension)` | `ICommandHandler<ModificarAreaAcademicaCommand>` (`IAreaAcademicaRepository`, `IUnitOfWork`) | 1. `ObtenerPorIdAsync`; si es `null`, `NoEncontrado`. 2. `Modificar`; si falla, devuelve su error. 3. `SaveChangesAsync` (una sola vez) |
| `DarDeBajaAreaAcademica.cs` | `DarDeBajaAreaAcademicaCommand(int Id)` | `ICommandHandler<DarDeBajaAreaAcademicaCommand>` (`IAreaAcademicaRepository`, `IUnitOfWork`, `TimeProvider`) | 1. `ObtenerPorIdAsync`; si es `null`, `NoEncontrado` (una dada de baja también, por D4). 2. `TieneEntidadesActivasAsync` → `TieneEntidadesActivas`. 3. `DarDeBaja(instante)` y `SaveChangesAsync` |

`ModificarAreaAcademica` no compara valores anteriores: si nada cambió, EF no emite `UPDATE` y el resultado es 204 igualmente.

### Infrastructure

- `AreaAcademicaConfiguration.cs`:
  - `ToTable("area_academica", "academico")`, con `HasKey`.
  - `Nombre` con `HasMaxLength(200)`.
  - `Telefono` con `HasMaxLength(10).IsUnicode(false).IsFixedLength()`, porque la columna es `char(10)`.
  - `Extension` con `HasMaxLength(10).IsUnicode(false)`.
  - El filtro `HasQueryFilter(FiltrosConsulta.BajaLogica, a => a.FechaEliminacion == null)`.
- `AreaAcademicaRepository.cs`:
  - `ExisteClaveAsync` usa `IgnoreQueryFilters([FiltrosConsulta.BajaLogica])`.
  - `TieneEntidadesActivasAsync` hace `contexto.Set<EntidadAcademica>().AnyAsync(e => e.AreaAcademicaId == id)`; el filtro de baja lógica de `EntidadAcademica` ya excluye las inactivas.
  - En el PR 3, `EntidadAcademica` todavía no está mapeada, así que ese método se agrega en el PR 4. Hasta entonces devuelve `Task.FromResult(false)` con el comentario `// PR 4: consultar entidades activas.`, y la prueba de ese 409 se escribe en el PR 4.
- `AreaAcademicaConsultas.cs`: `ListarAreasAcademicasHandler` (orden por `Clave`) y `ObtenerAreaAcademicaHandler` (404 `NoEncontrado`), con proyección directa a `AreaAcademicaResponse`.

### Endpoints

`Endpoints/AreasAcademicas/AreaAcademicaEndpoints.cs`, grupo `/areas-academicas` con tag `Áreas académicas`:

| Método y ruta | `WithName` | `WithSummary` | Body | Éxito | Errores declarados |
|---|---|---|---|---|---|
| `GET /` | `ListarAreasAcademicas` | Lista todas las áreas académicas activas, en orden de clave. | — | 200 arreglo | — |
| `GET /{id:int}` | `ObtenerAreaAcademica` | Obtiene un área académica activa. | — | 200 | 404 |
| `POST /` | `CrearAreaAcademica` | Registra un área académica. | `CrearAreaAcademicaCommand` enlazado directamente | 201 con `Location` (`CreatedAtRoute` a `ObtenerAreaAcademica`) y el recurso | 400, 409 |
| `PUT /{id:int}` | `ModificarAreaAcademica` | Modifica el nombre y los datos de contacto de un área académica. | `ModificarAreaAcademicaRequest(string Nombre, string Telefono, string? Extension)` | 204 | 400, 404 |
| `DELETE /{id:int}` | `DarDeBajaAreaAcademica` | Da de baja un área académica sin entidades académicas activas. | — | 204 | 404, 409 |

Ejemplo de `POST`:

```json
{ "clave": 30, "nombre": "Área Académica de Humanidades", "telefono": "2288421700", "extension": "11350" }
```

Respuesta 201 (y la de `GET /{id}`):

```json
{ "id": 1, "clave": 30, "nombre": "Área Académica de Humanidades", "telefono": "2288421700", "extension": "11350" }
```

## 8. Entidad académica

Unidad académica de un campus, clasificada por un área y localizada en un municipio (`DATABASE.md` §6.5). Tiene baja lógica y no se restaura (D3).

### Dominio

`Domain/EntidadesAcademicas/EntidadAcademica.cs`: `internal sealed class EntidadAcademica : Entity, IEliminable`.

| Propiedad | Tipo | Constante | Mutable |
|---|---|---|---|
| `Clave` | `string` | `LongitudMaximaClave = 50` | No |
| `Nombre` | `string` | `LongitudMaximaNombre = 200` | Sí |
| `Calle` | `string` | `LongitudMaximaCalle = 200` | Sí |
| `NumeroExterior` | `string?` | `LongitudMaximaNumeroExterior = 20` | Sí |
| `Colonia` | `string` | `LongitudMaximaColonia = 150` | Sí |
| `CodigoPostal` | `string` | `LongitudCodigoPostal = 5` | Sí |
| `Telefono` | `string` | `LongitudTelefono = 10` | Sí |
| `Extension` | `string?` | `LongitudMaximaExtension = 10` | Sí |
| `CampusId` | `int` | — | No |
| `AreaAcademicaId` | `int` | — | Sí (D6) |
| `MunicipioId` | `int` | — | Sí |
| `FechaEliminacion` | `DateTime?` | — | Solo con `DarDeBaja` |

Métodos:
- `static Result<EntidadAcademica> Crear(string clave, string nombre, string calle, string? numeroExterior, string colonia, string codigoPostal, string telefono, string? extension, int campusId, int areaAcademicaId, int municipioId)`. Valida en este orden: clave, nombre, calle, número exterior, colonia, código postal, teléfono y extensión. Los ids no se validan aquí: su existencia depende de la base y la comprueba el handler.
- `Result Modificar(string nombre, string calle, string? numeroExterior, string colonia, string codigoPostal, string telefono, string? extension, int areaAcademicaId, int municipioId)`. Mismo orden y mismas reglas que `Crear`, sin clave ni campus. Asigna todo solo si todo es válido, y es un reemplazo completo: `numeroExterior` o `extension` en `null` los quitan.
- `void DarDeBaja(DateTime utc)`.

`Domain/EntidadesAcademicas/EntidadAcademicaErrors.cs`. Todos los de validación llevan como campo el `nameof` de la propiedad:

| Código | Tipo | Campo | Mensaje |
|---|---|---|---|
| `EntidadAcademica.ClaveVacia` | Validation | `Clave` | La clave es obligatoria. |
| `EntidadAcademica.ClaveDemasiadoLarga` | Validation | `Clave` | La clave admite hasta 50 caracteres. |
| `EntidadAcademica.ClaveFormatoInvalido` | Validation | `Clave` | La clave solo admite letras de la A a la Z sin acentos y dígitos. |
| `EntidadAcademica.NombreVacio` / `NombreDemasiadoLargo` | Validation | `Nombre` | El nombre es obligatorio. / El nombre admite hasta 200 caracteres. |
| `EntidadAcademica.CalleVacia` / `CalleDemasiadoLarga` | Validation | `Calle` | La calle es obligatoria. / La calle admite hasta 200 caracteres. |
| `EntidadAcademica.NumeroExteriorVacio` / `NumeroExteriorDemasiadoLargo` | Validation | `NumeroExterior` | El número exterior no puede estar vacío; omítelo si el inmueble no tiene número. / El número exterior admite hasta 20 caracteres. |
| `EntidadAcademica.ColoniaVacia` / `ColoniaDemasiadoLarga` | Validation | `Colonia` | La colonia es obligatoria. / La colonia admite hasta 150 caracteres. |
| `EntidadAcademica.CodigoPostalVacio` / `CodigoPostalFormatoInvalido` | Validation | `CodigoPostal` | El código postal es obligatorio. / El código postal debe tener exactamente cinco dígitos. |
| `EntidadAcademica.TelefonoVacio` / `TelefonoFormatoInvalido` | Validation | `Telefono` | El teléfono es obligatorio. / El teléfono debe tener exactamente diez dígitos, sin espacios ni separadores. |
| `EntidadAcademica.ExtensionFormatoInvalido` | Validation | `Extension` | La extensión admite de uno a diez dígitos. |
| `EntidadAcademica.CampusInexistente` | Validation | `CampusId` | No existe el campus indicado. |
| `EntidadAcademica.AreaAcademicaInexistente` | Validation | `AreaAcademicaId` | No existe un área académica activa con ese id. |
| `EntidadAcademica.MunicipioInexistente` | Validation | `MunicipioId` | No existe el municipio indicado. |
| `EntidadAcademica.ClaveDuplicada` | Conflict | — | Ya existe una entidad académica con esa clave. |
| `EntidadAcademica.NoEncontrado(int id)` | NotFound | — | No existe la entidad académica {id}. |

`CampusInexistente`, `AreaAcademicaInexistente` y `MunicipioInexistente` son errores de validación con campo. Como no son reglas de forma, los devuelve el handler, no la fábrica; `ToProblem` los reporta en `errors` igual.

### Application

`Application/EntidadesAcademicas/IEntidadAcademicaRepository.cs`:

```csharp
internal interface IEntidadAcademicaRepository
{
    /// <summary>Solo activas.</summary>
    Task<EntidadAcademica?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Compara la clave ya normalizada e incluye las dadas de baja.</summary>
    Task<bool> ExisteClaveAsync(string clave, CancellationToken cancellationToken);

    Task<bool> ExisteCampusAsync(int campusId, CancellationToken cancellationToken);

    /// <summary>Solo áreas activas: una dada de baja cuenta como inexistente (D5).</summary>
    Task<bool> ExisteAreaAcademicaActivaAsync(int areaAcademicaId, CancellationToken cancellationToken);

    void Agregar(EntidadAcademica entidadAcademica);
}
```

La existencia del municipio se consulta con `IMunicipios.ExisteAsync` (sección 5); no pasa por el repositorio.

`Application/EntidadesAcademicas/EntidadAcademicaConsultas.cs`:

```csharp
internal sealed record ReferenciaResponse(int Id, string Nombre);

internal sealed record AreaAcademicaResumenResponse(int Id, int Clave, string Nombre);

internal sealed record EntidadAcademicaResponse(
    int Id,
    string Clave,
    string Nombre,
    string Calle,
    string? NumeroExterior,
    string Colonia,
    string CodigoPostal,
    string Telefono,
    string? Extension,
    CampusResponse Campus,
    AreaAcademicaResumenResponse AreaAcademica,
    ReferenciaResponse Municipio);

/// <summary>Filtros opcionales del listado; todos se combinan con AND. Un texto vacío o solo con espacios se ignora.</summary>
internal sealed record FiltrosEntidadesAcademicas(
    int? RegionId,
    int? CampusId,
    int? AreaAcademicaId,
    int? MunicipioId,
    string? Busqueda,
    string? Calle,
    string? Colonia,
    string? NumeroExterior,
    string? CodigoPostal,
    string? Telefono);

/// <summary>Entidades activas en orden de clave (con el id como desempate), paginadas.</summary>
internal sealed record ListarEntidadesAcademicasQuery(Paginacion Paginacion, FiltrosEntidadesAcademicas Filtros);

internal sealed record ObtenerEntidadAcademicaQuery(int Id);
```

`CampusResponse` es el de la sección 6.

`ListarEntidadesAcademicasValidator`, en el mismo archivo. Los nombres de campo coinciden con los parámetros HTTP, igual que `PaginacionValidator`:
- `Include(new PaginacionValidator<ListarEntidadesAcademicasQuery>(q => q.Paginacion))`.
- `RegionId`, `CampusId`, `AreaAcademicaId` y `MunicipioId`: `GreaterThan(0)` cuando no son `null`.
- `Busqueda`, `Calle`, `Colonia` y `NumeroExterior`: `MaximumLength(200)` cuando no son `null`.
- `CodigoPostal`, cuando no es `null` ni vacío después de recortar: exactamente 5 dígitos.
- `Telefono`, cuando no es `null` ni vacío después de recortar: exactamente 10 dígitos.
- Cada regla lleva `OverridePropertyName` con el nombre del parámetro (`regionId`, `codigoPostal`...) y `WithName` con el nombre legible (`región`, `código postal`...).

Comandos:

| Archivo | Command | Dependencias del handler | Pasos |
|---|---|---|---|
| `CrearEntidadAcademica.cs` | `CrearEntidadAcademicaCommand(string Clave, string Nombre, string Calle, string? NumeroExterior, string Colonia, string CodigoPostal, string Telefono, string? Extension, int CampusId, int AreaAcademicaId, int MunicipioId)`; el handler devuelve el `int` del id creado | `IEntidadAcademicaRepository`, `IMunicipios`, `IUnitOfWork` | 1. `EntidadAcademica.Crear`. 2. `ExisteCampusAsync` → `CampusInexistente`. 3. `ExisteAreaAcademicaActivaAsync` → `AreaAcademicaInexistente`. 4. `IMunicipios.ExisteAsync` → `MunicipioInexistente`. 5. `ExisteClaveAsync` (con la clave normalizada) → `ClaveDuplicada`. 6. `Agregar` y `SaveChangesAsync`. 7. Devuelve el `Id` |
| `ModificarEntidadAcademica.cs` | `ModificarEntidadAcademicaCommand(int Id, string Nombre, string Calle, string? NumeroExterior, string Colonia, string CodigoPostal, string Telefono, string? Extension, int AreaAcademicaId, int MunicipioId)` | `IEntidadAcademicaRepository`, `IMunicipios`, `IUnitOfWork` | 1. `ObtenerPorIdAsync` → `NoEncontrado`. 2. `Modificar`. 3. `ExisteAreaAcademicaActivaAsync` → `AreaAcademicaInexistente`. 4. `IMunicipios.ExisteAsync` → `MunicipioInexistente`. 5. `SaveChangesAsync`. El área siempre puede cambiar (D6; ver `pendientes.md`) |
| `DarDeBajaEntidadAcademica.cs` | `DarDeBajaEntidadAcademicaCommand(int Id)` | `IEntidadAcademicaRepository`, `IUnitOfWork`, `TimeProvider` | 1. `ObtenerPorIdAsync` → `NoEncontrado`. 2. `DarDeBaja(instante)` y `SaveChangesAsync`. No hay cascada ni bloqueos todavía (D6) |

En `ModificarEntidadAcademica`, `Modificar` se aplica antes de validar las referencias. Si una referencia falla, el handler devuelve el error **sin llamar a `SaveChangesAsync`**, así que no se guarda nada.

La creación devuelve el id y no la respuesta completa, porque esa respuesta necesita datos de otras tablas y de Catalogos que un comando no consulta. El endpoint arma la respuesta 201 como se describe abajo.

### Infrastructure

`EntidadAcademicaConfiguration.cs`:
- `ToTable("entidad_academica", "academico")`, con `HasKey`.
- Longitudes con las constantes de la entidad.
- `Clave` con `IsUnicode(false)`.
- `CodigoPostal` y `Telefono` con `IsUnicode(false).IsFixedLength()`.
- `Extension` con `IsUnicode(false)`.
- El filtro de baja lógica.
- Sin navegaciones: `CampusId`, `AreaAcademicaId` y `MunicipioId` son `int`.

`EntidadAcademicaRepository.cs`:
- `ExisteClaveAsync` usa `IgnoreQueryFilters([FiltrosConsulta.BajaLogica])`.
- `ExisteCampusAsync` consulta `Set<Campus>()`.
- `ExisteAreaAcademicaActivaAsync` consulta `Set<AreaAcademica>()`, con el filtro activo.

`EntidadAcademicaConsultas.cs`:
- `ObtenerEntidadAcademicaHandler` (dependencias `SgplaDbContext` e `IMunicipios`):
  1. Consulta la entidad con `join` a `Campus`, `Region` y `AreaAcademica`, y proyecta a un registro intermedio privado con todos los campos y `MunicipioId`.
  2. Si no existe, devuelve 404 `NoEncontrado`.
  3. Obtiene el nombre con `IMunicipios.ObtenerNombresAsync([municipioId])` y arma `EntidadAcademicaResponse`.
- `ListarEntidadesAcademicasHandler` (mismas dependencias):
  1. Parte de `Set<EntidadAcademica>()` con los mismos `join` y aplica cada filtro presente (tabla siguiente).
  2. Ordena por `Clave` y luego por `Id`, y proyecta al registro intermedio.
  3. Llama a `PaginarAsync`.
  4. Con los `MunicipioId` distintos de la página, hace **una** llamada a `ObtenerNombresAsync` y arma `Pagina<EntidadAcademicaResponse>` con los mismos `NumeroPagina`, `TamanoPagina` y `Total`.

| Filtro | Traducción |
|---|---|
| `regionId` | `campus.RegionId == regionId` |
| `campusId`, `areaAcademicaId`, `municipioId` | igualdad sobre la columna |
| `busqueda` | Se recorta y se colapsan espacios. Coincide si `e.Clave.Contains(busqueda.ToUpperInvariant())` **o** `EF.Functions.Collate(e.Nombre, "Modern_Spanish_100_CI_AI").Contains(busqueda)`. La colación explícita hace falta porque `nombre` no la declara en el baseline y la búsqueda debe ignorar mayúsculas y acentos |
| `calle`, `colonia`, `numeroExterior` | Se recortan y se colapsan espacios. `Contains` directo: esas columnas ya tienen `Modern_Spanish_100_CI_AI` |
| `codigoPostal`, `telefono` | Se recortan. Igualdad exacta |

`Contains` se traduce a `LIKE` y EF Core escapa `%`, `_` y `[` del texto; no se construyen patrones a mano. La comparación ocurre siempre en SQL, nunca en memoria.

### Endpoints

`Endpoints/EntidadesAcademicas/EntidadAcademicaEndpoints.cs`, grupo `/entidades-academicas` con tag `Entidades académicas`:

| Método y ruta | `WithName` | `WithSummary` | Entrada | Éxito | Errores declarados |
|---|---|---|---|---|---|
| `GET /` | `ListarEntidadesAcademicas` | Lista las entidades académicas activas, paginadas y con filtros opcionales. | `[AsParameters] ListarEntidadesAcademicasRequest` | 200 `Pagina<EntidadAcademicaResponse>` | 400 |
| `GET /{id:int}` | `ObtenerEntidadAcademica` | Obtiene una entidad académica activa. | — | 200 | 404 |
| `POST /` | `CrearEntidadAcademica` | Registra una entidad académica. | `CrearEntidadAcademicaCommand` enlazado directamente | 201 con `Location` y el recurso completo | 400, 409 |
| `PUT /{id:int}` | `ModificarEntidadAcademica` | Modifica los datos editables de una entidad académica; la clave y el campus no cambian. | `ModificarEntidadAcademicaRequest` (los campos del Command sin `Id`) | 204 | 400, 404 |
| `DELETE /{id:int}` | `DarDeBajaEntidadAcademica` | Da de baja una entidad académica. | — | 204 | 404 |

`ListarEntidadesAcademicasRequest` declara cada parámetro con `[FromQuery(Name = ...)]`: `pagina`, `tamanoPagina`, `regionId`, `campusId`, `areaAcademicaId`, `municipioId`, `busqueda`, `calle`, `colonia`, `numeroExterior`, `codigoPostal` y `telefono`. Además tiene los métodos `ComoPaginacion()` (valores por omisión del estándar) y `ComoFiltros()`.

`POST` en el endpoint:
1. Invoca `ICommandHandler<CrearEntidadAcademicaCommand, int>`. Si falla, devuelve `ToProblem()`.
2. Si tiene éxito, invoca `IQueryHandler<ObtenerEntidadAcademicaQuery, EntidadAcademicaResponse>` con el id y responde `CreatedAtRoute(respuesta, "ObtenerEntidadAcademica", new { id })`.

Es el único endpoint que usa dos handlers. Es válido porque el endpoint compone, y ningún handler llama a otro. Un `PUT` o `POST` que llegue con `clave` o `campusId` de más no falla: esos campos no existen en el request y se ignoran.

Ejemplo de `POST`:

```json
{
  "clave": "FLX",
  "nombre": "Facultad de Letras Españolas",
  "calle": "Francisco Moreno",
  "numeroExterior": null,
  "colonia": "Ferrer Guardia",
  "codigoPostal": "91020",
  "telefono": "2288421700",
  "extension": "11350",
  "campusId": 1,
  "areaAcademicaId": 3,
  "municipioId": 87
}
```

Respuesta 201, `GET /{id}` y cada elemento del listado:

```json
{
  "id": 7,
  "clave": "FLX",
  "nombre": "Facultad de Letras Españolas",
  "calle": "Francisco Moreno",
  "numeroExterior": null,
  "colonia": "Ferrer Guardia",
  "codigoPostal": "91020",
  "telefono": "2288421700",
  "extension": "11350",
  "campus": { "id": 1, "clave": "X", "nombre": "Xalapa", "region": { "id": 1, "clave": 1, "nombre": "Xalapa" } },
  "areaAcademica": { "id": 3, "clave": 30, "nombre": "Área Académica de Humanidades" },
  "municipio": { "id": 87, "nombre": "Xalapa" }
}
```

El listado responde `{ "elementos": [...], "pagina": 1, "tamanoPagina": 20, "total": 1 }`. La respuesta no incluye `fechaEliminacion`, porque la API nunca devuelve registros dados de baja (D3).

## 9. Composición del módulo

`InstitucionalModule.cs` queda así:

- `AddInstitucionalModule`: `AddPersistenciaModulo(ensamblado)`, `AddHandlersModulo(ensamblado)`, `AddScoped<IAreaAcademicaRepository, AreaAcademicaRepository>()` y `AddScoped<IEntidadAcademicaRepository, EntidadAcademicaRepository>()`.
- `MapInstitucionalEndpoints`: `var grupo = endpoints.MapGroup(Ruta);` seguido del comentario de autorización con la matriz de D7, y luego `MapRegionEndpoints`, `MapCampusEndpoints`, `MapAreaAcademicaEndpoints` y `MapEntidadAcademicaEndpoints`. Se quita el `.WithTags("Institucional")` actual, porque cada recurso declara su tag.

`Sgpla.Modules.Institucional.csproj` agrega:
- `ProjectReference` a `Sgpla.BuildingBlocks.Application`, que hoy falta y hace falta para los handlers;
- la referencia a Catalogos (PR 1);
- `<InternalsVisibleTo Include="Sgpla.UnitTests" />`.

`tests/Sgpla.UnitTests/Sgpla.UnitTests.csproj` agrega la referencia al proyecto de Institucional.

Estructura final:

```
src/Modules/Institucional/Sgpla.Modules.Institucional/
  Domain/
    Comun/Normalizacion.cs
    Ubicaciones/Region.cs, Campus.cs, UbicacionErrors.cs
    AreasAcademicas/AreaAcademica.cs, AreaAcademicaErrors.cs
    EntidadesAcademicas/EntidadAcademica.cs, EntidadAcademicaErrors.cs
  Application/
    Ubicaciones/UbicacionConsultas.cs
    AreasAcademicas/IAreaAcademicaRepository.cs, AreaAcademicaConsultas.cs,
                    CrearAreaAcademica.cs, ModificarAreaAcademica.cs, DarDeBajaAreaAcademica.cs
    EntidadesAcademicas/IEntidadAcademicaRepository.cs, EntidadAcademicaConsultas.cs,
                        CrearEntidadAcademica.cs, ModificarEntidadAcademica.cs, DarDeBajaEntidadAcademica.cs
  Infrastructure/
    Ubicaciones/RegionConfiguration.cs, CampusConfiguration.cs, UbicacionConsultas.cs
    AreasAcademicas/AreaAcademicaConfiguration.cs, AreaAcademicaRepository.cs, AreaAcademicaConsultas.cs
    EntidadesAcademicas/EntidadAcademicaConfiguration.cs, EntidadAcademicaRepository.cs, EntidadAcademicaConsultas.cs
  Endpoints/
    Ubicaciones/RegionEndpoints.cs, CampusEndpoints.cs
    AreasAcademicas/AreaAcademicaEndpoints.cs
    EntidadesAcademicas/EntidadAcademicaEndpoints.cs
  InstitucionalModule.cs
```

## 10. Entregas (PR)

Cuatro PR en orden, cada uno desde `develop` y con la convención de ramas, commits y PR del repositorio. Cada PR deja el CI en verde por sí solo.

### PR 1: municipio en Catalogos

- Código: todo lo de la sección 5.
- Pruebas:
  - `tests/Sgpla.IntegrationTests/Catalogos/MunicipioEndpointsTests.cs`:
    - `Listar_SinParametros_Devuelve212EnOrdenAlfabetico`: `total` 212, `pagina` 1, `tamanoPagina` 20, 20 elementos y el primero `"Acajete"`.
    - `Listar_ConBusqueda_IgnoraMayusculasYAcentos`: un `Theory` con `"xalapa"`, `"XALAPA"` y `"xálapa"`, cada uno encuentra un solo elemento, id 87 `"Xalapa"`.
    - `Listar_ConBusquedaSinCoincidencias_DevuelveTotalCero`.
    - `Listar_ConPaginacionInvalida_Responde400`: `pagina=0` y `tamanoPagina=101`.
    - `Listar_ConBusquedaDemasiadoLarga_Responde400ConErrorEnBusqueda`: 151 caracteres, comprueba `errors.busqueda`.
    - `Obtener_Existente_Responde200`: id 87 → `"Xalapa"`.
    - `Obtener_Inexistente_Responde404ConCodigo`: id 999 → `Municipio.NoEncontrado`.
    - `Escritura_NoExiste_Responde405`: `POST`, `PUT /1` y `DELETE /1`.
  - Unitarias: ninguna, porque es un catálogo fijo sin fábrica ni comportamiento propio.
  - `SeedTests` ya verifica el conteo de 212.
- Documentos: `PLAN_INICIAL.md` (tabla de módulos y grafo) y `tests/Sgpla.ArchitectureTests/Modulos.cs`.

### PR 2: región y campus de solo lectura

- Código: la sección 6 completa.
- Pruebas:
  - `tests/Sgpla.IntegrationTests/Institucional/RegionEndpointsTests.cs`: el listado exacto de las 5 regiones `(id, clave, nombre)` de la semilla, en orden de clave; obtener 200 (id 3 → `"Orizaba-Córdoba"`) y 404 (`Region.NoEncontrado`), y 405 al escribir.
  - `CampusEndpointsTests.cs`:
    - el listado completo tiene 24 campus en orden de clave, y el primero es `"A"` (Acayucan);
    - `?regionId=2` devuelve exactamente `B` (Boca del Río) y `V` (Veracruz), con su región anidada;
    - `?regionId=99` devuelve un arreglo vacío;
    - `?regionId=0` responde 400 con error en `regionId`;
    - obtener: id 1 responde 200 con la región Xalapa anidada, y un id inexistente responde 404 (`Campus.NoEncontrado`);
    - 405 al escribir.
- Esquema: la edición de `baseline.sql` de la sección 6 (D1). Las bases locales se recrean con `docker compose down -v`; las pruebas de integración crean la suya en cada ejecución.
- Documentos:
  - `DATABASE.md`:
    - §5: en "Excepciones deliberadas", región y campus son catálogos fijos de la semilla, sin baja lógica.
    - §6.1 y §6.2: se quita la fila `fecha_eliminacion` y se agrega "solo lectura; valores de la semilla".
    - §9.1: la cascada empieza en `entidad_academica`, porque región y campus ya no se dan de baja.
  - `DATABASE_DIAGRAM.md`: se quita `fecha_eliminacion` de `ACADEMICO_REGION` y `ACADEMICO_CAMPUS`.
  - `PLAN_INICIAL.md`: en la fila de Institucional, región y campus son de solo lectura.

### PR 3: área académica

- Código: la sección 3 (`IEliminable`, `FiltrosConsulta`, `TimeProvider` y `Normalizacion`) y la sección 7.
- Pruebas unitarias (`tests/Sgpla.UnitTests/Institucional/AreasAcademicas/`):
  - `AreaAcademicaTests`: cada error de la tabla de la sección 7 con su campo; normalización del nombre (recorte y espacios colapsados); extensión vacía → `null`; conservar ceros iniciales (`"0228..."`, `"007"`); `Modificar` que falla no cambia nada; `DarDeBaja` es idempotente.
  - `CrearAreaAcademicaHandlerTests`, `ModificarAreaAcademicaHandlerTests` y `DarDeBajaAreaAcademicaHandlerTests`, con un repositorio falso, `UnitOfWorkFalso` y un `TimeProviderFalso` escrito a mano en `tests/Sgpla.UnitTests/Institucional/Fakes.cs` (`internal sealed class TimeProviderFalso(DateTimeOffset ahora) : TimeProvider` que sobrescribe `GetUtcNow()`). No se agrega ningún paquete, igual que en el estándar para los fakes. Se comprueba que el instante guardado se trunca a segundos, usando un `ahora` con milisegundos.
- Pruebas de integración (`tests/Sgpla.IntegrationTests/Institucional/AreaAcademicaEndpointsTests.cs`), cada una con sus propios datos:
  - `Crear_ConDatosValidos_Responde201ConUbicacionYValoresNormalizados`;
  - `Crear_ConCampoInvalido_Responde400ConErrorEnElCampo`, un `Theory` sobre clave 0, nombre vacío, teléfono de 9 dígitos, teléfono con espacios y extensión con letras, que comprueba `errors.<campo>`;
  - `Crear_ConClaveExistente_Responde409`;
  - `Crear_ConClaveDeUnAreaDadaDeBaja_Responde409`;
  - `Obtener_Existente_Responde200` y `Obtener_Inexistente_Responde404ConCodigo`;
  - `Obtener_DadaDeBaja_Responde404`;
  - `Listar_IncluyeLasCreadasEnOrdenDeClaveYExcluyeLasDadasDeBaja`: crea tres, da de baja una y comprueba las ids presentes y su orden relativo, sin afirmar conteos globales;
  - `Modificar_Responde204YPersisteLosCambios` y `Modificar_SinExtension_LaQuita`;
  - `Modificar_Inexistente_Responde404` y `Modificar_DadaDeBaja_Responde404`;
  - `DarDeBaja_Activa_Responde204` y `DarDeBaja_YaDadaDeBaja_Responde404`.
- `DatosUnicos` agrega `ClaveEntera()`: un `int` positivo aleatorio con `Random.Shared.Next(1, int.MaxValue)`.
- Documentos: en `ESTANDAR_MODULOS.md` §2 se quitan los "Pendiente" de `IEliminable` y de los filtros de consulta, y se describen.

### PR 4: entidad académica

- Código: la sección 8, más `TieneEntidadesActivasAsync` real en `AreaAcademicaRepository`.
- Pruebas unitarias (`tests/Sgpla.UnitTests/Institucional/EntidadesAcademicas/`):
  - `EntidadAcademicaTests`: cada error de validación con su campo; clave `" flx1 "` → `"FLX1"`; clave con `Ñ` o guion → `ClaveFormatoInvalido`; número exterior `null` aceptado y `"   "` rechazado; extensión vacía → `null`; ceros iniciales en código postal, teléfono y extensión; `Modificar` que falla no cambia nada.
  - Pruebas de los handlers de comando con fakes, incluido un `IMunicipios` falso. En `ModificarEntidadAcademicaHandlerTests`, una referencia inválida no llama a `SaveChangesAsync`.
- Pruebas de integración (`EntidadAcademicaEndpointsTests.cs`). Cada prueba crea su propia área con `ClaveEntera()` y usa campus y municipios de la semilla:
  - crear responde 201 con `Location`, respuesta anidada completa y valores normalizados;
  - crear responde 400 con error en cada campo inválido;
  - crear responde 400 con campus, área o municipio inexistentes, y con un área dada de baja;
  - crear con clave repetida (activa o dada de baja, y también en minúsculas) responde 409;
  - obtener responde 200, 404 si no existe y 404 si está dada de baja;
  - modificar responde 204 y se puede cambiar el área (D6); también 404, y 400 con un área o municipio inexistentes;
  - dar de baja responde 204, y 404 si ya estaba dada de baja;
  - cada filtro se comprueba por separado, más uno combinado. En `busqueda`, `calle` y `colonia` se usan mayúsculas y acentos distintos: se crea `"Facultad de Música"` y se busca `"musica"`. El listado nunca devuelve entidades dadas de baja;
  - la paginación devuelve `tamanoPagina` y `total` correctos sobre un conjunto filtrado por un área propia de la prueba, y rechaza `pagina=0` y `tamanoPagina=101` con 400.
- `AreaAcademicaEndpointsTests` agrega `DarDeBaja_ConEntidadesActivas_Responde409` y `DarDeBaja_SiSusEntidadesEstanDadasDeBaja_Responde204`.
- `DatosUnicos` agrega `ClaveAlfanumerica()`: los primeros 20 caracteres de `Guid.NewGuid().ToString("N")` en mayúsculas.
- Documentos:
  - `DATABASE.md` §9.3: nota de que área y entidad académica no se restauran (D3).
  - `DATABASE.md` §14 ("Baja y restauración"): se ajustan los casos que no aplican a Institucional.
  - `DATABASE.md` §9.2: se remite a `pendientes.md` para los bloqueos por usuarios.
  - `PLAN_INICIAL.md`: en la fila de Institucional, "CRUD y baja lógica, sin restauración".

## 11. Criterios de terminado

Cada PR cumple la lista de verificación de `ESTANDAR_MODULOS.md` §15, con esta excepción: el PR 2 edita `baseline.sql` por D1.

La verificación se ejecuta solo con Docker, usando la imagen `mcr.microsoft.com/dotnet/sdk:10.0.401`, sobre una copia del repositorio con los finales de línea normalizados a LF (el checkout de Windows usa CRLF y `.editorconfig` exige LF):

```sh
docker run --rm \
  -v <ruta>/sgpla-backend:/src:ro -v <ruta>/.editorconfig:/.editorconfig:ro \
  -v sgpla-nuget:/root/.nuget/packages -v //var/run/docker.sock:/var/run/docker.sock \
  -e TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal \
  mcr.microsoft.com/dotnet/sdk:10.0.401 sh -c "
    mkdir -p /w && cp /.editorconfig /w/ && cd /src &&
    tar --exclude='./**/bin' --exclude='./**/obj' --exclude=./TestResults -cf - . | (mkdir -p /w/b && cd /w/b && tar xf -) &&
    cd /w/b && find . -type f \( -name '*.cs' -o -name '*.csproj' -o -name '*.props' -o -name '*.json' -o -name '*.slnx' \) -exec sed -i 's/\r$//' {} + &&
    dotnet restore Sgpla.slnx && dotnet build Sgpla.slnx -c Release --no-restore &&
    dotnet format Sgpla.slnx --verify-no-changes --no-restore &&
    dotnet test -c Release --no-build --project tests/Sgpla.ArchitectureTests &&
    dotnet test -c Release --no-build --project tests/Sgpla.UnitTests &&
    dotnet test -c Release --no-build --project tests/Sgpla.IntegrationTests"
```

Todo debe terminar sin advertencias ni fallos. Si una prueba falla por una razón ajena al PR, se reporta: no se modifica la prueba para ocultarla.
