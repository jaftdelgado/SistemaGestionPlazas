# Estándar de implementación de módulos

Este documento define cómo se implementa cada módulo del backend de SGPLa. Es normativo: todos los módulos se construyen de la misma forma, y una desviación se discute y se incorpora aquí antes de programarla.

Documentos relacionados:
- `PLAN_INICIAL.md`: módulos, grafo de dependencias y decisiones de arquitectura.
- `DATABASE.md`: modelo de datos, reglas de negocio y casos de aceptación.
- `sgpla-backend/README.md`: cómo compilar, probar y migrar.

El ejemplo que recorre el documento es el catálogo `GradoAcademico` del módulo Catalogos.

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
- **Application no conoce la tecnología.** Define lo que necesita mediante interfaces (puertos) y Infrastructure las implementa.
- **DbUp es dueño del esquema.** EF Core solo mapea; nunca crea ni modifica tablas.
- **Los errores de negocio son valores, no excepciones.** Los casos de uso devuelven `Result`; las excepciones quedan para fallos técnicos.
- **Las reglas se verifican solas.** Lo que se puede comprobar automáticamente está en `tests/Sgpla.ArchitectureTests` y falla en el CI.

## 2. Capas y dependencias

| Capa | Responsabilidad | Puede depender de | Nunca depende de |
|---|---|---|---|
| **Domain** | Entidades, invariantes, normalización y errores de dominio | SharedKernel | Las otras tres capas, EF Core, ASP.NET Core, FluentValidation, BuildingBlocks |
| **Application** | Casos de uso, puertos, validación de entrada y DTOs de respuesta | Domain, SharedKernel, BuildingBlocks.Application, FluentValidation, contratos de módulos permitidos | Infrastructure, Endpoints, EF Core, ASP.NET Core, BuildingBlocks.Infrastructure |
| **Infrastructure** | Implementación de puertos con EF Core, configuraciones de entidades y adaptadores externos | Application, Domain, BuildingBlocks.Infrastructure, EF Core | Endpoints |
| **Endpoints** | Rutas HTTP, modelos de request y traducción de `Result` a respuestas HTTP | Application, helpers HTTP de BuildingBlocks.Infrastructure, ASP.NET Core | Domain, Infrastructure, EF Core |
| **`<Modulo>Module.cs`** | Raíz de composición: registra dependencias y rutas | Todas las capas | — |

```
Endpoints ──► Application ──► Domain ──► SharedKernel
                  ▲
Infrastructure ───┘  (implementa los puertos de Application)
```

### Piezas compartidas

Viven en `src/BuildingBlocks` y se construyen junto con el primer módulo que las necesite. Un módulo no reimplementa nada de esta lista.

| Proyecto | Contenido |
|---|---|
| `Sgpla.SharedKernel` | `Entity` (clase base con `Id`), `IEliminable` (baja lógica), `Result`, `Result<T>`, `Error`, `ErrorType`, `ValidationError` |
| `Sgpla.BuildingBlocks.Application` | `ICommandHandler<TCommand>`, `ICommandHandler<TCommand, TResponse>`, `IQueryHandler<TQuery, TResponse>`, `IUnitOfWork`, `ICurrentUser`, `Paginacion`, `Pagina<T>`, decorador de validación |
| `Sgpla.BuildingBlocks.Infrastructure` | `SgplaDbContext`, implementación de `IUnitOfWork`, `AddPersistenciaModulo`, `AddHandlersModulo`, extensión `PaginarAsync`, helpers HTTP (`ToProblem`), manejador global de violaciones de unicidad (SQL 2601/2627 → 409), nombres de filtros de consulta |

Para el tiempo se usa `TimeProvider` de .NET, inyectado; no se llama a `DateTime.UtcNow` directamente. Para los logs se usa `ILogger<T>` de .NET; no se crea una abstracción propia (sección 11).

### `Result` y `Error`

| Tipo | Uso |
|---|---|
| `Result` | Resultado sin valor: `Result.Success()` o un `Error` (conversión implícita) |
| `Result<T>` | Resultado con valor: `T` o `Error` (conversiones implícitas). Expone `IsSuccess`, `IsFailure`, `Value` y `Error` |
| `Error(Code, Message, Type)` | Código estable (`GradoAcademico.NombreDuplicado`), mensaje en español para el usuario y tipo |
| `ErrorType` | `Validation`, `NotFound` o `Conflict`. Decide el código HTTP (sección 9) |
| `ValidationError` | `Error` de tipo `Validation` con los errores por campo que producen los validators |

## 3. Estructura de carpetas

Primero por capa y, dentro de cada capa, por recurso. Dentro de Application, una carpeta por caso de uso.

```
src/Modules/Catalogos/Sgpla.Modules.Catalogos/
  Domain/
    GradosAcademicos/
      GradoAcademico.cs
      GradoAcademicoErrors.cs
  Application/
    Contracts/                          API pública para otros módulos (sección 5)
      IReferenciasGradoAcademico.cs
    GradosAcademicos/
      IGradoAcademicoRepository.cs      puerto de escritura
      IGradoAcademicoQueries.cs         puerto de lectura
      GradoAcademicoResponse.cs
      Crear/
        CrearGradoAcademicoCommand.cs
        CrearGradoAcademicoHandler.cs
        CrearGradoAcademicoValidator.cs
      CorregirNombre/
      Obtener/
      Listar/
  Infrastructure/
    GradosAcademicos/
      GradoAcademicoConfiguration.cs
      GradoAcademicoRepository.cs
      GradoAcademicoQueries.cs
  Endpoints/
    GradosAcademicos/
      GradoAcademicoEndpoints.cs
      CrearGradoAcademicoRequest.cs
  CatalogosModule.cs
```

Las pruebas replican la misma organización: `tests/Sgpla.UnitTests/<Modulo>/<Recurso>/` y `tests/Sgpla.IntegrationTests/<Modulo>/`.

## 4. Convenciones de nombres

**Idioma:**
- Lo que nombra el negocio va en español y sin acentos: `GradoAcademico`, `CorregirNombre`, `ExisteNombreAsync`.
- Los sufijos de patrón y las abstracciones compartidas de BuildingBlocks van en inglés: `Handler`, `HandleAsync`, `Result`, `IsSuccess`, `SaveChangesAsync`.
- Las propiedades que mapean columnas conservan el nombre de la columna en PascalCase (`FechaEliminacion` ↔ `fecha_eliminacion`).
- El contrato HTTP (rutas, parámetros y campos JSON) va en español, porque lo consume el frontend.

| Elemento | Patrón | Ejemplo |
|---|---|---|
| Entidad | Sustantivo singular | `GradoAcademico` |
| Errores de dominio | `<Entidad>Errors` | `GradoAcademicoErrors` |
| Carpeta de recurso | Plural | `GradosAcademicos` |
| Carpeta de caso de uso | Verbo o acción | `Crear`, `CorregirNombre`, `DarDeBaja`, `Restaurar`, `Obtener`, `Listar` |
| Comando | `<Accion><Entidad>Command` | `CrearGradoAcademicoCommand` |
| Consulta | `<Accion><Entidad>Query` | `ListarGradosAcademicosQuery` |
| Handler | `<Accion><Entidad>Handler` | `CrearGradoAcademicoHandler` |
| Validator | `<Accion><Entidad>Validator` | `CrearGradoAcademicoValidator` |
| Puerto de escritura | `I<Entidad>Repository` | `IGradoAcademicoRepository` |
| Puerto de lectura | `I<Entidad>Queries` | `IGradoAcademicoQueries` |
| Respuesta | `<Entidad>Response` | `GradoAcademicoResponse` |
| Configuración EF | `<Entidad>Configuration` | `GradoAcademicoConfiguration` |
| Endpoints | `<Entidad>Endpoints` | `GradoAcademicoEndpoints` |
| Request HTTP | `<Accion><Entidad>Request` | `CrearGradoAcademicoRequest` |
| Ruta | `/api/v1/<modulo>/<recurso-en-plural>` en kebab-case | `/api/v1/catalogos/grados-academicos` |
| Código de error | `<Entidad>.<Motivo>` | `GradoAcademico.NombreDuplicado` |
| Prueba | `Metodo_Escenario_Resultado` | `Crear_ConNombreRepetido_Responde409` |

Las pruebas de arquitectura exigen que las clases terminadas en `Handler` y `Validator` estén en Application; las terminadas en `Configuration`, `Repository` y `Queries`, en Infrastructure, y las terminadas en `Endpoints`, en Endpoints.

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
| Un módulo necesita saber algo de los módulos que dependen de él | El módulo que pregunta, en su `Contracts` | Cada módulo dependiente (inversión de dependencias) | Catalogos pregunta si un grado tiene referencias; responden el propio Catalogos (tratamientos), Docentes y Aspirantes |

En el segundo caso, el consumidor recibe `IEnumerable<IReferenciasGradoAcademico>` y consulta todas las implementaciones registradas. Así puede aparecer un módulo nuevo que referencie grados sin modificar Catalogos.

Las pruebas de arquitectura exigen estas reglas: ningún tipo público fuera de las dos excepciones y ninguna dependencia hacia otro módulo fuera de su `Contracts`.

## 6. Domain

Reglas:
- Las entidades heredan de `Entity`. Si tienen baja lógica, implementan `IEliminable`.
- Tienen un constructor privado sin parámetros (lo usa EF Core) y propiedades con `private set`.
- Se crean solo con una fábrica estática `Crear(...)`, que normaliza (recorta los textos y pone en mayúsculas las claves y los códigos) y devuelve `Result<T>`.
- El comportamiento se expresa con métodos que nombran la intención (`CorregirNombre`, `DarDeBaja`, `Restaurar`). Nadie asigna propiedades desde fuera.
- Las longitudes y los formatos de `DATABASE.md` son constantes públicas de la entidad. Los validators y las configuraciones de EF reutilizan esas constantes.
- Los errores se declaran una sola vez en `<Entidad>Errors`.
- Domain no consulta la base. Las reglas que dependen de otros registros, como la unicidad o las referencias, las orquesta el handler con puertos.
- Las fechas llegan como parámetro (`DarDeBaja(DateTime utc)`); la entidad no lee el reloj.

```csharp
namespace Sgpla.Modules.Catalogos.Domain.GradosAcademicos;

internal sealed class GradoAcademico : Entity
{
    public const int LongitudMaximaNombre = 150;

    private GradoAcademico()
    {
    }

    public string Nombre { get; private set; } = string.Empty;

    public static Result<GradoAcademico> Crear(string nombre)
    {
        var normalizado = NormalizarNombre(nombre);
        if (normalizado.IsFailure)
        {
            return normalizado.Error;
        }

        return new GradoAcademico { Nombre = normalizado.Value };
    }

    public Result CorregirNombre(string nombre)
    {
        var normalizado = NormalizarNombre(nombre);
        if (normalizado.IsFailure)
        {
            return normalizado.Error;
        }

        Nombre = normalizado.Value;
        return Result.Success();
    }

    private static Result<string> NormalizarNombre(string? nombre)
    {
        var recortado = nombre?.Trim() ?? string.Empty;

        if (recortado.Length == 0)
        {
            return GradoAcademicoErrors.NombreVacio;
        }

        if (recortado.Length > LongitudMaximaNombre)
        {
            return GradoAcademicoErrors.NombreDemasiadoLargo;
        }

        return recortado;
    }
}
```

```csharp
namespace Sgpla.Modules.Catalogos.Domain.GradosAcademicos;

internal static class GradoAcademicoErrors
{
    public static readonly Error NombreVacio = Error.Validation(
        "GradoAcademico.NombreVacio", "El nombre es obligatorio.");

    public static readonly Error NombreDemasiadoLargo = Error.Validation(
        "GradoAcademico.NombreDemasiadoLargo",
        $"El nombre admite hasta {GradoAcademico.LongitudMaximaNombre} caracteres.");

    public static readonly Error NombreDuplicado = Error.Conflict(
        "GradoAcademico.NombreDuplicado", "Ya existe un grado académico con ese nombre.");

    public static readonly Error NombreInmutable = Error.Conflict(
        "GradoAcademico.NombreInmutable", "El nombre ya no puede corregirse porque el grado académico está en uso.");

    public static Error NoEncontrado(int id) => Error.NotFound(
        "GradoAcademico.NoEncontrado", $"No existe el grado académico {id}.");
}
```

**Baja lógica.** Una entidad `IEliminable`:
- `DarDeBaja(DateTime utc)` es idempotente: si ya tiene fecha, no la reemplaza.
- `Restaurar()` limpia la fecha.
- Las reglas de cascada y de padres activos (`DATABASE.md` §9) las orquesta el handler, porque involucran a otras entidades.

## 7. Application

### Casos de uso

Cada caso de uso tiene su carpeta con tres piezas:

| Pieza | Forma | Responsabilidad |
|---|---|---|
| Command o Query | `internal sealed record` | Datos de entrada, sin lógica |
| Validator | `AbstractValidator<T>` de FluentValidation | Forma de la entrada: obligatoriedad, longitudes, formatos y rangos |
| Handler | Implementa `ICommandHandler` o `IQueryHandler` | Orquesta: reglas que dependen de datos, llamada al dominio, persistencia y respuesta |

Los comandos modifican el estado; las consultas no. Un handler nunca llama a otro handler; si dos casos de uso comparten lógica, esa lógica va en el dominio o en un servicio de Application con su propia interfaz.

El decorador de validación ejecuta los validators antes del handler. Si hay errores, devuelve un `ValidationError` sin llamar al handler, así que los handlers no validan la forma de la entrada.

### Puertos

Se separan la escritura y la lectura (ISP):

| Puerto | Devuelve | Reglas |
|---|---|---|
| `I<Entidad>Repository` | Entidades de dominio | Métodos que el caso de uso necesita (`ObtenerPorIdAsync`, `ExisteNombreAsync`, `Agregar`). Nunca guarda: eso lo hace `IUnitOfWork` |
| `I<Entidad>Queries` | `Response` y `Pagina<T>` | Solo lectura, proyectada y sin tracking |

Un puerto se declara cuando un caso de uso lo necesita; no se crean repositorios genéricos ni métodos "por si acaso".

```csharp
namespace Sgpla.Modules.Catalogos.Application.GradosAcademicos;

internal interface IGradoAcademicoRepository
{
    Task<GradoAcademico?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken);

    Task<bool> ExisteNombreAsync(string nombre, int? excluirId, CancellationToken cancellationToken);

    void Agregar(GradoAcademico gradoAcademico);
}

internal interface IGradoAcademicoQueries
{
    Task<GradoAcademicoResponse?> ObtenerAsync(int id, CancellationToken cancellationToken);

    Task<Pagina<GradoAcademicoResponse>> ListarAsync(Paginacion paginacion, CancellationToken cancellationToken);
}
```

### Respuesta

```csharp
internal sealed record GradoAcademicoResponse(int Id, string Nombre)
{
    public static GradoAcademicoResponse Desde(GradoAcademico gradoAcademico) =>
        new(gradoAcademico.Id, gradoAcademico.Nombre);
}
```

Una entidad de dominio nunca sale de Application: los endpoints solo ven `Response`.

### Ejemplo: crear

```csharp
namespace Sgpla.Modules.Catalogos.Application.GradosAcademicos.Crear;

internal sealed record CrearGradoAcademicoCommand(string Nombre);

internal sealed class CrearGradoAcademicoValidator : AbstractValidator<CrearGradoAcademicoCommand>
{
    public CrearGradoAcademicoValidator()
    {
        RuleFor(c => c.Nombre).NotEmpty().MaximumLength(GradoAcademico.LongitudMaximaNombre);
    }
}

internal sealed class CrearGradoAcademicoHandler(
    IGradoAcademicoRepository repositorio,
    IUnitOfWork unidadDeTrabajo) : ICommandHandler<CrearGradoAcademicoCommand, GradoAcademicoResponse>
{
    public async Task<Result<GradoAcademicoResponse>> HandleAsync(
        CrearGradoAcademicoCommand command,
        CancellationToken cancellationToken)
    {
        var creado = GradoAcademico.Crear(command.Nombre);
        if (creado.IsFailure)
        {
            return creado.Error;
        }

        if (await repositorio.ExisteNombreAsync(creado.Value.Nombre, excluirId: null, cancellationToken))
        {
            return GradoAcademicoErrors.NombreDuplicado;
        }

        repositorio.Agregar(creado.Value);
        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);

        return GradoAcademicoResponse.Desde(creado.Value);
    }
}
```

### Ejemplo: corregir el nombre con referencias de otros módulos

`DATABASE.md` §15.2 permite corregir el nombre mientras el valor no tenga referencias. Las referencias pueden estar en otros módulos, así que el handler consulta el contrato `IReferenciasGradoAcademico` (sección 5).

```csharp
namespace Sgpla.Modules.Catalogos.Application.Contracts;

/// <summary>Lo implementa cada módulo que guarda referencias a un grado académico.</summary>
public interface IReferenciasGradoAcademico
{
    Task<bool> TieneReferenciasAsync(int gradoAcademicoId, CancellationToken cancellationToken);
}
```

```csharp
internal sealed class CorregirNombreGradoAcademicoHandler(
    IGradoAcademicoRepository repositorio,
    IEnumerable<IReferenciasGradoAcademico> referencias,
    IUnitOfWork unidadDeTrabajo) : ICommandHandler<CorregirNombreGradoAcademicoCommand>
{
    public async Task<Result> HandleAsync(
        CorregirNombreGradoAcademicoCommand command,
        CancellationToken cancellationToken)
    {
        var gradoAcademico = await repositorio.ObtenerPorIdAsync(command.Id, cancellationToken);
        if (gradoAcademico is null)
        {
            return GradoAcademicoErrors.NoEncontrado(command.Id);
        }

        foreach (var referencia in referencias)
        {
            if (await referencia.TieneReferenciasAsync(gradoAcademico.Id, cancellationToken))
            {
                return GradoAcademicoErrors.NombreInmutable;
            }
        }

        var corregido = gradoAcademico.CorregirNombre(command.Nombre);
        if (corregido.IsFailure)
        {
            return corregido;
        }

        if (await repositorio.ExisteNombreAsync(gradoAcademico.Nombre, gradoAcademico.Id, cancellationToken))
        {
            return GradoAcademicoErrors.NombreDuplicado;
        }

        await unidadDeTrabajo.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
```

### Reglas del handler

- Recibe sus dependencias por constructor primario, siempre como interfaces.
- Llama a `SaveChangesAsync` una sola vez, al final. Todo lo que modifica la operación se guarda de forma atómica.
- Si la operación afecta a varias filas con una misma fecha (baja en cascada), obtiene el instante una vez con `TimeProvider`, truncado a segundos por `datetime2(0)`, y lo reutiliza.
- No lanza excepciones por reglas de negocio: devuelve un `Error`.
- Propaga el `CancellationToken` a toda llamada asíncrona.

## 8. Infrastructure

### Configuración de entidades

Una `IEntityTypeConfiguration<T>` por entidad. `SgplaDbContext` las descubre en el ensamblado del módulo.

```csharp
namespace Sgpla.Modules.Catalogos.Infrastructure.GradosAcademicos;

internal sealed class GradoAcademicoConfiguration : IEntityTypeConfiguration<GradoAcademico>
{
    public void Configure(EntityTypeBuilder<GradoAcademico> builder)
    {
        builder.ToTable("grado_academico", "academico");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Nombre).HasMaxLength(GradoAcademico.LongitudMaximaNombre);
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

### Repositorios y consultas

```csharp
internal sealed class GradoAcademicoRepository(SgplaDbContext contexto) : IGradoAcademicoRepository
{
    public Task<GradoAcademico?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
        contexto.Set<GradoAcademico>().FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public Task<bool> ExisteNombreAsync(string nombre, int? excluirId, CancellationToken cancellationToken) =>
        contexto.Set<GradoAcademico>()
            .AnyAsync(g => g.Nombre == nombre && (excluirId == null || g.Id != excluirId), cancellationToken);

    public void Agregar(GradoAcademico gradoAcademico) => contexto.Set<GradoAcademico>().Add(gradoAcademico);
}

internal sealed class GradoAcademicoQueries(SgplaDbContext contexto) : IGradoAcademicoQueries
{
    public Task<GradoAcademicoResponse?> ObtenerAsync(int id, CancellationToken cancellationToken) =>
        contexto.Set<GradoAcademico>()
            .AsNoTracking()
            .Where(g => g.Id == id)
            .Select(g => new GradoAcademicoResponse(g.Id, g.Nombre))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<Pagina<GradoAcademicoResponse>> ListarAsync(Paginacion paginacion, CancellationToken cancellationToken) =>
        contexto.Set<GradoAcademico>()
            .AsNoTracking()
            .OrderBy(g => g.Nombre)
            .ThenBy(g => g.Id)
            .Select(g => new GradoAcademicoResponse(g.Id, g.Nombre))
            .PaginarAsync(paginacion, cancellationToken);
}
```

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
namespace Sgpla.Modules.Catalogos.Endpoints.GradosAcademicos;

internal static class GradoAcademicoEndpoints
{
    private const string NombreRutaObtener = "ObtenerGradoAcademico";

    public static RouteGroupBuilder MapGradoAcademicoEndpoints(this RouteGroupBuilder modulo)
    {
        var grupo = modulo.MapGroup("/grados-academicos").WithTags("Grados académicos");

        grupo.MapGet("/", Listar).WithName("ListarGradosAcademicos").WithSummary("Lista los grados académicos.");
        grupo.MapGet("/{id:int}", Obtener).WithName(NombreRutaObtener).WithSummary("Obtiene un grado académico.");
        grupo.MapPost("/", Crear).WithName("CrearGradoAcademico").WithSummary("Registra un grado académico.");
        grupo.MapPut("/{id:int}", CorregirNombre).WithName("CorregirNombreGradoAcademico")
            .WithSummary("Corrige el nombre mientras el grado no esté en uso.");

        return modulo;
    }

    private static async Task<Results<CreatedAtRoute<GradoAcademicoResponse>, ProblemHttpResult>> Crear(
        CrearGradoAcademicoRequest request,
        ICommandHandler<CrearGradoAcademicoCommand, GradoAcademicoResponse> handler,
        CancellationToken cancellationToken)
    {
        var resultado = await handler.HandleAsync(new CrearGradoAcademicoCommand(request.Nombre), cancellationToken);

        return resultado.IsSuccess
            ? TypedResults.CreatedAtRoute(resultado.Value, NombreRutaObtener, new { id = resultado.Value.Id })
            : resultado.Error.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> CorregirNombre(
        int id,
        CorregirNombreGradoAcademicoRequest request,
        ICommandHandler<CorregirNombreGradoAcademicoCommand> handler,
        CancellationToken cancellationToken)
    {
        var resultado = await handler.HandleAsync(
            new CorregirNombreGradoAcademicoCommand(id, request.Nombre), cancellationToken);

        return resultado.IsSuccess ? TypedResults.NoContent() : resultado.Error.ToProblem();
    }

    // Listar y Obtener siguen la misma forma con IQueryHandler.
}
```

Reglas:
- **Un request por operación** (`CrearGradoAcademicoRequest`), aunque se parezca al Command. El request es el contrato HTTP; el Command es el del caso de uso, y pueden evolucionar por separado (por ejemplo, el `id` viene de la ruta).
- **Siempre `TypedResults` y tipos de retorno `Results<...>`,** para que OpenAPI documente cada respuesta. Todo endpoint lleva `WithName` y `WithSummary`.
- **Rutas con restricción de tipo:** `{id:int}`.
- **Autorización:** se declara en el grupo del módulo, o del recurso si difiere, con `RequireAuthorization(<política>)`. Las políticas (`Superusuario`, `Dgaa`, `EntidadAcademica`) llegan con el módulo Usuarios. Hasta entonces, el grupo lleva un comentario en ese punto, sin otra solución provisional.

### Operaciones estándar

| Operación | Método y ruta | Éxito | Errores |
|---|---|---|---|
| Listar | `GET /` con `pagina` (desde 1), `tamanoPagina` (1 a 100, 20 por omisión) e `incluirEliminados` si hay baja lógica | 200 con `Pagina<T>` | 400 |
| Obtener | `GET /{id}` | 200 | 404 |
| Crear | `POST /` | 201 con `Location` y el recurso | 400, 409 |
| Editar | `PUT /{id}` con los campos editables | 204 | 400, 404, 409 |
| Dar de baja | `DELETE /{id}` (baja lógica, idempotente) | 204 | 404, 409 |
| Restaurar | `POST /{id}/restaurar` | 204 | 404, 409 |
| Acción de estado | `POST /{id}/<accion>` (`enviar`, `avalar`, `cancelar`...) | 204 o 200 | 404, 409 |

Un recurso expone solo las operaciones que `PLAN_INICIAL.md` y `DATABASE.md` le permiten. Por ejemplo, los catálogos permanentes no tienen baja y `municipio` es de solo lectura.

`Pagina<T>` se serializa como:

```json
{ "elementos": [], "pagina": 1, "tamanoPagina": 20, "total": 0 }
```

### Errores

Todos los errores se responden como ProblemDetails, con la extensión `codigo` igual al `Error.Code`.

| `ErrorType` | HTTP | Cuándo |
|---|---|---|
| `Validation` | 400 | Entrada mal formada; incluye `errors` por campo |
| `NotFound` | 404 | El recurso no existe (o está dado de baja y la operación no admite bajas) |
| `Conflict` | 409 | Duplicados, valores inmutables, transiciones de estado inválidas, padres inactivos |

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflicto",
  "status": 409,
  "detail": "Ya existe un grado académico con ese nombre.",
  "codigo": "GradoAcademico.NombreDuplicado"
}
```

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
        services.AddHandlersModulo(ensamblado);   // handlers, validators y decorador de validación

        services.AddScoped<IGradoAcademicoRepository, GradoAcademicoRepository>();
        services.AddScoped<IGradoAcademicoQueries, GradoAcademicoQueries>();
        services.AddScoped<IReferenciasGradoAcademico, ReferenciasGradoAcademicoEnTratamientos>();

        return services;
    }

    public static IEndpointRouteBuilder MapCatalogosEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup(Ruta);
        // Autorización: grupo.RequireAuthorization(...) cuando exista JWT (módulo Usuarios).

        grupo.MapGradoAcademicoEndpoints();

        return endpoints;
    }
}
```

Reglas:
- Los handlers y validators se registran por escaneo con `AddHandlersModulo`.
- Los puertos se registran uno por uno, con ciclo de vida `Scoped`, para que la composición sea visible y revisable.
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
- La prueba de integración `CorrelacionTests` verifica que el `traceId` de una respuesta de error aparece en los logs de esa misma petición. Hoy provoca un 404 de ruta inexistente (lo responde `UseStatusCodePages`); cuando exista el primer endpoint que use `ToProblem`, la prueba debe provocar el error en ese endpoint, y con eso queda cubierto para todos los módulos, porque el helper es compartido.
- `EnableSensitiveDataLogging` de EF Core no se activa en ningún entorno. Una prueba de integración lo verifica.

## 12. Pruebas

| Proyecto | Qué se prueba | Obligatorio |
|---|---|---|
| `Sgpla.UnitTests` | Fábricas, normalización e invariantes de cada entidad. Ramas de reglas de un handler con puertos falsos, cuando el handler tiene lógica propia | Sí, para toda entidad |
| `Sgpla.IntegrationTests` | Cada endpoint de punta a punta contra SQL Server en contenedor: éxito y cada error que documenta (400, 404, 409) | Sí, para todo endpoint |
| `Sgpla.ArchitectureTests` | Las reglas de este documento | Automático; no se edita salvo para agregar módulos o reglas |

### Unitarias

```csharp
namespace Sgpla.UnitTests.Catalogos.GradosAcademicos;

public sealed class GradoAcademicoTests
{
    [Fact]
    public void Crear_ConEspaciosExteriores_RecortaElNombre()
    {
        var resultado = GradoAcademico.Crear("  Doctorado  ");

        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.Nombre.ShouldBe("Doctorado");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_SinNombre_FallaConNombreVacio(string nombre)
    {
        GradoAcademico.Crear(nombre).Error.ShouldBe(GradoAcademicoErrors.NombreVacio);
    }
}
```

Los puertos falsos se escriben a mano dentro de las pruebas (clases pequeñas en memoria); no se agrega una biblioteca de mocks.

### Integración

- Una clase por recurso: `tests/Sgpla.IntegrationTests/<Modulo>/<Entidad>EndpointsTests.cs`, con `SgplaApiFactory`.
- **Cada prueba crea sus propios datos con valores únicos** (sufijo aleatorio) y no afirma conteos globales. Las pruebas comparten la base y corren en paralelo, así que no dependen de que una tabla esté vacía ni se limpian entre sí. Esto reemplaza el uso de Respawn previsto en `PLAN_INICIAL.md`.
- Se verifican las reglas de `DATABASE.md` §14 que tocan al recurso, en especial la equivalencia por mayúsculas y acentos y la inmutabilidad.

```csharp
namespace Sgpla.IntegrationTests.Catalogos;

public sealed class GradoAcademicoEndpointsTests(SqlServerFixture sqlServer) : IAsyncDisposable
{
    private static readonly Uri Ruta = new("/api/v1/catalogos/grados-academicos", UriKind.Relative);

    private readonly SgplaApiFactory _api = new(sqlServer);

    [Fact]
    public async Task Crear_ConNombreEquivalenteEnMayusculas_Responde409()
    {
        using var cliente = _api.CreateClient();
        var nombre = DatosUnicos.Nombre("Maestría");
        using var primera = await cliente.PostAsJsonAsync(Ruta, new { nombre }, TestContext.Current.CancellationToken);

        using var respuesta = await cliente.PostAsJsonAsync(
            Ruta, new { nombre = nombre.ToUpperInvariant() }, TestContext.Current.CancellationToken);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    public ValueTask DisposeAsync() => _api.DisposeAsync();
}
```

## 13. SOLID aplicado

| Principio | Cómo lo cumple el estándar |
|---|---|
| **Responsabilidad única** | Un handler por caso de uso, un validator por entrada, una configuración por entidad y un archivo de endpoints por recurso. Cada capa tiene una sola razón para cambiar: el negocio (Domain), el flujo (Application), la tecnología (Infrastructure) o el protocolo (Endpoints) |
| **Abierto/cerrado** | Un caso de uso nuevo es una carpeta nueva, sin tocar las existentes. Los comportamientos transversales (validación) son decoradores. Un módulo nuevo que referencie un catálogo implementa su contrato sin modificar al dueño |
| **Sustitución de Liskov** | Toda implementación de un puerto cumple el contrato completo: sin `NotImplementedException`, sin efectos ocultos (un repositorio no guarda) y con las mismas garantías que las implementaciones falsas de las pruebas |
| **Segregación de interfaces** | Puertos por recurso y separados en escritura (`Repository`) y lectura (`Queries`). Los contratos entre módulos exponen solo lo que el consumidor necesita |
| **Inversión de dependencias** | Application declara los puertos que necesita e Infrastructure los implementa. Los módulos dependientes implementan los contratos del módulo del que dependen. `<Modulo>Module` compone todo en un solo lugar |

## 14. Receta para un recurso nuevo

1. **Leer la especificación.** Revisar la tabla en `DATABASE.md` (columnas, restricciones, baja lógica, inmutabilidad, casos de aceptación) y las operaciones permitidas en `PLAN_INICIAL.md`.
2. **Domain.** Crear la entidad con sus constantes, la fábrica `Crear`, los métodos de comportamiento y `<Entidad>Errors`. Escribir sus pruebas unitarias.
3. **Application.** Crear los puertos, el `Response` y, por cada operación, la carpeta con Command o Query, Validator y Handler. Si otro módulo participa, declarar o usar el contrato en `Contracts`.
4. **Infrastructure.** Crear la configuración de EF, el repositorio y las consultas. Si el recurso necesita un cambio de esquema, crear la migración en `Scripts/`.
5. **Endpoints.** Crear los requests y `<Entidad>Endpoints` con la tabla de operaciones estándar.
6. **Composición.** Registrar los puertos en `<Modulo>Module` y mapear los endpoints en el grupo del módulo.
7. **Pruebas de integración.** Cubrir cada endpoint en su caso de éxito y en cada error documentado.
8. **Verificar.** Recorrer la lista de la sección 15.

## 15. Lista de verificación

Un módulo o recurso está terminado cuando:

- [ ] `dotnet build -c Release` compila sin advertencias.
- [ ] `dotnet format --verify-no-changes` no reporta cambios.
- [ ] `dotnet test --solution Sgpla.slnx` pasa completo, incluidas las pruebas de arquitectura.
- [ ] Toda entidad tiene pruebas unitarias de su fábrica y sus invariantes.
- [ ] Todo endpoint tiene pruebas de integración de éxito y de cada error que documenta.
- [ ] No hay tipos públicos fuera de `<Modulo>Module` y `Application/Contracts`.
- [ ] Los endpoints aparecen documentados en `/scalar/v1` con nombre, resumen y respuestas.
- [ ] Los logs siguen la sección 11: solo `[LoggerMessage]` y sin datos personales ni secretos. La correlación por `traceId` la verifica `CorrelacionTests` y no requiere revisión manual.
- [ ] Los cambios de esquema están en una migración nueva; no se editaron `baseline.sql` ni `seed.sql`.
- [ ] Si cambió una regla del estándar, este documento se actualizó en el mismo PR.
- [ ] Los commits y el PR siguen la convención del repositorio.

## 16. Lo que no se hace

| No hacer | Por qué | En su lugar |
|---|---|---|
| Usar `SgplaDbContext` en un handler | Acopla Application a EF Core y rompe la inversión de dependencias | Un puerto implementado en Infrastructure |
| Devolver entidades de dominio desde un endpoint | Expone el modelo interno y acopla el contrato HTTP al dominio | Un `Response` de Application |
| Asignar propiedades de una entidad desde fuera | Permite estados inválidos | Métodos de comportamiento que validan |
| Lanzar excepciones por reglas de negocio | Oculta los flujos esperados y complica el mapeo HTTP | Devolver `Error` en un `Result` |
| Validar reglas de negocio en el endpoint | Se duplican o se saltan si el caso de uso se invoca desde otro lugar | Validator o dominio |
| Llamar a `SaveChangesAsync` en un repositorio | Rompe la atomicidad del caso de uso | `IUnitOfWork` en el handler, una vez |
| Usar tipos internos de otro módulo | Acopla módulos y rompe el grafo | `Application/Contracts` del otro módulo |
| Crear un repositorio genérico `IRepository<T>` | Expone operaciones que el caso de uso no necesita | Puertos específicos por recurso |
| Declarar índices o restricciones en EF | El esquema pertenece a DbUp | Migración en `Scripts/` |
| Comparar textos en memoria (`ToLower`, `ToUpper`) para la unicidad | Ignora la intercalación de la columna | Delegar la comparación a SQL Server |
| Leer `DateTime.UtcNow` | Hace el código difícil de probar | `TimeProvider` inyectado |
| Registrar datos de una persona o el mensaje de una excepción SQL | Los logs no tienen el control de acceso de la base y el mensaje de 2627 incluye el valor duplicado | Registrar el `id` o el nombre de la restricción |
| Registrar un `Result` fallido o capturar una excepción para registrarla y relanzarla | Duplica lo que ya ve el cliente o lo que ya registra el middleware | Nada: el error ya es visible |
