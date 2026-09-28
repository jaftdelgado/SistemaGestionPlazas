# Módulo Usuarios

Especificación completa del módulo Usuarios. Es normativa: quien lo implemente no debe decidir nada que no esté escrito aquí. Si algo falta o se contradice, se detiene la implementación y se aclara en este documento antes de programar.

Documentos relacionados:
- `ESTANDAR_MODULOS.md`: cómo se implementa cualquier módulo. Todo lo que este documento no dice explícitamente se hace como indica el estándar.
- `DATABASE.md`: §6.16 a §6.20, §9.3, §10, §13 y §14 ("Usuarios y autenticación") describen las tablas y reglas de este módulo. Este documento registra dónde se desvía de ellas.
- `Modulo_Institucional.md`: el módulo del que depende Usuarios, y cuya autorización se completa aquí (su decisión D7).
- `pendientes.md`: P3 a P6 se resuelven en este módulo.
- Referencia de implementación: Institucional (`src/Modules/Institucional`) para comandos, consultas paginadas con filtros y contratos entre módulos.

## Contenido

1. [Alcance](#1-alcance)
2. [Decisiones y desviaciones](#2-decisiones-y-desviaciones)
3. [Piezas compartidas](#3-piezas-compartidas)
4. [Configuración, host e imagen](#4-configuración-host-e-imagen)
5. [Dominio](#5-dominio)
6. [Autenticación y sesión](#6-autenticación-y-sesión)
7. [Administración de cuentas](#7-administración-de-cuentas)
8. [Bootstrap del primer Superusuario](#8-bootstrap-del-primer-superusuario)
9. [Contratos entre módulos](#9-contratos-entre-módulos)
10. [Autorización del resto del sistema](#10-autorización-del-resto-del-sistema)
11. [Auditoría](#11-auditoría)
12. [Composición del módulo](#12-composición-del-módulo)
13. [Entregas (PR)](#13-entregas-pr)
14. [Criterios de terminado](#14-criterios-de-terminado)

## 1. Alcance

| Recurso u operación | Tablas | Operaciones |
|---|---|---|
| Rol | `usuarios.rol` | Ninguna por HTTP. Es un catálogo fijo que ya carga la semilla; el código lo representa con el enum `Rol` (sección 3) |
| Sesión | `usuario`, `usuario_dgaa`, `usuario_entidad_academica`, `credencial_superusuario` | Iniciar sesión, consultar la sesión, cambiar la contraseña propia y cerrar sesión |
| Cuenta | Las mismas cuatro | Listar paginado con filtros, obtener, registrar, editar el nombre, restablecer la contraseña de un Superusuario y dar de baja |
| Bootstrap | `usuario`, `credencial_superusuario` | Subcomando `bootstrap-superusuario` de la imagen de la API |
| Autorización | — | JWT, políticas, `ICurrentUser` y la protección de Institucional y Catalogos (P3 a P6) |

Fuera de alcance:
- restauración de cuentas (D3);
- refresh tokens, MFA, recuperación por correo e invalidación de sesiones en el servidor (D4);
- rate limiting, que `DATABASE.md` §13.4 acepta como riesgo;
- las políticas `Dgaa` y `EntidadAcademica`, que se agregan con el primer módulo que las use (D13).

## 2. Decisiones y desviaciones

| # | Decisión | Desviación respecto a |
|---|---|---|
| D1 | Un **solo endpoint de login**, `POST /api/v1/usuarios/iniciar-sesion`. Decide el mecanismo según el rol de la cuenta: LDAP para DGAA y Entidad Académica, y la contraseña local Argon2id para el Superusuario. Si el texto recibido no trae `@`, se completa con `@uv.mx` (solo en el login, no en el alta) | — |
| D2 | **LDAP con bind directo** usando el correo como identidad (`correo@uv.mx`), sin base DN, búsqueda ni cuenta de servicio, igual que el sistema anterior de la UV. La seguridad del canal es configurable: `Ldaps` (por omisión), `StartTls` o `SinTls`. `SinTls` solo se acepta en el entorno Development, y la API no arranca si se configura en otro entorno | `DATABASE.md` §13.3 ("mediante TLS", salvo en Development). `PLAN_INICIAL.md` (base DN y formato de bind en `LdapOptions`) |
| D3 | Las cuentas **no se restauran**, como en Institucional. Una cuenta dada de baja no existe para la API: `GET`, `PUT`, `DELETE` y restablecer responden 404. El correo queda libre para una cuenta nueva | `DATABASE.md` §6.17 (nota de restauración), §9.3, §13.2 (reactivar un Superusuario) y §14 (casos de restauración). `PLAN_INICIAL.md` (tabla de módulos) |
| D4 | La sesión es un **JWT HS256 de 8 horas, sin refresh**. En cada petición se consulta la base: es la fuente del estado vigente (cuenta activa, ámbito activo y contraseña pendiente). **Cerrar sesión solo tiene efecto en el cliente**: un token copiado sigue sirviendo hasta que expira, salvo que la cuenta o su ámbito se den de baja | — (§13.5 ya deja sesiones y refresh tokens fuera del modelo) |
| D5 | Las fallas del login usan **mensajes específicos**: 401 con un `codigo` distinto para una cuenta no registrada, un ámbito inactivo o una contraseña incorrecta, y 503 si el LDAP no responde. Se acepta que así se puede averiguar qué correos tienen cuenta | — |
| D6 | La **contraseña temporal la genera la API**: 12 caracteres aleatorios que cumplen la política. Se devuelve una sola vez, en el alta y en el restablecimiento de un Superusuario, y nunca vuelve a mostrarse | — |
| D7 | El primer Superusuario se crea con el **subcomando `bootstrap-superusuario` de la imagen de la API**, no del CLI de migraciones. Su contraseña también es temporal | `PLAN_INICIAL.md` ("subcomando reservado en la CLI") |
| D8 | Solo el Superusuario administra cuentas, y **nunca la suya**: no puede darse de baja ni restablecer su propia contraseña (409). Para cambiarla usa "cambiar mi contraseña". Solo se edita el nombre. DGAA y Entidad Académica no consultan cuentas | — |
| D9 | La contraseña nueva **no puede ser igual a la actual**. Es la única comparación con valores anteriores: no hay historial | `DATABASE.md` §13.4 lo permitía (solo "sin historial") |
| D10 | `ICurrentUser` vive en `BuildingBlocks.Application` y lo **implementa el módulo Usuarios**, no el host. Usuarios también registra JwtBearer, las políticas y la verificación por petición, porque consultan sus tablas; el host solo agrega `UseAuthentication` y `UseAuthorization` | `PLAN_INICIAL.md` (`ICurrentUser` en SharedKernel, "los implementa el host") |
| D11 | Las pruebas de integración usan **tokens reales** emitidos por la API, no un `TestAuthHandler`. Para reemplazar el adaptador LDAP por un falso, Usuarios declara `InternalsVisibleTo` también para `Sgpla.IntegrationTests` | `PLAN_INICIAL.md` (`TestAuthHandler`). `ESTANDAR_MODULOS.md` §5 (solo `Sgpla.UnitTests`) |
| D12 | Los eventos de autenticación de §13.5 se emiten como **logs estructurados** con `[LoggerMessage]` y `EventId` fijos. Se exportarán cuando el host configure la telemetría | `DATABASE.md` §13.5 ("telemetría externa"): el destino queda para el despliegue |
| D13 | Las políticas de hoy son `SesionIniciada`, `Autenticado` y `Superusuario`. `Dgaa` y `EntidadAcademica` se crean cuando un endpoint las necesite; el filtro por ámbito de P5 se aplica dentro de los handlers | `PLAN_INICIAL.md` (cuatro políticas, incluida `CambioContrasenaPendiente`, que aquí es un requisito de `Autenticado`) |
| D14 | `ErrorType` agrega `Unauthorized` (401) y `Unavailable` (503). `Normalizacion` sale del dominio de Institucional y pasa a SharedKernel, pública, para que la usen ambos módulos | `ESTANDAR_MODULOS.md` §2 y §9 ("Errores") |

## 3. Piezas compartidas

Se agregan en el PR 1 (sección 13).

### SharedKernel

`src/BuildingBlocks/Sgpla.SharedKernel/Rol.cs`:

```csharp
namespace Sgpla.SharedKernel;

/// <summary>Roles fijos de DATABASE.md §6.16. Los valores son los ids de <c>usuarios.rol</c>; no son configurables.</summary>
public enum Rol : byte
{
    Superusuario = 1,
    Dgaa = 2,
    EntidadAcademica = 3,
}
```

`src/BuildingBlocks/Sgpla.SharedKernel/Normalizacion.cs`: la clase de `Institucional/Domain/Comun/Normalizacion.cs` se mueve sin cambios de comportamiento. Pasa a `public static partial class Normalizacion` en el namespace `Sgpla.SharedKernel`. Se borra la carpeta `Domain/Comun` de Institucional, y sus usos solo cambian el `using`.

`Error.cs`:
- `ErrorType` agrega `Unauthorized` y `Unavailable`.
- `Error` agrega las fábricas `Error.Unauthorized(code, message)` y `Error.Unavailable(code, message)`.

### BuildingBlocks.Application

`src/BuildingBlocks/Sgpla.BuildingBlocks.Application/Autorizacion.cs`:

```csharp
namespace Sgpla.BuildingBlocks.Application;

/// <summary>Usuario de la petición en curso, ya verificado contra la base (Modulo_Usuarios.md §6).</summary>
public interface ICurrentUser
{
    int Id { get; }

    Rol Rol { get; }

    /// <summary>Solo para <see cref="Rol.Dgaa"/>.</summary>
    int? AreaAcademicaId { get; }

    /// <summary>Solo para <see cref="Rol.EntidadAcademica"/>.</summary>
    int? EntidadAcademicaId { get; }
}

/// <summary>Nombres de las políticas de autorización. Se usan con <c>RequireAuthorization</c>.</summary>
public static class Politicas
{
    /// <summary>Cualquier sesión válida, aunque tenga la contraseña pendiente de cambio.</summary>
    public const string SesionIniciada = nameof(SesionIniciada);

    /// <summary>Sesión válida y sin contraseña pendiente de cambio.</summary>
    public const string Autenticado = nameof(Autenticado);

    /// <summary><see cref="Autenticado"/> con el rol Superusuario.</summary>
    public const string Superusuario = nameof(Superusuario);
}
```

Leer `ICurrentUser` en una petición sin sesión lanza `InvalidOperationException`. Eso es un error de programación: la ruta debió exigir una política.

### BuildingBlocks.Infrastructure

`ErrorHttpExtensions.ToProblem` traduce los nuevos tipos:

| `ErrorType` | HTTP | `title` |
|---|---|---|
| `Unauthorized` | 401 | No autenticado |
| `Unavailable` | 503 | Servicio no disponible |

`ESTANDAR_MODULOS.md` §9 ("Errores") agrega las dos filas: `Unauthorized` es para credenciales rechazadas y `Unavailable` para un servicio externo caído.

## 4. Configuración, host e imagen

### Opciones

Cada sección se enlaza a una clase `<Nombre>Options` interna de Usuarios, con `ValidateOnStart`.

| Sección | Clave | Valor | Regla |
|---|---|---|---|
| `Jwt` | `Emisor` | `sgpla` | Obligatorio |
| | `Audiencia` | `sgpla-web` | Obligatorio |
| | `Clave` | Secreto | Al menos 32 bytes en UTF-8. **Nunca** va en `appsettings*.json`: se toma de la variable `Jwt__Clave` o de un secreto |
| | `DuracionHoras` | `8` | De 1 a 24 |
| `Argon2` | `MemoriaKib` | `19456` (19 MiB) | Al menos 19456 |
| | `Iteraciones` | `2` | Al menos 2 |
| | `Paralelismo` | `1` | Al menos 1 |
| `Ldap` | `Servidor` | Host o IP | Obligatorio |
| | `Puerto` | `636` | De 1 a 65535 |
| | `Seguridad` | `Ldaps` | `Ldaps`, `StartTls` o `SinTls`. `SinTls` solo en Development (D2) |
| | `TiempoEsperaSegundos` | `10` | De 1 a 60 |

Los valores de la columna "Valor" van en `appsettings.json`, salvo `Clave` y `Servidor`, que siempre llegan por el entorno.

La API no arranca si falta la clave o si alguna regla falla.

### docker compose y `.env`

- `.env.example` agrega:
  - `SGPLA_JWT_CLAVE`, con un valor de desarrollo de 32 caracteres o más y la advertencia de no usarlo fuera de desarrollo;
  - `SGPLA_LDAP_SERVIDOR=148.226.12.10`, `SGPLA_LDAP_PUERTO=389` y `SGPLA_LDAP_SEGURIDAD=SinTls`, que son los valores actuales de la UV y solo sirven en Development;
  - `SGPLA_BOOTSTRAP_CORREO` y `SGPLA_BOOTSTRAP_NOMBRE`, vacíos.
- El servicio `api` de `docker-compose.yml` los pasa como `Jwt__Clave`, `Ldap__Servidor`, `Ldap__Puerto`, `Ldap__Seguridad`, `SGPLA_BOOTSTRAP_CORREO` y `SGPLA_BOOTSTRAP_NOMBRE`.

### `Program.cs`

- Después de `UseCors`, agrega `app.UseAuthentication()` y `app.UseAuthorization()`.
- Después de `builder.Build()` y antes de configurar el pipeline, detecta el subcomando: si `args` es exactamente `["bootstrap-superusuario"]`, termina con el código que devuelva `UsuariosModule.EjecutarBootstrapAsync(app.Services, Console.Out, CancellationToken.None)` y no arranca el servidor web (sección 8).
- OpenAPI declara el esquema de seguridad Bearer mediante un transformador de documento, para que Scalar pueda enviar el token. No cambia nada más del host.

### Imagen de la API

`src/Sgpla.Api/Dockerfile` instala la biblioteca nativa de OpenLDAP que necesita `System.DirectoryServices.Protocols` en Linux. Se instala en la etapa `final`, antes de `USER $APP_UID`, con `apt-get install -y --no-install-recommends` y limpiando `/var/lib/apt/lists`. El sistema anterior usa el paquete `libldap2`; hay que confirmar el nombre en la distribución de `aspnet:10.0` y que el adaptador cargue `libldap` dentro del contenedor.

### Paquetes

Se agregan a `Directory.Packages.props`, con la última versión estable compatible con .NET 10:
- `Microsoft.AspNetCore.Authentication.JwtBearer`;
- `Isopoh.Cryptography.Argon2`;
- `System.DirectoryServices.Protocols`.

## 5. Dominio

`Domain/Cuentas/`. La carpeta no se llama `Usuarios`, para no repetir el nombre del módulo en el namespace.

### `Usuario`

`internal sealed class Usuario : Entity, IEliminable`.

| Propiedad | Tipo | Columna | Constante | Mutable |
|---|---|---|---|---|
| `Correo` | `string` | `correo` | `LongitudMaximaCorreo = 254` | No |
| `Nombre` | `string` | `nombre` | `LongitudMaximaNombre = 200` | Sí (`CambiarNombre`) |
| `Rol` | `Rol` | `rol_id` | — | No |
| `PerfilDgaa` | `PerfilDgaa?` | `usuario_dgaa` | — | No |
| `PerfilEntidadAcademica` | `PerfilEntidadAcademica?` | `usuario_entidad_academica` | — | No |
| `Credencial` | `CredencialSuperusuario?` | `credencial_superusuario` | — | Solo con los métodos de contraseña |
| `FechaEliminacion` | `DateTime?` | `fecha_eliminacion` | — | Solo con `DarDeBaja` |

Derivadas, sin columna:
- `AreaAcademicaId => PerfilDgaa?.AreaAcademicaId`;
- `EntidadAcademicaId => PerfilEntidadAcademica?.EntidadAcademicaId`;
- `CambioContrasenaPendiente => Credencial is { FechaActualizacion: null }`.

Los perfiles y la credencial son partes del mismo agregado. Son clases `internal sealed` con constructor privado y `private set`, sin `Id` propio:
- `PerfilDgaa { int AreaAcademicaId }`;
- `PerfilEntidadAcademica { int EntidadAcademicaId }`;
- `CredencialSuperusuario { string Contrasena; DateTime? FechaActualizacion; DateTime? FechaEliminacion }`, con `LongitudMaximaContrasena = 500`.

Las fábricas garantizan la invariante de §13.1: exactamente la parte que corresponde al rol, y ninguna otra.

Fábricas:
- `static Result<Usuario> CrearSuperusuario(string correo, string nombre, string verificadorTemporal)`: crea la credencial con `FechaActualizacion = null`.
- `static Result<Usuario> CrearDgaa(string correo, string nombre, int areaAcademicaId)`.
- `static Result<Usuario> CrearEntidadAcademica(string correo, string nombre, int entidadAcademicaId)`.

Las tres validan en este orden: correo y luego nombre. Las de DGAA y Entidad Académica exigen además el dominio institucional. Los ids de ámbito no se validan aquí: su existencia la comprueba el handler.

Métodos:

| Método | Efecto |
|---|---|
| `Result CambiarNombre(string nombre)` | Valida y normaliza como en la fábrica. Si falla, la entidad no cambia |
| `void DarDeBaja(DateTime utc)` | `FechaEliminacion ??= utc`, y si hay credencial, `Credencial.FechaEliminacion ??= utc`: el mismo instante en las dos tablas (§13.2) |
| `void EstablecerContrasena(string verificador, DateTime utc)` | Cambio hecho por el propio Superusuario: reemplaza `Contrasena` y asigna `FechaActualizacion = utc` |
| `void RestablecerContrasena(string verificadorTemporal)` | Reemplaza `Contrasena` y regresa `FechaActualizacion` a `null` |
| `void ActualizarVerificador(string verificador)` | Rehash después de un acceso exitoso: reemplaza `Contrasena` y conserva `FechaActualizacion` |

Los tres métodos de contraseña lanzan `InvalidOperationException` si la cuenta no es de Superusuario. Es un error de programación, porque los handlers lo comprueban antes.

Normalización:

| Dato | Normalización | Reglas, en orden |
|---|---|---|
| Correo | `Normalizacion.Recortar` y `ToLowerInvariant()` | No vacío. Hasta 254 caracteres. Formato: solo caracteres ASCII visibles (de `!` a `~`), exactamente una `@`, al menos un carácter antes de ella y un dominio con un punto que tenga caracteres a ambos lados (refleja `ck_usuario__correo_formato`). Para DGAA y Entidad Académica, además, termina en `@uv.mx` y tiene al menos un carácter antes (refleja `ck_usuario__correo_institucional`) |
| Nombre | `Normalizacion.Texto` | No vacío. Hasta 200 caracteres |

### `PoliticaContrasena`

`Domain/Cuentas/PoliticaContrasena.cs`: `internal static class` con `Result Validar(string contrasena)`. Aplica la política de §13.4 **sin normalizar**: los espacios cuentan.
1. La longitud debe estar entre 8 y 128 caracteres.
2. Debe tener al menos una mayúscula (`char.IsUpper`), una minúscula (`char.IsLower`), un dígito ASCII del `0` al `9` y un símbolo (un carácter que no sea letra, dígito ni espacio en blanco).

Sus errores se reportan en el campo `ContrasenaNueva`, porque solo la usa el cambio de contraseña. Las temporales las genera el sistema, y una prueba unitaria verifica que siempre la cumplan.

### `UsuarioErrors`

Cada mensaje con una longitud se construye con la constante correspondiente.

| Código | Tipo | Campo | Mensaje |
|---|---|---|---|
| `Usuario.CorreoVacio` | Validation | `Correo` | El correo es obligatorio. |
| `Usuario.CorreoDemasiadoLargo` | Validation | `Correo` | El correo admite hasta 254 caracteres. |
| `Usuario.CorreoFormatoInvalido` | Validation | `Correo` | El correo no tiene un formato válido. |
| `Usuario.CorreoNoInstitucional` | Validation | `Correo` | Las cuentas DGAA y Entidad Académica requieren un correo @uv.mx. |
| `Usuario.NombreVacio` | Validation | `Nombre` | El nombre es obligatorio. |
| `Usuario.NombreDemasiadoLargo` | Validation | `Nombre` | El nombre admite hasta 200 caracteres. |
| `Usuario.AreaAcademicaInexistente` | Validation | `AreaAcademicaId` | No existe un área académica activa con ese id. |
| `Usuario.EntidadAcademicaInexistente` | Validation | `EntidadAcademicaId` | No existe una entidad académica activa con ese id. |
| `Usuario.ContrasenaActualIncorrecta` | Validation | `ContrasenaActual` | La contraseña actual no es correcta. |
| `Usuario.ContrasenaLongitudInvalida` | Validation | `ContrasenaNueva` | La contraseña debe tener entre 8 y 128 caracteres. |
| `Usuario.ContrasenaDebil` | Validation | `ContrasenaNueva` | La contraseña debe incluir al menos una mayúscula, una minúscula, un número y un símbolo. |
| `Usuario.ContrasenaNuevaIgualActual` | Validation | `ContrasenaNueva` | La contraseña nueva debe ser distinta de la actual. |
| `Usuario.CuentaNoRegistrada` | Unauthorized | — | La cuenta no está registrada en el sistema. |
| `Usuario.AmbitoInactivo` | Unauthorized | — | El área o la entidad académica de la cuenta está dada de baja. |
| `Usuario.CredencialesInvalidas` | Unauthorized | — | La contraseña es incorrecta. |
| `Usuario.LdapNoDisponible` | Unavailable | — | El servicio de autenticación de la UV no está disponible. Intenta más tarde. |
| `Usuario.CorreoDuplicado` | Conflict | — | Ya existe una cuenta activa con ese correo. |
| `Usuario.BajaPropia` | Conflict | — | No puedes dar de baja tu propia cuenta. |
| `Usuario.UltimoSuperusuario` | Conflict | — | No se puede dar de baja al último Superusuario activo. |
| `Usuario.RestablecimientoPropio` | Conflict | — | No puedes restablecer tu propia contraseña; usa el cambio de contraseña. |
| `Usuario.RestablecimientoNoAplica` | Conflict | — | Solo se restablece la contraseña de un Superusuario; las cuentas UV usan su contraseña institucional. |
| `Usuario.CambioContrasenaNoAplica` | Conflict | — | Las cuentas UV cambian su contraseña en los servicios de la UV. |
| `Usuario.SuperusuarioExistente` | Conflict | — | Ya existe un Superusuario activo; no se creó nada. |
| `Usuario.NoEncontrado(int id)` | NotFound | — | No existe la cuenta {id}. |

`Domain/Cuentas/NombresRol.cs` asocia cada `Rol` con su nombre (`Superusuario`, `DGAA` y `Entidad Académica`), exactamente como en la semilla. Lo usan las respuestas, así que no hace falta consultar `usuarios.rol`.

## 6. Autenticación y sesión

### Puertos (`Application/Autenticacion/`)

```csharp
internal enum VerificacionContrasena { Incorrecta, Correcta, CorrectaRequiereRehash }

internal interface IHasherContrasenas
{
    /// <summary>Cadena PHC de Argon2id con una sal aleatoria de 16 bytes.</summary>
    string Hashear(string contrasena);

    /// <summary>Comparación en tiempo constante. Pide rehash si algún parámetro de la cadena está por debajo de la configuración.</summary>
    VerificacionContrasena Verificar(string verificador, string contrasena);
}

internal interface IGeneradorContrasenas
{
    /// <summary>12 caracteres que cumplen <see cref="PoliticaContrasena"/>.</summary>
    string GenerarTemporal();
}

internal enum ResultadoLdap { Autenticado, CredencialesInvalidas, NoDisponible }

internal interface ILdapAutenticador
{
    Task<ResultadoLdap> AutenticarAsync(string correo, string contrasena, CancellationToken cancellationToken);
}

internal sealed record TokenEmitido(string Token, DateTime ExpiraEn);

internal interface IEmisorTokens
{
    TokenEmitido Emitir(Usuario usuario);
}
```

`IUsuarioRepository` (`Application/Cuentas/IUsuarioRepository.cs`). Todos sus métodos ven solo cuentas activas, gracias al filtro de baja lógica:

```csharp
internal interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Compara el correo ya normalizado; la columna ignora mayúsculas y acentos.</summary>
    Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken cancellationToken);

    Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancellationToken);

    Task<int> ContarSuperusuariosAsync(CancellationToken cancellationToken);

    void Agregar(Usuario usuario);
}
```

A diferencia de las claves de Institucional, la unicidad del correo solo considera las cuentas activas (`ux_usuario__correo_activo` es un índice filtrado), así que `ExisteCorreoAsync` **no** usa `IgnoreQueryFilters`.

### Adaptadores (`Infrastructure/Autenticacion/`)

- **`HasherArgon2`**:
  - usa `Isopoh.Cryptography.Argon2` con `Argon2Type.HybridAddressing` (Argon2id), versión 19, hash de 32 bytes, sal de 16 bytes de `RandomNumberGenerator` y los parámetros de `Argon2Options`;
  - `Verificar` lee `m`, `t` y `p` de la cadena PHC, y responde `CorrectaRequiereRehash` si alguno es menor que la configuración;
  - una cadena ilegible cuenta como `Incorrecta`.
- **`GeneradorContrasenas`**:
  - usa cuatro alfabetos sin caracteres ambiguos: mayúsculas sin `I` ni `O`, minúsculas sin `l`, dígitos del `2` al `9` y los símbolos `!@#$%&*?-_=+`;
  - toma un carácter de cada alfabeto, completa hasta 12 con la unión de los cuatro y mezcla el resultado;
  - toda elección usa `RandomNumberGenerator.GetInt32`.
- **`LdapAutenticador`**:
  - usa `LdapConnection` con `LdapDirectoryIdentifier(Servidor, Puerto)`, `AuthType.Basic`, protocolo versión 3, `ReferralChasing` desactivado y el tiempo de espera configurado;
  - según `Seguridad`: con `Ldaps` activa `SecureSocketLayer`; con `StartTls` llama a `StartTransportLayerSecurity` antes del bind; con `SinTls` no hace nada;
  - hace el bind con `NetworkCredential(correo, contrasena)`. Como la biblioteca no tiene bind asíncrono, se ejecuta con `Task.Run`;
  - un `LdapException` con código 49 se traduce a `CredencialesInvalidas`; cualquier otro `LdapException`, o un tiempo de espera agotado, a `NoDisponible`;
  - nunca registra la contraseña ni el correo; solo el código de error LDAP.
  - Una contraseña vacía nunca llega al adaptador, porque el validator del login la rechaza antes. El bind con contraseña vacía sería anónimo y "exitoso".
- **`EmisorTokens`**:
  - usa `JsonWebTokenHandler` con HS256 y la clave de `JwtOptions`;
  - los claims son `sub` (id), `rol` (el número del rol), `area_academica_id` o `entidad_academica_id` según el rol, `jti`, `iat`, `iss`, `aud` y `exp`, que es el instante actual de `TimeProvider` más `DuracionHoras`;
  - `ExpiraEn` es ese mismo instante, en UTC;
  - el token no lleva el estado de la contraseña: la base es la fuente (D4).

### Verificación en cada petición (`Infrastructure/Autenticacion/`)

`AddUsuariosModule` registra `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)`, con esta configuración:
- `MapInboundClaims = false`;
- valida emisor, audiencia, firma y expiración, con un `ClockSkew` de 1 minuto.

Y con estos eventos:
- **`OnTokenValidated`** consulta la cuenta por `sub` con `AsNoTracking` (el filtro de baja lógica excluye las dadas de baja) y proyecta el rol, el ámbito, `Credencial.FechaActualizacion` y `Credencial.FechaEliminacion`. Falla (`context.Fail`) en cualquiera de estos casos:
  1. la cuenta no existe;
  2. el rol de la base no coincide con el claim;
  3. un Superusuario tiene la credencial dada de baja;
  4. un DGAA o una Entidad Académica tiene el ámbito inactivo, según `IAmbitosInstitucionales` (sección 9).

  Si la contraseña está pendiente, agrega a la identidad el claim `cambio_contrasena_pendiente = true`.
- **`OnChallenge`** escribe con `IProblemDetailsService` un 401 con `codigo` `Autenticacion.NoAutenticado` y `detail` "Se requiere iniciar sesión.", y llama a `HandleResponse()`. Cubre la falta de token, un token inválido o vencido y una verificación fallida.

`UsuarioActual : ICurrentUser` (registrado como `Scoped`, junto con `AddHttpContextAccessor`) lee `sub`, `rol` y el ámbito de `HttpContext.User`.

### Políticas y respuestas 403

| Política | Requisitos |
|---|---|
| `SesionIniciada` | Usuario autenticado |
| `Autenticado` | Usuario autenticado, sin el claim `cambio_contrasena_pendiente` |
| `Superusuario` | `Autenticado`, con `rol = 1` |

El requisito "sin contraseña pendiente" es una clase propia (`SinCambioPendienteRequirement` con su handler), para poder distinguirlo al responder.

Un `IAuthorizationMiddlewareResultHandler` propio responde un 403 con ProblemDetails:
- si falló `SinCambioPendienteRequirement`, el código es `Autenticacion.CambioContrasenaPendiente` y el detalle, "Debes cambiar tu contraseña temporal antes de continuar.";
- en cualquier otro caso, el código es `Autorizacion.SinPermiso` y el detalle, "No tienes permiso para realizar esta operación.".

Todos los demás resultados se delegan al handler por omisión. Las respuestas 401 y 403 llevan `traceId`, como cualquier ProblemDetails.

### Endpoints de sesión

`Endpoints/Sesion/SesionEndpoints.cs`, colgados del grupo del módulo `/api/v1/usuarios`, con el tag `Sesión`:

| Método y ruta | `WithName` | `WithSummary` | Autorización | Éxito | Errores declarados |
|---|---|---|---|---|---|
| `POST /iniciar-sesion` | `IniciarSesion` | Inicia sesión con el correo (o el usuario UV) y la contraseña. | `AllowAnonymous` | 200 `SesionResponse` | 400, 401, 503 |
| `GET /sesion` | `ObtenerSesion` | Obtiene la cuenta de la sesión en curso. | `SesionIniciada` | 200 `UsuarioSesionResponse` | 401 |
| `POST /sesion/cambiar-contrasena` | `CambiarContrasena` | Cambia la contraseña del Superusuario en sesión y emite un token nuevo. | `SesionIniciada` | 200 `SesionResponse` | 400, 401, 409 |
| `POST /cerrar-sesion` | `CerrarSesion` | Cierra la sesión; el token se descarta en el cliente. | `SesionIniciada` | 204 | 401 |

Respuestas (`Application/Sesion/SesionConsultas.cs`):

```csharp
internal sealed record RolResponse(byte Id, string Nombre);

internal sealed record AreaAcademicaResumenResponse(int Id, int Clave, string Nombre);

internal sealed record EntidadAcademicaResumenResponse(int Id, string Clave, string Nombre);

internal sealed record UsuarioSesionResponse(
    int Id,
    string Correo,
    string Nombre,
    RolResponse Rol,
    AreaAcademicaResumenResponse? AreaAcademica,
    EntidadAcademicaResumenResponse? EntidadAcademica,
    bool CambioContrasenaPendiente);

internal sealed record SesionResponse(string Token, DateTime ExpiraEn, UsuarioSesionResponse Usuario);

/// <summary>La cuenta activa <paramref name="UsuarioId"/> con su ámbito. 404 si no existe.</summary>
internal sealed record ObtenerUsuarioSesionQuery(int UsuarioId);
```

`ObtenerUsuarioSesionHandler` (en `Infrastructure/Sesion/SesionConsultas.cs`) sigue estos pasos:
1. proyecta la cuenta;
2. obtiene el nombre del área o la entidad con una sola llamada a `IAmbitosInstitucionales`;
3. arma la respuesta.

La usan tres endpoints:
- `GET /sesion` le pasa el id de `ICurrentUser`;
- iniciar sesión le pasa el id devuelto por el comando;
- cambiar la contraseña hace lo mismo que iniciar sesión.

Así un comando no arma respuestas que dependen de otro módulo, igual que el `POST` de entidades en Institucional.

Ejemplo de respuesta de iniciar sesión:

```json
{
  "token": "eyJhbGciOi...",
  "expiraEn": "2026-09-27T23:15:00Z",
  "usuario": {
    "id": 12,
    "correo": "jperez@uv.mx",
    "nombre": "Juan Pérez",
    "rol": { "id": 2, "nombre": "DGAA" },
    "areaAcademica": { "id": 3, "clave": 30, "nombre": "Área Académica de Humanidades" },
    "entidadAcademica": null,
    "cambioContrasenaPendiente": false
  }
}
```

### Comando `IniciarSesion`

`Application/Sesion/IniciarSesion.cs`:
- El comando es `IniciarSesionCommand(string Correo, string Contrasena)`.
- El resultado es `ICommandHandler<IniciarSesionCommand, SesionIniciada>`, con `internal sealed record SesionIniciada(int UsuarioId, TokenEmitido Token)`.
- `IniciarSesionValidator` rechaza un `Correo` vacío o solo con espacios (nombre legible: "correo") y una `Contrasena` vacía ("contraseña"). La contraseña no se recorta.

Dependencias del handler:
- `IUsuarioRepository`, `IAmbitosInstitucionales`, `ILdapAutenticador`, `IHasherContrasenas`, `IEmisorTokens`, `IUnitOfWork` y `ILogger`.

El handler sigue el orden de §13.3:
1. **Normaliza el correo:** lo recorta y lo pasa a minúsculas. Si no contiene `@`, le agrega `@uv.mx` (D1).
2. **Busca la cuenta:** `ObtenerPorCorreoAsync`. Si no hay una cuenta activa, responde `CuentaNoRegistrada`.
3. **Si es DGAA o Entidad Académica:**
   - comprueba el ámbito con `AreaAcademicaActivaAsync` o `EntidadAcademicaActivaAsync`; si no está activo, responde `AmbitoInactivo`;
   - llama a `AutenticarAsync`. Con `CredencialesInvalidas` responde `CredencialesInvalidas`, y con `NoDisponible`, `LdapNoDisponible`.
4. **Si es Superusuario:**
   - si la credencial está dada de baja, responde `CuentaNoRegistrada`;
   - `Verificar`: con `Incorrecta` responde `CredencialesInvalidas`; con `CorrectaRequiereRehash` llama a `ActualizarVerificador(Hashear(contrasena))` y a `SaveChangesAsync`.
5. **Emite el token** y devuelve `SesionIniciada`.

Cada salida registra su evento de auditoría (sección 11). La contraseña se descarta al terminar: nunca se guarda, se registra ni se devuelve.

Una cuenta con la contraseña pendiente sí inicia sesión (200 con `cambioContrasenaPendiente = true`). Su token solo sirve con la política `SesionIniciada`.

El endpoint compone el resultado:
1. invoca el comando; si falla, responde con `ToProblem()`;
2. invoca `ObtenerUsuarioSesionQuery`;
3. responde 200 con `SesionResponse`.

### Comando `CambiarContrasena`

`Application/Sesion/CambiarContrasena.cs`:
- El comando es `CambiarContrasenaCommand(string ContrasenaActual, string ContrasenaNueva)` y devuelve `SesionIniciada`.
- Sus dependencias son `ICurrentUser`, `IUsuarioRepository`, `IHasherContrasenas`, `IEmisorTokens`, `IUnitOfWork`, `TimeProvider` y `ILogger`.

Pasos:
1. Obtiene la cuenta con `ObtenerPorIdAsync(actual.Id)`. Si es `null` (se dio de baja durante la petición), responde `CuentaNoRegistrada`.
2. Si no es Superusuario, responde `CambioContrasenaNoAplica`.
3. Verifica `ContrasenaActual` con `Verificar`. Si es `Incorrecta`, responde `ContrasenaActualIncorrecta`.
4. `PoliticaContrasena.Validar(ContrasenaNueva)`: si falla, devuelve su error.
5. Si la nueva es igual a la actual (comparación ordinal), responde `ContrasenaNuevaIgualActual`.
6. `EstablecerContrasena(Hashear(nueva), instante)`, con el instante truncado a segundos, y `SaveChangesAsync`.
7. Registra el evento y emite un token nuevo.

El endpoint compone la respuesta igual que en iniciar sesión.

Como no hay invalidación en el servidor (D4), el token anterior sigue siendo válido hasta que expira. Pero con la contraseña ya cambiada la base no marca pendiente, así que ese token deja de estar limitado.

### Cerrar sesión

El endpoint solo responde 204. No tiene comando ni cambia nada en el servidor (D4).

## 7. Administración de cuentas

### Consultas (`Application/Cuentas/CuentaConsultas.cs`)

```csharp
internal sealed record CuentaResponse(
    int Id,
    string Correo,
    string Nombre,
    RolResponse Rol,
    AreaAcademicaResumenResponse? AreaAcademica,
    EntidadAcademicaResumenResponse? EntidadAcademica);

/// <summary>Filtros opcionales, combinados con AND. Un texto vacío o solo con espacios se ignora.</summary>
internal sealed record FiltrosCuentas(byte? RolId, int? AreaAcademicaId, int? EntidadAcademicaId, string? Busqueda);

/// <summary>Cuentas activas en orden de id, paginadas.</summary>
internal sealed record ListarCuentasQuery(Paginacion Paginacion, FiltrosCuentas Filtros);

internal sealed record ObtenerCuentaQuery(int Id);
```

`ListarCuentasValidator`:
- incluye `PaginacionValidator`;
- `RolId` debe estar entre 1 y 3 ("rol");
- `AreaAcademicaId` y `EntidadAcademicaId` deben ser mayores que 0 ("área académica" y "entidad académica");
- `Busqueda` admite hasta 200 caracteres ("búsqueda");
- todas estas reglas se aplican solo cuando el valor no es `null`, y usan `OverridePropertyName` con el nombre del parámetro HTTP.

Handlers (`Infrastructure/Cuentas/CuentaConsultas.cs`), con `AsNoTracking` y proyección en SQL:

| Filtro | Traducción |
|---|---|
| `rolId` | `u.Rol == (Rol)rolId` |
| `areaAcademicaId` | Los DGAA del área **y** las cuentas de Entidad Académica cuyas entidades pertenecen al área. Los ids de esas entidades, incluidas las dadas de baja, se obtienen antes con `IAmbitosInstitucionales.ObtenerEntidadesDeAreaAsync` (sección 9). Traducción: `u.PerfilDgaa.AreaAcademicaId == area \|\| entidades.Contains(u.PerfilEntidadAcademica.EntidadAcademicaId)` |
| `entidadAcademicaId` | `u.PerfilEntidadAcademica.EntidadAcademicaId == entidad` |
| `busqueda` | Se recorta y se colapsan los espacios. Coincide si `u.Correo.Contains(busqueda)` (la columna ya es `Latin1_General_100_CI_AI`) **o** si `EF.Functions.Collate(u.Nombre, "Modern_Spanish_100_CI_AI").Contains(busqueda)` (`nombre` no declara colación) |

- El orden es por `Id`.
- `ListarCuentasHandler` llama a `PaginarAsync`. Después resuelve los nombres de áreas y entidades de la página con una llamada a `ObtenerAreasAsync` y otra a `ObtenerEntidadesAsync`, y arma `Pagina<CuentaResponse>`.
- `ObtenerCuentaHandler` responde 404 `Usuario.NoEncontrado` si la cuenta no existe o está dada de baja.

### Comandos (`Application/Cuentas/`)

**`CrearCuenta.cs`**
- Comando: `CrearCuentaCommand(string Correo, string Nombre, byte RolId, int? AreaAcademicaId, int? EntidadAcademicaId)`.
- Handler: `ICommandHandler<CrearCuentaCommand, CuentaCreada>`, con `CuentaCreada(int Id, string? ContrasenaTemporal)`.
- `CrearCuentaValidator` revisa la forma de la entrada, que el dominio no cubre:
  - `RolId` debe estar entre 1 y 3 ("rol");
  - `AreaAcademicaId` es obligatorio y mayor que 0 si el rol es 2, y debe ser `null` en otro caso. El mensaje para un valor de más es "El área académica solo aplica a cuentas DGAA.";
  - `EntidadAcademicaId` sigue la misma regla con el rol 3. El mensaje para un valor de más es "La entidad académica solo aplica a cuentas de Entidad Académica.".
- Dependencias: `IUsuarioRepository`, `IAmbitosInstitucionales`, `IHasherContrasenas`, `IGeneradorContrasenas`, `ICurrentUser`, `IUnitOfWork` y `ILogger`.
- Pasos:
  1. Crea la cuenta con la fábrica de su rol. Para un Superusuario, primero genera la temporal y crea la cuenta con `Hashear(temporal)`. Si la fábrica falla, devuelve su error.
  2. Verifica el ámbito: si el área o la entidad no están activas, responde `AreaAcademicaInexistente` o `EntidadAcademicaInexistente`.
  3. Si `ExisteCorreoAsync` encuentra el correo normalizado, responde `CorreoDuplicado`.
  4. `Agregar` y `SaveChangesAsync`.
  5. Registra el evento y devuelve el id con la temporal, que es `null` para DGAA y Entidad Académica.

**`ModificarCuenta.cs`**
- Comando: `ModificarCuentaCommand(int Id, string Nombre)`.
- Pasos:
  1. Obtiene la cuenta; si no existe, responde `NoEncontrado`.
  2. `CambiarNombre`; si falla, devuelve su error.
  3. `SaveChangesAsync`.
  4. Registra el evento.

**`RestablecerContrasena.cs`**
- Comando: `RestablecerContrasenaCommand(int Id)`, que devuelve `string` (la temporal).
- Pasos:
  1. Si `Id` es el de `ICurrentUser`, responde `RestablecimientoPropio`.
  2. Obtiene la cuenta; si no existe, responde `NoEncontrado`.
  3. Si no es Superusuario, responde `RestablecimientoNoAplica`.
  4. Genera la temporal y llama a `RestablecerContrasena(Hashear(temporal))`.
  5. `SaveChangesAsync`.
  6. Registra el evento y devuelve la temporal.

**`DarDeBajaCuenta.cs`**
- Comando: `DarDeBajaCuentaCommand(int Id)`.
- Pasos:
  1. Si `Id` es el de `ICurrentUser`, responde `BajaPropia`.
  2. Obtiene la cuenta; si no existe, responde `NoEncontrado`. También una cuenta ya dada de baja, como en la decisión D4 de Institucional.
  3. Si es Superusuario y `ContarSuperusuariosAsync() <= 1`, responde `UltimoSuperusuario`.
  4. Llama a `DarDeBaja(instante)` y a `SaveChangesAsync`.
  5. Registra el evento.

Sobre el paso 3: como un Superusuario no puede darse de baja a sí mismo, quien da de baja a otro deja al menos uno activo. Aun así, la comprobación se hace por la regla de §14. Dos bajas simultáneas entre los dos últimos Superusuarios podrían dejar cero: el riesgo se acepta porque requiere dos administradores actuando a la vez uno contra el otro.

### Endpoints de cuentas

`Endpoints/Cuentas/CuentaEndpoints.cs`. El grupo es `/cuentas` dentro de `/api/v1/usuarios`, lleva el tag `Cuentas` y `RequireAuthorization(Politicas.Superusuario)`. Todas las rutas declaran además 401 y 403.

| Método y ruta | `WithName` | `WithSummary` | Entrada | Éxito | Errores declarados |
|---|---|---|---|---|---|
| `GET /` | `ListarCuentas` | Lista las cuentas activas, paginadas y con filtros opcionales. | `[AsParameters] ListarCuentasRequest`: `pagina`, `tamanoPagina`, `rolId`, `areaAcademicaId`, `entidadAcademicaId` y `busqueda` | 200 `Pagina<CuentaResponse>` | 400 |
| `GET /{id:int}` | `ObtenerCuenta` | Obtiene una cuenta activa. | — | 200 `CuentaResponse` | 404 |
| `POST /` | `CrearCuenta` | Registra una cuenta; la de un Superusuario recibe una contraseña temporal. | `CrearCuentaCommand`, enlazado directamente | 201 con `Location` y `CuentaCreadaResponse` | 400, 409 |
| `PUT /{id:int}` | `ModificarCuenta` | Modifica el nombre de una cuenta. | `ModificarCuentaRequest(string Nombre)` | 204 | 400, 404 |
| `POST /{id:int}/restablecer-contrasena` | `RestablecerContrasena` | Asigna una contraseña temporal nueva a otro Superusuario. | — | 200 `ContrasenaTemporalResponse` | 404, 409 |
| `DELETE /{id:int}` | `DarDeBajaCuenta` | Da de baja una cuenta que no es la propia. | — | 204 | 404, 409 |

- El `POST` compone igual que el de entidades en Institucional: invoca el comando y luego `ObtenerCuentaQuery`, y responde `CreatedAtRoute(new CuentaCreadaResponse(cuenta, contrasenaTemporal), "ObtenerCuenta", new { id })`.
- Las dos respuestas nuevas son `CuentaCreadaResponse(CuentaResponse Cuenta, string? ContrasenaTemporal)` y `ContrasenaTemporalResponse(string ContrasenaTemporal)`.
- Un `POST` o `PUT` con campos de más (por ejemplo, `correo` en el `PUT`) no falla: esos campos se ignoran.

Ejemplo de `POST`:

```json
{ "correo": "jperez@uv.mx", "nombre": "Juan Pérez", "rolId": 2, "areaAcademicaId": 3, "entidadAcademicaId": null }
```

## 8. Bootstrap del primer Superusuario

Se ejecuta con `docker compose run --rm api bootstrap-superusuario`. En producción, se ejecuta la misma imagen con el mismo argumento.

`UsuariosModule.EjecutarBootstrapAsync(IServiceProvider servicios, TextWriter salida, CancellationToken cancellationToken)` es pública y devuelve `Task<int>`:
1. Lee `SGPLA_BOOTSTRAP_CORREO` y `SGPLA_BOOTSTRAP_NOMBRE` de `IConfiguration`. Si falta alguna, escribe "Faltan SGPLA_BOOTSTRAP_CORREO o SGPLA_BOOTSTRAP_NOMBRE." y devuelve **2**.
2. En un scope nuevo invoca `ICommandHandler<CrearSuperusuarioInicialCommand, CuentaCreada>`, de `Application/Cuentas/CrearSuperusuarioInicial.cs`, con dependencias `IUsuarioRepository`, `IHasherContrasenas`, `IGeneradorContrasenas`, `IUnitOfWork` y `ILogger`. El handler:
   1. si `ContarSuperusuariosAsync() > 0`, responde `SuperusuarioExistente`;
   2. genera la temporal y llama a `CrearSuperusuario`;
   3. si el correo ya está en uso (`ExisteCorreoAsync`), responde `CorreoDuplicado`;
   4. `Agregar`, `SaveChangesAsync` y registra el evento.
3. Según el resultado:
   - con `SuperusuarioExistente`, escribe su mensaje y devuelve **0**, para poder ejecutarlo en cada despliegue;
   - con cualquier otro error, escribe el mensaje y devuelve **1**;
   - si tiene éxito, escribe "Superusuario {id} creado." y "Contraseña temporal: {temporal} (cámbiala al iniciar sesión)", y devuelve **0**.

La temporal solo se escribe en `salida`, nunca en el log.

## 9. Contratos entre módulos

### Institucional → Usuarios (consulta de ámbitos)

`Institucional/Application/Contracts/IAmbitosInstitucionales.cs` (PR 1):

```csharp
namespace Sgpla.Modules.Institucional.Application.Contracts;

/// <summary>Consulta de áreas y entidades académicas para otros módulos (Usuarios, por el ámbito de una cuenta).</summary>
public interface IAmbitosInstitucionales
{
    Task<bool> AreaAcademicaActivaAsync(int areaAcademicaId, CancellationToken cancellationToken);

    Task<bool> EntidadAcademicaActivaAsync(int entidadAcademicaId, CancellationToken cancellationToken);

    /// <summary>Incluye las dadas de baja: una cuenta siempre puede mostrar su ámbito. Los ids inexistentes no aparecen.</summary>
    Task<IReadOnlyDictionary<int, AreaAcademicaResumen>> ObtenerAreasAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken);

    /// <summary>Igual que <see cref="ObtenerAreasAsync"/>, para entidades.</summary>
    Task<IReadOnlyDictionary<int, EntidadAcademicaResumen>> ObtenerEntidadesAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken);

    /// <summary>Ids de las entidades del área, incluidas las dadas de baja.</summary>
    Task<IReadOnlyCollection<int>> ObtenerEntidadesDeAreaAsync(int areaAcademicaId, CancellationToken cancellationToken);
}

public sealed record AreaAcademicaResumen(int Id, int Clave, string Nombre);

public sealed record EntidadAcademicaResumen(int Id, string Clave, string Nombre);
```

Implementación: `Institucional/Infrastructure/Contratos/AmbitosInstitucionales.cs`, `internal sealed class AmbitosInstitucionales(SgplaDbContext contexto)`.
- Todas las consultas usan `AsNoTracking`.
- Las que incluyen bajas usan `IgnoreQueryFilters([FiltrosConsulta.BajaLogica])`.
- Con una colección de ids vacía, devuelve un diccionario vacío sin consultar.
- Se registra en `AddInstitucionalModule`.

### Usuarios → Institucional (P3 y P4)

En `Institucional/Application/Contracts/` (PR 2), como prevé `pendientes.md`:

```csharp
/// <summary>Lo implementa Usuarios. Con usuarios DGAA activos, el área no se da de baja (DATABASE.md §9.2).</summary>
public interface IUsuariosDeAreaAcademica
{
    Task<bool> TieneUsuariosActivosAsync(int areaAcademicaId, CancellationToken cancellationToken);
}

/// <summary>Lo implementa Usuarios. Con usuarios de entidad activos, la entidad no se da de baja (DATABASE.md §9.2).</summary>
public interface IUsuariosDeEntidadAcademica
{
    Task<bool> TieneUsuariosActivosAsync(int entidadAcademicaId, CancellationToken cancellationToken);
}
```

- **Handlers de Institucional:**
  - `DarDeBajaAreaAcademicaHandler` recibe `IEnumerable<IUsuariosDeAreaAcademica>`. Después de comprobar las entidades activas, si alguna implementación responde `true`, devuelve `AreaAcademicaErrors.TieneUsuariosActivos` (Conflict, "El área académica tiene usuarios DGAA activos.").
  - `DarDeBajaEntidadAcademicaHandler` recibe `IEnumerable<IUsuariosDeEntidadAcademica>`. Antes de dar de baja, devuelve `EntidadAcademicaErrors.TieneUsuariosActivos` (Conflict, "La entidad académica tiene usuarios activos.").
  - Los dos errores se agregan a las tablas de `Modulo_Institucional.md` §7 y §8.
- **Implementación en Usuarios:** `Infrastructure/Contratos/UsuariosDeAmbito.cs` implementa ambas interfaces con `AnyAsync` sobre las cuentas activas del área (solo DGAA) o de la entidad. Se registran en `AddUsuariosModule`.

## 10. Autorización del resto del sistema

Se hace en el PR 3 y reemplaza los comentarios de autorización que hoy tienen los grupos.

| Grupo o ruta | Política |
|---|---|
| Grupo `/api/v1/institucional` | `Autenticado` |
| `POST`, `PUT` y `DELETE` de `/areas-academicas` y `/entidades-academicas` | `Superusuario`, en cada ruta de escritura |
| Grupo `/api/v1/catalogos` | `Autenticado` |
| `POST` y `PUT` de `/catalogos/articulos` | `Superusuario` (P6) |
| `/health`, `/openapi` y `/scalar` | Sin cambios (anónimos) |

**Filtro por ámbito (P5).** `ListarEntidadesAcademicasHandler` y `ObtenerEntidadAcademicaHandler` reciben `ICurrentUser` y aplican, antes de los demás filtros:
- Superusuario: ningún filtro;
- DGAA: `e.AreaAcademicaId == actual.AreaAcademicaId`;
- Entidad Académica: `e.Id == actual.EntidadAcademicaId`.

Una entidad fuera del ámbito responde 404 `EntidadAcademica.NoEncontrado`, para no revelar que existe. Regiones, campus y áreas no se filtran.

Documentos:
- en `ESTANDAR_MODULOS.md` §9, la regla de autorización pasa a describir `RequireAuthorization` con `Politicas`, sin el texto de "hasta entonces";
- se actualizan los ejemplos de §9 y §10 del estándar;
- `Modulo_Institucional.md` D7 anota "resuelta en `Modulo_Usuarios.md`".

## 11. Auditoría

`[LoggerMessage]` en clases `<Clase>Log` junto a cada handler (`ESTANDAR_MODULOS.md` §11). Solo registran ids, `Rol` y el motivo como enum: nunca correo, nombre, contraseña, verificador ni token.

| EventId | Nivel | Evento | Parámetros |
|---|---|---|---|
| 1001 | Information | Acceso exitoso | `usuarioId`, `rol` |
| 1002 | Warning | Acceso fallido | `usuarioId` (puede ser `null`), `motivo` (`MotivoAccesoFallido`: `CuentaNoRegistrada`, `AmbitoInactivo`, `CredencialesInvalidas`, `LdapNoDisponible`) |
| 1003 | Information | Verificador actualizado (rehash) | `usuarioId` |
| 1004 | Information | Contraseña cambiada | `usuarioId` |
| 1005 | Information | Contraseña restablecida | `usuarioId`, `porUsuarioId` |
| 1006 | Information | Cuenta creada | `usuarioId`, `rol`, `porUsuarioId` |
| 1007 | Information | Cuenta modificada | `usuarioId`, `porUsuarioId` |
| 1008 | Information | Cuenta dada de baja | `usuarioId`, `porUsuarioId` |
| 1009 | Information | Superusuario inicial creado | `usuarioId` |
| 1010 | Warning | LDAP no disponible | `codigoLdap` (entero) |

## 12. Composición del módulo

`UsuariosModule.cs`:
- **`AddUsuariosModule`**:
  - `AddPersistenciaModulo` y `AddHandlersModulo`;
  - las tres opciones con validación;
  - `IUsuarioRepository`, `IHasherContrasenas`, `IGeneradorContrasenas`, `ILdapAutenticador` e `IEmisorTokens`, todos `Scoped` salvo el generador, que es `Singleton`;
  - autenticación, políticas, el `IAuthorizationMiddlewareResultHandler`, `AddHttpContextAccessor` e `ICurrentUser`;
  - desde el PR 2, `IUsuariosDeAreaAcademica` e `IUsuariosDeEntidadAcademica`.
- **`MapUsuariosEndpoints`:** `var grupo = endpoints.MapGroup(Ruta);`, luego `grupo.MapSesionEndpoints();` y `grupo.MapCuentaEndpoints();`. Se quita el `WithTags("Usuarios")` actual.
- **`EjecutarBootstrapAsync`**, como en la sección 8.

`Sgpla.Modules.Usuarios.csproj` agrega:
- las referencias a `Sgpla.BuildingBlocks.Application` y a los tres paquetes;
- `<InternalsVisibleTo Include="Sgpla.UnitTests" />` y `<InternalsVisibleTo Include="Sgpla.IntegrationTests" />` (D11).

`Sgpla.UnitTests.csproj` agrega la referencia a Usuarios.

Configuración EF (`Infrastructure/Cuentas/UsuarioConfiguration.cs`):
- `ToTable("usuario", "usuarios")` y el filtro de baja lógica;
- `Correo` con `HasMaxLength(254).IsUnicode(false)`;
- `Rol` con `HasColumnName("rol_id")` y conversión a `byte`;
- `PerfilDgaa`, `PerfilEntidadAcademica` y `Credencial` como `OwnsOne` en sus tablas (`usuario_dgaa`, `usuario_entidad_academica` y `credencial_superusuario`), con la clave `usuario_id`;
- `Contrasena` con `HasMaxLength(500).IsUnicode(false)`.

Si EF Core no admite este mapeo tal como está descrito, se detiene y se pregunta: no se cambia el modelo por cuenta propia.

Estructura final:

```
src/Modules/Usuarios/Sgpla.Modules.Usuarios/
  Domain/Cuentas/Usuario.cs, PerfilDgaa.cs, PerfilEntidadAcademica.cs, CredencialSuperusuario.cs,
                 PoliticaContrasena.cs, NombresRol.cs, UsuarioErrors.cs
  Application/
    Autenticacion/IHasherContrasenas.cs, IGeneradorContrasenas.cs, ILdapAutenticador.cs, IEmisorTokens.cs
    Sesion/SesionConsultas.cs, IniciarSesion.cs, CambiarContrasena.cs
    Cuentas/IUsuarioRepository.cs, CuentaConsultas.cs, CrearCuenta.cs, ModificarCuenta.cs,
            RestablecerContrasena.cs, DarDeBajaCuenta.cs, CrearSuperusuarioInicial.cs
  Infrastructure/
    Autenticacion/JwtOptions.cs, Argon2Options.cs, LdapOptions.cs, HasherArgon2.cs, GeneradorContrasenas.cs,
                   LdapAutenticador.cs, EmisorTokens.cs, VerificacionSesion.cs, Politicas (requisito y handler 403),
                   UsuarioActual.cs
    Sesion/SesionConsultas.cs
    Cuentas/UsuarioConfiguration.cs, UsuarioRepository.cs, CuentaConsultas.cs
    Contratos/UsuariosDeAmbito.cs                       (PR 2)
  Endpoints/
    Sesion/SesionEndpoints.cs
    Cuentas/CuentaEndpoints.cs
  UsuariosModule.cs
```

## 13. Entregas (PR)

Tres PR en orden, cada uno desde `develop`, con la convención de ramas, commits y PR del repositorio. Cada PR deja el CI en verde por sí solo.

### PR 1: autenticación

- **Código:**
  - las secciones 3, 4, 5 y 6, y la 8;
  - de la 9, solo `IAmbitosInstitucionales` y su implementación;
  - de la 11, los eventos de sesión y del bootstrap;
  - de la 12, lo que corresponde a estas piezas.
  - Todavía no hay endpoints de cuentas.
- **Pruebas unitarias** (`tests/Sgpla.UnitTests/Usuarios/`):
  - `UsuarioTests`:
    - cada error de validación con su campo;
    - normalización del correo (recorte y minúsculas) y del nombre;
    - `@uv.mx` obligatorio para DGAA y Entidad Académica, y cualquier dominio válido para el Superusuario;
    - cada fábrica crea solo su parte;
    - `CambiarNombre` que falla no cambia nada;
    - `DarDeBaja` asigna el mismo instante a la cuenta y a la credencial, y es idempotente;
    - `EstablecerContrasena`, `RestablecerContrasena` y `ActualizarVerificador` dejan `FechaActualizacion` como indica la sección 5;
    - los métodos de contraseña lanzan una excepción en una cuenta que no es de Superusuario.
  - `PoliticaContrasenaTests`: longitudes 7, 8, 128 y 129, la falta de cada tipo de carácter, y que los espacios cuentan.
  - `GeneradorContrasenasTests`: mil temporales, todas de 12 caracteres, que cumplen la política, sin caracteres ambiguos y distintas entre sí.
  - `HasherArgon2Tests`:
    - la cadena empieza con `$argon2id$v=19$m=19456,t=2,p=1$`;
    - dos hashes de la misma contraseña son distintos;
    - la verificación correcta e incorrecta;
    - un parámetro menor pide rehash;
    - una cadena ilegible cuenta como `Incorrecta`.
  - `IniciarSesionHandlerTests`, con fakes de los puertos:
    - cada rama;
    - que se agrega `@uv.mx`;
    - que el rehash guarda;
    - que el LDAP no se llama con un ámbito inactivo.
  - `CambiarContrasenaHandlerTests`: el orden de los errores y que el instante se trunca a segundos.
  - `CrearSuperusuarioInicialHandlerTests`: con y sin un Superusuario activo.
  - Los fakes van en `tests/Sgpla.UnitTests/Usuarios/Fakes.cs`. `TimeProviderFalso` y `UnitOfWorkFalso` se reutilizan con `using`.
- **Pruebas de integración:**
  - **Infraestructura de pruebas:**
    - `SgplaApiFactory` fija `Jwt:Clave` y una configuración `Ldap` válida, y reemplaza `ILdapAutenticador` por `LdapFalso` en `tests/Sgpla.IntegrationTests/Infraestructura/LdapFalso.cs`. `LdapFalso` acepta la contraseña `LdapFalso.ContrasenaValida`, responde `NoDisponible` para correos que empiezan con `ldap-caido` y `CredencialesInvalidas` para todo lo demás.
    - Helpers de la fábrica: `CrearClienteSuperusuarioAsync()`, `CrearClienteDgaaAsync(int areaAcademicaId)` y `CrearClienteEntidadAcademicaAsync(int entidadAcademicaId)`. Cada uno crea una cuenta nueva en la base con el dominio y el hasher de la aplicación (el Superusuario, con la contraseña ya cambiada) y devuelve un `HttpClient` con el token de `IEmisorTokens`.
    - Además, `CrearSuperusuarioAsync()` devuelve el correo y una contraseña conocida, para las pruebas de login.
    - Los correos únicos salen de `DatosUnicos.Correo(dominio)`: `u{guid:N}@{dominio}`.
  - **`SesionEndpointsTests`:**
    - login de Superusuario 200, con `cambioContrasenaPendiente` según corresponda;
    - login de DGAA y de Entidad Académica por LDAP, 200, con su ámbito anidado;
    - el usuario sin dominio se completa con `@uv.mx`;
    - `CuentaNoRegistrada` para una cuenta inexistente y para una dada de baja;
    - `AmbitoInactivo`, dando de baja el ámbito por SQL directo en la prueba;
    - `CredencialesInvalidas` para Superusuario y para LDAP;
    - 503 con LDAP caído;
    - 400 con correo o contraseña vacíos;
    - `GET /sesion` 200, y 401 sin token, con un token alterado o vencido y con una cuenta dada de baja después de emitir el token;
    - un token con la contraseña pendiente recibe 403 `CambioContrasenaPendiente` en una ruta con política `Autenticado`. Mientras el PR 1 no protege ninguna, se usa una ruta de prueba registrada solo en `SgplaApiFactory`; el PR 2 la cambia por `GET /cuentas`;
    - cambiar la contraseña: 200 con un token nuevo que ya no está limitado, 400 por contraseña actual incorrecta, 400 por política, 400 si es igual a la actual y 409 para una cuenta UV;
    - cerrar sesión 204;
    - ningún log de la prueba contiene la contraseña ni el correo usados (`ProveedorLogsEnMemoria`).
  - **`BootstrapTests`:** con un Superusuario activo, `EjecutarBootstrapAsync` devuelve 0, no crea nada y lo dice en la salida. La creación se cubre con la prueba unitaria, porque la base compartida siempre tiene Superusuarios.
  - **`AmbitosInstitucionalesTests`:** cada método, incluidas las bajas.
- **Documentos:**
  - `DATABASE.md` §13.3 (D2) y §13.5 (D12);
  - `PLAN_INICIAL.md`: D7, D10, D11 y la sección de seguridad;
  - `ESTANDAR_MODULOS.md` §2 (`Rol`, `Normalizacion`, `ICurrentUser`, `Politicas` y los nuevos `ErrorType`), §5 (excepción de D11 para Usuarios) y §9 ("Errores");
  - `.env.example` y el README del backend, con el bootstrap y las variables nuevas.

### PR 2: administración de cuentas

- **Código:**
  - la sección 7;
  - de la 9, los contratos de P3 y P4 con sus implementaciones y los cambios en los handlers de baja de Institucional;
  - los eventos de cuentas de la sección 11.
- **Pruebas unitarias:**
  - `CrearCuentaHandlerTests`: cada rol, el ámbito inactivo, el correo duplicado y que solo el Superusuario recibe temporal;
  - `RestablecerContrasenaHandlerTests`: propia, no Superusuario y éxito con `FechaActualizacion = null`;
  - `DarDeBajaCuentaHandlerTests`: propia, último Superusuario (el fake cuenta 1) y la credencial con el mismo instante;
  - en Institucional, las bajas de área y entidad bloqueadas por usuarios con un fake de cada contrato.
- **Pruebas de integración** (`CuentaEndpointsTests`, con el cliente de Superusuario):
  - crear cada rol responde 201 con `Location`, valores normalizados y el ámbito anidado. El Superusuario recibe una temporal que permite iniciar sesión con `cambioContrasenaPendiente = true`;
  - el validator responde 400 si falta el ámbito o sobra uno, o si el rol es inválido;
  - errores de dominio con su campo, y 400 con un área o entidad inexistentes o dadas de baja;
  - 409 por correo duplicado, también en mayúsculas;
  - se puede reutilizar el correo de una cuenta dada de baja (201);
  - obtener 200, y 404 si no existe o está dada de baja;
  - modificar el nombre 204, 400 y 404;
  - restablecer 200: la contraseña anterior deja de servir y la nueva deja la cuenta pendiente. También 404 y 409 (propia y cuenta UV);
  - dar de baja 204: la cuenta ya no inicia sesión y su token recibe 401. También 404 y 409 (propia);
  - cada filtro por separado, combinados y la búsqueda sin mayúsculas ni acentos: se crea "José Núñez" y se busca "jose nunez". Todo sobre cuentas de un área y una entidad creadas por la prueba;
  - la paginación, que rechaza `pagina=0` y `tamanoPagina=101`;
  - un DGAA o una Entidad Académica reciben 403 `SinPermiso`, y sin token se recibe 401;
  - en `AreaAcademicaEndpointsTests` y `EntidadAcademicaEndpointsTests`: `DarDeBaja_ConUsuariosActivos_Responde409` y `DarDeBaja_SiSusUsuariosEstanDadosDeBaja_Responde204`.
  - La regla del último Superusuario solo se prueba con pruebas unitarias, porque la base compartida siempre tiene varios.
- **Documentos:**
  - `DATABASE.md` §6.17, §9.3, §13.2 y §14 (D3);
  - `pendientes.md`: se borran P3 y P4;
  - `Modulo_Institucional.md` §7 y §8 (errores nuevos).

### PR 3: autorización del resto del sistema

- **Código:** la sección 10.
- **Pruebas:**
  - Las pruebas de integración existentes de Institucional y Catalogos cambian `_api.CreateClient()` por el cliente de Superusuario. No cambia ninguna aserción.
  - Nuevas pruebas en `AutorizacionTests`:
    - sin token, cada grupo responde 401;
    - DGAA y Entidad Académica leen regiones, campus, áreas y catálogos (200);
    - DGAA y Entidad Académica reciben 403 al escribir áreas, entidades y artículos;
    - un DGAA lista solo las entidades de su área y recibe 404 al obtener una de otra;
    - una Entidad Académica lista solo la suya y recibe 404 al obtener otra;
    - con la contraseña pendiente, se recibe 403 `CambioContrasenaPendiente` en Catalogos.
  - La ruta de prueba del PR 1 se elimina si ya no hace falta.
- **Documentos:**
  - `ESTANDAR_MODULOS.md` §9 y §10;
  - `pendientes.md`: se borran P5 y P6;
  - `Modulo_Institucional.md` D7.

## 14. Criterios de terminado

Cada PR cumple la lista de verificación de `ESTANDAR_MODULOS.md` §15. Ningún PR edita `baseline.sql` ni agrega migraciones: las tablas de `usuarios` ya existen tal como las necesita este documento.

La verificación se ejecuta solo con Docker, con el comando de `Modulo_Institucional.md` §11 (imagen `mcr.microsoft.com/dotnet/sdk:10.0.401`, copia con finales de línea LF y las tres suites). Además, antes de cada commit se regenera el entorno con `docker compose down -v` y `docker compose up -d --build`, y se prueba a mano:
- **PR 1:**
  - el bootstrap crea el primer Superusuario, y un segundo intento devuelve 0 sin crear nada;
  - el login con la temporal marca la contraseña como pendiente;
  - el cambio de contraseña funciona;
  - `GET /sesion` responde 200.
  - Si hay red hacia la UV, también un login real de una cuenta DGAA en modo `SinTls` de Development. Este paso es opcional y se reporta.
- **PR 2:** crear una cuenta DGAA y otra de Superusuario, restablecer la contraseña del Superusuario y dar de baja la cuenta DGAA.
- **PR 3:** sin token, `GET /api/v1/catalogos/grados-academicos` responde 401, y con token de Superusuario, 200.

Todo debe terminar sin advertencias ni fallos. Si una prueba falla por una razón ajena al PR, se reporta: no se modifica la prueba para ocultarla.
