# Módulo OfertaEducativa

Especificación completa del módulo OfertaEducativa. Es normativa: quien lo implemente no debe decidir nada que no esté escrito aquí. Si algo falta o se contradice, se detiene la implementación y se aclara en este documento antes de programar.

Documentos relacionados:
- `ESTANDAR_MODULOS.md`: cómo se implementa cualquier módulo. Todo lo que este documento no dice explícitamente se hace como indica el estándar.
- `DATABASE.md`: §6.6 a §6.15, §7 a §12 y §14 describen las tablas y reglas de este módulo. Este documento registra dónde se desvía de ellas, y el PR 1 actualiza `DATABASE.md` para que coincida.
- `Modulo_Institucional.md`: el módulo del que depende OfertaEducativa. P1 y P2 cambian dos de sus handlers.
- `Modulo_Usuarios.md`: `ICurrentUser`, las políticas y la decisión D13 (políticas por rol cuando se necesiten).
- `pendientes.md`: P1 y P2 se resuelven en este módulo; P7, P8 y P9 nacen aquí.
- Referencias de implementación: Institucional (comandos, consultas paginadas con filtros y ámbito, contratos entre módulos) y Catalogos (`CatalogoFijo`, catálogos con columnas adicionales).

## Contenido

1. [Alcance](#1-alcance)
2. [Decisiones y desviaciones](#2-decisiones-y-desviaciones)
3. [Piezas compartidas](#3-piezas-compartidas)
4. [Normalización y validación comunes](#4-normalización-y-validación-comunes)
5. [Clasificaciones académicas (en Catalogos)](#5-clasificaciones-académicas-en-catalogos)
6. [Autorización y ámbito](#6-autorización-y-ámbito)
7. [Periodo escolar](#7-periodo-escolar)
8. [Programa educativo](#8-programa-educativo)
9. [Plan de estudios](#9-plan-de-estudios)
10. [Experiencia educativa](#10-experiencia-educativa)
11. [Programación académica y horarios](#11-programación-académica-y-horarios)
12. [Contratos entre módulos](#12-contratos-entre-módulos)
13. [Auditoría](#13-auditoría)
14. [Composición del módulo](#14-composición-del-módulo)
15. [Entregas (PR)](#15-entregas-pr)
16. [Criterios de terminado](#16-criterios-de-terminado)

## 1. Alcance

| Recurso | Tabla | Operaciones | Quién escribe | Módulo |
|---|---|---|---|---|
| Sistema educativo | `academico.sistema_educativo` | Listar y obtener (catálogo fijo) | Nadie | **Catalogos** |
| Nivel de formación | `academico.nivel_formacion` | Listar y obtener (catálogo fijo) | Nadie | **Catalogos** |
| Área de formación | `academico.area_formacion` | Listar y obtener (catálogo fijo) | Nadie | **Catalogos** |
| Periodo escolar | `academico.periodo_escolar` | Listar, obtener, crear, modificar fechas y dar de baja | Superusuario | OfertaEducativa |
| Programa educativo | `academico.programa_educativo` | Listar paginado con filtros, obtener, crear, modificar y dar de baja | DGAA | OfertaEducativa |
| Plan de estudios | `academico.plan_estudios` | Listar paginado con filtros, obtener, importar (crear con sus EE), dar de baja (con sus EE) y exportar a Excel | DGAA | OfertaEducativa |
| Experiencia educativa | `academico.experiencia_educativa` | Listar las de un plan, obtener, crear, modificar y dar de baja | DGAA | OfertaEducativa |
| Programación académica | `academico.programacion_academica` | Listar paginado con filtros y obtener (solo lectura) | Integracion (futuro) | OfertaEducativa |
| Horario | `academico.horario_programacion` | Listar las sesiones de una programación (solo lectura) | Integracion (futuro) | OfertaEducativa |

Fuera de alcance:
- restauración (D4);
- el archivo del plan de estudios, que se elimina del modelo (D5);
- lectura de archivos Excel en la API: el front interpreta el Excel y envía JSON (D6);
- alta, baja y horarios de programaciones, que hará la sincronización con PLANEA (D12, pendiente P9);
- los bloqueos por Solicitudes de Apertura, Avisos y sincronizaciones: aquí solo se declaran los contratos (P7 y P8);
- eventos de auditoría (sección 13).

## 2. Decisiones y desviaciones

| # | Decisión | Desviación respecto a |
|---|---|---|
| D1 | `sistema_educativo`, `nivel_formacion` y `area_formacion` son **catálogos fijos de solo lectura** con los valores de la semilla, y viven en **Catalogos** para reutilizar `CatalogoFijo`. Se quita `fecha_eliminacion` de las tres tablas **editando `baseline.sql`**, como excepción documentada (igual que D1 de Institucional) | `DATABASE.md` §5, §6.6, §6.7, §6.10, §9.2 y §10. `PLAN_INICIAL.md` (tabla de módulos) |
| D2 | La semilla de `area_formacion` se rehace con las claves que usa la UV en sus planes (`CODE_AREA_F`): `111` Área de Formación Básica, `112` Área de Formación Disciplinaria y `113` Área de Formación Terminal | `Baseline/seed.sql` (AFBG, AID, AFD, AFT y AFEL) |
| D3 | El grafo pasa a **OfertaEducativa → Institucional, Catalogos**. Catalogos expone el contrato `IClasificacionesAcademicas` | `PLAN_INICIAL.md` (grafo) |
| D4 | **Sin restauración** en todo el módulo, como en Institucional y Usuarios. Un registro dado de baja no existe para la API: `GET`, `PUT` y `DELETE` sobre él responden 404, y no hay `incluirEliminados`. `DELETE` sobre un registro ya dado de baja responde 404 | `DATABASE.md` §9.3 y §14 ("Baja y restauración"). `ESTANDAR_MODULOS.md` §6 y §9 |
| D5 | **El plan no tiene archivo**. Se eliminan la tabla `archivo_plan_estudios` y la columna `plan_estudios.archivo_plan_estudios_id`, con su FK y su UNIQUE, **editando `baseline.sql`**. La base es la única fuente de verdad: el Excel solo sirve para importar, y la descarga se genera desde la base (D7) | `DATABASE.md` §5, §6.9, §7, §8, §10, §11, §14 y §17. `PLAN_INICIAL.md` y `DATABASE_DIAGRAM.md` |
| D6 | **Importación del plan en JSON**. El front abre el Excel de la UV, permite corregirlo y envía `POST /planes-estudio` con el programa, el código y de 1 a 300 EE. La API no lee Excel. Todo o nada: si una EE falla, no se crea nada y se responde el **primer** error, con el índice de la EE en el campo (`experienciasEducativas[3].creditos`). La sección 9 documenta cómo mapea el front cada columna | — |
| D7 | **Exportación a Excel**. `GET /planes-estudio/{id}/excel` genera un `.xlsx` con los **14 encabezados** exactos del formato de la UV y las EE activas, con ClosedXML (licencia MIT) | — |
| D8 | El plan **no tiene campos editables**: el código y el programa son inmutables. "Modificar un plan" es crear, modificar o dar de baja sus EE una por una | `DATABASE.md` §6.9 (archivo reemplazable) |
| D9 | **Solo el DGAA escribe** programas, planes y EE, y solo en las entidades de su área. El Superusuario **lee todo y no escribe** en la estructura curricular. La Entidad Académica solo lee lo de su entidad. Nace la política `Dgaa` (D13 de Usuarios) | `DATABASE.md` §13.1 y §15.10 (acceso administrativo global del Superusuario) |
| D10 | **Sin cascada: la baja se bloquea con hijos activos** (409). La entidad académica no se da de baja con programas activos (P1). El programa no se da de baja con planes activos. La EE no se da de baja con programaciones activas. **Excepción:** `DELETE` de un plan da de baja el plan y todas sus EE activas con el mismo instante UTC, y se bloquea si alguna EE tiene programaciones activas | `DATABASE.md` §9.1 (cascada entidad → programa → plan → EE → programación). `pendientes.md` P1 (resolución prevista original) |
| D11 | **Periodo escolar:** el Superusuario lo crea, modifica sus fechas **en cualquier momento** y lo da de baja. La baja se **bloquea** si el periodo tiene cualquier programación (incluidas las dadas de baja) o cualquier referencia de otro módulo (P8). No hay cascada. La clave es inmutable | `DATABASE.md` §9.1 (periodo → programación en cascada) |
| D12 | **Programación académica y horarios son de solo lectura** en OfertaEducativa. Los crea y los da de baja la sincronización con PLANEA (Integracion), que se especifica después (P9) | `DATABASE.md` §12.1 (presuponía programaciones cargadas antes de sincronizar) |
| D13 | **Congelamiento de una EE:** horas teóricas, horas prácticas, créditos y área de formación solo cambian si la EE **nunca** tuvo una programación, incluidas las dadas de baja (409 si cambian). Nombre, perfil docente y cupos cambian siempre | Aclara `DATABASE.md` §10 ("antes de la primera programación") |
| D14 | P2 se mantiene: el área académica de una entidad se congela en cuanto la entidad tiene **cualquier** programa, incluidos los dados de baja | — (`DATABASE.md` §6.5 y §10) |
| D15 | Una referencia del cuerpo a un registro inexistente, dado de baja o **fuera del ámbito** del usuario responde 400 con su campo, sin revelar si existe (como D5 de Institucional). Un recurso de la ruta fuera del ámbito responde 404 | `ESTANDAR_MODULOS.md` §9 (409 para "padres inactivos") |
| D16 | `EntidadAcademicaResumen` (contrato de Institucional) agrega `AreaAcademicaId`, para resolver el ámbito del DGAA y el área de la exportación sin otro contrato | — |

## 3. Piezas compartidas

### Política `Dgaa` (PR 2)

`src/BuildingBlocks/Sgpla.BuildingBlocks.Application/Autorizacion.cs` agrega a `Politicas`:

```csharp
/// <summary><see cref="Autenticado"/> con el rol DGAA.</summary>
public const string Dgaa = nameof(Dgaa);
```

`UsuariosModule.AddUsuariosModule` la registra junto a las demás, igual que `Superusuario` pero con `Rol.Dgaa`:

```csharp
.AddPolicy(Politicas.Dgaa, politica => politica
    .RequireAuthenticatedUser()
    .AddRequirements(new SinCambioPendienteRequirement())
    .RequireClaim("rol", ((byte)Rol.Dgaa).ToString(CultureInfo.InvariantCulture)))
```

Un usuario que no cumple la política recibe 403 `Autorizacion.SinPermiso`, como con `Superusuario`. `EntidadAcademica` sigue sin política, porque ningún endpoint la necesita (D13 de Usuarios).

### `EntidadAcademicaResumen` (PR 2, D16)

En `Institucional/Application/Contracts/IAmbitosInstitucionales.cs`:

```csharp
public sealed record EntidadAcademicaResumen(int Id, string Clave, string Nombre, int AreaAcademicaId);
```

`AmbitosInstitucionales.ObtenerEntidadesAsync` proyecta también `e.AreaAcademicaId`. Usuarios no cambia: solo lee `Id`, `Clave` y `Nombre`.

### Paquete ClosedXML (PR 3)

Se agrega `ClosedXML` a `Directory.Packages.props` (grupo nuevo "Documentos (OfertaEducativa)"), con la última versión estable compatible con .NET 10, y se referencia desde `Sgpla.Modules.OfertaEducativa.csproj` y `Sgpla.IntegrationTests.csproj` (para leer el archivo generado en las pruebas). Si la última versión no compila sin advertencias con .NET 10, se detiene y se pregunta.

### Normalización

Se usa `Sgpla.SharedKernel.Normalizacion` (`Texto`, `Recortar` y `SonDigitos`). No se crea otra.

## 4. Normalización y validación comunes

Todas estas reglas las aplica el **dominio** y devuelven `Error.Validation(código, mensaje, campo)`, con el campo tomado con `nameof` de la propiedad. No hay validators de FluentValidation para los datos de una entidad (`ESTANDAR_MODULOS.md` §7). Cada fábrica devuelve el **primer** error, en el orden de la tabla de su recurso.

| Tipo de dato | Normalización | Regla | Errores (sufijo del código) |
|---|---|---|---|
| Nombre (programa, EE) | `Normalizacion.Texto` | No vacío; máximo 200 | `NombreVacio`, `NombreDemasiadoLargo` |
| Código de plan | `Normalizacion.Recortar` y `ToUpperInvariant()` | No vacío; máximo 50; solo `A`-`Z`, `0`-`9` y `-` (refleja `ck_plan_estudios__codigo_formato`) | `CodigoVacio`, `CodigoDemasiadoLargo`, `CodigoFormatoInvalido` |
| Materia y curso de una EE | `Normalizacion.Recortar` y `ToUpperInvariant()` | No vacío; máximo 50; solo `A`-`Z` y `0`-`9`. Conserva los ceros iniciales (`00001`) | `MateriaVacia`, `MateriaDemasiadoLarga`, `MateriaFormatoInvalido`, `CursoVacio`, `CursoDemasiadoLargo`, `CursoFormatoInvalido` |
| Horas teóricas y prácticas | Ninguna (`int`) | Mayor o igual a 0 | `HorasTeoricasNegativas`, `HorasPracticasNegativas` |
| Créditos | Ninguna (`int`) | Mayor que 0 | `CreditosNoPositivos` |
| Cupos (opcionales) | Ninguna (`int?`) | Cada uno, si existe, mayor o igual a 0; si existen los dos, mínimo <= máximo | `CupoMinimoNegativo`, `CupoMaximoNegativo`, `CuposInvertidos` (campo `CupoMinimo`) |
| Perfil docente (opcional) | `Normalizacion.Recortar`; si queda vacío (o llega `null`), es `null`. **No** colapsa espacios ni saltos de línea internos | Sin longitud máxima (`nvarchar(max)`) | — |
| Clave de periodo | `Normalizacion.Recortar` | No vacía; exactamente 6 caracteres y todos dígitos ASCII | `ClaveVacia`, `ClaveFormatoInvalido` |
| Fechas de periodo | Ninguna (`DateOnly`) | `FechaInicio <= FechaFin` | `RangoFechasInvalido` (campo `FechaFin`) |

Los mensajes con longitudes se construyen con las constantes de la entidad. Un JSON mal formado o de tipo incorrecto lo rechaza ASP.NET Core con 400 antes de llegar al handler; ese caso no se prueba.

## 5. Clasificaciones académicas (en Catalogos)

Tres catálogos fijos (D1). La semilla carga sus valores con ids estables; no tienen fábrica, comportamiento ni baja.

### Esquema y semilla (PR 1)

`baseline.sql`:
- se quita `fecha_eliminacion datetime2(0) NULL,` de `academico.sistema_educativo`, `academico.nivel_formacion` y `academico.area_formacion`;
- no cambia nada más de esas tablas.

`seed.sql`, sección 5 (`academico.area_formacion`), con este comentario y estos valores:

```sql
-- 5. academico.area_formacion
-- Áreas de formación de los planes de la UV; clave = CODE_AREA_F del formato de plan de estudios.

SET IDENTITY_INSERT academico.area_formacion ON;

INSERT INTO academico.area_formacion (id, clave, nombre)
VALUES (1, '111', N'Área de Formación Básica'),
       (2, '112', N'Área de Formación Disciplinaria'),
       (3, '113', N'Área de Formación Terminal');

SET IDENTITY_INSERT academico.area_formacion OFF;
```

`sistema_educativo` (6) y `nivel_formacion` (6) no cambian.

### Dominio

`Catalogos/.../Domain/CatalogosFijos/`:

| Clase | Base | Constantes | Columnas adicionales | Resumen |
|---|---|---|---|---|
| `SistemaEducativo` | `CatalogoFijo` | `LongitudMaximaNombre = 200` | — | Sistemas educativos (modalidades) de la UV (DATABASE.md §6.6). |
| `NivelFormacion` | `CatalogoFijo` | `LongitudMaximaNombre = 200`, `LongitudMaximaClave = 50` | `string Clave` | Niveles de formación de los programas educativos (DATABASE.md §6.7). |
| `AreaFormacion` | `CatalogoFijo` | `LongitudMaximaNombre = 200`, `LongitudMaximaClave = 50` | `string Clave` | Áreas de formación de las experiencias educativas (DATABASE.md §6.10); distintas del área académica. |

Todas son `internal sealed`, con constructor privado y `private set`.

### Configuración EF

En `Infrastructure/CatalogosFijos/CatalogoFijoConfiguration.cs`, una clase de una línea por catálogo, con esquema `academico` y tablas `sistema_educativo`, `nivel_formacion` y `area_formacion`. `Clave` sale de la convención snake_case, como `grado_academico_id` en `TratamientoAcademico`.

### Consultas y endpoints

| Catálogo | Implementación | Ruta |
|---|---|---|
| Sistema educativo | Genérica: `AddCatalogoFijo<SistemaEducativo>()` y `MapCatalogoFijo<SistemaEducativo>("/sistemas-educativos", "Sistemas educativos")` | `GET /api/v1/catalogos/sistemas-educativos` y `/{id}` |
| Nivel de formación | Consultas propias, como `ModalidadRecepcion` ("si tiene columnas adicionales"): `NivelFormacionConsultas.cs` en Application e Infrastructure, y `NivelFormacionEndpoints.cs` | `GET /api/v1/catalogos/niveles-formacion` y `/{id}` |
| Área de formación | Igual que nivel: `AreaFormacionConsultas.cs` y `AreaFormacionEndpoints.cs` | `GET /api/v1/catalogos/areas-formacion` y `/{id}` |

- Respuesta de nivel y área: `internal sealed record ClasificacionConClaveResponse(int Id, string Clave, string Nombre);`, compartida por los dos y declarada en `Application/CatalogosFijos/NivelFormacionConsultas.cs`.
- Queries: `ListarNivelesFormacionQuery`, `ObtenerNivelFormacionQuery(int Id)`, `ListarAreasFormacionQuery` y `ObtenerAreaFormacionQuery(int Id)`. Sin filtros ni paginación.
- El listado devuelve un arreglo con todos los valores, **en orden de id**. Obtener responde 200, o 404 con `CatalogoFijoErrors.NoEncontrado<T>` (códigos `NivelFormacion.NoEncontrado` y `AreaFormacion.NoEncontrado`).
- `WithName` y `WithSummary`: `ListarNivelesFormacion` / "Lista los niveles de formación, en orden de id."; `ObtenerNivelFormacion` / "Obtiene un nivel de formación."; `ListarAreasFormacion` / "Lista las áreas de formación, en orden de id."; `ObtenerAreaFormacion` / "Obtiene un área de formación.". Tags: `Niveles de formación` y `Áreas de formación`.
- Sin rutas de escritura: `POST`, `PUT` y `DELETE` responden 405.

Ejemplo de `GET /api/v1/catalogos/areas-formacion`:

```json
[
  { "id": 1, "clave": "111", "nombre": "Área de Formación Básica" },
  { "id": 2, "clave": "112", "nombre": "Área de Formación Disciplinaria" },
  { "id": 3, "clave": "113", "nombre": "Área de Formación Terminal" }
]
```

### Contrato `IClasificacionesAcademicas` (PR 2)

`Catalogos/.../Application/Contracts/IClasificacionesAcademicas.cs`:

```csharp
namespace Sgpla.Modules.Catalogos.Application.Contracts;

/// <summary>
/// Consulta de sistemas educativos, niveles y áreas de formación para otros módulos (OfertaEducativa). Un id existe si
/// aparece en el diccionario; los inexistentes no aparecen. Con una colección vacía no se consulta la base.
/// </summary>
public interface IClasificacionesAcademicas
{
    Task<IReadOnlyDictionary<int, SistemaEducativoResumen>> ObtenerSistemasEducativosAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<int, NivelFormacionResumen>> ObtenerNivelesFormacionAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<int, AreaFormacionResumen>> ObtenerAreasFormacionAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken);
}

public sealed record SistemaEducativoResumen(int Id, string Nombre);

public sealed record NivelFormacionResumen(int Id, string Clave, string Nombre);

public sealed record AreaFormacionResumen(int Id, string Clave, string Nombre);
```

Implementación: `Catalogos/.../Infrastructure/CatalogosFijos/ClasificacionesAcademicas.cs`, `internal sealed class ClasificacionesAcademicas(SgplaDbContext contexto)`, con `AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionaryAsync(...)`. Se registra en `AddCatalogosModule` junto a `IMunicipios`.

## 6. Autorización y ámbito

El grupo `/api/v1/oferta-educativa` lleva `RequireAuthorization(Politicas.Autenticado)` y declara 401 y 403, como Institucional y Catalogos. Las rutas de escritura agregan su política.

| Operación | Superusuario | DGAA | Entidad Académica | Política de la ruta |
|---|---|---|---|---|
| Consultar periodos | Sí | Sí | Sí | (grupo) |
| Crear, modificar y dar de baja periodos | Sí | No (403) | No (403) | `Superusuario` |
| Consultar programas, planes, EE, programaciones y horarios | Todos | Los de las entidades de su área | Los de su entidad | (grupo) |
| Exportar un plan a Excel | Todos | Los de su área | Los de su entidad | (grupo) |
| Crear, modificar y dar de baja programas, planes y EE | No (403) | En las entidades de su área | No (403) | `Dgaa` |

### Puerto `IAmbitoOfertaEducativa` (PR 2)

`Application/Ambito/IAmbitoOfertaEducativa.cs`. Concentra el ámbito para que handlers y consultas no repitan la regla:

```csharp
namespace Sgpla.Modules.OfertaEducativa.Application.Ambito;

/// <summary>Ámbito del usuario en curso sobre la oferta educativa (DATABASE.md §13.1; Modulo_OfertaEducativa.md §6).</summary>
internal interface IAmbitoOfertaEducativa
{
    /// <summary>
    /// Ids de las entidades académicas que el usuario puede consultar, incluidas las dadas de baja; <c>null</c> significa
    /// todas (Superusuario).
    /// </summary>
    Task<IReadOnlyCollection<int>?> EntidadesVisiblesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// <c>true</c> si el usuario es DGAA y la entidad está activa y pertenece a su área. Para cualquier otro rol,
    /// <c>false</c>.
    /// </summary>
    Task<bool> PuedeEscribirEnEntidadAsync(int entidadAcademicaId, CancellationToken cancellationToken);
}
```

Implementación: `Infrastructure/Ambito/AmbitoOfertaEducativa.cs`, `internal sealed class AmbitoOfertaEducativa(ICurrentUser actual, IAmbitosInstitucionales ambitos)`:
- `EntidadesVisiblesAsync`:
  - Superusuario: `null`;
  - DGAA: `ObtenerEntidadesDeAreaAsync(actual.AreaAcademicaId)`;
  - Entidad Académica: `[actual.EntidadAcademicaId]`;
  - cualquier otro valor de `Rol`: una colección vacía (falla cerrada).
- `PuedeEscribirEnEntidadAsync`: si el rol no es DGAA, `false`. Si lo es, `EntidadAcademicaActivaAsync(id)` y, con `ObtenerEntidadesAsync([id])`, que `AreaAcademicaId` coincida con el del usuario.

Uso:
- **Consultas:** filtran por `EntidadesVisiblesAsync` **antes** de los demás filtros, con `entidades.Contains(p.EntidadAcademicaId)` sobre el programa (directo o por `join`). Fuera del ámbito, obtener responde 404 con el `NoEncontrado` del recurso, y el listado simplemente no lo incluye.
- **Comandos del DGAA:**
  - un recurso de la ruta (programa, plan o EE) cuya entidad no cumple `PuedeEscribirEnEntidadAsync` responde 404 `NoEncontrado`;
  - una referencia del cuerpo (`entidadAcademicaId`, `programaEducativoId` o `planEstudiosId`) que no la cumple responde 400 con el error `...Inexistente` de ese campo (D15).

## 7. Periodo escolar

Periodo al que pertenecen las programaciones (`DATABASE.md` §6.12). Lo administra el Superusuario (D11). Tiene baja lógica, bloqueada con referencias, y no se restaura (D4).

### Dominio

`Domain/PeriodosEscolares/PeriodoEscolar.cs`: `internal sealed class PeriodoEscolar : Entity, IEliminable`.

| Propiedad | Tipo | Constante | Mutable |
|---|---|---|---|
| `Clave` | `string` | `LongitudClave = 6` | No |
| `FechaInicio` | `DateOnly` | — | Sí |
| `FechaFin` | `DateOnly` | — | Sí |
| `FechaEliminacion` | `DateTime?` | — | Solo con `DarDeBaja` |

Métodos:
- `static Result<PeriodoEscolar> Crear(string clave, DateOnly fechaInicio, DateOnly fechaFin)`. Valida en este orden: clave y rango de fechas.
- `Result Modificar(DateOnly fechaInicio, DateOnly fechaFin)`. Valida el rango y solo asigna si es válido.
- `void DarDeBaja(DateTime utc)`, idempotente.

`Domain/PeriodosEscolares/PeriodoEscolarErrors.cs`:

| Código | Tipo | Campo | Mensaje |
|---|---|---|---|
| `PeriodoEscolar.ClaveVacia` | Validation | `Clave` | La clave es obligatoria. |
| `PeriodoEscolar.ClaveFormatoInvalido` | Validation | `Clave` | La clave debe tener exactamente seis dígitos. |
| `PeriodoEscolar.RangoFechasInvalido` | Validation | `FechaFin` | La fecha de fin no puede ser anterior a la fecha de inicio. |
| `PeriodoEscolar.ClaveDuplicada` | Conflict | — | Ya existe un periodo escolar con esa clave. |
| `PeriodoEscolar.TieneReferencias` | Conflict | — | El periodo escolar tiene programaciones u otros registros que lo usan. |
| `PeriodoEscolar.NoEncontrado(int id)` | NotFound | — | No existe el periodo escolar {id}. |

### Application

`Application/PeriodosEscolares/IPeriodoEscolarRepository.cs`:

```csharp
internal interface IPeriodoEscolarRepository
{
    /// <summary>Solo activos.</summary>
    Task<PeriodoEscolar?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Incluye los dados de baja: la clave no se reutiliza.</summary>
    Task<bool> ExisteClaveAsync(string clave, CancellationToken cancellationToken);

    /// <summary>Cualquier programación del periodo, incluidas las dadas de baja (D11).</summary>
    Task<bool> TieneProgramacionesAsync(int periodoEscolarId, CancellationToken cancellationToken);

    void Agregar(PeriodoEscolar periodoEscolar);
}
```

`Application/PeriodosEscolares/PeriodoEscolarConsultas.cs`:

```csharp
internal sealed record PeriodoEscolarResponse(int Id, string Clave, DateOnly FechaInicio, DateOnly FechaFin)
{
    public static PeriodoEscolarResponse Desde(PeriodoEscolar periodo) =>
        new(periodo.Id, periodo.Clave, periodo.FechaInicio, periodo.FechaFin);
}

/// <summary>Todos los periodos activos, del más reciente al más antiguo (clave descendente).</summary>
internal sealed record ListarPeriodosEscolaresQuery;

internal sealed record ObtenerPeriodoEscolarQuery(int Id);
```

Comandos:

| Archivo | Command | Dependencias del handler | Pasos |
|---|---|---|---|
| `CrearPeriodoEscolar.cs` | `CrearPeriodoEscolarCommand(string Clave, DateOnly FechaInicio, DateOnly FechaFin)`; devuelve `PeriodoEscolarResponse` | `IPeriodoEscolarRepository`, `IUnitOfWork` | 1. `PeriodoEscolar.Crear`. 2. `ExisteClaveAsync` (clave normalizada) → `ClaveDuplicada`. 3. `Agregar` y `SaveChangesAsync`. 4. `PeriodoEscolarResponse.Desde` |
| `ModificarPeriodoEscolar.cs` | `ModificarPeriodoEscolarCommand(int Id, DateOnly FechaInicio, DateOnly FechaFin)` | `IPeriodoEscolarRepository`, `IUnitOfWork` | 1. `ObtenerPorIdAsync` → `NoEncontrado`. 2. `Modificar`. 3. `SaveChangesAsync` |
| `DarDeBajaPeriodoEscolar.cs` | `DarDeBajaPeriodoEscolarCommand(int Id)` | `IPeriodoEscolarRepository`, `IEnumerable<IReferenciasPeriodoEscolar>`, `IUnitOfWork`, `TimeProvider` | 1. `ObtenerPorIdAsync` → `NoEncontrado`. 2. `TieneProgramacionesAsync` → `TieneReferencias`. 3. Si alguna implementación de `IReferenciasPeriodoEscolar` responde `true` → `TieneReferencias`. 4. `DarDeBaja(instante)` y `SaveChangesAsync` |

`ModificarPeriodoEscolar` no mira las programaciones: las fechas siempre se pueden cambiar (D11), porque una sesión de PLANEA fuera del rango solo genera una advertencia (`DATABASE.md` §12.3).

### Infrastructure

- `PeriodoEscolarConfiguration.cs`:
  - `ToTable("periodo_escolar", "academico")`;
  - `Clave` con `HasMaxLength(6).IsUnicode(false).IsFixedLength()`;
  - las fechas como `date` (convención de `DateOnly`);
  - el filtro de baja lógica.
- `PeriodoEscolarRepository.cs`:
  - `ExisteClaveAsync` usa `IgnoreQueryFilters([FiltrosConsulta.BajaLogica])`;
  - `TieneProgramacionesAsync` consulta `Set<ProgramacionAcademica>().IgnoreQueryFilters([FiltrosConsulta.BajaLogica])`.
- `PeriodoEscolarConsultas.cs`: `ListarPeriodosEscolaresHandler` (orden `Clave` descendente) y `ObtenerPeriodoEscolarHandler`.

### Endpoints

`Endpoints/PeriodosEscolares/PeriodoEscolarEndpoints.cs`, grupo `/periodos-escolares` con tag `Periodos escolares`:

| Método y ruta | `WithName` | `WithSummary` | Autorización | Entrada | Éxito | Errores declarados |
|---|---|---|---|---|---|---|
| `GET /` | `ListarPeriodosEscolares` | Lista los periodos escolares activos, del más reciente al más antiguo. | (grupo) | — | 200 arreglo | — |
| `GET /{id:int}` | `ObtenerPeriodoEscolar` | Obtiene un periodo escolar activo. | (grupo) | — | 200 | 404 |
| `POST /` | `CrearPeriodoEscolar` | Registra un periodo escolar. | `Superusuario` | `CrearPeriodoEscolarCommand` | 201 con `Location` y el recurso | 400, 409 |
| `PUT /{id:int}` | `ModificarPeriodoEscolar` | Modifica las fechas de un periodo escolar; la clave no cambia. | `Superusuario` | `ModificarPeriodoEscolarRequest(DateOnly FechaInicio, DateOnly FechaFin)` | 204 | 400, 404 |
| `DELETE /{id:int}` | `DarDeBajaPeriodoEscolar` | Da de baja un periodo escolar sin programaciones ni otras referencias. | `Superusuario` | — | 204 | 404, 409 |

Ejemplo de `POST` y su respuesta:

```json
{ "clave": "202701", "fechaInicio": "2026-08-10", "fechaFin": "2027-01-22" }
```

```json
{ "id": 4, "clave": "202701", "fechaInicio": "2026-08-10", "fechaFin": "2027-01-22" }
```

## 8. Programa educativo

Programa que ofrece una entidad académica, con un sistema educativo y un nivel de formación (`DATABASE.md` §6.8). Lo administra el DGAA del área de la entidad (D9). La baja se bloquea con planes activos (D10) y no se restaura.

### Dominio

`Domain/ProgramasEducativos/ProgramaEducativo.cs`: `internal sealed class ProgramaEducativo : Entity, IEliminable`.

| Propiedad | Tipo | Constante | Mutable |
|---|---|---|---|
| `Nombre` | `string` | `LongitudMaximaNombre = 200` | Sí |
| `EntidadAcademicaId` | `int` | — | No |
| `SistemaEducativoId` | `int` | — | Solo si nunca tuvo planes |
| `NivelFormacionId` | `int` | — | Solo si nunca tuvo planes |
| `FechaEliminacion` | `DateTime?` | — | Solo con `DarDeBaja` |

Métodos:
- `static Result<ProgramaEducativo> Crear(string nombre, int entidadAcademicaId, int sistemaEducativoId, int nivelFormacionId)`. Valida el nombre; los ids los comprueba el handler.
- `Result Modificar(string nombre, int sistemaEducativoId, int nivelFormacionId, bool tuvoPlanes)`. Valida el nombre. Si `tuvoPlanes` y cambia el sistema o el nivel, devuelve `ClasificacionInmutable`. Asigna solo si todo es válido.
- `void DarDeBaja(DateTime utc)`.

`Domain/ProgramasEducativos/ProgramaEducativoErrors.cs`:

| Código | Tipo | Campo | Mensaje |
|---|---|---|---|
| `ProgramaEducativo.NombreVacio` / `NombreDemasiadoLargo` | Validation | `Nombre` | El nombre es obligatorio. / El nombre admite hasta 200 caracteres. |
| `ProgramaEducativo.EntidadAcademicaInexistente` | Validation | `EntidadAcademicaId` | No existe una entidad académica activa con ese id en tu ámbito. |
| `ProgramaEducativo.SistemaEducativoInexistente` | Validation | `SistemaEducativoId` | No existe el sistema educativo indicado. |
| `ProgramaEducativo.NivelFormacionInexistente` | Validation | `NivelFormacionId` | No existe el nivel de formación indicado. |
| `ProgramaEducativo.NombreDuplicado` | Conflict | — | La entidad académica ya tiene un programa con ese nombre en ese sistema educativo. |
| `ProgramaEducativo.ClasificacionInmutable` | Conflict | — | El sistema educativo y el nivel de formación no cambian una vez que el programa tuvo un plan de estudios. |
| `ProgramaEducativo.TienePlanesActivos` | Conflict | — | El programa educativo tiene planes de estudio activos. |
| `ProgramaEducativo.NoEncontrado(int id)` | NotFound | — | No existe el programa educativo {id}. |

### Application

`Application/ProgramasEducativos/IProgramaEducativoRepository.cs`:

```csharp
internal interface IProgramaEducativoRepository
{
    /// <summary>Solo activos.</summary>
    Task<ProgramaEducativo?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Misma entidad, mismo sistema y nombre equivalente (la columna ignora mayúsculas y acentos), incluidos los dados de
    /// baja. <paramref name="excluirId"/> omite al propio programa al modificar.
    /// </summary>
    Task<bool> ExisteNombreAsync(
        int entidadAcademicaId,
        string nombre,
        int sistemaEducativoId,
        int? excluirId,
        CancellationToken cancellationToken);

    /// <summary>Cualquier plan, incluidos los dados de baja (DATABASE.md §10).</summary>
    Task<bool> TuvoPlanesAsync(int programaEducativoId, CancellationToken cancellationToken);

    Task<bool> TienePlanesActivosAsync(int programaEducativoId, CancellationToken cancellationToken);

    void Agregar(ProgramaEducativo programaEducativo);
}
```

`Application/ProgramasEducativos/ProgramaEducativoConsultas.cs`:

```csharp
internal sealed record EntidadAcademicaResumenResponse(int Id, string Clave, string Nombre);

internal sealed record SistemaEducativoResponse(int Id, string Nombre);

internal sealed record NivelFormacionResponse(int Id, string Clave, string Nombre);

internal sealed record ProgramaEducativoResponse(
    int Id,
    string Nombre,
    EntidadAcademicaResumenResponse EntidadAcademica,
    SistemaEducativoResponse SistemaEducativo,
    NivelFormacionResponse NivelFormacion);

/// <summary>Filtros opcionales, combinados con AND. Un texto vacío o solo con espacios se ignora.</summary>
internal sealed record FiltrosProgramasEducativos(
    int? EntidadAcademicaId,
    int? SistemaEducativoId,
    int? NivelFormacionId,
    string? Busqueda);

/// <summary>Programas activos del ámbito, en orden de nombre (con el id como desempate), paginados.</summary>
internal sealed record ListarProgramasEducativosQuery(Paginacion Paginacion, FiltrosProgramasEducativos Filtros);

internal sealed record ObtenerProgramaEducativoQuery(int Id);
```

`ListarProgramasEducativosValidator`: `PaginacionValidator`; los tres ids `GreaterThan(0)` cuando no son `null`; `Busqueda` `MaximumLength(200)`; cada regla con `OverridePropertyName` (`entidadAcademicaId`, `sistemaEducativoId`, `nivelFormacionId`, `busqueda`) y `WithName` legible.

Comandos:

| Archivo | Command | Dependencias del handler | Pasos |
|---|---|---|---|
| `CrearProgramaEducativo.cs` | `CrearProgramaEducativoCommand(string Nombre, int EntidadAcademicaId, int SistemaEducativoId, int NivelFormacionId)`; devuelve el `int` del id | `IProgramaEducativoRepository`, `IAmbitoOfertaEducativa`, `IClasificacionesAcademicas`, `IUnitOfWork` | 1. `ProgramaEducativo.Crear`. 2. `PuedeEscribirEnEntidadAsync` → `EntidadAcademicaInexistente`. 3. Sistema en `ObtenerSistemasEducativosAsync` → `SistemaEducativoInexistente`. 4. Nivel en `ObtenerNivelesFormacionAsync` → `NivelFormacionInexistente`. 5. `ExisteNombreAsync` (nombre normalizado, `excluirId: null`) → `NombreDuplicado`. 6. `Agregar` y `SaveChangesAsync`. 7. Devuelve el `Id` |
| `ModificarProgramaEducativo.cs` | `ModificarProgramaEducativoCommand(int Id, string Nombre, int SistemaEducativoId, int NivelFormacionId)` | Las mismas | 1. `ObtenerPorIdAsync` → `NoEncontrado`. 2. `PuedeEscribirEnEntidadAsync(programa.EntidadAcademicaId)` → `NoEncontrado`. 3. `TuvoPlanesAsync`. 4. `Modificar(..., tuvoPlanes)`. 5. Sistema y nivel existentes → sus errores `...Inexistente`. 6. `ExisteNombreAsync(..., excluirId: Id)` → `NombreDuplicado`. 7. `SaveChangesAsync` |
| `DarDeBajaProgramaEducativo.cs` | `DarDeBajaProgramaEducativoCommand(int Id)` | `IProgramaEducativoRepository`, `IAmbitoOfertaEducativa`, `IUnitOfWork`, `TimeProvider` | 1. `ObtenerPorIdAsync` → `NoEncontrado`. 2. `PuedeEscribirEnEntidadAsync` → `NoEncontrado`. 3. `TienePlanesActivosAsync` → `TienePlanesActivos`. 4. `DarDeBaja(instante)` y `SaveChangesAsync` |

Si una comprobación falla después de `Modificar`, el handler devuelve el error **sin** `SaveChangesAsync`. La unicidad se vuelve a comprobar siempre al modificar, porque puede cambiar el nombre o el sistema (`DATABASE.md` §10).

### Infrastructure

- `ProgramaEducativoConfiguration.cs`: `ToTable("programa_educativo", "academico")`, `Nombre` con `HasMaxLength(200)` y el filtro de baja lógica. Sin navegaciones.
- `ProgramaEducativoRepository.cs`: `ExisteNombreAsync` y `TuvoPlanesAsync` usan `IgnoreQueryFilters([FiltrosConsulta.BajaLogica])`. `ExisteNombreAsync` compara con `==` sobre la columna, que ya tiene `Modern_Spanish_100_CI_AI`.
- `ProgramaEducativoConsultas.cs` (dependencias `SgplaDbContext`, `IAmbitoOfertaEducativa`, `IAmbitosInstitucionales`, `IClasificacionesAcademicas`):
  1. aplica el ámbito (sección 6) y después los filtros;
  2. ordena por `Nombre` y `Id`, proyecta a un registro intermedio con los ids y pagina;
  3. resuelve con **una** llamada por contrato los nombres de las entidades, sistemas y niveles de la página, y arma la respuesta.
  - Obtener hace lo mismo con un solo id, y responde 404 `NoEncontrado` si no existe o está fuera del ámbito.

| Filtro | Traducción |
|---|---|
| `entidadAcademicaId`, `sistemaEducativoId`, `nivelFormacionId` | Igualdad sobre la columna |
| `busqueda` | Se recorta y se colapsan espacios; `p.Nombre.Contains(busqueda)` (la columna ya ignora mayúsculas y acentos) |

### Endpoints

`Endpoints/ProgramasEducativos/ProgramaEducativoEndpoints.cs`, grupo `/programas-educativos` con tag `Programas educativos`:

| Método y ruta | `WithName` | `WithSummary` | Autorización | Entrada | Éxito | Errores declarados |
|---|---|---|---|---|---|---|
| `GET /` | `ListarProgramasEducativos` | Lista los programas educativos activos de tu ámbito, paginados y con filtros opcionales. | (grupo) | `[AsParameters] ListarProgramasEducativosRequest`: `pagina`, `tamanoPagina`, `entidadAcademicaId`, `sistemaEducativoId`, `nivelFormacionId` y `busqueda` | 200 `Pagina<ProgramaEducativoResponse>` | 400 |
| `GET /{id:int}` | `ObtenerProgramaEducativo` | Obtiene un programa educativo activo de tu ámbito. | (grupo) | — | 200 | 404 |
| `POST /` | `CrearProgramaEducativo` | Registra un programa educativo en una entidad académica de tu área. | `Dgaa` | `CrearProgramaEducativoCommand` | 201 con `Location` y el recurso | 400, 409 |
| `PUT /{id:int}` | `ModificarProgramaEducativo` | Modifica el nombre, el sistema educativo y el nivel de un programa; la entidad no cambia. | `Dgaa` | `ModificarProgramaEducativoRequest(string Nombre, int SistemaEducativoId, int NivelFormacionId)` | 204 | 400, 404, 409 |
| `DELETE /{id:int}` | `DarDeBajaProgramaEducativo` | Da de baja un programa educativo sin planes de estudio activos. | `Dgaa` | — | 204 | 404, 409 |

El `POST` compone como el de entidades en Institucional: invoca el comando y después `ObtenerProgramaEducativoQuery`, y responde `CreatedAtRoute(respuesta, "ObtenerProgramaEducativo", new { id })`.

Ejemplo de respuesta:

```json
{
  "id": 12,
  "nombre": "Ingeniería de Software",
  "entidadAcademica": { "id": 7, "clave": "FEIX", "nombre": "Facultad de Estadística e Informática" },
  "sistemaEducativo": { "id": 1, "nombre": "Escolarizada" },
  "nivelFormacion": { "id": 3, "clave": "LIC", "nombre": "Licenciatura" }
}
```

## 9. Plan de estudios

Plan de un programa educativo, identificado por un código opaco (`DATABASE.md` §6.9). No tiene archivo (D5) ni campos editables (D8). Se crea importando sus EE (D6) y se da de baja junto con ellas (D10).

### Dominio

`Domain/PlanesEstudio/PlanEstudios.cs`: `internal sealed class PlanEstudios : Entity, IEliminable`. Es el agregado que crea las EE: una EE solo nace dentro de su plan.

| Propiedad | Tipo | Constante | Mutable |
|---|---|---|---|
| `Codigo` | `string` | `LongitudMaximaCodigo = 50` | No |
| `ProgramaEducativoId` | `int` | — | No |
| `ExperienciasEducativas` | `IReadOnlyCollection<ExperienciaEducativa>` | `MinimoExperienciasImportacion = 1`, `MaximoExperienciasImportacion = 300` | Solo con `AgregarExperiencia` y `DarDeBaja` |
| `FechaEliminacion` | `DateTime?` | — | Solo con `DarDeBaja` |

`ExperienciasEducativas` es una navegación de solo lectura sobre una lista privada.

`Domain/PlanesEstudio/DatosExperienciaEducativa.cs`:

```csharp
/// <summary>Datos de entrada de una EE, sin normalizar, tal como llegan en la importación o en el alta individual.</summary>
internal sealed record DatosExperienciaEducativa(
    string Nombre,
    string Materia,
    string Curso,
    int HorasTeoricas,
    int HorasPracticas,
    int Creditos,
    int? CupoMinimo,
    int? CupoMaximo,
    string? PerfilDocente,
    int AreaFormacionId);
```

Métodos:
- `static Result<PlanEstudios> Crear(string codigo, int programaEducativoId, IReadOnlyList<DatosExperienciaEducativa> experiencias)`, en este orden:
  1. valida el código;
  2. si la lista está vacía, `SinExperiencias`; si pasa de 300, `DemasiadasExperiencias`;
  3. crea cada EE con `ExperienciaEducativa.Crear`. El primer error se devuelve con el campo prefijado: `error with { Campo = $"ExperienciasEducativas[{i}].{error.Campo}" }` (índice desde 0);
  4. si dos EE de la lista tienen la misma materia y curso ya normalizados, `ExperienciaRepetida` en el campo `ExperienciasEducativas[{i}].Curso` de la segunda.
- `Result<ExperienciaEducativa> AgregarExperiencia(DatosExperienciaEducativa datos)`: crea la EE y la agrega a la lista. La unicidad contra la base la comprueba el handler.
- `void DarDeBaja(DateTime utc)`: `FechaEliminacion ??= utc` y `DarDeBaja(utc)` en cada EE cargada (D10). Idempotente.

`Domain/PlanesEstudio/PlanEstudiosErrors.cs`:

| Código | Tipo | Campo | Mensaje |
|---|---|---|---|
| `PlanEstudios.CodigoVacio` / `CodigoDemasiadoLargo` | Validation | `Codigo` | El código es obligatorio. / El código admite hasta 50 caracteres. |
| `PlanEstudios.CodigoFormatoInvalido` | Validation | `Codigo` | El código solo admite letras de la A a la Z sin acentos, dígitos y guiones. |
| `PlanEstudios.SinExperiencias` | Validation | `ExperienciasEducativas` | El plan debe incluir al menos una experiencia educativa. |
| `PlanEstudios.DemasiadasExperiencias` | Validation | `ExperienciasEducativas` | El plan admite hasta 300 experiencias educativas por importación. |
| `PlanEstudios.ExperienciaRepetida` | Validation | `ExperienciasEducativas[i].Curso` | La materia y el curso ya aparecen en otra experiencia educativa del plan. |
| `PlanEstudios.ProgramaEducativoInexistente` | Validation | `ProgramaEducativoId` | No existe un programa educativo activo con ese id en tu ámbito. |
| `PlanEstudios.AreaFormacionInexistente` | Validation | `ExperienciasEducativas[i].AreaFormacionId` | No existe el área de formación indicada. |
| `PlanEstudios.CodigoDuplicado` | Conflict | — | El programa educativo ya tiene un plan con ese código. |
| `PlanEstudios.ExperienciasConProgramacionesActivas` | Conflict | — | Alguna experiencia educativa del plan tiene programaciones activas. |
| `PlanEstudios.NoEncontrado(int id)` | NotFound | — | No existe el plan de estudios {id}. |

### Reglas de mapeo del Excel (front)

La API no lee Excel (D6); estas reglas son el contrato con el front y quedan documentadas aquí para que la exportación (D7) sea su inverso. El formato de la UV tiene una hoja con estos encabezados en la fila 1:

| Columna | Uso en la importación |
|---|---|
| `DESC_AREA_ACAD` | Se ignora |
| `CODIGO_PLAN` | `codigo`. Todas las filas con datos deben traer el mismo valor; si no, el front no envía |
| `DESCRIPCION` | Se ignora (el programa se elige en la pantalla) |
| `CODIGO_PER_CAT`, `DESC_PER_CAT` | Se ignoran |
| `MATERIA_EE` | `materia` |
| `CURSO_EE` | `curso`, como texto (conserva ceros iniciales: `00001`) |
| `DESC_EE` | `nombre` (la API recorta y colapsa espacios) |
| `HT_EE`, `HP_EE` | `horasTeoricas`, `horasPracticas`; vacío = 0 |
| `CREDITOS_EE` | `creditos`; obligatorio |
| `CODE_AREA_F` | Se traduce a `areaFormacionId` con `GET /api/v1/catalogos/areas-formacion` (por `clave`) |
| `DESC_AREA_F` | Se ignora |
| `PERFIL_DOC` | `perfilDocente`; vacío = `null` |

Las columnas se ubican por nombre de encabezado, no por posición, y las filas completamente vacías se ignoran. Los cupos no vienen en el Excel: el front puede enviarlos en la misma importación o después, al modificar cada EE.

### Application

`Application/PlanesEstudio/IPlanEstudiosRepository.cs`:

```csharp
internal interface IPlanEstudiosRepository
{
    /// <summary>Solo activos, sin sus EE.</summary>
    Task<PlanEstudios?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Solo activos, con sus EE activas cargadas (para la baja conjunta).</summary>
    Task<PlanEstudios?> ObtenerConExperienciasAsync(int id, CancellationToken cancellationToken);

    /// <summary>Mismo programa y código normalizado, incluidos los dados de baja.</summary>
    Task<bool> ExisteCodigoAsync(int programaEducativoId, string codigo, CancellationToken cancellationToken);

    /// <summary>Entidad del programa activo, o <c>null</c> si el programa no existe o está dado de baja.</summary>
    Task<int?> ObtenerEntidadDeProgramaActivoAsync(int programaEducativoId, CancellationToken cancellationToken);

    /// <summary>Entidad del plan (por su programa), para el ámbito.</summary>
    Task<int> ObtenerEntidadDelPlanAsync(int planEstudiosId, CancellationToken cancellationToken);

    Task<bool> TieneExperienciasConProgramacionesActivasAsync(int planEstudiosId, CancellationToken cancellationToken);

    void Agregar(PlanEstudios planEstudios);
}
```

`Application/PlanesEstudio/PlanEstudiosConsultas.cs`:

```csharp
internal sealed record ProgramaEducativoResumenResponse(int Id, string Nombre);

internal sealed record PlanEstudiosResponse(
    int Id,
    string Codigo,
    ProgramaEducativoResumenResponse ProgramaEducativo,
    EntidadAcademicaResumenResponse EntidadAcademica,
    int ExperienciasEducativas);

/// <summary>Filtros opcionales, combinados con AND.</summary>
internal sealed record FiltrosPlanesEstudio(int? ProgramaEducativoId, int? EntidadAcademicaId, string? Busqueda);

/// <summary>Planes activos del ámbito, en orden de código (con el id como desempate), paginados.</summary>
internal sealed record ListarPlanesEstudioQuery(Paginacion Paginacion, FiltrosPlanesEstudio Filtros);

internal sealed record ObtenerPlanEstudiosQuery(int Id);

/// <summary>El .xlsx con el formato de la UV (D7).</summary>
internal sealed record ExportarPlanEstudiosQuery(int Id);

internal sealed record ArchivoGenerado(string Nombre, string TipoContenido, byte[] Contenido);
```

`ExperienciasEducativas` en la respuesta es el **número** de EE activas del plan. `ListarPlanesEstudioValidator` sigue el patrón de programas (`programaEducativoId`, `entidadAcademicaId` y `busqueda`, que busca en el código).

Comandos:

| Archivo | Command | Dependencias del handler | Pasos |
|---|---|---|---|
| `ImportarPlanEstudios.cs` | `ImportarPlanEstudiosCommand(int ProgramaEducativoId, string Codigo, IReadOnlyList<DatosExperienciaEducativa> ExperienciasEducativas)`; devuelve el `int` del id | `IPlanEstudiosRepository`, `IAmbitoOfertaEducativa`, `IClasificacionesAcademicas`, `IUnitOfWork` | 1. `PlanEstudios.Crear`. 2. `ObtenerEntidadDeProgramaActivoAsync` y `PuedeEscribirEnEntidadAsync`; si falla cualquiera → `ProgramaEducativoInexistente`. 3. Con **una** llamada a `ObtenerAreasFormacionAsync` (ids distintos), la primera EE con un área inexistente → `AreaFormacionInexistente` con su índice. 4. `ExisteCodigoAsync` → `CodigoDuplicado`. 5. `Agregar` y **un** `SaveChangesAsync` (plan y EE en la misma transacción). 6. Devuelve el `Id` |
| `DarDeBajaPlanEstudios.cs` | `DarDeBajaPlanEstudiosCommand(int Id)` | `IPlanEstudiosRepository`, `IAmbitoOfertaEducativa`, `IEnumerable<IReferenciasExperienciaEducativa>`, `IUnitOfWork`, `TimeProvider` | 1. `ObtenerConExperienciasAsync` → `NoEncontrado`. 2. Ámbito de escritura de su entidad → `NoEncontrado`. 3. `TieneExperienciasConProgramacionesActivasAsync` → `ExperienciasConProgramacionesActivas`. 4. Si alguna implementación de `IReferenciasExperienciaEducativa` responde `true` para los ids de las EE activas → `ExperienciaEducativa.TieneReferencias` (sección 10), el mismo error que la baja individual, porque el motivo es el mismo. 5. `DarDeBaja(instante)` y `SaveChangesAsync` |

El endpoint de importación enlaza un request propio (`ImportarPlanEstudiosRequest`, con `ExperienciaEducativaRequest` por elemento) y lo convierte al comando. El orden de la validación es el de los pasos: primero la forma de todo el plan (dominio), después las referencias.

### Exportación (D7)

`Infrastructure/PlanesEstudio/ExportarPlanEstudiosHandler.cs` (`IQueryHandler<ExportarPlanEstudiosQuery, ArchivoGenerado>`, dependencias `SgplaDbContext`, `IAmbitoOfertaEducativa`, `IAmbitosInstitucionales`, `IClasificacionesAcademicas`):
1. obtiene el plan activo del ámbito (404 `PlanEstudios.NoEncontrado` si no);
2. lee su programa, sus EE activas en orden de `Materia` y `Curso`, el nombre del área académica de la entidad (`ObtenerEntidadesAsync` → `AreaAcademicaId` → `ObtenerAreasAsync`) y las áreas de formación;
3. genera con ClosedXML un libro con una hoja `Hoja1` y los 14 encabezados en la fila 1, en este orden exacto: `DESC_AREA_ACAD`, `CODIGO_PLAN`, `DESCRIPCION`, `CODIGO_PER_CAT`, `DESC_PER_CAT`, `MATERIA_EE`, `CURSO_EE`, `DESC_EE`, `HT_EE`, `HP_EE`, `CREDITOS_EE`, `CODE_AREA_F`, `DESC_AREA_F`, `PERFIL_DOC`;
4. escribe una fila por EE:
   - `DESC_AREA_ACAD`: el nombre del área académica;
   - `CODIGO_PLAN`: el código del plan;
   - `DESCRIPCION`: el nombre del programa;
   - `CODIGO_PER_CAT` y `DESC_PER_CAT`: vacías;
   - `MATERIA_EE`, `CURSO_EE`, `DESC_EE`, `CODE_AREA_F` y `DESC_AREA_F`: texto (`CURSO_EE` y `CODE_AREA_F` como texto para conservar los ceros);
   - `HT_EE`, `HP_EE` y `CREDITOS_EE`: números;
   - `PERFIL_DOC`: el perfil, o vacía si es `null`;
5. devuelve `ArchivoGenerado($"{codigo}.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", bytes)`.

Un plan sin EE activas genera solo la fila de encabezados.

### Infrastructure

- `PlanEstudiosConfiguration.cs`:
  - `ToTable("plan_estudios", "academico")`;
  - `Codigo` con `HasMaxLength(50).IsUnicode(false)`;
  - `HasMany(p => p.ExperienciasEducativas).WithOne().HasForeignKey(e => e.PlanEstudiosId)` sobre el campo privado de la lista;
  - el filtro de baja lógica.
- `PlanEstudiosRepository.cs`:
  - `ExisteCodigoAsync` usa `IgnoreQueryFilters`;
  - `ObtenerConExperienciasAsync` usa `Include(p => p.ExperienciasEducativas)`: el filtro global de `ExperienciaEducativa` deja fuera las dadas de baja;
  - `TieneExperienciasConProgramacionesActivasAsync` hace `AnyAsync` sobre `ProgramacionAcademica` (activas) con `join` a las EE activas del plan.
- `PlanEstudiosConsultas.cs`: listado y obtener con el ámbito por `join` a `ProgramaEducativo`, el conteo de EE activas en la proyección y los nombres de entidad por contrato.

### Endpoints

`Endpoints/PlanesEstudio/PlanEstudiosEndpoints.cs`, grupo `/planes-estudio` con tag `Planes de estudio`:

| Método y ruta | `WithName` | `WithSummary` | Autorización | Entrada | Éxito | Errores declarados |
|---|---|---|---|---|---|---|
| `GET /` | `ListarPlanesEstudio` | Lista los planes de estudio activos de tu ámbito, paginados y con filtros opcionales. | (grupo) | `[AsParameters] ListarPlanesEstudioRequest`: `pagina`, `tamanoPagina`, `programaEducativoId`, `entidadAcademicaId` y `busqueda` | 200 `Pagina<PlanEstudiosResponse>` | 400 |
| `GET /{id:int}` | `ObtenerPlanEstudios` | Obtiene un plan de estudios activo de tu ámbito. | (grupo) | — | 200 | 404 |
| `GET /{id:int}/experiencias-educativas` | `ListarExperienciasEducativasDePlan` | Lista las experiencias educativas activas del plan, en orden de materia y curso. | (grupo) | — | 200 arreglo de `ExperienciaEducativaResponse` | 404 |
| `GET /{id:int}/excel` | `ExportarPlanEstudios` | Genera el Excel del plan con el formato de la UV. | (grupo) | — | 200 archivo `.xlsx` | 404 |
| `POST /` | `ImportarPlanEstudios` | Registra un plan de estudios con sus experiencias educativas. | `Dgaa` | `ImportarPlanEstudiosRequest` | 201 con `Location` y el recurso | 400, 409 |
| `DELETE /{id:int}` | `DarDeBajaPlanEstudios` | Da de baja un plan de estudios y sus experiencias educativas. | `Dgaa` | — | 204 | 404, 409 |

- El `POST` compone igual que el de programas.
- `ExportarPlanEstudios` responde `TypedResults.File(contenido, tipoContenido, nombre)` y declara `Produces(200, contentType: tipoContenido)`.
- `ListarExperienciasEducativasDePlan` usa la consulta de la sección 10.
- No hay `PUT` (D8).

Ejemplo de `POST` (dos EE del plan de ejemplo):

```json
{
  "programaEducativoId": 12,
  "codigo": "ISOF-14-E-CR",
  "experienciasEducativas": [
    {
      "materia": "ENSO", "curso": "38003", "nombre": "HABILIDADES DE COMUNICACION",
      "horasTeoricas": 2, "horasPracticas": 2, "creditos": 6,
      "cupoMinimo": null, "cupoMaximo": null,
      "perfilDocente": "Licenciado en Ingeniería de Software, en Informática o afín, ...",
      "areaFormacionId": 1
    },
    {
      "materia": "EXAV", "curso": "00001", "nombre": "ACREDITACION DEL IDIOMA INGLES",
      "horasTeoricas": 0, "horasPracticas": 0, "creditos": 6,
      "cupoMinimo": null, "cupoMaximo": null, "perfilDocente": null,
      "areaFormacionId": 3
    }
  ]
}
```

Respuesta 201 y `GET /{id}`:

```json
{
  "id": 5,
  "codigo": "ISOF-14-E-CR",
  "programaEducativo": { "id": 12, "nombre": "Ingeniería de Software" },
  "entidadAcademica": { "id": 7, "clave": "FEIX", "nombre": "Facultad de Estadística e Informática" },
  "experienciasEducativas": 2
}
```

## 10. Experiencia educativa

EE exclusiva de un plan, identificada dentro de él por materia y curso (`DATABASE.md` §6.11). Nace con su plan (importación) o con un alta individual. Sus atributos curriculares se congelan con la primera programación (D13).

### Dominio

`Domain/PlanesEstudio/ExperienciaEducativa.cs` (misma carpeta que su agregado): `internal sealed class ExperienciaEducativa : Entity, IEliminable`.

| Propiedad | Tipo | Columna | Constante | Mutable |
|---|---|---|---|---|
| `Nombre` | `string` | `nombre` | `LongitudMaximaNombre = 200` | Sí |
| `Materia` | `string` | `materia_ee` | `LongitudMaximaMateria = 50` | No |
| `Curso` | `string` | `curso_ee` | `LongitudMaximaCurso = 50` | No |
| `HorasTeoricas` | `int` | `horas_teoricas` | — | Solo sin programaciones |
| `HorasPracticas` | `int` | `horas_practicas` | — | Solo sin programaciones |
| `Creditos` | `int` | `creditos` | — | Solo sin programaciones |
| `CupoMinimo` | `int?` | `cupo_minimo` | — | Sí |
| `CupoMaximo` | `int?` | `cupo_maximo` | — | Sí |
| `PerfilDocente` | `string?` | `perfil_docente` | — | Sí |
| `AreaFormacionId` | `int` | `area_formacion_id` | — | Solo sin programaciones |
| `PlanEstudiosId` | `int` | `plan_estudios_id` | — | No |
| `FechaEliminacion` | `DateTime?` | `fecha_eliminacion` | — | Solo con `DarDeBaja` |

Métodos:
- `internal static Result<ExperienciaEducativa> Crear(DatosExperienciaEducativa datos)`: la usa solo `PlanEstudios`. Valida en este orden: nombre, materia, curso, horas teóricas, horas prácticas, créditos, cupo mínimo, cupo máximo y cupos invertidos. El perfil se normaliza sin validar.
- `Result Modificar(string nombre, int horasTeoricas, int horasPracticas, int creditos, int? cupoMinimo, int? cupoMaximo, string? perfilDocente, int areaFormacionId, bool tuvoProgramaciones)`. Valida igual. Si `tuvoProgramaciones` y cambia cualquiera de horas, créditos o área, devuelve `AtributosCurricularesInmutables`. Es un reemplazo completo: un cupo o un perfil en `null` los quita. Asigna solo si todo es válido.
- `void DarDeBaja(DateTime utc)`.

`Domain/PlanesEstudio/ExperienciaEducativaErrors.cs`. Los de validación llevan el campo de la tabla de la sección 4:

| Código | Tipo | Campo | Mensaje |
|---|---|---|---|
| `ExperienciaEducativa.NombreVacio` / `NombreDemasiadoLargo` | Validation | `Nombre` | El nombre es obligatorio. / El nombre admite hasta 200 caracteres. |
| `ExperienciaEducativa.MateriaVacia` / `MateriaDemasiadoLarga` / `MateriaFormatoInvalido` | Validation | `Materia` | La materia es obligatoria. / La materia admite hasta 50 caracteres. / La materia solo admite letras de la A a la Z sin acentos y dígitos. |
| `ExperienciaEducativa.CursoVacio` / `CursoDemasiadoLargo` / `CursoFormatoInvalido` | Validation | `Curso` | El curso es obligatorio. / El curso admite hasta 50 caracteres. / El curso solo admite letras de la A a la Z sin acentos y dígitos. |
| `ExperienciaEducativa.HorasTeoricasNegativas` | Validation | `HorasTeoricas` | Las horas teóricas no pueden ser negativas. |
| `ExperienciaEducativa.HorasPracticasNegativas` | Validation | `HorasPracticas` | Las horas prácticas no pueden ser negativas. |
| `ExperienciaEducativa.CreditosNoPositivos` | Validation | `Creditos` | Los créditos deben ser mayores que cero. |
| `ExperienciaEducativa.CupoMinimoNegativo` | Validation | `CupoMinimo` | El cupo mínimo no puede ser negativo. |
| `ExperienciaEducativa.CupoMaximoNegativo` | Validation | `CupoMaximo` | El cupo máximo no puede ser negativo. |
| `ExperienciaEducativa.CuposInvertidos` | Validation | `CupoMinimo` | El cupo mínimo no puede ser mayor que el cupo máximo. |
| `ExperienciaEducativa.PlanEstudiosInexistente` | Validation | `PlanEstudiosId` | No existe un plan de estudios activo con ese id en tu ámbito. |
| `ExperienciaEducativa.AreaFormacionInexistente` | Validation | `AreaFormacionId` | No existe el área de formación indicada. |
| `ExperienciaEducativa.MateriaCursoDuplicado` | Conflict | — | El plan ya tiene una experiencia educativa con esa materia y ese curso. |
| `ExperienciaEducativa.AtributosCurricularesInmutables` | Conflict | — | Las horas, los créditos y el área de formación no cambian una vez que la experiencia educativa fue programada. |
| `ExperienciaEducativa.TieneProgramacionesActivas` | Conflict | — | La experiencia educativa tiene programaciones activas. |
| `ExperienciaEducativa.TieneReferencias` | Conflict | — | La experiencia educativa tiene solicitudes u otros registros vigentes que la usan. |
| `ExperienciaEducativa.NoEncontrado(int id)` | NotFound | — | No existe la experiencia educativa {id}. |

`MateriaCursoDuplicado` también aplica cuando la EE existente está dada de baja: la combinación no se reutiliza (`DATABASE.md` §5) y no hay restauración (D4).

### Application

`Application/ExperienciasEducativas/IExperienciaEducativaRepository.cs`:

```csharp
internal interface IExperienciaEducativaRepository
{
    /// <summary>Solo activas.</summary>
    Task<ExperienciaEducativa?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Mismo plan, materia y curso normalizados, incluidas las dadas de baja.</summary>
    Task<bool> ExisteMateriaCursoAsync(int planEstudiosId, string materia, string curso, CancellationToken cancellationToken);

    /// <summary>Cualquier programación de la EE, incluidas las dadas de baja (D13).</summary>
    Task<bool> TuvoProgramacionesAsync(int experienciaEducativaId, CancellationToken cancellationToken);

    Task<bool> TieneProgramacionesActivasAsync(int experienciaEducativaId, CancellationToken cancellationToken);

    /// <summary>Entidad de la EE (por su plan y su programa), para el ámbito.</summary>
    Task<int> ObtenerEntidadAsync(int experienciaEducativaId, CancellationToken cancellationToken);
}
```

`Application/ExperienciasEducativas/ExperienciaEducativaConsultas.cs`:

```csharp
internal sealed record AreaFormacionResponse(int Id, string Clave, string Nombre);

internal sealed record PlanEstudiosResumenResponse(int Id, string Codigo);

internal sealed record ExperienciaEducativaResponse(
    int Id,
    string Nombre,
    string Materia,
    string Curso,
    int HorasTeoricas,
    int HorasPracticas,
    int Creditos,
    int? CupoMinimo,
    int? CupoMaximo,
    string? PerfilDocente,
    AreaFormacionResponse AreaFormacion,
    PlanEstudiosResumenResponse PlanEstudios,
    bool TuvoProgramaciones);

/// <summary>EE activas de un plan activo del ámbito, en orden de materia, curso e id. 404 si el plan no está a la vista.</summary>
internal sealed record ListarExperienciasEducativasDePlanQuery(int PlanEstudiosId);

internal sealed record ObtenerExperienciaEducativaQuery(int Id);
```

`TuvoProgramaciones` permite al front deshabilitar horas, créditos y área cuando ya no se pueden cambiar.

Comandos:

| Archivo | Command | Dependencias del handler | Pasos |
|---|---|---|---|
| `CrearExperienciaEducativa.cs` | `CrearExperienciaEducativaCommand(int PlanEstudiosId, DatosExperienciaEducativa Datos)`; devuelve el `int` del id | `IPlanEstudiosRepository`, `IExperienciaEducativaRepository`, `IAmbitoOfertaEducativa`, `IClasificacionesAcademicas`, `IUnitOfWork` | 1. `IPlanEstudiosRepository.ObtenerPorIdAsync`; si no existe o su entidad no cumple el ámbito de escritura → `PlanEstudiosInexistente`. 2. `plan.AgregarExperiencia(datos)`; si falla, su error. 3. Área existente → `AreaFormacionInexistente`. 4. `ExisteMateriaCursoAsync` → `MateriaCursoDuplicado`. 5. `SaveChangesAsync`. 6. Devuelve el `Id` |
| `ModificarExperienciaEducativa.cs` | `ModificarExperienciaEducativaCommand(int Id, string Nombre, int HorasTeoricas, int HorasPracticas, int Creditos, int? CupoMinimo, int? CupoMaximo, string? PerfilDocente, int AreaFormacionId)` | `IExperienciaEducativaRepository`, `IAmbitoOfertaEducativa`, `IClasificacionesAcademicas`, `IUnitOfWork` | 1. `ObtenerPorIdAsync` → `NoEncontrado`. 2. Ámbito de escritura de su entidad → `NoEncontrado`. 3. `TuvoProgramacionesAsync`. 4. `Modificar(..., tuvoProgramaciones)`. 5. Área existente → `AreaFormacionInexistente`. 6. `SaveChangesAsync` |
| `DarDeBajaExperienciaEducativa.cs` | `DarDeBajaExperienciaEducativaCommand(int Id)` | `IExperienciaEducativaRepository`, `IAmbitoOfertaEducativa`, `IEnumerable<IReferenciasExperienciaEducativa>`, `IUnitOfWork`, `TimeProvider` | 1. `ObtenerPorIdAsync` → `NoEncontrado`. 2. Ámbito → `NoEncontrado`. 3. `TieneProgramacionesActivasAsync` → `TieneProgramacionesActivas`. 4. Alguna implementación de `IReferenciasExperienciaEducativa` con `true` → `TieneReferencias`. 5. `DarDeBaja(instante)` y `SaveChangesAsync` |

Dar de baja la última EE activa de un plan no da de baja el plan: queda un plan sin EE, que el DGAA puede completar con altas individuales o dar de baja.

### Infrastructure

- `ExperienciaEducativaConfiguration.cs`:
  - `ToTable("experiencia_educativa", "academico")`;
  - `Materia` y `Curso` con `HasColumnName("materia_ee")` y `HasColumnName("curso_ee")`, `HasMaxLength(50).IsUnicode(false)`;
  - `Nombre` con `HasMaxLength(200)`; `PerfilDocente` sin longitud (`nvarchar(max)`);
  - el filtro de baja lógica.
- `ExperienciaEducativaRepository.cs`: `ExisteMateriaCursoAsync` y `TuvoProgramacionesAsync` con `IgnoreQueryFilters`.
- `ExperienciaEducativaConsultas.cs`: obtener y listar por plan, con el ámbito por `join` a `PlanEstudios` y `ProgramaEducativo`, `TuvoProgramaciones` como `Any` sobre `ProgramacionAcademica` con `IgnoreQueryFilters`, y los nombres de las áreas de formación con **una** llamada a `ObtenerAreasFormacionAsync`.

### Endpoints

`Endpoints/ExperienciasEducativas/ExperienciaEducativaEndpoints.cs`, grupo `/experiencias-educativas` con tag `Experiencias educativas`:

| Método y ruta | `WithName` | `WithSummary` | Autorización | Entrada | Éxito | Errores declarados |
|---|---|---|---|---|---|---|
| `GET /{id:int}` | `ObtenerExperienciaEducativa` | Obtiene una experiencia educativa activa de tu ámbito. | (grupo) | — | 200 | 404 |
| `POST /` | `CrearExperienciaEducativa` | Agrega una experiencia educativa a un plan de estudios. | `Dgaa` | `CrearExperienciaEducativaRequest` (`planEstudiosId` más los campos de `DatosExperienciaEducativa`) | 201 con `Location` y el recurso | 400, 409 |
| `PUT /{id:int}` | `ModificarExperienciaEducativa` | Modifica una experiencia educativa; materia, curso y plan no cambian. | `Dgaa` | `ModificarExperienciaEducativaRequest` (los campos del comando sin `Id`) | 204 | 400, 404, 409 |
| `DELETE /{id:int}` | `DarDeBajaExperienciaEducativa` | Da de baja una experiencia educativa sin programaciones activas. | `Dgaa` | — | 204 | 404, 409 |

El listado de las EE de un plan está en `GET /planes-estudio/{id}/experiencias-educativas` (sección 9). No hay un listado global de EE.

## 11. Programación académica y horarios

Solo lectura (D12). Las filas las creará Integracion; hoy solo pueden existir si se insertan directamente en la base.

### Dominio

`Domain/Programaciones/ProgramacionAcademica.cs`: `internal sealed class ProgramacionAcademica : Entity, IEliminable`, con constructor privado, sin fábrica ni métodos:

| Propiedad | Tipo | Constante |
|---|---|---|
| `Nrc` | `string` | `LongitudMaximaNrc = 20` |
| `PeriodoEscolarId` | `int` | — |
| `ExperienciaEducativaId` | `int` | — |
| `FechaEliminacion` | `DateTime?` | — |

`Domain/Programaciones/HorarioProgramacion.cs`: `internal sealed class HorarioProgramacion : Entity`, sin baja lógica:

| Propiedad | Tipo | Constante |
|---|---|---|
| `ProgramacionAcademicaId` | `int` | — |
| `SincronizacionPlaneaId` | `int` | — (sin navegación: la tabla es de Integracion) |
| `DiaSemana` | `byte` | 1 = lunes … 6 = sábado |
| `HoraInicio`, `HoraFin` | `TimeOnly` | — |
| `FechaInicio`, `FechaFin` | `DateOnly` | — |
| `Edificio`, `Aula` | `string?` | `LongitudMaximaEspacio = 100` |

`Domain/Programaciones/ProgramacionAcademicaErrors.cs`: solo `ProgramacionAcademica.NoEncontrado(int id)` (NotFound, "No existe la programación académica {id}.").

El PR 2 agrega `ProgramacionAcademica` y su configuración (sin consultas ni endpoints), porque la baja de periodos la necesita. El PR 4 agrega `HorarioProgramacion`, las consultas y los endpoints.

### Application

`Application/Programaciones/ProgramacionAcademicaConsultas.cs`:

```csharp
internal sealed record PeriodoEscolarResumenResponse(int Id, string Clave);

internal sealed record ExperienciaEducativaResumenResponse(int Id, string Materia, string Curso, string Nombre);

internal sealed record ProgramacionAcademicaResponse(
    int Id,
    string Nrc,
    PeriodoEscolarResumenResponse PeriodoEscolar,
    ExperienciaEducativaResumenResponse ExperienciaEducativa,
    PlanEstudiosResumenResponse PlanEstudios,
    ProgramaEducativoResumenResponse ProgramaEducativo,
    EntidadAcademicaResumenResponse EntidadAcademica);

internal sealed record HorarioResponse(
    int Id,
    byte DiaSemana,
    TimeOnly HoraInicio,
    TimeOnly HoraFin,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    string? Edificio,
    string? Aula);

/// <summary>Filtros opcionales, combinados con AND. <paramref name="Nrc"/> se recorta, se pasa a mayúsculas y se compara exacto.</summary>
internal sealed record FiltrosProgramacionesAcademicas(
    int? PeriodoEscolarId,
    int? EntidadAcademicaId,
    int? ProgramaEducativoId,
    int? PlanEstudiosId,
    int? ExperienciaEducativaId,
    string? Nrc);

/// <summary>Programaciones activas del ámbito, por clave de periodo descendente, NRC e id, paginadas.</summary>
internal sealed record ListarProgramacionesAcademicasQuery(Paginacion Paginacion, FiltrosProgramacionesAcademicas Filtros);

internal sealed record ObtenerProgramacionAcademicaQuery(int Id);

/// <summary>Sesiones vigentes de una programación del ámbito, por día, hora de inicio e id. 404 si no está a la vista.</summary>
internal sealed record ListarHorariosQuery(int ProgramacionAcademicaId);
```

`ListarProgramacionesAcademicasValidator`: `PaginacionValidator`, ids `GreaterThan(0)` y `Nrc` `MaximumLength(20)`, con `OverridePropertyName` y `WithName`.

### Infrastructure

- `ProgramacionAcademicaConfiguration.cs`: `ToTable("programacion_academica", "academico")`, `Nrc` con `HasMaxLength(20).IsUnicode(false)` y el filtro de baja lógica.
- `HorarioProgramacionConfiguration.cs`: `ToTable("horario_programacion", "academico")`, `Edificio` y `Aula` con `HasMaxLength(100)`.
- `ProgramacionAcademicaConsultas.cs`: `join` a `PeriodoEscolar`, `ExperienciaEducativa`, `PlanEstudios` y `ProgramaEducativo` (todas activas por el filtro global), ámbito sobre `ProgramaEducativo.EntidadAcademicaId` antes de los filtros, y nombres de entidad con **una** llamada a `ObtenerEntidadesAsync`.

### Endpoints

`Endpoints/Programaciones/ProgramacionAcademicaEndpoints.cs`, grupo `/programaciones-academicas` con tag `Programaciones académicas`:

| Método y ruta | `WithName` | `WithSummary` | Entrada | Éxito | Errores declarados |
|---|---|---|---|---|---|
| `GET /` | `ListarProgramacionesAcademicas` | Lista las programaciones académicas activas de tu ámbito, paginadas y con filtros opcionales. | `[AsParameters] ListarProgramacionesAcademicasRequest`: `pagina`, `tamanoPagina`, `periodoEscolarId`, `entidadAcademicaId`, `programaEducativoId`, `planEstudiosId`, `experienciaEducativaId` y `nrc` | 200 `Pagina<ProgramacionAcademicaResponse>` | 400 |
| `GET /{id:int}` | `ObtenerProgramacionAcademica` | Obtiene una programación académica activa de tu ámbito. | — | 200 | 404 |
| `GET /{id:int}/horarios` | `ListarHorariosProgramacion` | Lista las sesiones vigentes de una programación, importadas de PLANEA. | — | 200 arreglo de `HorarioResponse` | 404 |

Todas usan la autorización del grupo. Un `POST`, `PUT` o `DELETE` responde 405. Las horas se serializan como `"08:00:00"` y las fechas como `"2026-08-10"`.

## 12. Contratos entre módulos

### Institucional ← OfertaEducativa (P1 y P2, PR 2)

`Institucional/Application/Contracts/IProgramasDeEntidadAcademica.cs`:

```csharp
namespace Sgpla.Modules.Institucional.Application.Contracts;

/// <summary>Lo implementa OfertaEducativa (P1 y P2 de pendientes.md).</summary>
public interface IProgramasDeEntidadAcademica
{
    /// <summary>Con programas activos, la entidad no se da de baja (Modulo_OfertaEducativa.md, D10).</summary>
    Task<bool> TieneProgramasActivosAsync(int entidadAcademicaId, CancellationToken cancellationToken);

    /// <summary>Con cualquier programa, incluidos los dados de baja, el área de la entidad no cambia (DATABASE.md §10).</summary>
    Task<bool> TieneProgramasAsync(int entidadAcademicaId, CancellationToken cancellationToken);
}
```

Cambios en Institucional:
- `DarDeBajaEntidadAcademicaHandler` recibe `IEnumerable<IProgramasDeEntidadAcademica>`. Después de comprobar los usuarios activos, si alguna implementación responde `true` en `TieneProgramasActivosAsync`, devuelve `EntidadAcademicaErrors.TieneProgramasActivos` (Conflict, "La entidad académica tiene programas educativos activos.").
- `ModificarEntidadAcademicaHandler` recibe `IEnumerable<IProgramasDeEntidadAcademica>`. Si el área pedida es distinta de la actual y alguna implementación responde `true` en `TieneProgramasAsync`, devuelve `EntidadAcademicaErrors.AreaAcademicaInmutable` (Conflict, "El área académica no cambia una vez que la entidad tiene programas educativos."). Se comprueba antes de llamar a `Modificar`, para no tocar la entidad.
- Se actualizan D6 y las tablas de errores de `Modulo_Institucional.md` §8, y el resumen `<summary>` de ambos handlers deja de citar P1 y D6.

Implementación: `OfertaEducativa/Infrastructure/Contratos/ProgramasDeEntidadAcademica.cs`, con `AnyAsync` sobre `ProgramaEducativo` (activos, o con `IgnoreQueryFilters` para `TieneProgramasAsync`). Se registra en `AddOfertaEducativaModule`.

### Catalogos → OfertaEducativa (PR 2)

`IClasificacionesAcademicas`, sección 5.

### Institucional → OfertaEducativa (existente)

`IAmbitosInstitucionales`, con `EntidadAcademicaResumen.AreaAcademicaId` (D16).

### OfertaEducativa ← módulos futuros (P7 y P8)

En `OfertaEducativa/Application/Contracts/`. Hoy no tienen implementaciones: los handlers reciben `IEnumerable<...>` vacío, como `IReferenciasArticulo` en Catalogos.

```csharp
namespace Sgpla.Modules.OfertaEducativa.Application.Contracts;

/// <summary>
/// Lo implementan los módulos que referencian experiencias educativas (SolicitudesApertura, P7). <c>true</c> bloquea la
/// baja de esas EE y de su plan. Cada implementación decide qué referencias bloquean (DATABASE.md §16.6).
/// </summary>
public interface IReferenciasExperienciaEducativa
{
    Task<bool> TieneReferenciasAsync(
        IReadOnlyCollection<int> experienciaEducativaIds,
        CancellationToken cancellationToken);
}

/// <summary>
/// Lo implementan los módulos que referencian periodos escolares (Integracion, SolicitudesApertura y Publicacion, P8).
/// Cualquier referencia bloquea la baja del periodo (D11).
/// </summary>
public interface IReferenciasPeriodoEscolar
{
    Task<bool> TieneReferenciasAsync(int periodoEscolarId, CancellationToken cancellationToken);
}
```

`IReferenciasPeriodoEscolar` nace en el PR 2 y `IReferenciasExperienciaEducativa` en el PR 3.

## 13. Auditoría

No hay eventos `[LoggerMessage]` en este módulo. Todas sus operaciones son síncronas y su resultado se ve en la respuesta HTTP, así que no cumplen el criterio de `ESTANDAR_MODULOS.md` §11. Institucional sigue el mismo criterio. Los eventos de la sincronización con PLANEA serán de Integracion.

## 14. Composición del módulo

`OfertaEducativaModule.cs`:
- **`AddOfertaEducativaModule`:**
  - `AddPersistenciaModulo(ensamblado)` y `AddHandlersModulo(ensamblado)`;
  - `IAmbitoOfertaEducativa`, los repositorios de periodo, programa, plan y EE, todos `Scoped`;
  - `IProgramasDeEntidadAcademica` (PR 2).
- **`MapOfertaEducativaEndpoints`:**
  - `var grupo = endpoints.MapGroup(Ruta).RequireAuthorization(Politicas.Autenticado);` y `grupo.ProducesProblem(401).ProducesProblem(403)`;
  - `MapPeriodoEscolarEndpoints`, `MapProgramaEducativoEndpoints`, `MapPlanEstudiosEndpoints`, `MapExperienciaEducativaEndpoints` y `MapProgramacionAcademicaEndpoints`, cada uno en el PR que lo introduce;
  - se quita el `WithTags("OfertaEducativa")` actual.

`Sgpla.Modules.OfertaEducativa.csproj` agrega (PR 2):
- `ProjectReference` a `Sgpla.BuildingBlocks.Application` y a Catalogos;
- `<InternalsVisibleTo Include="Sgpla.UnitTests" />`;
- ClosedXML en el PR 3.

`tests/Sgpla.UnitTests/Sgpla.UnitTests.csproj` agrega la referencia a OfertaEducativa (PR 2). `tests/Sgpla.ArchitectureTests/Modulos.cs`: `["OfertaEducativa"] = ["Institucional", "Catalogos"]` (PR 2).

Estructura final:

```
src/Modules/OfertaEducativa/Sgpla.Modules.OfertaEducativa/
  Domain/
    PeriodosEscolares/PeriodoEscolar.cs, PeriodoEscolarErrors.cs
    ProgramasEducativos/ProgramaEducativo.cs, ProgramaEducativoErrors.cs
    PlanesEstudio/PlanEstudios.cs, PlanEstudiosErrors.cs, DatosExperienciaEducativa.cs,
                  ExperienciaEducativa.cs, ExperienciaEducativaErrors.cs
    Programaciones/ProgramacionAcademica.cs, HorarioProgramacion.cs, ProgramacionAcademicaErrors.cs
  Application/
    Ambito/IAmbitoOfertaEducativa.cs
    Contracts/IReferenciasPeriodoEscolar.cs, IReferenciasExperienciaEducativa.cs
    PeriodosEscolares/IPeriodoEscolarRepository.cs, PeriodoEscolarConsultas.cs,
                      CrearPeriodoEscolar.cs, ModificarPeriodoEscolar.cs, DarDeBajaPeriodoEscolar.cs
    ProgramasEducativos/IProgramaEducativoRepository.cs, ProgramaEducativoConsultas.cs,
                        CrearProgramaEducativo.cs, ModificarProgramaEducativo.cs, DarDeBajaProgramaEducativo.cs
    PlanesEstudio/IPlanEstudiosRepository.cs, PlanEstudiosConsultas.cs,
                  ImportarPlanEstudios.cs, DarDeBajaPlanEstudios.cs
    ExperienciasEducativas/IExperienciaEducativaRepository.cs, ExperienciaEducativaConsultas.cs,
                           CrearExperienciaEducativa.cs, ModificarExperienciaEducativa.cs,
                           DarDeBajaExperienciaEducativa.cs
    Programaciones/ProgramacionAcademicaConsultas.cs
  Infrastructure/
    Ambito/AmbitoOfertaEducativa.cs
    Contratos/ProgramasDeEntidadAcademica.cs
    PeriodosEscolares/PeriodoEscolarConfiguration.cs, PeriodoEscolarRepository.cs, PeriodoEscolarConsultas.cs
    ProgramasEducativos/ProgramaEducativoConfiguration.cs, ProgramaEducativoRepository.cs,
                        ProgramaEducativoConsultas.cs
    PlanesEstudio/PlanEstudiosConfiguration.cs, PlanEstudiosRepository.cs, PlanEstudiosConsultas.cs,
                  ExportarPlanEstudiosHandler.cs
    ExperienciasEducativas/ExperienciaEducativaConfiguration.cs, ExperienciaEducativaRepository.cs,
                           ExperienciaEducativaConsultas.cs
    Programaciones/ProgramacionAcademicaConfiguration.cs, HorarioProgramacionConfiguration.cs,
                   ProgramacionAcademicaConsultas.cs
  Endpoints/
    PeriodosEscolares/PeriodoEscolarEndpoints.cs
    ProgramasEducativos/ProgramaEducativoEndpoints.cs
    PlanesEstudio/PlanEstudiosEndpoints.cs
    ExperienciasEducativas/ExperienciaEducativaEndpoints.cs
    Programaciones/ProgramacionAcademicaEndpoints.cs
  OfertaEducativaModule.cs
```

## 15. Entregas (PR)

Cuatro PR en orden, cada uno desde `develop` y con la convención de ramas, commits y PR del repositorio. Cada PR deja el CI en verde por sí solo.

### PR 1: clasificaciones fijas y modelo de datos

- **Código:** la sección 5, salvo el contrato `IClasificacionesAcademicas` (PR 2).
- **Esquema** (excepciones D1 y D5, las únicas ediciones de `baseline.sql` de este módulo):
  - se quita `fecha_eliminacion` de `sistema_educativo`, `nivel_formacion` y `area_formacion`;
  - se elimina `CREATE TABLE academico.archivo_plan_estudios` completo;
  - en `academico.plan_estudios` se eliminan la columna `archivo_plan_estudios_id`, `uq_plan_estudios__archivo_plan_estudios` (con su comentario) y `fk_plan_estudios__archivo_plan_estudios`;
  - se busca cualquier otra mención a `archivo_plan_estudios` en `baseline.sql` y `seed.sql`; si aparece una que este documento no prevé, se detiene y se pregunta.
- **Semilla:** `area_formacion` de la sección 5 (D2).
- **Pruebas:**
  - `SeedTests`: `academico.area_formacion` pasa de 5 a 3;
  - `tests/Sgpla.IntegrationTests/Catalogos/ClasificacionesAcademicasEndpointsTests.cs`, con el cliente de Superusuario:
    - listado exacto de los 6 sistemas `(id, nombre)`, de los 6 niveles `(id, clave, nombre)` y de las 3 áreas `(id, clave, nombre)`, en orden de id;
    - obtener 200 (sistema 1 `Escolarizada`, nivel 3 `LIC`, área 2 `112`) y 404 con `SistemaEducativo.NoEncontrado`, `NivelFormacion.NoEncontrado` y `AreaFormacion.NoEncontrado`;
    - 405 al escribir en las tres rutas;
  - `AutorizacionTests`: DGAA y Entidad Académica leen las tres rutas (200), y sin token responden 401;
  - `EsqueletoTests.Migraciones_CreanLasTablasDeLaBaseInicial`: `academico` pasa de 24 a 23 tablas;
  - `EsqueletoTests` agrega `Migraciones_NoCreanArchivoDelPlanNiBajaEnClasificaciones`, que consulta `sys.columns` y verifica que no existen `plan_estudios.archivo_plan_estudios_id` ni `fecha_eliminacion` en `sistema_educativo`, `nivel_formacion` y `area_formacion`.
- **Documentos:**
  - `DATABASE.md`:
    - §5: los tres catálogos pasan a "Excepciones deliberadas" como fijos de la semilla, sin baja lógica; se quita la mención a binarios del plan;
    - §6.6, §6.7 y §6.10: sin la fila `fecha_eliminacion`, "catálogo fijo de solo lectura; valores de la semilla", y en §6.10 la lista de claves 111 a 113;
    - §6.9: sin `archivo_plan_estudios_id` ni la subsección `archivo_plan_estudios`; el plan no tiene archivo, y sus EE se importan (D5, D6);
    - §7: sin la fila `plan_estudios` / `archivo_plan_estudios`;
    - §8: sin `ix_plan_estudios__archivo_plan_estudios_id`;
    - §9.1: sin cascada; la baja se bloquea con hijos activos, con la excepción del plan y sus EE, y el periodo se bloquea con referencias (D10, D11); los horarios se borran físicamente cuando Integracion dé de baja una programación;
    - §9.2: los tres catálogos ya no se dan de baja;
    - §9.3: OfertaEducativa no restaura (D4); la restauración deja de aplicar a todo el modelo académico;
    - §10: sin la regla del archivo; los catálogos fijos incluyen los tres nuevos; el congelamiento de la EE cuenta las programaciones dadas de baja (D13);
    - §11: sin la ruta al documento del plan;
    - §12: una nota al inicio: "Pendiente de revisión contra el payload real de PLANEA (`pendientes.md`, P9)";
    - §14: se ajustan los casos de restauración, propagación de bajas y reemplazo del archivo del plan;
    - §17: el almacenamiento externo ya no aplica al plan de estudios;
  - `DATABASE.md` §1 o donde se cite el número de tablas del modelo (hoy "~55 tablas" en `PLAN_INICIAL.md`): se ajusta si da un número exacto;
  - `DATABASE_DIAGRAM.md`: sin `ACADEMICO_ARCHIVO_PLAN_ESTUDIOS` ni sus dos relaciones, sin `archivo_plan_estudios_id` en el plan y sin `fecha_eliminacion` en los tres catálogos;
  - `PLAN_INICIAL.md`: en la tabla de módulos, los tres catálogos pasan a Catalogos, y la fila de OfertaEducativa queda "programa_educativo, plan_estudios, experiencia_educativa, periodo_escolar, programacion_academica, horario_programacion; CRUD con baja bloqueada por hijos activos, sin restauración; importación del plan en JSON y exportación a Excel; programación y horario de solo lectura".

### PR 2: periodos escolares y programas educativos

- **Código:**
  - la sección 3 (política `Dgaa` y `EntidadAcademicaResumen`);
  - `IClasificacionesAcademicas` (sección 5);
  - la sección 6 (`IAmbitoOfertaEducativa`);
  - las secciones 7 y 8;
  - de la 11, solo `ProgramacionAcademica` y su configuración;
  - de la 12, P1 y P2 (contrato, implementación y cambios en Institucional) e `IReferenciasPeriodoEscolar`;
  - la composición de la sección 14 para estas piezas, `Modulos.cs` y el `.csproj`.
- **Pruebas unitarias** (`tests/Sgpla.UnitTests/OfertaEducativa/`):
  - `PeriodoEscolarTests`: cada error con su campo, la clave recortada, `Modificar` que falla no cambia nada, `DarDeBaja` idempotente;
  - `ProgramaEducativoTests`: nombre normalizado, errores con su campo, `ClasificacionInmutable` solo con `tuvoPlanes` y cambio real;
  - handlers de comando de ambos recursos con fakes escritos a mano (`Fakes.cs`): repositorios, `IAmbitoOfertaEducativa`, `IClasificacionesAcademicas`, `IReferenciasPeriodoEscolar`; reutilizan `TimeProviderFalso` y `UnitOfWorkFalso`. Una referencia inválida no llama a `SaveChangesAsync`;
  - `AmbitoOfertaEducativaTests` con fakes de `ICurrentUser` e `IAmbitosInstitucionales`: cada rol, entidad de otra área, entidad dada de baja y rol desconocido;
  - en Institucional: la baja de entidad con programas activos y el cambio de área con programas, con un fake del contrato.
- **Pruebas de integración:**
  - `OfertaEducativa/PeriodoEscolarEndpointsTests.cs` (Superusuario): crear 201 con `Location`; 400 por cada error; 409 por clave duplicada y por clave de un periodo dado de baja; listar en orden de clave descendente; modificar 204 y 404; dar de baja 204, 404 y 409 con una programación insertada por SQL (activa y dada de baja); un DGAA recibe 403 al escribir y 200 al leer.
  - `OfertaEducativa/ProgramaEducativoEndpointsTests.cs` (DGAA del área creada por la prueba):
    - crear 201 con la respuesta anidada; 400 por nombre, sistema, nivel, entidad inexistente, entidad dada de baja y entidad de **otra área**; 409 por nombre equivalente sin acentos ni mayúsculas en la misma entidad y sistema; 201 con el mismo nombre en otro sistema;
    - modificar 204; 409 `ClasificacionInmutable` con un plan insertado por SQL (también dado de baja); 404 para un programa de otra área;
    - dar de baja 204, y 409 con un plan activo insertado por SQL;
    - ámbito de lectura: el DGAA lista solo los de su área, la Entidad Académica solo los suyos (404 al obtener otro) y el Superusuario todos;
    - el Superusuario y la Entidad Académica reciben 403 al escribir;
    - filtros y paginación (`pagina=0` y `tamanoPagina=101` → 400).
  - `Institucional/EntidadAcademicaEndpointsTests.cs`: `DarDeBaja_ConProgramasActivos_Responde409`, `DarDeBaja_ConProgramasDadosDeBaja_Responde204`, `Modificar_CambiandoAreaConProgramas_Responde409` (también con programas dados de baja) y `Modificar_SinCambiarAreaConProgramas_Responde204`.
  - `AmbitosInstitucionalesTests`: `ObtenerEntidadesAsync` devuelve `AreaAcademicaId`.
  - Los planes y programaciones "insertados por SQL" usan un helper de pruebas con `SqlConnection` sobre la base del fixture. Un plan necesita un programa; una programación necesita una EE y un periodo. Hasta el PR 3 no hay endpoint de planes, por eso se usa SQL.
- **Documentos:**
  - `pendientes.md`: se borran P1 y P2;
  - `Modulo_Institucional.md`: D6 anota "P1 y P2 resueltas en `Modulo_OfertaEducativa.md` (PR 2)", y §8 agrega los dos errores nuevos;
  - `PLAN_INICIAL.md`: el grafo pasa a "OfertaEducativa → Institucional, Catalogos";
  - `ESTANDAR_MODULOS.md` §9 ("Autorización"): `Politicas` incluye `Dgaa`.

### PR 3: planes de estudio y experiencias educativas

- **Código:** las secciones 9 y 10, `IReferenciasExperienciaEducativa` y el paquete ClosedXML (sección 3).
- **Pruebas unitarias:**
  - `PlanEstudiosTests`: código normalizado y con guiones; errores del código; lista vacía y de 301; el índice en el campo del primer error de una EE (`ExperienciasEducativas[2].Creditos`); `ExperienciaRepetida` tras normalizar (`" enso "` y `"ENSO"`); `DarDeBaja` marca plan y EE con el mismo instante y es idempotente;
  - `ExperienciaEducativaTests`: cada error con su campo; materia y curso en mayúsculas y con ceros iniciales; perfil vacío → `null` y saltos de línea conservados; `Modificar` con y sin `tuvoProgramaciones`, incluido que nombre, perfil y cupos siempre cambian;
  - handlers de importar, dar de baja el plan, crear, modificar y dar de baja una EE, con fakes (incluido `IReferenciasExperienciaEducativa`).
- **Pruebas de integración** (`OfertaEducativa/PlanEstudiosEndpointsTests.cs` y `ExperienciaEducativaEndpointsTests.cs`, con DGAA):
  - importar las 58 EE del plan de ejemplo como datos de prueba en código (no se lee el `.xlsx`): 201 con `experienciasEducativas: 58`;
  - importar: 400 con el índice correcto (créditos 0 en la EE 4, área inexistente en la EE 7, EE repetida), 400 por programa de otra área o dado de baja, 409 por código repetido en el programa, y 201 con el mismo código en otro programa;
  - listar las EE del plan en orden de materia y curso, con `tuvoProgramaciones`;
  - exportar: `Content-Type` y nombre de archivo correctos; con ClosedXML se leen los 14 encabezados exactos y una fila completa, con `CURSO_EE` `00001` como texto;
  - crear una EE: 201; 409 por materia y curso existentes, también de una EE dada de baja; 400 por plan de otra área;
  - modificar una EE: 204; con una programación insertada por SQL (activa o dada de baja), 409 al cambiar horas y 204 al cambiar solo nombre, perfil y cupos; 400 por cupos invertidos;
  - dar de baja una EE: 204, y 409 con una programación activa;
  - dar de baja el plan: 204 y sus EE dejan de verse (404); 409 si una EE tiene una programación activa;
  - ámbito: Entidad Académica y Superusuario leen y exportan en su ámbito y reciben 403 al escribir; un DGAA de otra área recibe 404.
- **Documentos:** ninguno adicional.

### PR 4: programación académica y horarios

- **Código:** la sección 11 completa (consultas, `HorarioProgramacion` y endpoints).
- **Pruebas de integración** (`OfertaEducativa/ProgramacionAcademicaEndpointsTests.cs`), con programaciones, sincronizaciones y horarios insertados por SQL:
  - listar con cada filtro, el NRC en minúsculas encuentra el mismo registro, y el orden por periodo descendente y NRC;
  - obtener 200 con la respuesta anidada y 404 para una dada de baja;
  - horarios en orden de día y hora, con edificio y aula nulos;
  - ámbito: DGAA y Entidad Académica solo ven lo suyo (404 fuera);
  - 405 al escribir.
- **Documentos:** `pendientes.md` P9 anota que la lectura ya existe.

## 16. Criterios de terminado

Cada PR cumple la lista de verificación de `ESTANDAR_MODULOS.md` §15. Solo el PR 1 edita `baseline.sql` y `seed.sql` (D1, D2 y D5); ningún PR agrega migraciones. El Excel de ejemplo de la UV no se versiona.

La verificación se ejecuta solo con Docker, con el comando de `Modulo_Institucional.md` §11 (imagen `mcr.microsoft.com/dotnet/sdk:10.0.401`, copia con finales de línea LF y las tres suites). Además, antes de cada commit se regenera el entorno con `docker compose down -v` y `docker compose up -d --build`, y se prueba a mano con `curl` sobre `http://localhost:8180`:
- **PR 1:** con un token de Superusuario (bootstrap, iniciar sesión y cambiar la contraseña), `GET /api/v1/catalogos/areas-formacion` devuelve las tres áreas `111`, `112` y `113`; y `sqlcmd` en el contenedor `sqlserver` confirma que `academico.archivo_plan_estudios` no existe.
- **PR 2:** el Superusuario crea un área, una entidad, una cuenta DGAA de esa área y un periodo. Como el LDAP real no está disponible, el login del DGAA solo se prueba si hay red hacia la UV; si no, se reporta como omitido.
- **PR 3 y PR 4:** `GET /api/v1/oferta-educativa/planes-estudio` y `/programaciones-academicas` responden 200 con el token de Superusuario.

Todo debe terminar sin advertencias ni fallos. Si una prueba falla por una razón ajena al PR, se reporta: no se modifica la prueba para ocultarla.
