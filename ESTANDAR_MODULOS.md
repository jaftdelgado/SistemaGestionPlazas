# Estándar de implementación de módulos

Este documento define cómo se implementa cada módulo del backend de SGPLa. Es normativo: todos los módulos se construyen de la misma forma, y una desviación se discute y se incorpora aquí antes de programarla.

Documentos relacionados:
- `PLAN_INICIAL.md`: módulos, grafo de dependencias y decisiones de arquitectura.
- `DATABASE.md`: modelo de datos, reglas de negocio y casos de aceptación.
- `sgpla-backend/README.md`: cómo compilar, probar y migrar.

Los ejemplos salen del módulo Catalogos: `GradoAcademico` ilustra un catálogo fijo, de solo lectura, y `Articulo` un recurso administrable, con alta y modificación.

## Contenido

1. [Principios](#1-principios)
2. [Capas y dependencias](#2-capas-y-dependencias)
3. [Estructura de carpetas](#3-estructura-de-carpetas)
4. [Convenciones de nombres](#4-convenciones-de-nombres)
5. [Visibilidad y contratos entre módulos](#5-visibilidad-y-contratos-entre-módulos)
6. [Domain](#6-domain)
7. [Application](#7-application)
8. [Infrastructure](#8-infrastructure)
9. [Endpoints](#9-endpoints)
10. [Composición del módulo](#10-composición-del-módulo)
11. [Observabilidad](#11-observabilidad)
12. [Pruebas](#12-pruebas)
13. [SOLID aplicado](#13-solid-aplicado)
14. [Receta para un recurso nuevo](#14-receta-para-un-recurso-nuevo)
15. [Lista de verificación](#15-lista-de-verificación)
16. [Lo que no se hace](#16-lo-que-no-se-hace)

## 1. Principios

- **Clean Architecture por módulo.** Cada módulo es un proyecto (`Sgpla.Modules.<Modulo>`) con cuatro capas en carpetas: `Domain`, `Application`, `Infrastructure` y `Endpoints`. Las dependencias apuntan hacia el dominio.
- **El dominio protege sus invariantes.** Una entidad nunca queda en un estado inválido, sin importar quién la use.
- **Application no conoce la tecnología.** Los comandos declaran lo que necesitan mediante interfaces (puertos) y Infrastructure las implementa.
- **Las consultas no pasan por puertos.** Una consulta solo proyecta datos y no tiene reglas que aislar, así que su handler es un adaptador de Infrastructure que consulta EF Core directamente.
- **La ceremonia es proporcional a la lógica.** Un recurso sin reglas, como un catálogo fijo, no lleva comandos, validators ni handlers propios, y una regla se declara en un solo lugar.
- **DbUp es dueño del esquema.** EF Core solo mapea; nunca crea ni modifica tablas.
- **Los errores de negocio son valores, no excepciones.** Los casos de uso devuelven `Result`; las excepciones quedan para fallos técnicos.
- **Las reglas se verifican solas.** Lo que se puede comprobar automáticamente está en `tests/Sgpla.ArchitectureTests` y falla en el CI.

## 2. Capas y dependencias

| Capa | Responsabilidad | Puede depender de | Nunca depende de |
|---|---|---|---|
| **Domain** | Entidades, invariantes, normalización y errores de dominio | SharedKernel | Las otras tres capas, EF Core, ASP.NET Core, FluentValidation, BuildingBlocks |
| **Application** | Comandos y sus handlers, puertos de escritura, contratos de consulta (Query y Response) y validators de lo que el dominio no cubre | Domain, SharedKernel, BuildingBlocks.Application, FluentValidation, contratos de módulos permitidos | Infrastructure, Endpoints, EF Core, ASP.NET Core, BuildingBlocks.Infrastructure |
| **Infrastructure** | Configuraciones de EF Core, repositorios (implementan los puertos), handlers de consulta y adaptadores externos | Application, Domain, BuildingBlocks.Infrastructure, EF Core | Endpoints |
| **Endpoints** | Rutas HTTP, modelos de request y traducción de `Result` a respuestas HTTP | Application, helpers HTTP de BuildingBlocks.Infrastructure, ASP.NET Core | Domain, Infrastructure, EF Core |
| **`<Modulo>Module.cs`** | Raíz de composición: registra dependencias y rutas | Todas las capas | — |

```
Endpoints ──► Application ──► Domain ──► SharedKernel
                  ▲
Infrastructure ───┘  (implementa los repositorios y resuelve las consultas de Application)
```

### Piezas compartidas

Viven en `src/BuildingBlocks` y se construyen junto con el primer módulo que las necesite. Un módulo no reimplementa nada de esta lista.

| Proyecto | Contenido |
|---|---|
| `Sgpla.SharedKernel` | `Entity` (clase base con `Id`), `Result`, `Result<T>`, `Error` (con `Campo` opcional), `ErrorType`, `ValidationError`, `IEliminable` (baja lógica: `FechaEliminacion`, `null` significa activa), `Rol` (roles fijos de `usuarios.rol`), `Normalizacion` (texto, recorte y dígitos ASCII) |
| `Sgpla.BuildingBlocks.Application` | `ICommandHandler<TCommand>`, `ICommandHandler<TCommand, TResponse>`, `IQueryHandler<TQuery, TResponse>`, `IUnitOfWork`, `Paginacion`, `Pagina<T>`, `PaginacionValidator<T>`, decoradores de validación, `ICurrentUser` y `Politicas` (nombres de las políticas de autorización) |
| `Sgpla.BuildingBlocks.Infrastructure` | `SgplaDbContext`, implementación de `IUnitOfWork`, `AddPersistenciaModulo`, `AddHandlersModulo`, extensión `PaginarAsync`, helpers HTTP (`ToProblem`, `ToOk`, `ToNoContent`), manejador global de violaciones de unicidad (`ViolacionUnicidadExceptionHandler`: SQL 2601/2627 → 409), `FiltrosConsulta` (nombres de los filtros de consulta globales de EF Core; hoy solo `BajaLogica`, que oculta las filas con `fecha_eliminacion`) |

Para el tiempo se usa `TimeProvider` de .NET, inyectado; no se llama a `DateTime.UtcNow` directamente. Para los logs se usa `ILogger<T>` de .NET; no se crea una abstracción propia (sección 11).

### `Result` y `Error`

| Tipo | Uso |
|---|---|
| `Result` | Resultado sin valor: `Result.Success()` o un `Error` (conversión implícita) |
| `Result<T>` | Resultado con valor: `T` o `Error` (conversiones implícitas). Expone `IsSuccess`, `IsFailure`, `Value` y `Error`. Si `T` es una interfaz (`IReadOnlyList<T>`), se crea con `Result.Success(valor)` |
| `Error(Code, Message, Type)` | Código estable (`Articulo.NumeroDuplicado`), mensaje en español para el usuario y tipo. Un error de validación del dominio declara además `Campo`, la propiedad de la entrada (`nameof(Articulo.Numero)`), y la respuesta lo reporta en `errors` como uno de un validator |
| `ErrorType` | `Validation`, `NotFound` o `Conflict`. Decide el código HTTP (sección 9) |
| `ValidationError` | `Error` de tipo `Validation` con código `Validacion.EntradaInvalida` y los errores por campo que producen los validators |

## 3. Estructura de carpetas

Primero por capa y, dentro de cada capa, por recurso. Un archivo agrupa lo que cambia junto: un comando con su validator y su handler; las consultas de un recurso con su `Response`.

```
src/Modules/Catalogos/Sgpla.Modules.Catalogos/
  Domain/
    CatalogosFijos/                     catálogos fijos (solo lectura)
      CatalogoFijo.cs                   clase base: Id y Nombre
      CatalogoFijoErrors.cs
      GradoAcademico.cs
      ModalidadRecepcion.cs             con una columna adicional
    Articulos/                          recurso administrable
      Articulo.cs
      ArticuloErrors.cs
  Application/
    Contracts/                          API pública para otros módulos (sección 5)
      IReferenciasArticulo.cs
    CatalogosFijos/
      CatalogoFijoConsultas.cs          Response y Queries genéricas
      ModalidadRecepcionConsultas.cs
    Articulos/
      IArticuloRepository.cs            puerto de escritura
      ArticuloConsultas.cs              Response y Queries (con sus validators, si los hay)
      CrearArticulo.cs                  Command, Validator (si hace falta) y Handler
      ModificarArticulo.cs
  Infrastructure/
    CatalogosFijos/
      CatalogoFijoConfiguration.cs      configuración base y una clase de una línea por catálogo
      CatalogoFijoConsultas.cs          handlers genéricos de Listar y Obtener
      ModalidadRecepcionConsultas.cs
    Articulos/
      ArticuloConfiguration.cs
      ArticuloRepository.cs
      ArticuloConsultas.cs              handlers de las consultas
  Endpoints/
    CatalogosFijos/
      CatalogoFijoEndpoints.cs          MapCatalogoFijo<T>
      ModalidadRecepcionEndpoints.cs
    Articulos/
      ArticuloEndpoints.cs              rutas y requests del recurso
  CatalogosModule.cs
```

Las pruebas replican la misma organización: `tests/Sgpla.UnitTests/<Modulo>/<Recurso>/` y `tests/Sgpla.IntegrationTests/<Modulo>/`.

## 4. Convenciones de nombres

**Idioma:**
- Lo que nombra el negocio va en español y sin acentos: `Articulo`, `Modificar`, `ExisteNumeroAsync`.
- Los sufijos de patrón y las abstracciones compartidas de BuildingBlocks van en inglés: `Handler`, `HandleAsync`, `Result`, `IsSuccess`, `SaveChangesAsync`.
- Las propiedades que mapean columnas conservan el nombre de la columna en PascalCase (`FechaEliminacion` ↔ `fecha_eliminacion`).
- El contrato HTTP (rutas, parámetros y campos JSON) va en español, porque lo consume el frontend.

| Elemento | Patrón | Ejemplo |
|---|---|---|
| Entidad | Sustantivo singular | `Articulo` |
| Errores de dominio | `<Entidad>Errors` | `ArticuloErrors` |
| Carpeta de recurso | Plural | `Articulos` |
| Archivo de un comando | `<Accion><Entidad>.cs`, con acciones como `Crear`, `Modificar`, `DarDeBaja`, `Restaurar` | `CrearArticulo.cs` |
| Archivo de consultas | `<Entidad>Consultas.cs`, en Application (Queries y Response) y en Infrastructure (handlers) | `ArticuloConsultas.cs` |
| Comando | `<Accion><Entidad>Command` | `CrearArticuloCommand` |
| Consulta | `<Accion><Entidad>Query` | `ListarArticulosQuery` |
| Handler | `<Accion><Entidad>Handler` | `CrearArticuloHandler`, `ObtenerArticuloHandler` |
| Validator | `<Accion><Entidad>Validator` | `ListarTratamientosAcademicosValidator` |
| Puerto de escritura | `I<Entidad>Repository` | `IArticuloRepository` |
| Respuesta | `<Entidad>Response` | `ArticuloResponse` |
| Configuración EF | `<Entidad>Configuration` | `ArticuloConfiguration` |
| Endpoints | `<Entidad>Endpoints` | `ArticuloEndpoints` |
| Request HTTP | `<Accion><Entidad>Request`, solo si difiere del Command (sección 9) | `ModificarArticuloRequest` |
| Ruta | `/api/v1/<modulo>/<recurso-en-plural>` en kebab-case | `/api/v1/catalogos/grados-academicos` |
| Código de error | `<Entidad>.<Motivo>` | `Articulo.NumeroDuplicado` |
| Prueba | `Metodo_Escenario_Resultado` | `Crear_ConNumeroRepetido_Responde409` |

Las pruebas de arquitectura exigen que los handlers de comandos estén en Application y los de consultas en Infrastructure, sellados; lo comprueban por la interfaz que implementan, así que también cubren los genéricos. Además, las clases terminadas en `Validator` van en Application; las terminadas en `Configuration` y `Repository`, en Infrastructure, y las terminadas en `Endpoints`, en Endpoints.

## 5. Visibilidad y contratos entre módulos

- Todo tipo de un módulo es `internal`, y las clases son `sealed` salvo que se diseñen para herencia.
- Solo hay dos excepciones públicas:
  - la clase `<Modulo>Module`, que usa el host;
  - los tipos de `Application/Contracts`, que usan otros módulos.
- Un módulo solo puede usar el namespace `Application.Contracts` de los módulos que el grafo de `PLAN_INICIAL.md` le permite. Nunca usa sus entidades, handlers ni repositorios.
- El módulo declara `<InternalsVisibleTo Include="Sgpla.UnitTests" />` en su `.csproj` para las pruebas unitarias. Las de integración entran por HTTP y no lo necesitan.

Hay dos formas de colaborar entre módulos, siempre a través de `Contracts`:

| Necesidad | Quién define la interfaz | Quién la implementa | Ejemplo |
|---|---|---|---|
| Un módulo consulta o invoca a otro del que depende | El módulo invocado, en su `Contracts` | El módulo invocado | Docentes pregunta a Catalogos si un grado académico existe |
| Un módulo necesita saber algo de los módulos que dependen de él | El módulo que pregunta, en su `Contracts` | Cada módulo dependiente (inversión de dependencias) | Catalogos pregunta si un artículo tiene referencias; responde Publicacion, por sus Avisos |

En el segundo caso, el consumidor recibe `IEnumerable<IReferenciasArticulo>` y consulta todas las implementaciones registradas. Así puede aparecer un módulo nuevo que referencie artículos sin modificar Catalogos.

Las pruebas de arquitectura exigen estas reglas: ningún tipo público fuera de las dos excepciones y ninguna dependencia hacia otro módulo fuera de su `Contracts`.

## 6. Domain

Reglas:
- Las entidades heredan de `Entity`. Si tienen baja lógica, implementan `IEliminable`.
- Tienen un constructor privado sin parámetros (lo usa EF Core) y propiedades con `private set`.
- Se crean solo con una fábrica estática `Crear(...)`, que normaliza (recorta los textos y pone en mayúsculas las claves y los códigos) y devuelve `Result<T>`.
- El comportamiento se expresa con métodos que nombran la intención (`Modificar`, `DarDeBaja`, `Restaurar`). Nadie asigna propiedades desde fuera.
- Las longitudes y los formatos de `DATABASE.md` son constantes públicas de la entidad. Las configuraciones de EF reutilizan esas constantes.
- **El dominio es la única fuente de las reglas de forma de sus datos:** obligatoriedad, longitudes, formatos y normalización. Cada uno de esos errores declara su campo con `nameof`, así la respuesta HTTP los reporta en `errors` sin que un validator repita la regla (sección 7).
- Los errores se declaran una sola vez en `<Entidad>Errors`.
- Domain no consulta la base. Las reglas que dependen de otros registros, como la unicidad o las referencias, las orquesta el handler con puertos.
- Las fechas llegan como parámetro (`DarDeBaja(DateTime utc)`); la entidad no lee el reloj.
- Excepción: un catálogo fijo hereda de `CatalogoFijo` y no tiene fábrica ni comportamiento (sección 9, "Catálogos fijos").

```csharp
namespace Sgpla.Modules.Catalogos.Domain.Articulos;

/// <summary>
/// Artículo que fundamenta un Aviso (DATABASE.md §15.2). El número es una referencia opaca (<c>42</c>, <c>42 BIS</c>)
/// que queda inmutable cuando un Aviso usa el artículo; la descripción es obligatoria y siempre editable.
/// </summary>
internal sealed partial class Articulo : Entity
{
    public const int LongitudMaximaNumero = 50;
    public const int LongitudMaximaDescripcion = 1000;

    private Articulo()
    {
    }

    public string Numero { get; private set; } = string.Empty;

    public string Descripcion { get; private set; } = string.Empty;

    public static Result<Articulo> Crear(string numero, string descripcion)
    {
        var numeroNormalizado = NormalizarNumero(numero);
        if (numeroNormalizado.IsFailure)
        {
            return numeroNormalizado.Error;
        }

        var descripcionNormalizada = NormalizarDescripcion(descripcion);
        if (descripcionNormalizada.IsFailure)
        {
            return descripcionNormalizada.Error;
        }

        return new Articulo { Numero = numeroNormalizado.Value, Descripcion = descripcionNormalizada.Value };
    }

    /// <summary>
    /// Modifica el número y, si llega, la descripción; una descripción <c>null</c> conserva la actual. Si el número
    /// puede cambiar lo decide el handler, porque depende de si algún Aviso usa el artículo.
    /// </summary>
    public Result Modificar(string numero, string? descripcion)
    {
        var numeroNormalizado = NormalizarNumero(numero);
        if (numeroNormalizado.IsFailure)
        {
            return numeroNormalizado.Error;
        }

        Result<string> descripcionNormalizada = descripcion is null ? Descripcion : NormalizarDescripcion(descripcion);
        if (descripcionNormalizada.IsFailure)
        {
            return descripcionNormalizada.Error;
        }

        Numero = numeroNormalizado.Value;
        Descripcion = descripcionNormalizada.Value;
        return Result.Success();
    }

    /// <summary>"  42  bis " → "42 BIS": sin espacios exteriores, espacios internos simples, ASCII imprimible y en mayúsculas.</summary>
    private static Result<string> NormalizarNumero(string? numero)
    {
        var colapsado = EspaciosRepetidos().Replace(numero?.Trim() ?? string.Empty, " ");

        if (colapsado.Length == 0)
        {
            return ArticuloErrors.NumeroVacio;
        }

        if (colapsado.Length > LongitudMaximaNumero)
        {
            return ArticuloErrors.NumeroDemasiadoLargo;
        }

        if (!colapsado.All(caracter => caracter is >= ' ' and <= '~'))
        {
            return ArticuloErrors.NumeroNoAscii;
        }

        return colapsado.ToUpperInvariant();
    }

    private static Result<string> NormalizarDescripcion(string? descripcion)
    {
        var recortada = descripcion?.Trim() ?? string.Empty;

        if (recortada.Length == 0)
        {
            return ArticuloErrors.DescripcionVacia;
        }

        if (recortada.Length > LongitudMaximaDescripcion)
        {
            return ArticuloErrors.DescripcionDemasiadoLarga;
        }

        return recortada;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspaciosRepetidos();
}
```

```csharp
namespace Sgpla.Modules.Catalogos.Domain.Articulos;

internal static class ArticuloErrors
{
    public static readonly Error NumeroVacio = Error.Validation(
        "Articulo.NumeroVacio", "El número es obligatorio.", nameof(Articulo.Numero));

    public static readonly Error NumeroDemasiadoLargo = Error.Validation(
        "Articulo.NumeroDemasiadoLargo",
        $"El número admite hasta {Articulo.LongitudMaximaNumero} caracteres.",
        nameof(Articulo.Numero));

    public static readonly Error NumeroNoAscii = Error.Validation(
        "Articulo.NumeroNoAscii",
        "El número solo admite letras sin acentos, dígitos, espacios y signos ASCII.",
        nameof(Articulo.Numero));

    public static readonly Error DescripcionVacia = Error.Validation(
        "Articulo.DescripcionVacia", "La descripción es obligatoria.", nameof(Articulo.Descripcion));

    public static readonly Error DescripcionDemasiadoLarga = Error.Validation(
        "Articulo.DescripcionDemasiadoLarga",
        $"La descripción admite hasta {Articulo.LongitudMaximaDescripcion} caracteres.",
        nameof(Articulo.Descripcion));

    public static readonly Error NumeroDuplicado = Error.Conflict(
        "Articulo.NumeroDuplicado", "Ya existe un artículo con ese número.");

    public static readonly Error NumeroInmutable = Error.Conflict(
        "Articulo.NumeroInmutable", "El número ya no puede modificarse porque un Aviso usa el artículo.");

    public static Error NoEncontrado(int id) => Error.NotFound(
        "Articulo.NoEncontrado", $"No existe el artículo {id}.");
}
```

El dominio devuelve el primer error que encuentra, no todos los campos a la vez. Basta para un formulario que ya valida en el cliente, y evita mantener la misma regla en dos lugares.

**Baja lógica.** Una entidad `IEliminable`:
- `DarDeBaja(DateTime utc)` es idempotente: si ya tiene fecha, no la reemplaza.
- `Restaurar()` limpia la fecha.
- Las reglas de cascada y de padres activos (`DATABASE.md` §9) las orquesta el handler, porque involucran a otras entidades.

## 7. Application

Application distingue dos tipos de caso de uso:

| Tipo | Qué hace | Dónde vive su handler | Pasa por el dominio |
|---|---|---|---|
| **Comando** | Modifica el estado: crea, modifica, da de baja, cambia un estado | Application, junto al Command | Sí, mediante puertos |
| **Consulta** | Lee y proyecta a un `Response` | Infrastructure (sección 8) | No |

### Comandos

Cada comando vive en un archivo `<Accion><Entidad>.cs` con sus piezas:

| Pieza | Forma | Responsabilidad |
|---|---|---|
| Command | `internal sealed record` | Datos de entrada, sin lógica |
| Validator | `AbstractValidator<T>` de FluentValidation | Opcional: solo lo que el dominio no valida (tabla siguiente). Cada regla declara con `WithName` el nombre del campo como lo lee el usuario |
| Handler | Implementa `ICommandHandler` | Orquesta: llamada al dominio, reglas que dependen de datos, persistencia y respuesta |

Un handler nunca llama a otro handler; si dos casos de uso comparten lógica, esa lógica va en el dominio o en un servicio de Application con su propia interfaz.

### Dónde se valida

| Regla | Dónde | Ejemplo |
|---|---|---|
| Forma de los datos de una entidad: obligatoriedad, longitudes, formatos, normalización | Dominio, con `Error.Validation(código, mensaje, campo)` | `ArticuloErrors.NumeroVacio` |
| Parámetros que no son datos de una entidad | Validator de la Query o del Command | `gradoAcademicoId > 0`, paginación |
| Reglas que dependen de otros registros | Handler de comando, con puertos | número duplicado, referencias de otro módulo |

El decorador de validación ejecuta los validators antes del handler. Si hay errores, devuelve un `ValidationError` sin llamar al handler.

### Consultas

Una consulta no tiene reglas: proyecta. Application declara solo su contrato, en `<Entidad>Consultas.cs`: el `Response`, las Queries y, si reciben parámetros que validar, sus validators.

```csharp
using Sgpla.Modules.Catalogos.Domain.Articulos;

namespace Sgpla.Modules.Catalogos.Application.Articulos;

internal sealed record ArticuloResponse(int Id, string Numero, string Descripcion)
{
    public static ArticuloResponse Desde(Articulo articulo) => new(articulo.Id, articulo.Numero, articulo.Descripcion);
}

/// <summary>Todos los artículos, en orden alfabético del número (con el id como desempate).</summary>
internal sealed record ListarArticulosQuery;

internal sealed record ObtenerArticuloQuery(int Id);
```

Una entidad de dominio nunca sale de Application: los endpoints solo ven `Response`.

### Puertos

Solo hay puertos de escritura. `I<Entidad>Repository` devuelve entidades de dominio y tiene los métodos que los comandos necesitan (`ObtenerPorIdAsync`, `ExisteNumeroAsync`, `Agregar`). Nunca guarda: eso lo hace `IUnitOfWork`.

Un puerto se declara cuando un comando lo necesita; no se crean repositorios genéricos ni métodos "por si acaso". Las consultas no tienen puerto, y un recurso sin comandos (un catálogo fijo) tampoco.

```csharp
using Sgpla.Modules.Catalogos.Domain.Articulos;

namespace Sgpla.Modules.Catalogos.Application.Articulos;

internal interface IArticuloRepository
{
    Task<Articulo?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Compara el número ya normalizado, en la base.</summary>
    Task<bool> ExisteNumeroAsync(string numero, int? excluirId, CancellationToken cancellationToken);

    void Agregar(Articulo articulo);
}
```

### Ejemplo: crear

La forma del número y la descripción la valida `Articulo.Crear`, así que el comando no tiene validator.

```csharp
using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Catalogos.Domain.Articulos;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Catalogos.Application.Articulos;

internal sealed record CrearArticuloCommand(string Numero, string Descripcion);

/// <summary>La forma de la entrada la valida <see cref="Articulo.Crear"/>; aquí solo queda la unicidad del número.</summary>
internal sealed class CrearArticuloHandler(
    IArticuloRepository repositorio,
    IUnitOfWork unidadDeTrabajo) : ICommandHandler<CrearArticuloCommand, ArticuloResponse>
{
    public async Task<Result<ArticuloResponse>> HandleAsync(
        CrearArticuloCommand command,
        CancellationToken cancellationToken)
    {
        var creado = Articulo.Crear(command.Numero, command.Descripcion);
        if (creado.IsFailure)
        {
            return creado.Error;
        }

        if (await repositorio.ExisteNumeroAsync(creado.Value.Numero, excluirId: null, cancellationToken))
        {
            return ArticuloErrors.NumeroDuplicado;
        }

        repositorio.Agregar(creado.Value);
        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);

        return ArticuloResponse.Desde(creado.Value);
    }
}
```

### Ejemplo: modificar con referencias de otros módulos

`DATABASE.md` §15.2 permite modificar la descripción de un artículo siempre, y su número solo mientras ningún Aviso lo use. Las referencias están en otro módulo (los Avisos de Publicacion), así que el handler consulta el contrato `IReferenciasArticulo` (sección 5), y solo cuando el número cambia. Repetir los valores actuales no es una modificación: responde éxito sin guardar, para que la operación sea idempotente.

```csharp
namespace Sgpla.Modules.Catalogos.Application.Contracts;

/// <summary>
/// Lo implementa cada módulo que guarda referencias a un artículo (Publicacion, por sus Avisos). Con una referencia,
/// el número del artículo queda inmutable.
/// </summary>
public interface IReferenciasArticulo
{
    Task<bool> TieneReferenciasAsync(int articuloId, CancellationToken cancellationToken);
}
```

```csharp
using Sgpla.BuildingBlocks.Application;
using Sgpla.Modules.Catalogos.Application.Contracts;
using Sgpla.Modules.Catalogos.Domain.Articulos;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Catalogos.Application.Articulos;

/// <param name="Descripcion"><c>null</c> conserva la descripción actual.</param>
internal sealed record ModificarArticuloCommand(int Id, string Numero, string? Descripcion);

/// <summary>
/// Modifica número y descripción. La descripción siempre puede cambiar; el número solo mientras ningún Aviso use el
/// artículo (DATABASE.md §15.2). Las referencias están en otros módulos, así que se consulta a todas las
/// implementaciones de <see cref="IReferenciasArticulo"/>.
/// </summary>
internal sealed class ModificarArticuloHandler(
    IArticuloRepository repositorio,
    IEnumerable<IReferenciasArticulo> referencias,
    IUnitOfWork unidadDeTrabajo) : ICommandHandler<ModificarArticuloCommand>
{
    public async Task<Result> HandleAsync(ModificarArticuloCommand command, CancellationToken cancellationToken)
    {
        var articulo = await repositorio.ObtenerPorIdAsync(command.Id, cancellationToken);
        if (articulo is null)
        {
            return ArticuloErrors.NoEncontrado(command.Id);
        }

        var (numeroAnterior, descripcionAnterior) = (articulo.Numero, articulo.Descripcion);
        var modificado = articulo.Modificar(command.Numero, command.Descripcion);
        if (modificado.IsFailure)
        {
            return modificado;
        }

        var cambiaNumero = !string.Equals(articulo.Numero, numeroAnterior, StringComparison.Ordinal);
        var cambiaDescripcion = !string.Equals(articulo.Descripcion, descripcionAnterior, StringComparison.Ordinal);
        if (!cambiaNumero && !cambiaDescripcion)
        {
            return Result.Success();
        }

        if (cambiaNumero)
        {
            foreach (var referencia in referencias)
            {
                if (await referencia.TieneReferenciasAsync(articulo.Id, cancellationToken))
                {
                    return ArticuloErrors.NumeroInmutable;
                }
            }

            if (await repositorio.ExisteNumeroAsync(articulo.Numero, articulo.Id, cancellationToken))
            {
                return ArticuloErrors.NumeroDuplicado;
            }
        }

        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
```

### Reglas del handler de comando

- Recibe sus dependencias por constructor primario, siempre como interfaces.
- Llama a `SaveChangesAsync` una sola vez, al final. Todo lo que modifica la operación se guarda de forma atómica.
- Si la operación afecta a varias filas con una misma fecha (baja en cascada), obtiene el instante una vez con `TimeProvider`, truncado a segundos por `datetime2(0)`, y lo reutiliza.
- No lanza excepciones por reglas de negocio: devuelve un `Error`.
- Propaga el `CancellationToken` a toda llamada asíncrona.

## 8. Infrastructure

### Configuración de entidades

Una `IEntityTypeConfiguration<T>` por entidad. `SgplaDbContext` las descubre en el ensamblado del módulo.

```csharp
namespace Sgpla.Modules.Catalogos.Infrastructure.Articulos;

internal sealed class ArticuloConfiguration : IEntityTypeConfiguration<Articulo>
{
    public void Configure(EntityTypeBuilder<Articulo> builder)
    {
        builder.ToTable("articulo", "plazas");
        builder.HasKey(a => a.Id);

        // varchar en la base: sin IsUnicode(false) los parámetros serían nvarchar y forzarían una conversión.
        builder.Property(a => a.Numero).HasMaxLength(Articulo.LongitudMaximaNumero).IsUnicode(false);
        builder.Property(a => a.Descripcion).HasMaxLength(Articulo.LongitudMaximaDescripcion);
    }
}
```

Reglas:
- Tabla y esquema explícitos con `ToTable`. Los nombres de columna salen de la convención snake_case.
- Longitudes con las constantes de la entidad.
- Los estados se guardan como texto con `HasConversion<string>()`.
- Una entidad `IEliminable` declara el filtro con nombre de baja lógica:

  ```csharp
  builder.HasQueryFilter(FiltrosConsulta.BajaLogica, r => r.FechaEliminacion == null);
  ```

  Para incluir bajas (restaurar, `incluirEliminados`) se usa `IgnoreQueryFilters([FiltrosConsulta.BajaLogica])`.
- No se declaran índices, restricciones únicas, CHECK ni intercalaciones. El esquema lo define DbUp, y un cambio de esquema es una migración nueva en `Scripts/` (ver el README del backend).
- Las referencias a actores (`cargado_por_usuario_id` y similares) se mapean como `int` sin navegación, para no depender del módulo Usuarios.

### Repositorios

```csharp
namespace Sgpla.Modules.Catalogos.Infrastructure.Articulos;

internal sealed class ArticuloRepository(SgplaDbContext contexto) : IArticuloRepository
{
    public Task<Articulo?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
        contexto.Set<Articulo>().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<bool> ExisteNumeroAsync(string numero, int? excluirId, CancellationToken cancellationToken) =>
        contexto.Set<Articulo>()
            .AnyAsync(a => a.Numero == numero && (excluirId == null || a.Id != excluirId), cancellationToken);

    public void Agregar(Articulo articulo) => contexto.Set<Articulo>().Add(articulo);
}
```

### Handlers de consulta

Un handler de consulta implementa `IQueryHandler` en Infrastructure y consulta `SgplaDbContext` directamente: `AsNoTracking`, proyección al `Response` en SQL y `Result` con el error `NotFound` del dominio. No hay puerto intermedio: la consulta no tiene reglas que aislar, EF Core ya es la abstracción sobre la base, y las pruebas de integración la cubren contra SQL Server real. Se registran por escaneo y, como cualquier handler, pasan por el decorador de validación.

Los handlers de consulta de un recurso comparten el archivo `<Entidad>Consultas.cs`.

```csharp
using Microsoft.EntityFrameworkCore;
using Sgpla.BuildingBlocks.Application;
using Sgpla.BuildingBlocks.Infrastructure.Persistence;
using Sgpla.Modules.Catalogos.Application.Articulos;
using Sgpla.Modules.Catalogos.Domain.Articulos;
using Sgpla.SharedKernel;

namespace Sgpla.Modules.Catalogos.Infrastructure.Articulos;

internal sealed class ListarArticulosHandler(SgplaDbContext contexto)
    : IQueryHandler<ListarArticulosQuery, IReadOnlyList<ArticuloResponse>>
{
    public async Task<Result<IReadOnlyList<ArticuloResponse>>> HandleAsync(
        ListarArticulosQuery query,
        CancellationToken cancellationToken) =>
        Result.Success<IReadOnlyList<ArticuloResponse>>(await contexto.Set<Articulo>()
            .AsNoTracking()
            .OrderBy(a => a.Numero)
            .ThenBy(a => a.Id)
            .Select(a => new ArticuloResponse(a.Id, a.Numero, a.Descripcion))
            .ToListAsync(cancellationToken));
}

internal sealed class ObtenerArticuloHandler(SgplaDbContext contexto) : IQueryHandler<ObtenerArticuloQuery, ArticuloResponse>
{
    public async Task<Result<ArticuloResponse>> HandleAsync(ObtenerArticuloQuery query, CancellationToken cancellationToken)
    {
        var articulo = await contexto.Set<Articulo>()
            .AsNoTracking()
            .Where(a => a.Id == query.Id)
            .Select(a => new ArticuloResponse(a.Id, a.Numero, a.Descripcion))
            .FirstOrDefaultAsync(cancellationToken);

        return articulo is null ? ArticuloErrors.NoEncontrado(query.Id) : articulo;
    }
}
```

Un listado paginado termina en `.PaginarAsync(paginacion, cancellationToken)` en lugar de `ToListAsync`.

Reglas:
- **Comparaciones de texto en la base, no en memoria.** Las columnas tienen su intercalación (por ejemplo, `Modern_Spanish_100_CI_AI`), así que `g.Nombre == nombre` ya ignora mayúsculas y acentos si se ejecuta en SQL Server.
- **La unicidad incluye las filas dadas de baja** (`DATABASE.md` §5). En entidades con baja lógica, las comprobaciones de unicidad usan `IgnoreQueryFilters`.
- **Los listados siempre tienen un orden total**, con el `Id` como desempate, para que la paginación sea estable.
- **La comprobación previa de unicidad no basta ante dos peticiones simultáneas.** La restricción de la base es la última defensa, y el manejador global traduce su violación a 409.

## 9. Endpoints

Cada recurso tiene una clase estática `<Entidad>Endpoints` con un método de extensión que cuelga sus rutas del grupo del módulo. Cada endpoint hace cuatro cosas:
1. recibe el request;
2. lo convierte en Command o Query;
3. invoca el handler;
4. traduce el `Result`.

La lógica de negocio no vive aquí.

```csharp
namespace Sgpla.Modules.Catalogos.Endpoints.Articulos;

internal static class ArticuloEndpoints
{
    private const string NombreRutaObtener = "ObtenerArticulo";

    public static RouteGroupBuilder MapArticuloEndpoints(this RouteGroupBuilder modulo)
    {
        var grupo = modulo.MapGroup("/articulos").WithTags("Artículos");
        // Autorización: cuando exista JWT (módulo Usuarios), registrar y modificar quedan solo para el Superusuario.

        grupo.MapGet("/", Listar).WithName("ListarArticulos")
            .WithSummary("Lista todos los artículos, en orden alfabético del número.");
        grupo.MapGet("/{id:int}", Obtener).WithName(NombreRutaObtener).WithSummary("Obtiene un artículo.")
            .ProducesProblem(StatusCodes.Status404NotFound);
        grupo.MapPost("/", Crear).WithName("CrearArticulo").WithSummary("Registra un artículo.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
        grupo.MapPut("/{id:int}", Modificar).WithName("ModificarArticulo")
            .WithSummary("Modifica el artículo: la descripción siempre; el número, solo mientras ningún Aviso lo use.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return modulo;
    }

    private static async Task<Results<Ok<IReadOnlyList<ArticuloResponse>>, ProblemHttpResult>> Listar(
        IQueryHandler<ListarArticulosQuery, IReadOnlyList<ArticuloResponse>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ListarArticulosQuery(), cancellationToken)).ToOk();

    private static async Task<Results<Ok<ArticuloResponse>, ProblemHttpResult>> Obtener(
        int id,
        IQueryHandler<ObtenerArticuloQuery, ArticuloResponse> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ObtenerArticuloQuery(id), cancellationToken)).ToOk();

    /// <summary>El cuerpo coincide con el comando, así que se enlaza directamente sin un request propio.</summary>
    private static async Task<Results<CreatedAtRoute<ArticuloResponse>, ProblemHttpResult>> Crear(
        CrearArticuloCommand command,
        ICommandHandler<CrearArticuloCommand, ArticuloResponse> handler,
        CancellationToken cancellationToken)
    {
        var resultado = await handler.HandleAsync(command, cancellationToken);

        return resultado.IsSuccess
            ? TypedResults.CreatedAtRoute(resultado.Value, NombreRutaObtener, new { id = resultado.Value.Id })
            : resultado.Error.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Modificar(
        int id,
        ModificarArticuloRequest request,
        ICommandHandler<ModificarArticuloCommand> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ModificarArticuloCommand(id, request.Numero, request.Descripcion), cancellationToken))
            .ToNoContent();
}

/// <param name="Descripcion">Opcional: si no llega (o llega <c>null</c>), se conserva la descripción actual.</param>
internal sealed record ModificarArticuloRequest(string Numero, string? Descripcion);
```

Reglas:
- **Un request solo si difiere del Command.** Si el cuerpo coincide con el Command, el endpoint lo enlaza directamente (`CrearArticuloCommand`). Si difieren, por ejemplo porque el `id` viene de la ruta, hay un `<Accion><Entidad>Request` en el mismo archivo que los endpoints del recurso.
- **Siempre `TypedResults` y tipos de retorno `Results<...>`,** para que OpenAPI documente cada respuesta. `ToOk()` y `ToNoContent()` traducen el `Result`; `ToProblem()` queda para las respuestas que esos helpers no cubren, como un 201 con `CreatedAtRoute`. Todo endpoint lleva `WithName` y `WithSummary`.
- **Cada error de la tabla de operaciones se declara** con `ProducesValidationProblem()` (400) y `ProducesProblem(<código>)` (404, 409). `ProblemHttpResult` no publica sus códigos, así que sin esas llamadas OpenAPI no los muestra.
- **Rutas con restricción de tipo:** `{id:int}`.
- **Autorización:** se declara en el grupo del módulo, o del recurso si difiere, con `RequireAuthorization(<política>)`. Las políticas (`Superusuario`, `Dgaa`, `EntidadAcademica`) llegan con el módulo Usuarios. Hasta entonces, el grupo lleva un comentario en ese punto, sin otra solución provisional.

### Operaciones estándar

| Operación | Método y ruta | Éxito | Errores |
|---|---|---|---|
| Listar | `GET /` con `pagina` (desde 1), `tamanoPagina` (1 a 100, 20 por omisión) e `incluirEliminados` si hay baja lógica | 200 con `Pagina<T>` | 400 |
| Listar completo | `GET /` sin paginación, para catálogos fijos y recursos con pocos registros (como `Articulo`), con filtros opcionales | 200 con un arreglo en un orden total | 400 si un filtro es inválido |
| Obtener | `GET /{id}` | 200 | 404 |
| Crear | `POST /` | 201 con `Location` y el recurso | 400, 409 |
| Editar | `PUT /{id}` con los campos editables | 204 | 400, 404, 409 |
| Dar de baja | `DELETE /{id}` (baja lógica, idempotente) | 204 | 404, 409 |
| Restaurar | `POST /{id}/restaurar` | 204 | 404, 409 |
| Acción de estado | `POST /{id}/<accion>` (`enviar`, `avalar`, `cancelar`...) | 204 o 200 | 404, 409 |

Un recurso expone solo las operaciones que `PLAN_INICIAL.md` y `DATABASE.md` le permiten. Por ejemplo, los catálogos no tienen baja, y los catálogos fijos (`municipio`, `grado_academico`...) solo se listan y se obtienen.

`Pagina<T>` se serializa como:

```json
{ "elementos": [], "pagina": 1, "tamanoPagina": 20, "total": 0 }
```

Los parámetros de un listado paginado llegan en un request propio enlazado con `[AsParameters]`, que los convierte en `Paginacion`. Cada parámetro declara su nombre en camelCase con `[FromQuery(Name = ...)]`, para que OpenAPI lo documente igual que se envía. El validator del listado incluye las reglas compartidas, que reportan los errores con el nombre del parámetro HTTP (`pagina`, `tamanoPagina`). Con un recurso genérico `Recurso`:

```csharp
internal sealed record ListarRecursosRequest(
    [FromQuery(Name = "pagina")] int? Pagina,
    [FromQuery(Name = "tamanoPagina")] int? TamanoPagina)
{
    public Paginacion ComoPaginacion() =>
        new(Pagina ?? 1, TamanoPagina ?? Paginacion.TamanoPorOmision);
}

internal sealed class ListarRecursosValidator : AbstractValidator<ListarRecursosQuery>
{
    public ListarRecursosValidator()
    {
        Include(new PaginacionValidator<ListarRecursosQuery>(q => q.Paginacion));
    }
}
```

### Errores

Todos los errores se responden como ProblemDetails, con la extensión `codigo` igual al `Error.Code`.

| `ErrorType` | HTTP | Cuándo |
|---|---|---|
| `Validation` | 400 | Entrada mal formada; incluye `errors` por campo, de un validator o del `Campo` de un error de dominio. También un `id` del cuerpo que referencia a un registro inexistente (por ejemplo, el grado de un tratamiento) |
| `NotFound` | 404 | El recurso de la ruta no existe (o está dado de baja y la operación no admite bajas) |
| `Conflict` | 409 | Duplicados, valores inmutables, transiciones de estado inválidas, padres inactivos |
| `Unauthorized` | 401 | Credenciales rechazadas o sesión inválida |
| `Unavailable` | 503 | Un servicio externo (LDAP) no está disponible |

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflicto",
  "status": 409,
  "detail": "Ya existe un artículo con ese número.",
  "codigo": "Articulo.NumeroDuplicado"
}
```

### Catálogos fijos

Un catálogo fijo tiene valores definidos por el negocio que ningún usuario agrega, modifica ni elimina (`DATABASE.md` §5; por ejemplo, `grado_academico`). No tiene reglas, así que casi todo es compartido:

| Pieza | Regla |
|---|---|
| Datos | Se cargan en `Baseline/seed.sql` con ids fijos (`SET IDENTITY_INSERT ... ON`) y quedan documentados en `DATABASE.md` |
| Domain | Subclase sellada de `CatalogoFijo` (`Id` y `Nombre`) en `Domain/CatalogosFijos`, con constructor privado, la constante `LongitudMaximaNombre` y, si las tiene, sus columnas adicionales con `private set`. Sin fábrica, comportamiento ni `Errors` propio: el 404 es `CatalogoFijoErrors.NoEncontrado<T>`, con código `<Catalogo>.NoEncontrado` |
| Infrastructure | Una clase de una línea que hereda de `CatalogoFijoConfiguration<T>` con esquema, tabla y longitud |
| Si solo tiene nombre | Nada más: `AddCatalogoFijo<T>()` registra los handlers genéricos y `MapCatalogoFijo<T>(ruta, etiqueta)` mapea las rutas con `CatalogoFijoResponse` |
| Si tiene columnas adicionales o filtros | Sus propias Queries, handlers y endpoints, como cualquier consulta (`ModalidadRecepcion`, `TratamientoAcademico`) |
| Si es grande o se consulta con búsqueda | Sus propias Queries, handlers y endpoints, como el caso anterior, pero el listado se pagina en orden alfabético (`Pagina<CatalogoFijoResponse>`) y admite el parámetro opcional `busqueda`, que filtra por `Contains` sin distinguir mayúsculas ni acentos gracias a la colación de la columna (`Municipio`). `GET /{id}` no cambia |
| Endpoints | El caso general: `GET /` devuelve un arreglo JSON con todos los valores, ordenado por `id`, y puede aceptar filtros opcionales por query (por ejemplo, `gradoAcademicoId` en tratamientos académicos; un filtro sin coincidencias devuelve un arreglo vacío). `GET /{id}` responde 200 o 404 con `codigo`. No hay rutas de escritura: un `POST`, `PUT` o `DELETE` responde 405 |
| Pruebas | Sin pruebas unitarias. Una prueba de semilla con los ids y nombres exactos, y pruebas de integración del listado exacto, de obtener (200 y 404) y del 405 de escritura. Mientras un catálogo no tenga valores definidos, la semilla lo deja vacío y las pruebas verifican el arreglo vacío |

Un catálogo nuevo que solo tiene nombre son la entidad, su configuración y dos líneas en `<Modulo>Module`:

```csharp
// Domain/CatalogosFijos/TipoPlaza.cs
internal sealed class TipoPlaza : CatalogoFijo
{
    public const int LongitudMaximaNombre = 150;

    private TipoPlaza()
    {
    }
}

// Infrastructure/CatalogosFijos/CatalogoFijoConfiguration.cs
internal sealed class TipoPlazaConfiguration()
    : CatalogoFijoConfiguration<TipoPlaza>("plazas", "tipo_plaza", TipoPlaza.LongitudMaximaNombre);

// CatalogosModule.cs
services.AddCatalogoFijo<TipoPlaza>();
grupo.MapCatalogoFijo<TipoPlaza>("/tipos-plaza", "Tipos de plaza");
```

`MapCatalogoFijo<T>` no conoce el dominio: recibe el tipo como parámetro genérico sin restricciones, y quien lo cierra con la entidad es la raíz de composición.

## 10. Composición del módulo

`<Modulo>Module.cs` es el único lugar que conoce todas las capas. El host (`Program.cs`) solo llama a sus dos métodos.

```csharp
namespace Sgpla.Modules.Catalogos;

/// <summary>Punto de registro del módulo Catalogos en el host.</summary>
public static class CatalogosModule
{
    public const string Ruta = "/api/v1/catalogos";

    public static IServiceCollection AddCatalogosModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var ensamblado = typeof(CatalogosModule).Assembly;

        services.AddPersistenciaModulo(ensamblado);
        services.AddHandlersModulo(ensamblado);

        // Catálogos fijos que solo tienen nombre: handlers genéricos. Los demás se registran por escaneo.
        services.AddCatalogoFijo<GradoAcademico>();
        services.AddCatalogoFijo<TipoDocumentoExpediente>();
        services.AddCatalogoFijo<TipoPlaza>();
        services.AddCatalogoFijo<TipoContratacion>();

        // Artículos. IReferenciasArticulo lo registra cada módulo que lo implementa (Publicacion).
        services.AddScoped<IArticuloRepository, ArticuloRepository>();

        return services;
    }

    public static IEndpointRouteBuilder MapCatalogosEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup(Ruta);
        // Autorización: cuando exista JWT (módulo Usuarios), las consultas quedan para cualquier usuario autenticado.

        grupo.MapCatalogoFijo<GradoAcademico>("/grados-academicos", "Grados académicos");
        grupo.MapCatalogoFijo<TipoDocumentoExpediente>("/tipos-documento-expediente", "Tipos de documento de expediente");
        grupo.MapCatalogoFijo<TipoPlaza>("/tipos-plaza", "Tipos de plaza");
        grupo.MapCatalogoFijo<TipoContratacion>("/tipos-contratacion", "Tipos de contratación");
        grupo.MapTratamientoAcademicoEndpoints();
        grupo.MapModalidadRecepcionEndpoints();
        grupo.MapArticuloEndpoints();

        return endpoints;
    }

    private static void AddCatalogoFijo<TCatalogo>(this IServiceCollection services)
        where TCatalogo : CatalogoFijo
    {
        services.AddScoped<
            IQueryHandler<ListarCatalogoFijoQuery<TCatalogo>, IReadOnlyList<CatalogoFijoResponse>>,
            ListarCatalogoFijoHandler<TCatalogo>>();
        services.AddScoped<
            IQueryHandler<ObtenerCatalogoFijoQuery<TCatalogo>, CatalogoFijoResponse>,
            ObtenerCatalogoFijoHandler<TCatalogo>>();
    }
}
```

El contrato `IReferenciasArticulo` no se registra aquí: lo registra cada módulo que lo implementa, en su propio `<Modulo>Module` (por ejemplo, `services.AddScoped<IReferenciasArticulo, ReferenciasArticuloEnAvisos>()` en Publicacion). Mientras ninguno lo implemente, el handler recibe una lista vacía y el número siempre puede modificarse.

Reglas:
- Los handlers y validators se registran por escaneo con `AddHandlersModulo`, estén en Application (comandos) o en Infrastructure (consultas). Cada handler se resuelve por su interfaz envuelto en el decorador de validación; la decoración es propia (`ActivatorUtilities`), sin bibliotecas de terceros.
- Los handlers genéricos de catálogos fijos no se pueden escanear, porque la interfaz no tiene la misma aridad que la clase; los registra `AddCatalogoFijo<T>` sin decorador, porque no reciben parámetros que validar.
- Los repositorios se registran uno por uno, con ciclo de vida `Scoped`, para que la composición sea visible y revisable.
- La configuración del módulo, cuando exista, se enlaza a una clase `<Nombre>Options` validada al arrancar (`ValidateOnStart`).
- Una dependencia nueva entre módulos requiere tres cambios en el mismo PR:
  - la referencia en el `.csproj`;
  - la entrada en `tests/Sgpla.ArchitectureTests/Modulos.cs`;
  - el grafo de `PLAN_INICIAL.md`.

## 11. Observabilidad

Se usan las abstracciones de .NET, sin envoltorios propios: `ILogger<T>` para logs y `Activity` para correlación. El formato de salida, el destino y las métricas son decisiones de despliegue; se configuran en el host y no afectan a los módulos.

### Qué se registra

| Se registra | No se registra |
|---|---|
| Eventos que no se ven en la respuesta HTTP: una sincronización de PLANEA fallida, un reintento, un acceso (`DATABASE.md` §13.5) | Un `Result` fallido: el cliente ya recibe el 4xx con su `codigo` |
| Decisiones automáticas del sistema, como una asignación cerrada al avalar un Acta | Excepciones que ya registra `UseExceptionHandler`: no se captura una excepción para registrarla y relanzarla |

### Cómo se registra

- Solo con métodos `[LoggerMessage]`, en una clase `internal static partial class <Clase>Log` junto a quien registra. El analizador lo exige (CA1848 y CA2254 como error).
- Los parámetros solo llevan identificadores y valores no personales: numéricos, `bool`, `Guid`, enums, fechas y `string` cuyo nombre termine en `Clave`, `Codigo` o `Estado`. Una prueba de arquitectura lo verifica.
- Nunca se registran secretos (contraseñas, verificadores, tokens, cadenas de conexión), datos de personas (nombres, correos, número de personal, perfiles y formaciones de Aspirantes y Docentes), texto libre de evaluación (observaciones, motivos, comentarios) ni nombres de archivo de expedientes. Se registra el `id`, no el dato.
- El mensaje de una violación de unicidad de SQL Server incluye el valor duplicado; por eso el manejador global de 2601/2627 registra solo el nombre de la restricción.

```csharp
internal static partial class SincronizarPlaneaLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Sincronización {SincronizacionId} del periodo {PeriodoClave} terminó en {Estado}")]
    public static partial void SincronizacionFallida(this ILogger logger, int sincronizacionId, string periodoClave, string estado);
}
```

### Correlación

- Toda respuesta de error (ProblemDetails) incluye `traceId`. El helper `ToProblem` lo conserva.
- Los logs incluyen el `TraceId` en sus scopes. `Program.cs` lo fija con `ActivityTrackingOptions = TraceId | SpanId`, en lugar de depender del valor por omisión del host.
- El `traceId` de la respuesta tiene formato W3C (`00-<TraceId>-<SpanId>-00`), y su segmento central es el que aparece en los logs; así, un error reportado desde el frontend se localiza en los logs por ese valor.
- La prueba de integración `CorrelacionTests` verifica que el `traceId` de una respuesta de error aparece en los logs de esa misma petición. Provoca el 404 de un grado académico inexistente, que responde `ToProblem`; como el helper es compartido, queda cubierto para todos los módulos.
- `EnableSensitiveDataLogging` de EF Core no se activa en ningún entorno. Una prueba de integración lo verifica.
- EF Core no registra el evento `SaveChangesFailed`, porque su mensaje incluye la excepción completa, y la de una violación de unicidad trae el valor duplicado. La excepción no se pierde: la traduce el manejador global o la registra `UseExceptionHandler`. `ViolacionUnicidadTests` verifica esa configuración, que el manejador global solo registra el nombre de la restricción y que ningún log de EF Core lleva el valor duplicado.

## 12. Pruebas

| Proyecto | Qué se prueba | Obligatorio |
|---|---|---|
| `Sgpla.UnitTests` | Fábricas, normalización e invariantes de cada entidad. Ramas de reglas de un handler de comando con puertos falsos, cuando tiene lógica propia. Los handlers de consulta no llevan pruebas unitarias: los cubren las de integración | Sí, para toda entidad con comportamiento (los catálogos fijos no tienen) |
| `Sgpla.IntegrationTests` | Cada endpoint de punta a punta contra SQL Server en contenedor: éxito y cada error que documenta (400, 404, 409). La semilla de cada catálogo fijo | Sí, para todo endpoint |
| `Sgpla.ArchitectureTests` | Las reglas de este documento | Automático; no se edita salvo para agregar módulos o reglas |

### Unitarias

```csharp
namespace Sgpla.UnitTests.Catalogos.Articulos;

public sealed class ArticuloTests
{
    [Fact]
    public void Crear_ConEspaciosYMinusculas_NormalizaElNumero()
    {
        var resultado = Articulo.Crear("  42  bis ", "Descripción.");

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.Numero.ShouldBe("42 BIS");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_SinNumero_FallaConNumeroVacio(string numero)
    {
        Articulo.Crear(numero, "Descripción.").Error.ShouldBe(ArticuloErrors.NumeroVacio);
    }
}
```

Los puertos falsos se escriben a mano dentro de las pruebas (clases pequeñas en memoria); no se agrega una biblioteca de mocks.

### Integración

- Una clase por recurso: `tests/Sgpla.IntegrationTests/<Modulo>/<Entidad>EndpointsTests.cs`, con `SgplaApiFactory`.
- **Cada prueba crea sus propios datos con valores únicos** (`DatosUnicos`) y no afirma conteos globales. Las pruebas comparten la base y corren en paralelo, así que no dependen de que una tabla esté vacía ni se limpian entre sí. Esto reemplaza el uso de Respawn previsto en `PLAN_INICIAL.md`.
- Excepción: un catálogo fijo sí se afirma completo, porque sus valores los define la semilla y ninguna prueba los modifica.
- Se verifican las reglas de `DATABASE.md` §14 que tocan al recurso, en especial la equivalencia por mayúsculas y acentos y la inmutabilidad.

```csharp
namespace Sgpla.IntegrationTests.Catalogos;

public sealed class ArticuloEndpointsTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private static readonly Uri Ruta = new("/api/v1/catalogos/articulos", UriKind.Relative);

    private readonly SgplaApiFactory _api = new(sqlServer);

    [Fact]
    public async Task Crear_ConNumeroEquivalenteEnMinusculas_Responde409()
    {
        using var cliente = _api.CreateClient();
        var numero = $"{DatosUnicos.Numero()} BIS";
        const string descripcion = "Fundamento de prueba.";
        using var primera = await cliente.PostAsJsonAsync(Ruta, new { numero, descripcion }, TestContext.Current.CancellationToken);

        using var respuesta = await cliente.PostAsJsonAsync(
            Ruta, new { numero = numero.ToLowerInvariant(), descripcion }, TestContext.Current.CancellationToken);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();
}
```

## 13. SOLID aplicado

| Principio | Cómo lo cumple el estándar |
|---|---|
| **Responsabilidad única** | Un handler por caso de uso, una configuración por entidad y un archivo de endpoints por recurso. Cada regla vive en un solo lugar: la forma de los datos en el dominio, las reglas entre registros en el handler. Cada capa tiene una sola razón para cambiar: el negocio (Domain), el flujo (Application), la tecnología (Infrastructure) o el protocolo (Endpoints) |
| **Abierto/cerrado** | Un caso de uso nuevo es un archivo nuevo, sin tocar los existentes; un catálogo fijo nuevo reutiliza `CatalogoFijo`, su configuración y sus handlers sin modificarlos. Los comportamientos transversales (validación) son decoradores. Un módulo nuevo que referencie un artículo implementa su contrato sin modificar al dueño |
| **Sustitución de Liskov** | Toda implementación de un puerto cumple el contrato completo: sin `NotImplementedException`, sin efectos ocultos (un repositorio no guarda) y con las mismas garantías que las implementaciones falsas de las pruebas |
| **Segregación de interfaces** | Puertos de escritura por recurso, con solo los métodos que usan sus comandos; las consultas no necesitan puerto. Los contratos entre módulos exponen solo lo que el consumidor necesita |
| **Inversión de dependencias** | Application declara los puertos que necesitan sus comandos e Infrastructure los implementa. Los handlers de consulta dependen de los contratos de Application (Query y Response), nunca al revés. Los módulos dependientes implementan los contratos del módulo del que dependen. `<Modulo>Module` compone todo en un solo lugar |

## 14. Receta para un recurso nuevo

1. **Leer la especificación.** Revisar la tabla en `DATABASE.md` (columnas, restricciones, baja lógica, inmutabilidad, casos de aceptación) y las operaciones permitidas en `PLAN_INICIAL.md`.
2. **¿Es un catálogo fijo?** Seguir la subsección "Catálogos fijos" de la sección 9 y saltar al paso 7.
3. **Domain.** Crear la entidad con sus constantes, la fábrica `Crear`, los métodos de comportamiento y `<Entidad>Errors`, con el campo en cada error de validación. Escribir sus pruebas unitarias.
4. **Application.** Crear `<Entidad>Consultas.cs` con el `Response` y las Queries, el repositorio que necesiten los comandos y un archivo `<Accion><Entidad>.cs` por comando. Agregar un validator solo para lo que el dominio no valida. Si otro módulo participa, declarar o usar el contrato en `Contracts`.
5. **Infrastructure.** Crear la configuración de EF, el repositorio y `<Entidad>Consultas.cs` con los handlers de consulta. Si el recurso necesita un cambio de esquema, crear la migración en `Scripts/`.
6. **Endpoints.** Crear `<Entidad>Endpoints` con la tabla de operaciones estándar, y un request solo donde difiera del Command.
7. **Composición.** Registrar los repositorios (o `AddCatalogoFijo<T>`) en `<Modulo>Module` y mapear los endpoints en el grupo del módulo.
8. **Pruebas de integración.** Cubrir cada endpoint en su caso de éxito y en cada error documentado.
9. **Verificar.** Recorrer la lista de la sección 15.

## 15. Lista de verificación

Un módulo o recurso está terminado cuando:

- [ ] `dotnet build -c Release` compila sin advertencias.
- [ ] `dotnet format --verify-no-changes` no reporta cambios.
- [ ] `dotnet test --solution Sgpla.slnx` pasa completo, incluidas las pruebas de arquitectura.
- [ ] Toda entidad con comportamiento tiene pruebas unitarias de su fábrica y sus invariantes; todo catálogo fijo, su prueba de semilla.
- [ ] Todo endpoint tiene pruebas de integración de éxito y de cada error que documenta.
- [ ] No hay tipos públicos fuera de `<Modulo>Module` y `Application/Contracts`.
- [ ] Los endpoints aparecen documentados en `/scalar/v1` con nombre, resumen y respuestas.
- [ ] Los logs siguen la sección 11: solo `[LoggerMessage]` y sin datos personales ni secretos. La correlación por `traceId` la verifica `CorrelacionTests` y no requiere revisión manual.
- [ ] Los cambios de esquema están en una migración nueva y `baseline.sql` no se editó. `seed.sql` solo cambia para cargar un catálogo fijo.
- [ ] Si cambió una regla del estándar, este documento se actualizó en el mismo PR.
- [ ] Los commits y el PR siguen la convención del repositorio.

## 16. Lo que no se hace

| No hacer | Por qué | En su lugar |
|---|---|---|
| Usar `SgplaDbContext` en un handler de comando | Acopla las reglas de negocio a EF Core y no se pueden probar con puertos falsos | Un repositorio implementado en Infrastructure |
| Crear un puerto y un handler que solo reenvía la llamada para una consulta | Tres piezas sin lógica por cada lectura | Un handler de consulta en Infrastructure |
| Repetir en un validator una regla que ya valida el dominio | Dos fuentes de verdad que terminan divergiendo | `Error.Validation` con su campo en el dominio |
| Escribir entidad con comportamiento, handlers, respuestas y endpoints propios para un catálogo fijo que solo tiene nombre | Repetición sin reglas que proteger | `CatalogoFijo`, `AddCatalogoFijo<T>` y `MapCatalogoFijo<T>` |
| Devolver entidades de dominio desde un endpoint | Expone el modelo interno y acopla el contrato HTTP al dominio | Un `Response` de Application |
| Asignar propiedades de una entidad desde fuera | Permite estados inválidos | Métodos de comportamiento que validan |
| Lanzar excepciones por reglas de negocio | Oculta los flujos esperados y complica el mapeo HTTP | Devolver `Error` en un `Result` |
| Validar reglas de negocio en el endpoint | Se duplican o se saltan si el caso de uso se invoca desde otro lugar | Dominio, handler o validator (sección 7) |
| Llamar a `SaveChangesAsync` en un repositorio | Rompe la atomicidad del caso de uso | `IUnitOfWork` en el handler, una vez |
| Usar tipos internos de otro módulo | Acopla módulos y rompe el grafo | `Application/Contracts` del otro módulo |
| Crear un repositorio genérico `IRepository<T>` | Expone operaciones que el caso de uso no necesita | Puertos específicos por recurso |
| Declarar índices o restricciones en EF | El esquema pertenece a DbUp | Migración en `Scripts/` |
| Comparar textos en memoria (`ToLower`, `ToUpper`) para la unicidad | Ignora la intercalación de la columna | Delegar la comparación a SQL Server |
| Leer `DateTime.UtcNow` | Hace el código difícil de probar | `TimeProvider` inyectado |
| Registrar datos de una persona o el mensaje de una excepción SQL | Los logs no tienen el control de acceso de la base y el mensaje de 2627 incluye el valor duplicado | Registrar el `id` o el nombre de la restricción |
| Registrar un `Result` fallido o capturar una excepción para registrarla y relanzarla | Duplica lo que ya ve el cliente o lo que ya registra el middleware | Nada: el error ya es visible |
