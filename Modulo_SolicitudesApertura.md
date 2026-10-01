# Módulo SolicitudesApertura

Especificación del módulo SolicitudesApertura: la petición formal de una Entidad Académica a su Área Académica para abrir una sección de una experiencia educativa en el periodo siguiente, con su oficio de respaldo en PDF. Depende solo de OfertaEducativa. Es normativa: quien lo implemente no decide nada que no esté escrito aquí. Si algo falta o se contradice, se detiene la implementación y se aclara en este documento antes de programar.

Documentos relacionados: `PLAN_INICIAL.md` (tabla de módulos y grafo), `DATABASE.md` (§16, más §2, §9.1 y §17), `ESTANDAR_MODULOS.md` y `DECISIONES.md` (OFE-D4, OFE-D9, OFE-D10, OFE-D11 y OFE-D15). Referencias de implementación: OfertaEducativa (consultas paginadas con filtros y ámbito, contratos entre módulos, `IAmbitoOfertaEducativa`) y Catalogos (`Articulo`, comandos y errores de dominio).

## Contenido

1. [Alcance](#1-alcance)
2. [Decisiones y desviaciones](#2-decisiones-y-desviaciones)
3. [Piezas compartidas](#3-piezas-compartidas)
4. [Normalización y validación comunes](#4-normalización-y-validación-comunes)
5. [Autorización y ámbito](#5-autorización-y-ámbito)
6. [Solicitud de apertura](#6-solicitud-de-apertura)
7. [Oficio de respaldo](#7-oficio-de-respaldo)
8. [Periodos configurados](#8-periodos-configurados)
9. [Contratos entre módulos](#9-contratos-entre-módulos)
10. [Auditoría](#10-auditoría)
11. [Composición del módulo](#11-composición-del-módulo)
12. [Entregas (PR)](#12-entregas-pr)
13. [Criterios de terminado](#13-criterios-de-terminado)

## 1. Alcance

| Recurso | Tabla | Operaciones | Quién escribe | PR |
|---|---|---|---|---|
| Solicitud de apertura | `academico.solicitud_apertura` | Listar paginado con filtros, obtener y crear | Entidad Académica | 1 |
| Solicitud de apertura | `academico.solicitud_apertura` | Modificar (en PENDIENTE), aceptar, rechazar y cancelar | Entidad Académica (modificar y cancelar), DGAA (aceptar y rechazar) | 2 |
| Oficio de respaldo | `academico.archivo_solicitud_apertura` | Se carga al crear y se reemplaza al modificar; descargar | Entidad Académica | 1 y 2 |
| Periodos configurados | — (configuración) | Consultar la pareja actual y siguiente | Nadie | 1 |

Incluye además:
- el almacenamiento de archivos compartido (`IAlmacenamientoArchivos`) con su implementación en el sistema de archivos local (PR 1);
- los contratos nuevos de OfertaEducativa `IExperienciasEducativas` e `IPeriodosEscolares` (PR 1);
- las implementaciones de `IReferenciasExperienciaEducativa` (resuelve P7) e `IReferenciasPeriodoEscolar` (parte de P8) (PR 1);
- las políticas `EntidadAcademica` y `DgaaOEntidadAcademica` (PR 1);
- el control de concurrencia optimista con `rowversion` y su manejador global (columna en el PR 1; manejador y uso en el PR 2).

Fuera de alcance:
- la vinculación de una solicitud aceptada con una programación académica, que se elimina del sistema (D1);
- el proveedor definitivo del almacenamiento de binarios, que sigue pendiente (`DATABASE.md` §17);
- eliminación de solicitudes: no tienen baja lógica ni física (`DATABASE.md` §16.2);
- eventos de auditoría de las transiciones (D19).

## 2. Decisiones y desviaciones

| # | Decisión | Desviación respecto a |
|---|---|---|
| D1 | **No hay vinculación con la programación académica.** Se quitan de `solicitud_apertura` las columnas `programacion_academica_id`, `vinculada_en` y `vinculada_por_usuario_id`, con sus FK, CHECK e índices, **editando `baseline.sql`** como excepción documentada (igual que INS-D1 y OFE-D5). No existe el comando vincular ni el contrato `IReferenciasProgramacionAcademica` | `DATABASE.md` §2, §16.2, §16.4, §16.5 y §16.6. `DATABASE_DIAGRAM.md`. `PLAN_INICIAL.md` (tabla de módulos). `AGENTS.md` regla 13 y `ESTANDAR_MODULOS.md` §15 (el baseline no se edita) |
| D2 | **Concurrencia optimista con `rowversion`.** `solicitud_apertura` agrega la columna `version rowversion NOT NULL`, **editando `baseline.sql`** con la misma excepción de D1, mapeada con `IsRowVersion()`. Si dos operaciones chocan (una edición mientras otro acepta), la segunda responde 409 `Persistencia.ModificacionConcurrente`, que traduce un manejador global de `DbUpdateConcurrencyException` (sección 3) | `DATABASE.md` §16.2 (columna nueva). `AGENTS.md` regla 13 y `ESTANDAR_MODULOS.md` §15. `ESTANDAR_MODULOS.md` §2 (pieza compartida nueva) |
| D3 | **Almacenamiento de archivos compartido.** `IAlmacenamientoArchivos` se declara en `BuildingBlocks.Application` (lo usan los handlers de comando) y se implementa en `BuildingBlocks.Infrastructure` sobre el sistema de archivos local, con `AlmacenamientoOptions` (ruta base y tamaño máximo) validadas al arrancar. En `docker compose` los archivos viven en un volumen propio. El proveedor definitivo sigue pendiente y se cambia sin tocar los módulos | `PLAN_INICIAL.md` (la interfaz en `BuildingBlocks.Infrastructure`). `ESTANDAR_MODULOS.md` §2 (piezas compartidas nuevas) |
| D4 | **Carga con compensación.** Primero se guarda el binario con una clave nueva y después se confirma la base. Si `SaveChangesAsync` falla, se elimina el binario recién guardado y la excepción sigue su curso. Al reemplazar un oficio, el binario anterior se elimina **después** de confirmar. Un binario que no se puede eliminar queda huérfano y solo se registra en el log (D19) | Resuelve para este módulo el "proceso seguro para confirmar o compensar cargas" de `DATABASE.md` §17 |
| D5 | **Tamaño máximo configurable**, 10 MiB (10 485 760 bytes) por omisión: `Almacenamiento:TamanoMaximoBytes`, entre 1 y 26 214 400 (25 MiB) para que el límite de 30 MB del cuerpo en Kestrel nunca corte antes el multipart. Un oficio mayor responde 400 en el campo `oficio`. El esquema sigue sin máximo | Aclara `DATABASE.md` §16.3 ("sin máximo fijo") |
| D6 | **El oficio debe ser un PDF de verdad:** la parte del multipart declara `application/pdf` y su contenido empieza con los bytes `%PDF-`. Si no, 400 en el campo `oficio` | — |
| D7 | **Carga multipart.** `POST /solicitudes` recibe `multipart/form-data` con los datos y el oficio en una sola petición. `PUT /solicitudes/{id}` (PR 2) es multipart con el oficio opcional: si no llega, se conserva. `GET /solicitudes/{id}/oficio` descarga el PDF. Las rutas multipart desactivan la validación antiforgery, porque la API se autentica con token bearer y no con cookies | `ESTANDAR_MODULOS.md` §9 (no tenía reglas para archivos; se agrega la subsección "Archivos") |
| D8 | **Contratos nuevos de OfertaEducativa**, que ella declara e implementa: `IExperienciasEducativas` (resúmenes de EE con su cadena, su entidad, su área y sus cupos, y los ids de las EE visibles como consulta componible `IQueryable<int>`) e `IPeriodosEscolares`. El área la resuelve OfertaEducativa con el contrato de Institucional que ya usa. El grafo no cambia | `ESTANDAR_MODULOS.md` §5 (primer contrato que devuelve una consulta componible) |
| D9 | **Periodos configurados:** `PeriodosOptions` (sección `SolicitudesApertura`, claves `PeriodoActual` y `PeriodoSiguiente`, de seis dígitos y distintas) validadas al arrancar. El cliente envía `periodoEscolarId`; crear exige que sea el siguiente configurado y que el actual y el siguiente existan activos. Un cambio posterior de la configuración no afecta a las solicitudes existentes: editar, aceptar, rechazar y cancelar no vuelven a mirar los periodos. `GET /periodos` devuelve la pareja vigente | — (`DATABASE.md` §16.1 y `PLAN_INICIAL.md`) |
| D10 | **Referencias inválidas al crear:** una EE inexistente, dada de baja, con su plan o su programa dados de baja o de otra entidad responde 400 en `experienciaEducativaId`; un periodo inexistente o dado de baja, 400 en `periodoEscolarId` (como OFE-D15). Un periodo válido que no es el siguiente configurado responde 409 `SolicitudApertura.PeriodoNoAbierto`, y una pareja configurada sin sus dos periodos activos, 409 `SolicitudApertura.PeriodosNoDisponibles` | `ESTANDAR_MODULOS.md` §9 (409 para "padres inactivos") |
| D11 | **Cupos:** al crear y al modificar, la cantidad no puede ser menor que el cupo mínimo **si existe** ni mayor que el máximo **si existe** (409 `SolicitudApertura.CantidadFueraDeCupos`); una EE sin cupos admite la solicitud. Aceptar exige los dos cupos (409 `SolicitudApertura.CuposIncompletos`) y la cantidad dentro del rango inclusivo (409 `SolicitudApertura.CantidadFueraDeCupos`). Siempre se usan los cupos vigentes de la EE en ese momento | Aclara `DATABASE.md` §16.4 |
| D12 | **Autorización:** la Entidad Académica crea, modifica y cancela las solicitudes de su entidad; la DGAA acepta y rechaza las de las entidades de su área. Las tres cuentas leen: la Entidad Académica, las de su entidad; la DGAA, las de su área; el **Superusuario, todas, en solo lectura**. El oficio solo lo descargan la Entidad Académica y la DGAA (403 para el Superusuario). Nacen las políticas `EntidadAcademica` y `DgaaOEntidadAcademica` (USU-D13) | `DATABASE.md` §16.4 ("El Superusuario no tiene acceso a este proceso"). `DATABASE_DIAGRAM.md` (reglas) |
| D13 | **Ámbito en la ruta:** una solicitud fuera del ámbito del usuario responde 404 en toda ruta `/{id}`, de lectura o de comando, sin revelar si existe (como OFE-D15). Un filtro del listado fuera del ámbito devuelve una página vacía, como en OfertaEducativa | — |
| D14 | **Baja de padres:** la EE y el plan no se dan de baja con solicitudes PENDIENTE o ACEPTADA de sus EE (P7); las RECHAZADAS y CANCELADAS no bloquean. El periodo no se da de baja con **cualquier** solicitud, en cualquier estado: prevalece OFE-D11 sobre `DATABASE.md` §16.6, que se corrige | `DATABASE.md` §16.6 (solo PENDIENTE y ACEPTADA bloqueaban también al periodo) |
| D15 | **Longitudes de aplicación:** la justificación y los comentarios de resolución admiten hasta **2000** caracteres, aunque las columnas son `nvarchar(max)`; el motivo de cancelación, hasta 1000, como su columna | Aclara `DATABASE.md` §16.2 |
| D16 | **Modificar** (PR 2) exige en cada petición la cantidad y la justificación completas; el oficio es opcional. Solo en PENDIENTE (409 `SolicitudApertura.NoPendiente`). Toda modificación actualiza `actualizada_en` y `actualizada_por_usuario_id`. Un oficio reemplazado elimina su fila de `archivo_solicitud_apertura` en la misma transacción y su binario después de confirmar (D4) | — (`DATABASE.md` §16.3 y §16.4) |
| D17 | **Acciones de estado** (PR 2): `POST /solicitudes/{id}/aceptar`, `/rechazar` y `/cancelar`, con cuerpo JSON, responden **204** sin cuerpo. Solo desde PENDIENTE (409 `SolicitudApertura.NoPendiente`). Aceptar admite comentarios opcionales; rechazar los exige; cancelar exige un motivo | — (`ESTANDAR_MODULOS.md` §9, acción de estado) |
| D18 | **Listado paginado** con filtros opcionales `estado`, `periodoEscolarId`, `experienciaEducativaId` y `entidadAcademicaId`, en orden de `creada_en` descendente con el `id` descendente como desempate. El `Response` incluye los datos derivados para mostrar (EE, plan, programa, entidad, modalidad y periodo), que **no** se guardan en la tabla | — (`DATABASE.md` §16.1) |
| D19 | **Auditoría:** solo se registra el binario que no se pudo eliminar (huérfano), con su clave. Las transiciones no se registran: quedan en la fila con su actor y su fecha, y el cliente ya ve el resultado | — (`ESTANDAR_MODULOS.md` §11) |
| D20 | **Rutas:** el recurso vive en `/api/v1/solicitudes-apertura/solicitudes` y la pareja de periodos en `/api/v1/solicitudes-apertura/periodos`, con el patrón `/api/v1/<modulo>/<recurso-en-plural>` del estándar | — (`ESTANDAR_MODULOS.md` §4) |
| D21 | **Estados en mayúsculas.** El estado es el enum `EstadoSolicitudApertura` y se guarda como `PENDIENTE`, `ACEPTADA`, `RECHAZADA` o `CANCELADA` (lo exige `ck_solicitud_apertura__estado`) con un convertidor explícito, no con `HasConversion<string>()`, que guardaría `Pendiente` | `ESTANDAR_MODULOS.md` §8 ("Los estados se guardan como texto con `HasConversion<string>()`") |

## 3. Piezas compartidas

### Almacenamiento de archivos (PR 1, D3 a D5)

`src/BuildingBlocks/Sgpla.BuildingBlocks.Application/Archivos.cs` (archivo nuevo). Debe quedar exactamente así, salvo el formato que imponga `dotnet format`:

```csharp
namespace Sgpla.BuildingBlocks.Application;

/// <summary>
/// Almacenamiento externo de binarios. La base guarda solo la clave, el tamaño y el SHA-256 que devuelve
/// <see cref="GuardarAsync"/>; el contenido nunca va a SQL Server.
/// </summary>
public interface IAlmacenamientoArchivos
{
    /// <summary>Tamaño máximo de un archivo, en bytes, según la configuración.</summary>
    long TamanoMaximoBytes { get; }

    /// <summary>
    /// Guarda el contenido con una clave nueva, <c>{carpeta}/{guid}{extension}</c>, y calcula su tamaño y su SHA-256
    /// mientras lo escribe. Si falla a medias, no deja el archivo.
    /// </summary>
    Task<ArchivoGuardado> GuardarAsync(string carpeta, string extension, Stream contenido, CancellationToken cancellationToken);

    /// <summary>Abre el archivo para lectura; <c>null</c> si no existe.</summary>
    Task<Stream?> AbrirAsync(string clave, CancellationToken cancellationToken);

    /// <summary>
    /// Elimina el archivo si existe. No lanza excepciones de entrada y salida: si no puede eliminarlo, registra el archivo
    /// huérfano y devuelve <c>false</c>.
    /// </summary>
    Task<bool> IntentarEliminarAsync(string clave, CancellationToken cancellationToken);
}

/// <summary>Resultado de <see cref="IAlmacenamientoArchivos.GuardarAsync"/>.</summary>
public sealed record ArchivoGuardado(string Clave, long Tamano, ReadOnlyMemory<byte> ChecksumSha256);

/// <summary>
/// Archivo recibido en una petición, antes de guardarlo. <see cref="AbrirLectura"/> abre el contenido desde el inicio y
/// puede llamarse más de una vez.
/// </summary>
public sealed record ArchivoRecibido(string Nombre, string TipoContenido, long Tamano, Func<Stream> AbrirLectura)
{
    /// <summary>
    /// Lee hasta <paramref name="longitud"/> bytes desde el inicio del contenido; menos si el contenido es más corto.
    /// Sirve para comprobar la firma de un archivo sin cargarlo completo.
    /// </summary>
    public async Task<byte[]> LeerEncabezadoAsync(int longitud, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(longitud);

        await using var contenido = AbrirLectura();
        var encabezado = new byte[longitud];
        var leidos = await contenido.ReadAtLeastAsync(encabezado, longitud, throwOnEndOfStream: false, cancellationToken);
        return encabezado[..leidos];
    }
}
```

`src/BuildingBlocks/Sgpla.BuildingBlocks.Infrastructure/Archivos/` (carpeta nueva):

- `AlmacenamientoOptions.cs`: `public sealed class AlmacenamientoOptions` con `public const string Seccion = "Almacenamiento";`,
  - `[Required] public string RutaBase { get; set; } = string.Empty;`, que además debe ser una ruta absoluta (`Path.IsPathRooted`), validada con `.Validate(...)` y el mensaje `Almacenamiento:RutaBase debe ser una ruta absoluta.`;
  - `[Range(1, 26_214_400)] public long TamanoMaximoBytes { get; set; } = 10_485_760;`.
- `AlmacenamientoLocal.cs`: `public sealed class AlmacenamientoLocal(IOptions<AlmacenamientoOptions> opciones, ILogger<AlmacenamientoLocal> logger) : IAlmacenamientoArchivos` (pública, como las demás piezas de BuildingBlocks, para probarla sin `InternalsVisibleTo`).
  - `GuardarAsync`: crea `{RutaBase}/{carpeta}` si no existe y escribe `{guid:N}{extension}` con `FileMode.CreateNew`, copiando el flujo por bloques y calculando el SHA-256 con `IncrementalHash` y el tamaño en la misma pasada. La clave usa `/` como separador (`solicitudes-apertura/3f2a…c1.pdf`). Si la copia falla, elimina el archivo parcial y relanza la excepción.
  - `AbrirAsync` e `IntentarEliminarAsync` resuelven la ruta completa y lanzan `ArgumentException` si queda fuera de `RutaBase` (una clave con `..` es un error de programación, no un dato del usuario).
  - `IntentarEliminarAsync` atrapa solo `IOException` y `UnauthorizedAccessException`, registra `ArchivoHuerfano` (sección 10) y devuelve `false`. Un archivo que ya no existe devuelve `true`.
- `AlmacenamientoExtensions.cs`: `public static IServiceCollection AddAlmacenamientoArchivos(this IServiceCollection services, IConfiguration configuration)`, que enlaza y valida las opciones con `ValidateDataAnnotations()` y `ValidateOnStart()`, y registra `IAlmacenamientoArchivos` → `AlmacenamientoLocal` como `Singleton`. `Program.cs` la llama justo después de `AddPersistenciaSgpla`.
- `src/BuildingBlocks/Sgpla.BuildingBlocks.Infrastructure/Http/ArchivoHttpExtensions.cs`: `public static ArchivoRecibido? ComoArchivoRecibido(this IFormFile? archivo)`, que devuelve `null` si `archivo` es `null` y, si no, `new ArchivoRecibido(archivo.FileName, archivo.ContentType, archivo.Length, archivo.OpenReadStream)`.

`BuildingBlocks.Infrastructure` ya referencia `Microsoft.AspNetCore.App`, que trae `ValidateDataAnnotations`; no se agrega ningún paquete.

Configuración:
- `docker-compose.yml`, servicio `api`: `Almacenamiento__RutaBase: "/var/lib/sgpla/archivos"`, el volumen `archivos-data:/var/lib/sgpla/archivos` y `archivos-data:` en `volumes`. No se publica `TamanoMaximoBytes`: se usa el valor por omisión.
- `src/Sgpla.Api/Dockerfile`, etapa `final`, antes de `USER $APP_UID`: `RUN mkdir -p /var/lib/sgpla/archivos && chown $APP_UID /var/lib/sgpla/archivos`, para que el volumen nombrado nazca con el dueño del proceso.
- `SgplaApiFactory` fija `Almacenamiento:RutaBase` en una carpeta temporal propia de cada instancia (`Path.Combine(Path.GetTempPath(), "sgpla-archivos", Guid.NewGuid().ToString("N"))`) y la borra en `DisposeAsync`.

### Concurrencia optimista (PR 2, D2)

`src/BuildingBlocks/Sgpla.BuildingBlocks.Infrastructure/Http/ConcurrenciaExceptionHandler.cs`: `public sealed class ConcurrenciaExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler`, con la misma forma que `ViolacionUnicidadExceptionHandler`:
- maneja solo `DbUpdateConcurrencyException`;
- responde 409 con `Title = "Conflicto"`, `Detail = "El registro cambió mientras lo modificabas. Consulta de nuevo y repite la operación."` y la extensión `codigo` = `public const string Codigo = "Persistencia.ModificacionConcurrente";`;
- no registra nada (el cliente ya recibe el 409).

`Program.cs` lo registra con `AddExceptionHandler<ConcurrenciaExceptionHandler>()` junto al de unicidad.

### Políticas `EntidadAcademica` y `DgaaOEntidadAcademica` (PR 1, D12)

`src/BuildingBlocks/Sgpla.BuildingBlocks.Application/Autorizacion.cs` agrega a `Politicas`, debajo de `Dgaa`:

```csharp
/// <summary><see cref="Autenticado"/> con el rol Entidad Académica.</summary>
public const string EntidadAcademica = nameof(EntidadAcademica);

/// <summary><see cref="Autenticado"/> con el rol DGAA o Entidad Académica.</summary>
public const string DgaaOEntidadAcademica = nameof(DgaaOEntidadAcademica);
```

`UsuariosModule.AddUsuariosModule` las registra igual que `Dgaa`; `DgaaOEntidadAcademica` pasa los dos valores a `RequireClaim("rol", …)`:

```csharp
.AddPolicy(Politicas.EntidadAcademica, politica => politica
    .RequireAuthenticatedUser()
    .AddRequirements(new SinCambioPendienteRequirement())
    .RequireClaim("rol", ((byte)Rol.EntidadAcademica).ToString(CultureInfo.InvariantCulture)))
.AddPolicy(Politicas.DgaaOEntidadAcademica, politica => politica
    .RequireAuthenticatedUser()
    .AddRequirements(new SinCambioPendienteRequirement())
    .RequireClaim(
        "rol",
        ((byte)Rol.Dgaa).ToString(CultureInfo.InvariantCulture),
        ((byte)Rol.EntidadAcademica).ToString(CultureInfo.InvariantCulture)))
```

Quien no cumple una política recibe 403 `Autorizacion.SinPermiso`.

### Contratos de OfertaEducativa (PR 1, D8)

Ver la sección 9.

## 4. Normalización y validación comunes

Las reglas de forma las aplica el **dominio** con `Error.Validation(código, mensaje, campo)`; no hay validators de FluentValidation para los datos de la entidad. Cada fábrica o método devuelve el **primer** error, en el orden de su tabla.

| Campo | Normalización | Validación | Error |
|---|---|---|---|
| `Seccion` | `Normalizacion.Recortar` y `ToUpperInvariant()` | No vacía; máximo 20; solo `A`-`Z` y `0`-`9` (refleja `ck_solicitud_apertura__seccion_formato`) | `SolicitudApertura.SeccionVacia`, `SeccionDemasiadoLarga`, `SeccionFormatoInvalido` |
| `CantidadEstudiantes` | Ninguna (`int`) | Mayor que 0 | `SolicitudApertura.CantidadNoPositiva` |
| `Justificacion` | `Normalizacion.Recortar`. **No** colapsa espacios ni saltos de línea internos | No vacía; máximo 2000 (D15) | `SolicitudApertura.JustificacionVacia`, `JustificacionDemasiadoLarga` |
| `Comentarios` (aceptar, PR 2) | `Normalizacion.Recortar`; vacío o `null` → `null` | Máximo 2000 | `SolicitudApertura.ComentariosDemasiadoLargos` |
| `Comentarios` (rechazar, PR 2) | `Normalizacion.Recortar` | No vacíos; máximo 2000 | `SolicitudApertura.ComentariosVacios`, `ComentariosDemasiadoLargos` |
| `Motivo` (cancelar, PR 2) | `Normalizacion.Recortar` | No vacío; máximo 1000 | `SolicitudApertura.MotivoVacio`, `MotivoDemasiadoLargo` |
| Nombre del oficio | Se queda con lo que sigue a la última `/` o `\` y aplica `Normalizacion.Recortar` | No vacío; máximo 260 | `ArchivoSolicitudApertura.NombreVacio`, `NombreDemasiadoLargo` |
| Tamaño del oficio | Ninguna | Mayor que 0; como máximo `IAlmacenamientoArchivos.TamanoMaximoBytes` (D5) | `ArchivoSolicitudApertura.Vacio`, `DemasiadoGrande` |
| Tipo del oficio | Ninguna | `TipoContenido` igual a `application/pdf` sin distinguir mayúsculas, y los primeros 5 bytes iguales a `%PDF-` (D6) | `ArchivoSolicitudApertura.NoEsPdf` |

El `mime` que se guarda es siempre `application/pdf`. Un campo de formulario que no llega se convierte en `0` o en texto vacío antes de llamar al handler, así que lo reporta el dominio; un valor que no se puede convertir (`cantidadEstudiantes=abc`) lo rechaza ASP.NET Core con 400 antes del handler, y ese caso no se prueba.

## 5. Autorización y ámbito

| Operación | Entidad Académica | DGAA | Superusuario | Política de la ruta |
|---|---|---|---|---|
| Listar y obtener solicitudes | Las de su entidad | Las de las entidades de su área | Todas | `Autenticado` (grupo del módulo) |
| Descargar el oficio | Las de su entidad | Las de su área | No (403) | `DgaaOEntidadAcademica` |
| Crear | En su entidad | No (403) | No (403) | `EntidadAcademica` |
| Modificar y cancelar (PR 2) | Las de su entidad | No (403) | No (403) | `EntidadAcademica` |
| Aceptar y rechazar (PR 2) | No (403) | Las de su área | No (403) | `Dgaa` |
| Consultar los periodos configurados | Sí | Sí | Sí | `Autenticado` (grupo del módulo) |

Cómo se aplica el ámbito:
- **Lectura** (listar, obtener y descargar): la consulta parte de `IExperienciasEducativas.ConsultarIdsVisiblesAsync` (sección 9), que devuelve los ids de las EE del ámbito de lectura de OfertaEducativa: la entidad de la Entidad Académica, las entidades del área de la DGAA o todas para el Superusuario, incluidas las EE, los planes, los programas y las entidades dados de baja. Es el mismo ámbito que D12 pide, porque `IAmbitoOfertaEducativa.EntidadesVisiblesAsync` ya lo calcula así. Una solicitud cuya EE no está entre esos ids responde 404 `SolicitudApertura.NoEncontrada` (D13).
- **Comandos** sobre una solicitud (PR 2): el handler carga la solicitud y pide a `IAmbitoSolicitudesApertura` el resumen de su EE: `ExperienciaDeSuEntidadAsync` para la Entidad Académica (la EE debe ser de `actual.EntidadAcademicaId`) y `ExperienciaDeSuAreaAsync` para la DGAA (la EE debe ser de una entidad de `actual.AreaAcademicaId`). Si devuelve `null`, 404 `SolicitudApertura.NoEncontrada`. El servicio vive en `Application/Ambito` y lo implementa `Infrastructure/Ambito/AmbitoSolicitudesApertura` con `IExperienciasEducativas` e `ICurrentUser`.
- **Crear:** la EE del cuerpo debe estar vigente y ser de la entidad del usuario; si no, 400 en `experienciaEducativaId` (D10).
- `ICurrentUser` ya garantiza por petición que la cuenta y su ámbito están activos (USU-D4); este módulo no lo vuelve a comprobar.

## 6. Solicitud de apertura

Petición de apertura de una sección de una EE en el periodo siguiente (`DATABASE.md` §16.2). No tiene baja lógica ni restauración; sus estados terminales conservan la petición y su oficio.

### Esquema (PR 1, D1 y D2)

`baseline.sql`, en `CREATE TABLE academico.solicitud_apertura`:
- se eliminan las líneas de las columnas `programacion_academica_id`, `vinculada_en` y `vinculada_por_usuario_id`;
- se agrega, después de `motivo_cancelacion`, la columna:

  ```sql
      version                    rowversion        NOT NULL,
  ```

- se eliminan `fk_solicitud_apertura__programacion_academica`, `fk_solicitud_apertura__usuario__vinculada_por`, `ck_solicitud_apertura__vinculacion_conjunta` y `ck_solicitud_apertura__vinculacion_posterior`;
- `ck_solicitud_apertura__estado_consistente` debe quedar exactamente así:

  ```sql
      CONSTRAINT ck_solicitud_apertura__estado_consistente CHECK (
             (estado = 'PENDIENTE' AND resuelta_en IS NULL AND comentarios_resolucion IS NULL AND cancelada_en IS NULL)
          OR (estado = 'ACEPTADA' AND resuelta_en IS NOT NULL AND cancelada_en IS NULL)
          OR (estado = 'RECHAZADA' AND resuelta_en IS NOT NULL AND comentarios_resolucion IS NOT NULL AND LEN(TRIM(comentarios_resolucion)) > 0 AND cancelada_en IS NULL)
          OR (estado = 'CANCELADA' AND cancelada_en IS NOT NULL AND resuelta_en IS NULL AND comentarios_resolucion IS NULL))
  ```

- después de la tabla se eliminan `ux_solicitud_apertura__programacion_academica_id` e `ix_solicitud_apertura__vinculada_por_usuario_id`.

Se busca cualquier otra mención a `programacion_academica_id`, `vinculada_en` o `vinculada_por_usuario_id` dentro de lo que toca a `solicitud_apertura` en `baseline.sql` y `seed.sql`; si aparece una que este documento no prevé, se detiene y se pregunta. `archivo_solicitud_apertura` no cambia.

### Dominio

`Domain/SolicitudesApertura/EstadoSolicitudApertura.cs`: `internal enum EstadoSolicitudApertura { Pendiente, Aceptada, Rechazada, Cancelada }`.

`Domain/SolicitudesApertura/DatosSolicitudApertura.cs`: `internal sealed record DatosSolicitudApertura(string Seccion, int CantidadEstudiantes, string Justificacion)` con `static Result<DatosSolicitudApertura> Crear(string? seccion, int cantidadEstudiantes, string? justificacion)`, que normaliza y valida en este orden: sección, cantidad y justificación. Existe para validar los datos **antes** de guardar el oficio (D4).

`Domain/SolicitudesApertura/SolicitudApertura.cs`: `internal sealed class SolicitudApertura : Entity`.

| Propiedad | Tipo | Constante | Mutable |
|---|---|---|---|
| `ExperienciaEducativaId` | `int` | — | No |
| `PeriodoEscolarId` | `int` | — | No |
| `Seccion` | `string` | `LongitudMaximaSeccion = 20` | No |
| `CantidadEstudiantes` | `int` | — | Con `Modificar` (PR 2) |
| `Justificacion` | `string` | `LongitudMaximaJustificacion = 2000` | Con `Modificar` (PR 2) |
| `OficioRespaldoId` | `int` | — | Con `ReemplazarOficio` (PR 2) |
| `Oficio` | `ArchivoSolicitudApertura` | — | Con `ReemplazarOficio` (PR 2) |
| `Estado` | `EstadoSolicitudApertura` | — | Con las transiciones (PR 2) |
| `CreadaEn`, `CreadaPorUsuarioId` | `DateTime`, `int` | — | No |
| `ActualizadaEn`, `ActualizadaPorUsuarioId` | `DateTime?`, `int?` | — | Con `Modificar` (PR 2) |
| `ResueltaEn`, `ResueltaPorUsuarioId` | `DateTime?`, `int?` | — | Con `Aceptar` y `Rechazar` (PR 2) |
| `ComentariosResolucion` | `string?` | `LongitudMaximaComentarios = 2000` | Con `Aceptar` y `Rechazar` (PR 2) |
| `CanceladaEn`, `CanceladaPorUsuarioId` | `DateTime?`, `int?` | — | Con `Cancelar` (PR 2) |
| `MotivoCancelacion` | `string?` | `LongitudMaximaMotivo = 1000` | Con `Cancelar` (PR 2) |
| `Version` | `byte[]` | — | La asigna SQL Server |

Métodos (PR 1):
- `static SolicitudApertura Crear(DatosSolicitudApertura datos, int experienciaEducativaId, int periodoEscolarId, ArchivoSolicitudApertura oficio, int usuarioId, DateTime utc)`: nace en `Pendiente`, con `CreadaEn = utc` y `CreadaPorUsuarioId = usuarioId`. No devuelve `Result`, porque `datos` ya viene validado.
- `static Result ValidarCupos(int cantidadEstudiantes, int? cupoMinimo, int? cupoMaximo)`: `CantidadFueraDeCupos` si la cantidad es menor que el mínimo existente o mayor que el máximo existente (D11). La usan crear y modificar.

Métodos (PR 2), cada uno valida en el orden indicado y no asigna nada si falla:
- `Result Modificar(int cantidadEstudiantes, string? justificacion, int? cupoMinimo, int? cupoMaximo, int usuarioId, DateTime utc)`: cantidad, justificación, `NoPendiente` y cupos (`ValidarCupos`). Asigna los dos campos, `ActualizadaEn` y `ActualizadaPorUsuarioId`.
- `ArchivoSolicitudApertura ReemplazarOficio(ArchivoSolicitudApertura nuevo)`: solo se llama después de un `Modificar` exitoso; asigna el nuevo oficio y devuelve el anterior.
- `Result Aceptar(string? comentarios, int? cupoMinimo, int? cupoMaximo, int usuarioId, DateTime utc)`: comentarios, `NoPendiente`, `CuposIncompletos` (falta alguno de los dos) y `CantidadFueraDeCupos`. Pasa a `Aceptada` con `ResueltaEn`, `ResueltaPorUsuarioId` y `ComentariosResolucion` (o `null`).
- `Result Rechazar(string? comentarios, int usuarioId, DateTime utc)`: comentarios obligatorios y `NoPendiente`. Pasa a `Rechazada`.
- `Result Cancelar(string? motivo, int usuarioId, DateTime utc)`: motivo y `NoPendiente`. Pasa a `Cancelada` con `CanceladaEn`, `CanceladaPorUsuarioId` y `MotivoCancelacion`.

`Domain/SolicitudesApertura/SolicitudAperturaErrors.cs` (los de la columna PR que dice 2 se agregan en ese PR):

| Código | Tipo | Campo | Mensaje | PR |
|---|---|---|---|---|
| `SolicitudApertura.SeccionVacia` | Validation | `Seccion` | La sección es obligatoria. | 1 |
| `SolicitudApertura.SeccionDemasiadoLarga` | Validation | `Seccion` | La sección admite hasta 20 caracteres. | 1 |
| `SolicitudApertura.SeccionFormatoInvalido` | Validation | `Seccion` | La sección solo admite letras sin acentos y dígitos, sin espacios. | 1 |
| `SolicitudApertura.CantidadNoPositiva` | Validation | `CantidadEstudiantes` | La cantidad de estudiantes debe ser mayor que cero. | 1 |
| `SolicitudApertura.JustificacionVacia` | Validation | `Justificacion` | La justificación es obligatoria. | 1 |
| `SolicitudApertura.JustificacionDemasiadoLarga` | Validation | `Justificacion` | La justificación admite hasta 2000 caracteres. | 1 |
| `SolicitudApertura.ExperienciaEducativaInvalida` | Validation | `ExperienciaEducativaId` | La experiencia educativa no existe, está dada de baja o no pertenece a tu entidad académica. | 1 |
| `SolicitudApertura.PeriodoEscolarInvalido` | Validation | `PeriodoEscolarId` | El periodo escolar no existe o está dado de baja. | 1 |
| `SolicitudApertura.PeriodosNoDisponibles` | Conflict | — | El periodo actual o el siguiente configurados no existen o están dados de baja. | 1 |
| `SolicitudApertura.PeriodoNoAbierto` | Conflict | — | Solo se reciben solicitudes para el periodo siguiente configurado. | 1 |
| `SolicitudApertura.CantidadFueraDeCupos` | Conflict | — | La cantidad de estudiantes está fuera de los cupos de la experiencia educativa. | 1 |
| `SolicitudApertura.SeccionDuplicada` | Conflict | — | Ya existe una solicitud pendiente de esa sección para la experiencia educativa y el periodo. | 1 |
| `SolicitudApertura.NoEncontrada(int id)` | NotFound | — | No existe la solicitud de apertura {id}. | 1 |
| `SolicitudApertura.ComentariosVacios` | Validation | `Comentarios` | Los comentarios son obligatorios para rechazar. | 2 |
| `SolicitudApertura.ComentariosDemasiadoLargos` | Validation | `Comentarios` | Los comentarios admiten hasta 2000 caracteres. | 2 |
| `SolicitudApertura.MotivoVacio` | Validation | `Motivo` | El motivo es obligatorio. | 2 |
| `SolicitudApertura.MotivoDemasiadoLargo` | Validation | `Motivo` | El motivo admite hasta 1000 caracteres. | 2 |
| `SolicitudApertura.NoPendiente` | Conflict | — | La solicitud ya no está pendiente. | 2 |
| `SolicitudApertura.CuposIncompletos` | Conflict | — | La experiencia educativa necesita cupo mínimo y máximo para aceptar la solicitud. | 2 |

Los mensajes con longitudes se construyen con las constantes de la entidad.

### Application

`Application/SolicitudesApertura/ISolicitudAperturaRepository.cs`:

```csharp
internal interface ISolicitudAperturaRepository
{
    /// <summary>Con su oficio cargado.</summary>
    Task<SolicitudApertura?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Solo solicitudes PENDIENTE; la sección llega ya normalizada.</summary>
    Task<bool> ExistePendienteAsync(
        int experienciaEducativaId,
        int periodoEscolarId,
        string seccion,
        CancellationToken cancellationToken);

    void Agregar(SolicitudApertura solicitud);

    /// <summary>Borrado físico de un oficio reemplazado.</summary>
    void EliminarOficio(ArchivoSolicitudApertura oficio);
}
```

`EliminarOficio` se agrega en el PR 2.

`Application/Periodos/IPeriodosConfigurados.cs`:

```csharp
/// <summary>Pareja de periodos que fija la configuración para recibir solicitudes.</summary>
internal interface IPeriodosConfigurados
{
    string ClaveActual { get; }

    string ClaveSiguiente { get; }
}
```

`Application/SolicitudesApertura/SolicitudAperturaConsultas.cs`:

```csharp
internal sealed record SolicitudAperturaResponse(
    int Id,
    string Estado,
    string Seccion,
    int CantidadEstudiantes,
    string Justificacion,
    ExperienciaSolicitadaResponse ExperienciaEducativa,
    PeriodoSolicitadoResponse PeriodoEscolar,
    OficioResponse Oficio,
    DateTime CreadaEn,
    int CreadaPorUsuarioId,
    DateTime? ActualizadaEn,
    int? ActualizadaPorUsuarioId,
    DateTime? ResueltaEn,
    int? ResueltaPorUsuarioId,
    string? ComentariosResolucion,
    DateTime? CanceladaEn,
    int? CanceladaPorUsuarioId,
    string? MotivoCancelacion);

/// <summary>Datos derivados de la cadena académica de la EE; no se guardan en la solicitud.</summary>
internal sealed record ExperienciaSolicitadaResponse(
    int Id,
    string Materia,
    string Curso,
    string Nombre,
    int? CupoMinimo,
    int? CupoMaximo,
    int PlanEstudiosId,
    string PlanEstudiosCodigo,
    int ProgramaEducativoId,
    string ProgramaEducativoNombre,
    int EntidadAcademicaId,
    string EntidadAcademicaClave,
    string EntidadAcademicaNombre,
    int SistemaEducativoId,
    string Modalidad);

internal sealed record PeriodoSolicitadoResponse(int Id, string Clave);

internal sealed record OficioResponse(string Nombre, long Tamano, DateTime CargadoEn);

/// <summary>Filtros opcionales, combinados con AND.</summary>
internal sealed record FiltrosSolicitudesApertura(
    string? Estado,
    int? PeriodoEscolarId,
    int? ExperienciaEducativaId,
    int? EntidadAcademicaId);

/// <summary>Solicitudes del ámbito, de la más reciente a la más antigua (con el id como desempate), paginadas.</summary>
internal sealed record ListarSolicitudesAperturaQuery(Paginacion Paginacion, FiltrosSolicitudesApertura Filtros);

internal sealed record ObtenerSolicitudAperturaQuery(int Id);

internal sealed record DescargarOficioSolicitudAperturaQuery(int Id);

/// <summary>El contenido lo cierra quien escribe la respuesta.</summary>
internal sealed record OficioDescarga(Stream Contenido, string Nombre);
```

- `Estado` del `Response` es el texto guardado (`PENDIENTE`…); `Modalidad` es el nombre del sistema educativo del programa (`DATABASE.md` §16.1).
- `ListarSolicitudesAperturaValidator`: incluye `PaginacionValidator`; `estado`, si llega, debe ser `PENDIENTE`, `ACEPTADA`, `RECHAZADA` o `CANCELADA` sin distinguir mayúsculas (`OverridePropertyName("estado")`, `WithName("estado")`); `periodoEscolarId`, `experienciaEducativaId` y `entidadAcademicaId`, si llegan, mayores que 0, con el mismo patrón que `ListarProgramasEducativosValidator`.

Comando del PR 1:

| Archivo | Command | Dependencias del handler | Pasos |
|---|---|---|---|
| `CrearSolicitudApertura.cs` | `CrearSolicitudAperturaCommand(int ExperienciaEducativaId, int PeriodoEscolarId, string? Seccion, int CantidadEstudiantes, string? Justificacion, ArchivoRecibido? Oficio)`; devuelve `SolicitudAperturaResponse` | `ISolicitudAperturaRepository`, `IExperienciasEducativas`, `IPeriodosEscolares`, `IPeriodosConfigurados`, `IAlmacenamientoArchivos`, `ICurrentUser`, `IUnitOfWork`, `TimeProvider` | 1. `DatosSolicitudApertura.Crear` (400). 2. Si no llega el oficio, `ArchivoSolicitudAperturaErrors.Obligatorio`; si llega, `ArchivoSolicitudApertura.ValidarOficio(oficio.Nombre, oficio.TipoContenido, oficio.Tamano, almacenamiento.TamanoMaximoBytes)` y la firma `%PDF-` con `oficio.LeerEncabezadoAsync(ArchivoSolicitudApertura.LongitudFirmaPdf, …)` (400, sección 7). 3. `ObtenerAsync([id])` de la EE: si no aparece, no está `Vigente` o su `EntidadAcademicaId` no es `actual.EntidadAcademicaId` → `ExperienciaEducativaInvalida`. 4. `ObtenerAsync([periodoId])` del periodo: si no aparece o no está `Activo` → `PeriodoEscolarInvalido`. 5. `ObtenerActivosPorClaveAsync([ClaveActual, ClaveSiguiente])`: si falta alguna → `PeriodosNoDisponibles`. 6. Si la clave del periodo no es `ClaveSiguiente` → `PeriodoNoAbierto`. 7. `SolicitudApertura.ValidarCupos` con los cupos del resumen. 8. `ExistePendienteAsync` → `SeccionDuplicada`. 9. `GuardarAsync("solicitudes-apertura", ".pdf", AbrirLectura())`. 10. `ArchivoSolicitudApertura.Crear` y `SolicitudApertura.Crear` con el mismo instante (truncado a segundos) y `Agregar`. 11. `SaveChangesAsync` dentro de un `try`; en el `catch` sin filtro se llama a `IntentarEliminarAsync(clave, CancellationToken.None)` y se relanza con `throw;`. 12. Arma el `Response` con los resúmenes de los pasos 3 y 4 |

Comandos del PR 2:

| Archivo | Command | Dependencias del handler | Pasos |
|---|---|---|---|
| `ModificarSolicitudApertura.cs` | `ModificarSolicitudAperturaCommand(int Id, int CantidadEstudiantes, string? Justificacion, ArchivoRecibido? Oficio)` | `ISolicitudAperturaRepository`, `IAmbitoSolicitudesApertura`, `IAlmacenamientoArchivos`, `ICurrentUser`, `IUnitOfWork`, `TimeProvider` | 1. `ObtenerPorIdAsync` → `NoEncontrada`. 2. `ExperienciaDeSuEntidadAsync` con la EE de la solicitud; `null` → `NoEncontrada`. 3. Si llega oficio: `ArchivoSolicitudApertura.ValidarOficio(oficio.Nombre, oficio.TipoContenido, oficio.Tamano, almacenamiento.TamanoMaximoBytes)` y la firma `%PDF-` con `oficio.LeerEncabezadoAsync(ArchivoSolicitudApertura.LongitudFirmaPdf, …)` (400); si no llega, se conserva el actual. 4. `Modificar` con los cupos del resumen. 5. Si llega oficio: `GuardarAsync`, `ArchivoSolicitudApertura.Crear`, `ReemplazarOficio` y `EliminarOficio(anterior)`. 6. `SaveChangesAsync` con la misma compensación que crear. 7. Si hubo reemplazo, `IntentarEliminarAsync(claveAnterior, CancellationToken.None)` después de confirmar |
| `AceptarSolicitudApertura.cs` | `AceptarSolicitudAperturaCommand(int Id, string? Comentarios)` | `ISolicitudAperturaRepository`, `IAmbitoSolicitudesApertura`, `ICurrentUser`, `IUnitOfWork`, `TimeProvider` | 1. `ObtenerPorIdAsync` → `NoEncontrada`. 2. `ExperienciaDeSuAreaAsync` con la EE de la solicitud; `null` → `NoEncontrada`. 3. `Aceptar` con los cupos vigentes del resumen. 4. `SaveChangesAsync` |
| `RechazarSolicitudApertura.cs` | `RechazarSolicitudAperturaCommand(int Id, string? Comentarios)` | Las mismas, sin cupos | 1 y 2 como aceptar. 3. `Rechazar`. 4. `SaveChangesAsync` |
| `CancelarSolicitudApertura.cs` | `CancelarSolicitudAperturaCommand(int Id, string? Motivo)` | `ISolicitudAperturaRepository`, `IAmbitoSolicitudesApertura`, `ICurrentUser`, `IUnitOfWork`, `TimeProvider` | 1. `ObtenerPorIdAsync` → `NoEncontrada`. 2. `ExperienciaDeSuEntidadAsync` con la EE de la solicitud; `null` → `NoEncontrada`. 3. `Cancelar`. 4. `SaveChangesAsync` |

Un choque de `rowversion` en cualquiera de los `SaveChangesAsync` del PR 2 lanza `DbUpdateConcurrencyException`, que responde 409 `Persistencia.ModificacionConcurrente` (sección 3); en modificar, la compensación elimina antes el binario nuevo.

### Infrastructure

- `SolicitudAperturaConfiguration.cs`:
  - `ToTable("solicitud_apertura", "academico")`;
  - `Seccion` con `HasMaxLength(LongitudMaximaSeccion).IsUnicode(false)`;
  - `Estado` con `HasMaxLength(20).IsUnicode(false)` y un convertidor explícito a mayúsculas (D21): `HasConversion(e => e.ToString().ToUpperInvariant(), t => Enum.Parse<EstadoSolicitudApertura>(t, ignoreCase: true))`;
  - `MotivoCancelacion` con `HasMaxLength(LongitudMaximaMotivo)`; `Justificacion` y `ComentariosResolucion` sin `HasMaxLength` (son `nvarchar(max)`);
  - `Version` con `IsRowVersion()` (columna `version`);
  - `CreadaEn`, `ActualizadaEn`, `ResueltaEn` y `CanceladaEn` se leen como UTC (`DateTimeKind.Utc`) con un convertidor, para que el JSON las devuelva con `Z`;
  - `HasOne(s => s.Oficio).WithOne().HasForeignKey<SolicitudApertura>(s => s.OficioRespaldoId)`;
  - sin filtro de baja lógica, porque la tabla no tiene `fecha_eliminacion`.
- `SolicitudAperturaRepository.cs`: `ObtenerPorIdAsync` con `Include(s => s.Oficio)`; `ExistePendienteAsync` compara en la base con `Estado == EstadoSolicitudApertura.Pendiente`.
- `SolicitudAperturaConsultas.cs`:
  - `ListarSolicitudesAperturaHandler`: parte de `visibles = await ConsultarIdsVisiblesAsync(filtros.EntidadAcademicaId, …)` y filtra `Set<SolicitudApertura>().AsNoTracking().Where(s => visibles.Contains(s.ExperienciaEducativaId))`. Después aplica `estado`, `periodoEscolarId` y `experienciaEducativaId`, ordena por `CreadaEn` descendente y `Id` descendente, y pagina con `PaginarAsync`. Los datos derivados se resuelven **después** de paginar, con una sola llamada a `IExperienciasEducativas.ObtenerAsync` y otra a `IPeriodosEscolares.ObtenerAsync` con los ids de la página;
  - `ObtenerSolicitudAperturaHandler`: la misma base visible y `Id`; si no aparece, `NoEncontrada`;
  - `DescargarOficioSolicitudAperturaHandler`: la misma base visible, proyecta `Oficio.Nombre` y `Oficio.ClaveAlmacenamiento`, y abre el binario con `IAlmacenamientoArchivos.AbrirAsync`. Si el binario no existe, lanza `InvalidOperationException` con el mensaje `No se encontró el binario del oficio de la solicitud de apertura {id}.` (una inconsistencia técnica: responde 500 y la registra `UseExceptionHandler`).
  - La consulta de `ConsultarIdsVisiblesAsync` desactiva los filtros de baja lógica de las tablas de OfertaEducativa, y en EF Core 10 eso alcanza a toda la consulta que la compone. No afecta a este módulo, porque sus tablas no tienen filtro, pero ninguna consulta de SolicitudesApertura debe combinarla con otra entidad que dependa de ese filtro.

### Endpoints

`Endpoints/SolicitudesApertura/SolicitudAperturaEndpoints.cs`, grupo `/solicitudes` con tag `Solicitudes de apertura`:

| Método y ruta | `WithName` | `WithSummary` | Autorización | Entrada | Éxito | Errores declarados | PR |
|---|---|---|---|---|---|---|---|
| `GET /` | `ListarSolicitudesApertura` | Lista las solicitudes de apertura del ámbito, de la más reciente a la más antigua. | (grupo) | `ListarSolicitudesAperturaRequest` con `[AsParameters]`: `pagina`, `tamanoPagina`, `estado`, `periodoEscolarId`, `experienciaEducativaId`, `entidadAcademicaId` | 200 `Pagina<SolicitudAperturaResponse>` | 400 | 1 |
| `GET /{id:int}` | `ObtenerSolicitudApertura` | Obtiene una solicitud de apertura del ámbito. | (grupo) | — | 200 | 404 | 1 |
| `GET /{id:int}/oficio` | `DescargarOficioSolicitudApertura` | Descarga el oficio de respaldo de una solicitud de apertura. | `DgaaOEntidadAcademica` | — | 200 `application/pdf` con el nombre del archivo | 404 | 1 |
| `POST /` | `CrearSolicitudApertura` | Registra una solicitud de apertura para el periodo siguiente, con su oficio en PDF. | `EntidadAcademica` | `multipart/form-data`: `experienciaEducativaId`, `periodoEscolarId`, `seccion`, `cantidadEstudiantes`, `justificacion` y el archivo `oficio` | 201 con `Location` y el recurso | 400, 409 | 1 |
| `PUT /{id:int}` | `ModificarSolicitudApertura` | Modifica una solicitud de apertura pendiente: cantidad, justificación y, si llega, el oficio. | `EntidadAcademica` | `multipart/form-data`: `cantidadEstudiantes`, `justificacion` y el archivo `oficio` opcional | 204 | 400, 404, 409 | 2 |
| `POST /{id:int}/aceptar` | `AceptarSolicitudApertura` | Acepta una solicitud de apertura pendiente. | `Dgaa` | `AceptarSolicitudAperturaRequest(string? Comentarios)` | 204 | 400, 404, 409 | 2 |
| `POST /{id:int}/rechazar` | `RechazarSolicitudApertura` | Rechaza una solicitud de apertura pendiente, con comentarios. | `Dgaa` | `RechazarSolicitudAperturaRequest(string? Comentarios)` | 204 | 400, 404, 409 | 2 |
| `POST /{id:int}/cancelar` | `CancelarSolicitudApertura` | Cancela una solicitud de apertura pendiente, con un motivo. | `EntidadAcademica` | `CancelarSolicitudAperturaRequest(string? Motivo)` | 204 | 400, 404, 409 | 2 |

Reglas propias de estas rutas:
- Los campos del formulario se enlazan como parámetros sueltos con `[FromForm(Name = "…")]` (los enteros como `int?`, convertidos con `?? 0`, y los textos como `string?`) y el archivo como `IFormFile? oficio`, convertido con `ComoArchivoRecibido()`.
- `POST /` y `PUT /{id:int}` llevan `.DisableAntiforgery()` con el comentario: `// La API se autentica con token bearer, sin cookies: no hay CSRF que prevenir.`
- La descarga responde `TypedResults.File(descarga.Contenido, "application/pdf", descarga.Nombre)` con el tipo de retorno `Results<FileStreamHttpResult, ProblemHttpResult>`, y declara `.Produces(StatusCodes.Status200OK, contentType: "application/pdf")`.
- El `Location` de `POST /` apunta a `ObtenerSolicitudApertura`.

Ejemplo de la respuesta de `GET /{id}`:

```json
{
  "id": 12,
  "estado": "PENDIENTE",
  "seccion": "A2",
  "cantidadEstudiantes": 25,
  "justificacion": "Demanda de estudiantes rezagados.",
  "experienciaEducativa": {
    "id": 301, "materia": "ISOF", "curso": "00001", "nombre": "Programación",
    "cupoMinimo": 10, "cupoMaximo": 40,
    "planEstudiosId": 7, "planEstudiosCodigo": "ISOF-14",
    "programaEducativoId": 5, "programaEducativoNombre": "Ingeniería de Software",
    "entidadAcademicaId": 3, "entidadAcademicaClave": "FEI", "entidadAcademicaNombre": "Facultad de Estadística e Informática",
    "sistemaEducativoId": 1, "modalidad": "Escolarizada"
  },
  "periodoEscolar": { "id": 9, "clave": "202651" },
  "oficio": { "nombre": "oficio-apertura.pdf", "tamano": 183044, "cargadoEn": "2026-10-01T15:04:05Z" },
  "creadaEn": "2026-10-01T15:04:05Z",
  "creadaPorUsuarioId": 44,
  "actualizadaEn": null, "actualizadaPorUsuarioId": null,
  "resueltaEn": null, "resueltaPorUsuarioId": null, "comentariosResolucion": null,
  "canceladaEn": null, "canceladaPorUsuarioId": null, "motivoCancelacion": null
}
```

## 7. Oficio de respaldo

Archivo único del oficio de una solicitud (`DATABASE.md` §16.3). No se versiona: al reemplazarlo, la fila anterior se borra físicamente.

### Dominio

`Domain/SolicitudesApertura/ArchivoSolicitudApertura.cs`: `internal sealed class ArchivoSolicitudApertura : Entity`.

| Propiedad | Tipo | Constante |
|---|---|---|
| `Nombre` | `string` | `LongitudMaximaNombre = 260` |
| `Mime` | `string` | `MimePdf = "application/pdf"`, `LongitudMaximaMime = 255` |
| `Tamano` | `long` | — |
| `ChecksumSha256` | `byte[]` | `LongitudChecksum = 32` |
| `ClaveAlmacenamiento` | `string` | `LongitudMaximaClave = 500` |
| `CargadoEn` | `DateTime` | — |
| `CargadoPorUsuarioId` | `int` | — |

Métodos:
- `static Result<string> ValidarOficio(string? nombre, string? tipoContenido, long tamano, long tamanoMaximoBytes)`: valida en este orden: el nombre (`NombreVacio`, `NombreDemasiadoLargo`), el tamaño (`Vacio`, `DemasiadoGrande`) y el tipo declarado (`NoEsPdf`). Devuelve el nombre normalizado. Recibe valores simples porque Domain no depende de `BuildingBlocks.Application`; que el oficio llegue (`Obligatorio`) lo comprueba el handler antes de llamarla.
- `static bool TieneFirmaPdf(ReadOnlySpan<byte> encabezado)`: `true` si los primeros `LongitudFirmaPdf` (5) bytes son `%PDF-`. El handler lee el encabezado con `ArchivoRecibido.LeerEncabezadoAsync(LongitudFirmaPdf, …)` y, si es `false`, devuelve `NoEsPdf`.
- `static ArchivoSolicitudApertura Crear(string nombre, long tamano, byte[] checksumSha256, string claveAlmacenamiento, int usuarioId, DateTime utc)`: con `Mime = MimePdf`; el handler pasa `Tamano`, `ChecksumSha256` y `Clave` de `ArchivoGuardado`.

`Domain/SolicitudesApertura/ArchivoSolicitudAperturaErrors.cs`, todos de tipo Validation y con el campo `Oficio`:

| Código | Mensaje |
|---|---|
| `ArchivoSolicitudApertura.Obligatorio` | El oficio de respaldo es obligatorio. |
| `ArchivoSolicitudApertura.NombreVacio` | El archivo del oficio debe tener nombre. |
| `ArchivoSolicitudApertura.NombreDemasiadoLargo` | El nombre del archivo del oficio admite hasta 260 caracteres. |
| `ArchivoSolicitudApertura.Vacio` | El archivo del oficio está vacío. |
| `ArchivoSolicitudApertura.DemasiadoGrande(long tamanoMaximoBytes)` | El oficio supera el tamaño máximo de {tamanoMaximoBytes} bytes. |
| `ArchivoSolicitudApertura.NoEsPdf` | El oficio debe ser un archivo PDF. |

`DemasiadoGrande` es un método porque el máximo sale de la configuración; el número se formatea con `CultureInfo.InvariantCulture`.

### Infrastructure

`ArchivoSolicitudAperturaConfiguration.cs`: `ToTable("archivo_solicitud_apertura", "academico")`; `Nombre` con `HasMaxLength(260)`; `Mime` con `HasMaxLength(255).IsUnicode(false)`; `ChecksumSha256` con `HasMaxLength(32).IsFixedLength()`; `ClaveAlmacenamiento` con `HasMaxLength(500)`. `CargadoEn` se lee como UTC con el mismo convertidor.

## 8. Periodos configurados

`Infrastructure/Periodos/PeriodosOptions.cs`: `internal sealed class PeriodosOptions` con `public const string Seccion = "SolicitudesApertura";` y

```csharp
[Required]
[RegularExpression("^[0-9]{6}$")]
public string PeriodoActual { get; set; } = string.Empty;

[Required]
[RegularExpression("^[0-9]{6}$")]
public string PeriodoSiguiente { get; set; } = string.Empty;
```

Se enlaza con `ValidateDataAnnotations()`, `.Validate(o => o.PeriodoActual != o.PeriodoSiguiente, "SolicitudesApertura:PeriodoActual y PeriodoSiguiente deben ser distintos.")` y `ValidateOnStart()`. `Infrastructure/Periodos/PeriodosConfigurados.cs` implementa `IPeriodosConfigurados` leyendo `IOptions<PeriodosOptions>`.

Configuración:
- `docker-compose.yml`, servicio `api`: `SolicitudesApertura__PeriodoActual: "${SGPLA_PERIODO_ACTUAL:-202601}"` y `SolicitudesApertura__PeriodoSiguiente: "${SGPLA_PERIODO_SIGUIENTE:-202651}"`. Son valores **solo de desarrollo**, sin relación con el calendario real de la UV.
- `.env.example`: las dos variables con esos valores y el comentario `# Periodos actual y siguiente para las solicitudes de apertura (claves de periodo_escolar). Valores de desarrollo.`
- Pruebas: `SqlServerFixture` inserta por SQL, después de migrar, los periodos `999800` (actual) y `999801` (siguiente), con fechas fijas válidas, y expone las dos claves como constantes públicas; `SgplaApiFactory` fija `SolicitudesApertura:PeriodoActual` y `PeriodoSiguiente` con ellas. `DatosUnicos.ClavePeriodo` empieza en 100000, así que no choca. Ninguna prueba da de baja esos dos periodos; para `PeriodosNoDisponibles`, la prueba crea un host derivado con `WithWebHostBuilder` y `UseSetting("SolicitudesApertura:PeriodoSiguiente", "999802")`, una clave que no existe.

### Consulta y endpoint

`Application/Periodos/PeriodosConsultas.cs`:

```csharp
internal sealed record PeriodosSolicitudAperturaResponse(PeriodoConfiguradoResponse Actual, PeriodoConfiguradoResponse Siguiente);

/// <summary>Id y fechas solo si el periodo existe activo; si no, <c>null</c>.</summary>
internal sealed record PeriodoConfiguradoResponse(string Clave, int? Id, DateOnly? FechaInicio, DateOnly? FechaFin);

internal sealed record ObtenerPeriodosSolicitudAperturaQuery;
```

`Infrastructure/Periodos/PeriodosConsultas.cs`: `ObtenerPeriodosSolicitudAperturaHandler`, con `IPeriodosConfigurados` e `IPeriodosEscolares.ObtenerActivosPorClaveAsync`.

`Endpoints/Periodos/PeriodoEndpoints.cs`, grupo `/periodos` con tag `Periodos de solicitud`:

| Método y ruta | `WithName` | `WithSummary` | Autorización | Éxito | Errores declarados |
|---|---|---|---|---|---|
| `GET /` | `ObtenerPeriodosSolicitudApertura` | Obtiene el periodo actual y el siguiente configurados para las solicitudes de apertura. | (grupo) | 200 | — |

## 9. Contratos entre módulos

### OfertaEducativa → SolicitudesApertura (nuevos, PR 1, D8)

`OfertaEducativa/Application/Contracts/IExperienciasEducativas.cs`:

```csharp
namespace Sgpla.Modules.OfertaEducativa.Application.Contracts;

/// <summary>Consulta de experiencias educativas para otros módulos (SolicitudesApertura).</summary>
public interface IExperienciasEducativas
{
    /// <summary>
    /// Resumen de cada id que existe, incluidas las EE dadas de baja o con su plan o su programa dados de baja. Los
    /// inexistentes no aparecen. Con una colección vacía no se consulta la base.
    /// </summary>
    Task<IReadOnlyDictionary<int, ExperienciaEducativaResumen>> ObtenerAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken);

    /// <summary>
    /// Ids de las EE que el usuario en curso puede consultar, incluidas las dadas de baja, como consulta componible
    /// sobre la misma base: se ejecuta dentro de la consulta de quien la usa. Con <paramref name="entidadAcademicaId"/>,
    /// solo las de esa entidad, y ninguna si está fuera del ámbito. Desactiva los filtros de baja lógica de sus tablas.
    /// </summary>
    Task<IQueryable<int>> ConsultarIdsVisiblesAsync(int? entidadAcademicaId, CancellationToken cancellationToken);
}

/// <param name="Vigente"><c>true</c> si la EE, su plan y su programa están activos.</param>
public sealed record ExperienciaEducativaResumen(
    int Id,
    string Materia,
    string Curso,
    string Nombre,
    int? CupoMinimo,
    int? CupoMaximo,
    bool Vigente,
    int PlanEstudiosId,
    string PlanEstudiosCodigo,
    int ProgramaEducativoId,
    string ProgramaEducativoNombre,
    int SistemaEducativoId,
    string SistemaEducativoNombre,
    int EntidadAcademicaId,
    string EntidadAcademicaClave,
    string EntidadAcademicaNombre,
    int AreaAcademicaId);
```

`OfertaEducativa/Application/Contracts/IPeriodosEscolares.cs`:

```csharp
namespace Sgpla.Modules.OfertaEducativa.Application.Contracts;

/// <summary>Consulta de periodos escolares para otros módulos (SolicitudesApertura).</summary>
public interface IPeriodosEscolares
{
    /// <summary>Incluye los dados de baja (<c>Activo = false</c>). Los inexistentes no aparecen. Con una colección vacía no se consulta la base.</summary>
    Task<IReadOnlyDictionary<int, PeriodoEscolarResumen>> ObtenerAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken);

    /// <summary>Solo activos: una clave sin periodo activo no aparece. Con una colección vacía no se consulta la base.</summary>
    Task<IReadOnlyDictionary<string, PeriodoEscolarResumen>> ObtenerActivosPorClaveAsync(
        IReadOnlyCollection<string> claves,
        CancellationToken cancellationToken);
}

public sealed record PeriodoEscolarResumen(int Id, string Clave, DateOnly FechaInicio, DateOnly FechaFin, bool Activo);
```

Implementaciones, en `OfertaEducativa/Infrastructure/Contratos/`:
- `ExperienciasEducativas.cs` (`SgplaDbContext`, `IAmbitoOfertaEducativa`, `IAmbitosInstitucionales`, `IClasificacionesAcademicas`):
  - `ObtenerAsync`: una consulta de nivel superior de EE, plan y programa con `IgnoreQueryFilters([FiltrosConsulta.BajaLogica])` en las tres, y después una sola llamada a `IAmbitosInstitucionales.ObtenerEntidadesAsync` (clave, nombre y área de la entidad) y otra a `IClasificacionesAcademicas.ObtenerSistemasEducativosAsync`. `Vigente` no mira la entidad: con un programa activo la entidad está activa, porque su baja se bloquea con programas activos (OFE-D10).
  - `ConsultarIdsVisiblesAsync`: obtiene `EntidadesVisiblesAsync()` (`null` = todas); si llega `entidadAcademicaId` y no es visible, devuelve una consulta vacía; si no, devuelve sin ejecutarla la consulta de ids de EE cuyo programa pertenece a las entidades resultantes, con `IgnoreQueryFilters([FiltrosConsulta.BajaLogica])` en EE, plan y programa.
- `PeriodosEscolares.cs` (`SgplaDbContext`): `ObtenerAsync` con `IgnoreQueryFilters([FiltrosConsulta.BajaLogica])` y `Activo = FechaEliminacion == null`; `ObtenerActivosPorClaveAsync` con el filtro de baja lógica normal.

`OfertaEducativaModule` los registra como `Scoped` junto a `IProgramasDeEntidadAcademica`.

### SolicitudesApertura → OfertaEducativa (existentes, PR 1, D14)

SolicitudesApertura implementa los dos contratos de referencias que OfertaEducativa ya consulta, en `SolicitudesApertura/Infrastructure/Contratos/`:
- `ReferenciasExperienciaEducativaEnSolicitudes : IReferenciasExperienciaEducativa`: `true` si alguna solicitud de esas EE está en `Pendiente` o `Aceptada` (P7). Las bajas de EE y de plan responden 409 `ExperienciaEducativa.TieneReferencias`, que ya existe.
- `ReferenciasPeriodoEscolarEnSolicitudes : IReferenciasPeriodoEscolar`: `true` si el periodo tiene cualquier solicitud, en cualquier estado (P8). La baja responde 409 `PeriodoEscolar.TieneReferencias`, que ya existe.

`SolicitudesAperturaModule` los registra como `Scoped`. OfertaEducativa no cambia.

## 10. Auditoría

Un solo evento, en `BuildingBlocks.Infrastructure/Archivos/AlmacenamientoLocal.cs` (D19):

```csharp
internal static partial class AlmacenamientoLocalLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudo eliminar el archivo {ArchivoClave}; quedó huérfano en el almacenamiento")]
    public static partial void ArchivoHuerfano(this ILogger logger, string archivoClave);
}
```

La clave del almacenamiento es un GUID con su carpeta y no identifica a nadie. Nunca se registran el nombre del archivo, la justificación, los comentarios ni el motivo.

## 11. Composición del módulo

`SolicitudesAperturaModule.AddSolicitudesAperturaModule`:
- `AddPersistenciaModulo` y `AddHandlersModulo` del ensamblado;
- `PeriodosOptions` (sección 8) e `IPeriodosConfigurados` → `PeriodosConfigurados` (`Singleton`);
- `ISolicitudAperturaRepository` → `SolicitudAperturaRepository` (`Scoped`);
- `IAmbitoSolicitudesApertura` → `AmbitoSolicitudesApertura` (`Scoped`);
- `IReferenciasExperienciaEducativa` → `ReferenciasExperienciaEducativaEnSolicitudes` e `IReferenciasPeriodoEscolar` → `ReferenciasPeriodoEscolarEnSolicitudes` (`Scoped`), con el comentario `// Contratos de OfertaEducativa que este módulo implementa (bajas de EE, plan y periodo).`

`MapSolicitudesAperturaEndpoints`: `MapGroup(Ruta).WithTags(...)` pasa a `MapGroup(Ruta).RequireAuthorization(Politicas.Autenticado)`, con `ProducesProblem(401)` y `ProducesProblem(403)` en el grupo, y mapea `MapSolicitudAperturaEndpoints()` y `MapPeriodoEndpoints()`.

`Sgpla.Modules.SolicitudesApertura.csproj` agrega `<InternalsVisibleTo Include="Sgpla.UnitTests" />`. El grafo no cambia: `Modulos.cs` ya permite `SolicitudesApertura → OfertaEducativa`.

## 12. Entregas (PR)

Dos PR en orden, cada uno en una rama nueva desde `origin/develop` y con el CI en verde por sí solo.

### PR 1: alta y consulta de solicitudes de apertura

Rama sugerida: `feat/solicitudes-apertura-alta`.

- **Código:**
  - de la sección 3: el almacenamiento, las políticas y la configuración de `docker-compose.yml` y del `Dockerfile`;
  - la sección 6 salvo lo marcado como PR 2, con el esquema completo (D1 y D2, las únicas ediciones de `baseline.sql` del módulo) y el mapeo de `Version`;
  - las secciones 7 y 8;
  - la sección 9 completa;
  - las secciones 10 y 11.
- **Pruebas unitarias** (`tests/Sgpla.UnitTests/SolicitudesApertura/`):
  - `SolicitudAperturaTests`: `DatosSolicitudApertura.Crear` con cada error y su campo, la sección recortada y en mayúsculas (`" a2 "` → `A2`), `"A 2"` y `"Á2"` inválidas, la justificación con saltos de línea conservados y 2001 caracteres inválidos; `Crear` nace `Pendiente` con actor y fecha; `ValidarCupos` sin cupos, solo con mínimo, solo con máximo y con los dos (bordes inclusivos);
  - `ArchivoSolicitudAperturaTests`: `ValidarOficio` en su orden, el nombre de `C:\fakepath\oficio.pdf` y de `carpeta/oficio.pdf` queda `oficio.pdf`, `DemasiadoGrande` con el número en el mensaje, `application/PDF` aceptado y `TieneFirmaPdf`;
  - `CrearSolicitudAperturaHandlerTests` con fakes escritos a mano (`Fakes.cs`): repositorio, `IExperienciasEducativas`, `IPeriodosEscolares`, `IPeriodosConfigurados`, `IAlmacenamientoArchivos` (en memoria, que registra lo guardado y lo eliminado), `ICurrentUser`, `IUnitOfWork` (que puede lanzar) y `TimeProvider`. Casos: cada error en el orden de la tabla del handler; ningún error guarda el binario; un `SaveChangesAsync` que lanza elimina el binario guardado y relanza la misma excepción;
  - `AlmacenamientoLocalTests` (en `tests/Sgpla.UnitTests/BuildingBlocks/`, sobre una carpeta temporal): guardar devuelve una clave `carpeta/…pdf`, el tamaño y el SHA-256 correctos; abrir una clave inexistente devuelve `null`; `IntentarEliminarAsync` devuelve `true` con un archivo inexistente; una clave con `..` lanza `ArgumentException`.
- **Pruebas de integración** (`tests/Sgpla.IntegrationTests/SolicitudesApertura/`):
  - `EscenarioSolicitud.cs`: sobre `EscenarioOferta`, crea un plan con una EE de cupos 10 a 40 (y otra sin cupos cuando la prueba lo pide), el cliente de Entidad Académica de la entidad y los ids de los periodos fijos `999800` y `999801`;
  - `DatosSolicitudesSql.cs`: inserta por SQL una solicitud en cualquier estado, con su archivo (clave única), para una EE y un periodo dados, respetando los CHECK; el actor es `(SELECT MIN(id) FROM usuarios.usuario)`, y la prueba crea su cuenta antes de llamarlo;
  - `SolicitudAperturaEndpointsTests.cs`:
    - crear 201 con `Location` y el `Response` completo (datos derivados incluidos); el oficio descargado es idéntico byte a byte al enviado, y `checksum_sha256` en la base coincide con el SHA-256 del contenido;
    - crear 400 por cada campo (sección, cantidad, justificación, oficio ausente, vacío, mayor que el máximo, `text/plain` y un `application/pdf` sin `%PDF-`), por EE de otra entidad, por EE dada de baja y por periodo inexistente;
    - crear 409 `PeriodoNoAbierto` con un periodo propio; 409 `PeriodosNoDisponibles` con el host derivado de la sección 8; 409 `CantidadFueraDeCupos` con 9 y con 41 (y 201 con una EE sin cupos); 409 `SeccionDuplicada` con la misma sección en minúsculas, y 201 con esa sección si la anterior está CANCELADA;
    - Superusuario y DGAA reciben 403 al crear;
    - listar y obtener por ámbito: la Entidad Académica solo ve las de su entidad (404 al obtener otra), la DGAA las de su área, el Superusuario todas; cada filtro; orden por `creadaEn` descendente; `estado=OTRO` y `tamanoPagina=101` → 400. Toda aserción sobre el listado se acota a las solicitudes que crea la prueba;
    - descargar: Entidad Académica y DGAA 200 con `Content-Type: application/pdf` y el nombre del archivo; Superusuario 403; fuera del ámbito 404;
  - `PeriodoSolicitudEndpointsTests.cs`: `GET /periodos` devuelve `999800` y `999801` con sus ids y fechas para los tres roles;
  - `ReferenciasSolicitudesAperturaTests.cs`, con solicitudes insertadas por SQL: la baja de una EE y de un plan responde 409 `ExperienciaEducativa.TieneReferencias` con una PENDIENTE o una ACEPTADA, y 204 con una RECHAZADA o una CANCELADA; la baja de un periodo **propio** responde 409 `PeriodoEscolar.TieneReferencias` con una solicitud CANCELADA;
  - `OfertaEducativa/ExperienciasEducativasContratoTests.cs`, contra el contrato resuelto del contenedor de servicios: `ObtenerAsync` con una EE activa (`Vigente = true`), una dada de baja y una de un plan dado de baja (`Vigente = false`), un id inexistente que no aparece y la colección vacía; `ConsultarIdsVisiblesAsync` con cada rol y con una entidad fuera del ámbito;
  - `EsqueletoTests`: `Migraciones_SolicitudAperturaSinVinculacionYConVersion`, que consulta `sys.columns` y verifica que no existen las tres columnas de D1 y que `version` es `rowversion`;
  - `Usuarios/PoliticasTests.cs`: el `Theory` existente agrega `[InlineData(Politicas.EntidadAcademica, Rol.EntidadAcademica)]`, y un caso nuevo verifica que `DgaaOEntidadAcademica` exige el claim `rol` con exactamente los valores `2` y `3`.
- **Documentos:**
  - `DATABASE.md`:
    - §2: `- solicitudes de apertura de grupos académicos y su vinculación posterior con una Programación Académica;` pasa a `- solicitudes de apertura de grupos académicos;`;
    - §16.2: sin las filas `programacion_academica_id`, `vinculada_en` y `vinculada_por_usuario_id`; con la fila `version | rowversion | NOT NULL | Control de concurrencia optimista`; `justificacion` "Texto no vacío; hasta 2000 caracteres en la aplicación"; `comentarios_resolucion` agrega "hasta 2000 caracteres en la aplicación"; en las restricciones, sin "ni programación vinculada", sin "puede tener, opcionalmente, una vinculación completa", sin "ni programación" y sin la línea de la vinculación; en índices, sin el índice único filtrado de `programacion_academica_id`;
    - §16.3: `tamano` "Mayor que cero; el máximo lo fija la configuración (10 MB por omisión)", y el contenido debe empezar con `%PDF-`;
    - §16.4: "puede crear, editar, cancelar o vincular" pasa a "puede crear, editar o cancelar"; "El Superusuario no tiene acceso a este proceso." pasa a "El Superusuario consulta las solicitudes, pero no descarga el oficio ni escribe (`Modulo_SolicitudesApertura.md`, D12)."
    - §16.5 se elimina y §16.6 pasa a ser §16.5, con este texto para su primer párrafo: "La baja de una EE o de un plan se bloquea mientras exista una Solicitud de Apertura PENDIENTE o ACEPTADA de sus EE; el programa y la entidad quedan cubiertos, porque su baja exige hijos dados de baja. La baja de un periodo se bloquea con cualquier Solicitud de Apertura, en cualquier estado (`DECISIONES.md`, OFE-D11)."; en el segundo párrafo, "no impiden la baja" pasa a "no impiden la baja de la EE ni del plan", y se quita "vinculación" de la lista de operaciones;
    - §17: `- definición del almacenamiento externo de binarios (Avisos, Actas, Docentes y Solicitudes de Apertura; el plan de estudios no tiene archivo) y del proceso seguro para confirmar o compensar cargas;` pasa a `- definición del proveedor definitivo del almacenamiento externo de binarios (Avisos, Actas, Docentes y Solicitudes de Apertura; el plan de estudios no tiene archivo). Hoy se usa el sistema de archivos local, con compensación de cargas fallidas (`Modulo_SolicitudesApertura.md`, D3 y D4);`;
  - `DATABASE_DIAGRAM.md`: sin la relación `vincula`, sin `materializa` y sin las tres columnas en `ACADEMICO_SOLICITUD_APERTURA`, con `rowversion version`; en las reglas, sin la línea de la vinculación, y "el Superusuario no participa en este proceso" pasa a "el Superusuario solo consulta";
  - `PLAN_INICIAL.md`: la fila de SolicitudesApertura queda "Crear/editar en PENDIENTE; comandos aceptar/rechazar/cancelar; sin eliminación ni vinculación"; en el árbol, `IAlmacenamientoArchivos` sale de la línea de `Sgpla.BuildingBlocks.Infrastructure/` (queda "almacenamiento local de archivos"); el párrafo de almacenamiento de binarios dice que la interfaz vive en `BuildingBlocks.Application`, que la implementación local usa `Almacenamiento:RutaBase` y `Almacenamiento:TamanoMaximoBytes`, y que el proveedor definitivo sigue pendiente;
  - `ESTANDAR_MODULOS.md`:
    - §2, "Piezas compartidas": `BuildingBlocks.Application` agrega `IAlmacenamientoArchivos`, `ArchivoGuardado` y `ArchivoRecibido`; `BuildingBlocks.Infrastructure` agrega `AddAlmacenamientoArchivos` (implementación local) y `ComoArchivoRecibido`;
    - §5: un contrato puede devolver `IQueryable<int>` de ids cuando el consumidor necesita filtrar en SQL por datos del otro módulo; nunca devuelve entidades ni consultas de tipos internos;
    - §8: los estados se guardan como texto con `HasConversion<string>()` o, si el CHECK los exige en mayúsculas, con un convertidor explícito;
    - §9, "Autorización": la lista de `Politicas` incluye `EntidadAcademica` y `DgaaOEntidadAcademica`;
    - §9: subsección nueva "Archivos": carga multipart con parámetros `[FromForm]` e `IFormFile`, `DisableAntiforgery` con su comentario, `ComoArchivoRecibido`, validación completa antes de guardar el binario, compensación al fallar la base, eliminación del binario reemplazado después de confirmar y descarga con `TypedResults.File`;
  - `pendientes.md`: se borra P7; P8 anota que SolicitudesApertura ya lo implementa y que faltan Integracion y Publicacion;
  - `sgpla-backend/README.md`: tabla nueva "Solicitudes de apertura y archivos" con `SGPLA_PERIODO_ACTUAL`, `SGPLA_PERIODO_SIGUIENTE` y el volumen `archivos-data`, y la nota de que una base local anterior a este PR se recrea con `docker compose down -v` (D1 y D2 editan el baseline).

### PR 2: ciclo de vida de la solicitud

Rama sugerida: `feat/solicitudes-apertura-ciclo`.

- **Código:** de la sección 3, la concurrencia optimista; de la sección 6, lo marcado como PR 2 (métodos, errores, comandos, `EliminarOficio` y las cuatro rutas).
- **Pruebas unitarias:**
  - `SolicitudAperturaTests`: `Modificar`, `Aceptar`, `Rechazar` y `Cancelar` con cada error en su orden, sin asignar nada si fallan, y cada transición desde un estado terminal → `NoPendiente`; `Aceptar` con comentarios vacíos → `null`;
  - handlers de modificar (con y sin oficio; compensación del binario nuevo si la base falla; eliminación del anterior solo después de confirmar), aceptar, rechazar y cancelar, con los fakes del PR 1.
  - `AmbitoSolicitudesAperturaTests`: cada rol con su entidad o su área, otro rol, una cuenta sin ámbito y una EE inexistente; y `BuildingBlocks/ArchivoRecibidoTests` para `LeerEncabezadoAsync`;
- **Pruebas de integración** (`SolicitudAperturaEndpointsTests.cs`):
  - modificar: 204 con y sin oficio (el anterior deja de existir en la base y en la carpeta del almacenamiento); 400 por cada campo; 409 `CantidadFueraDeCupos` y 409 `NoPendiente` fuera de PENDIENTE; 404 para otra entidad; 403 para DGAA y Superusuario;
  - aceptar: 204 con y sin comentarios; 409 `CuposIncompletos` con una EE sin cupo máximo; 409 `CantidadFueraDeCupos` después de reducir los cupos de la EE con la DGAA; 409 `NoPendiente`; 404 para una DGAA de otra área; 403 para Entidad Académica y Superusuario;
  - rechazar: 204; 400 sin comentarios; 409 `NoPendiente`;
  - cancelar: 204; 400 sin motivo y con 1001 caracteres; 409 `NoPendiente`;
  - después de aceptar, la baja de la EE sigue respondiendo 409; después de rechazar o cancelar, responde 204;
  - `ConcurrenciaTests.cs` (en la raíz de `Sgpla.IntegrationTests`, como `ViolacionUnicidadTests`): el manejador recibe una `DbUpdateConcurrencyException` y responde 409 con `codigo` `Persistencia.ModificacionConcurrente`, y no maneja otras excepciones; de punta a punta, un host derivado reemplaza `IUnitOfWork` por un decorador de prueba que, antes de guardar, actualiza por SQL la fila de la solicitud (`SET justificacion = justificacion`, que renueva su `rowversion`), y `POST /{id}/aceptar` responde 409 con ese `codigo`. Las pruebas de integración no ven los tipos internos del módulo, así que no usan su repositorio.
- **Documentos:** `ESTANDAR_MODULOS.md` §2 agrega `ConcurrenciaExceptionHandler` (`DbUpdateConcurrencyException` → 409) a `BuildingBlocks.Infrastructure`.

## 13. Criterios de terminado

Cada PR cumple la lista de verificación de `ESTANDAR_MODULOS.md` §15. Solo el PR 1 edita `baseline.sql` (D1 y D2); ningún PR agrega migraciones ni edita `seed.sql`. La verificación se ejecuta una vez, al terminar la implementación, con `sgpla-backend/scripts/verify.sh` (con `docker compose` detenido), seguida de `sgpla-backend/scripts/smoke.sh`, que deja un token de Superusuario en `sgpla-backend/TestResults/smoke-token.txt`. El login de DGAA y Entidad Académica necesita el LDAP de la UV: lo que dependa de él se reporta como omitido si no hay red.

Prueba de humo por PR, con `TOKEN=$(cat sgpla-backend/TestResults/smoke-token.txt)` y `API=http://localhost:8180`:
- **PR 1:**
  - `POST $API/api/v1/oferta-educativa/periodos-escolares` con `{"clave":"202601","fechaInicio":"2026-02-02","fechaFin":"2026-07-10"}` y con `{"clave":"202651","fechaInicio":"2026-08-10","fechaFin":"2027-01-22"}` → 201 cada uno;
  - `GET $API/api/v1/solicitudes-apertura/periodos` → 200 con los dos ids;
  - `GET $API/api/v1/solicitudes-apertura/solicitudes` → 200 con una página vacía;
  - `POST $API/api/v1/solicitudes-apertura/solicitudes` con `-F seccion=A1` → 403 (Superusuario);
  - `docker compose exec api sh -c 'touch /var/lib/sgpla/archivos/.prueba && rm /var/lib/sgpla/archivos/.prueba'` termina con código 0 (el volumen admite escritura);
  - `sqlcmd` en el contenedor `sqlserver`: `SELECT name FROM sys.columns WHERE object_id = OBJECT_ID('academico.solicitud_apertura')` no lista `programacion_academica_id` y sí `version`;
  - crear una solicitud con una cuenta de Entidad Académica: omitido si no hay red hacia el LDAP de la UV.
- **PR 2:** `POST $API/api/v1/solicitudes-apertura/solicitudes/1/aceptar` con el token de Superusuario → 403; el ciclo completo con DGAA y Entidad Académica, omitido si no hay red hacia el LDAP de la UV.

Todo debe terminar sin advertencias ni fallos. Si una prueba falla por una razón ajena al PR, se reporta: no se modifica la prueba para ocultarla.
