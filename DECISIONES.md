# Registro de decisiones

Este documento reúne las decisiones de diseño de los módulos cerrados y las desviaciones que introdujeron respecto a `DATABASE.md`, `PLAN_INICIAL.md` y `ESTANDAR_MODULOS.md`. Es el registro del porqué: el comportamiento vigente está en el código y en los documentos normativos, que citan estas decisiones por su identificador.

Convenciones:
- Cada decisión se identifica con el prefijo de su módulo y su número original: `INS-D1`, `USU-D3`, `OFE-D12`. Los identificadores no se reutilizan ni se renumeran.
- La columna **PR** indica el pull request en que la decisión entró a `develop`. El texto es el vigente al cerrar el módulo.
- Las entradas `P#` son pendientes de `pendientes.md`; las ya resueltas se retiraron de ese archivo.
- Al cerrar un módulo, la tabla de decisiones de su `Modulo_<X>.md` se agrega aquí con un prefijo nuevo, se actualizan las referencias y el documento del módulo se elimina.

## Contenido

- [Institucional (INS)](#institucional-ins)
- [Usuarios (USU)](#usuarios-usu)
- [OfertaEducativa (OFE)](#ofertaeducativa-ofe)
- [SolicitudesApertura (SOL)](#solicitudesapertura-sol)
- [Anexo A. Contrato de importación del Excel del plan (OFE-D6)](#anexo-a-contrato-de-importación-del-excel-del-plan-ofe-d6)

## Institucional (INS)

| # | Decisión | Desviación respecto a | PR |
|---|---|---|---|
| INS-D1 | Región y campus son de **solo lectura**: sus valores son los de la semilla (5 regiones y 24 campus) y no hay altas, modificaciones ni bajas. Se quita `fecha_eliminacion` de ambas tablas **editando `baseline.sql`**. Es una excepción puntual: la regla general del estándar ("el baseline no se edita, los cambios van en una migración") sigue vigente | `PLAN_INICIAL.md` (preveía CRUD), `DATABASE.md` §5, §6.1, §6.2 y §9.1, `ESTANDAR_MODULOS.md` §15 | #5 |
| INS-D2 | Municipio pasa a Catalogos como catálogo fijo. Institucional depende de Catalogos (grafo nuevo: `Institucional → Catalogos`) y usa el contrato `IMunicipios` | `PLAN_INICIAL.md` (tabla de módulos y grafo) | #5 |
| INS-D3 | Área y entidad académica **no se restauran**. Un registro dado de baja no existe para la API: `GET`, `PUT` y `DELETE` sobre él responden 404. No hay parámetro `incluirEliminados`. La excepción aplica **solo a Institucional**; el resto del modelo conserva la restauración | `DATABASE.md` §9.3 y §14 ("Baja y restauración"), `ESTANDAR_MODULOS.md` §9 (operaciones Restaurar y `incluirEliminados`) | #5 |
| INS-D4 | `DELETE` sobre un registro ya dado de baja responde **404**, no 204 | `ESTANDAR_MODULOS.md` §6 (baja idempotente) | #5 |
| INS-D5 | Una referencia a un área académica dada de baja se trata como inexistente: **400** con su campo, no 409 | `ESTANDAR_MODULOS.md` §9 (409 para "padres inactivos") | #5 |
| INS-D6 | Se posponen las reglas que dependen de OfertaEducativa y Usuarios (ver `pendientes.md`). Mientras tanto, el área académica de una entidad **siempre** puede cambiarse, y las bajas solo se bloquean por las reglas internas del módulo. P1 y P2 resueltas en OfertaEducativa (#12) | `DATABASE.md` §9.1, §9.2 y §10 | #5 |
| INS-D7 | Resuelta en Usuarios (#10): la lectura de entidades se filtra por ámbito (DGAA: las entidades de su área; Entidad Académica: solo la suya). Regiones, campus y áreas son visibles para cualquier usuario autenticado. La escritura es solo del Superusuario. | — | #10 |

## Usuarios (USU)

| # | Decisión | Desviación respecto a | PR |
|---|---|---|---|
| USU-D1 | Un **solo endpoint de login**, `POST /api/v1/usuarios/iniciar-sesion`. Decide el mecanismo según el rol de la cuenta: LDAP para DGAA y Entidad Académica, y la contraseña local Argon2id para el Superusuario. Si el texto recibido no trae `@`, se completa con `@uv.mx` (solo en el login, no en el alta) | — | #8 |
| USU-D2 | **LDAP con bind directo** usando el correo como identidad (`correo@uv.mx`), sin base DN, búsqueda ni cuenta de servicio, igual que el sistema anterior de la UV. La seguridad del canal es configurable: `Ldaps` (por omisión), `StartTls` o `SinTls`. `SinTls` solo se acepta en el entorno Development, y la API no arranca si se configura en otro entorno | `DATABASE.md` §13.3 ("mediante TLS", salvo en Development). `PLAN_INICIAL.md` (base DN y formato de bind en `LdapOptions`) | #8 |
| USU-D3 | Las cuentas **no se restauran**, como en Institucional. Una cuenta dada de baja no existe para la API: `GET`, `PUT`, `DELETE` y restablecer responden 404. El correo queda libre para una cuenta nueva | `DATABASE.md` §6.17 (nota de restauración), §9.3, §13.2 (reactivar un Superusuario) y §14 (casos de restauración). `PLAN_INICIAL.md` (tabla de módulos) | #8 |
| USU-D4 | La sesión es un **JWT HS256 de 8 horas, sin refresh**. En cada petición se consulta la base: es la fuente del estado vigente (cuenta activa, ámbito activo y contraseña pendiente). **Cerrar sesión solo tiene efecto en el cliente**: un token copiado sigue sirviendo hasta que expira, salvo que la cuenta o su ámbito se den de baja | — (§13.5 ya deja sesiones y refresh tokens fuera del modelo) | #8 |
| USU-D5 | Las fallas del login usan **mensajes específicos**: 401 con un `codigo` distinto para una cuenta no registrada, un ámbito inactivo o una contraseña incorrecta, y 503 si el LDAP no responde. Se acepta que así se puede averiguar qué correos tienen cuenta | — | #8 |
| USU-D6 | La **contraseña temporal la genera la API**: 12 caracteres aleatorios que cumplen la política. Se devuelve una sola vez, en el alta y en el restablecimiento de un Superusuario, y nunca vuelve a mostrarse | — | #8 |
| USU-D7 | El primer Superusuario se crea con el **subcomando `bootstrap-superusuario` de la imagen de la API**, no del CLI de migraciones. Su contraseña también es temporal | `PLAN_INICIAL.md` ("subcomando reservado en la CLI") | #8 |
| USU-D8 | Solo el Superusuario administra cuentas, y **nunca la suya**: no puede darse de baja ni restablecer su propia contraseña (409). Para cambiarla usa "cambiar mi contraseña". Solo se edita el nombre. DGAA y Entidad Académica no consultan cuentas | — | #8 |
| USU-D9 | La contraseña nueva **no puede ser igual a la actual**. Es la única comparación con valores anteriores: no hay historial | `DATABASE.md` §13.4 lo permitía (solo "sin historial") | #8 |
| USU-D10 | `ICurrentUser` vive en `BuildingBlocks.Application` y lo **implementa el módulo Usuarios**, no el host. Usuarios también registra JwtBearer, las políticas y la verificación por petición, porque consultan sus tablas; el host solo agrega `UseAuthentication` y `UseAuthorization` | `PLAN_INICIAL.md` (`ICurrentUser` en SharedKernel, "los implementa el host") | #8 |
| USU-D11 | Las pruebas de integración usan **tokens reales** emitidos por la API, no un `TestAuthHandler`. Para reemplazar el adaptador LDAP por un falso, Usuarios declara `InternalsVisibleTo` también para `Sgpla.IntegrationTests` | `PLAN_INICIAL.md` (`TestAuthHandler`). `ESTANDAR_MODULOS.md` §5 (solo `Sgpla.UnitTests`) | #8 |
| USU-D12 | Los eventos de autenticación de §13.5 se emiten como **logs estructurados** con `[LoggerMessage]` y `EventId` fijos. Se exportarán cuando el host configure la telemetría | `DATABASE.md` §13.5 ("telemetría externa"): el destino queda para el despliegue | #8 |
| USU-D13 | Las políticas de hoy son `SesionIniciada`, `Autenticado` y `Superusuario`. `Dgaa` y `EntidadAcademica` se crean cuando un endpoint las necesite; el filtro por ámbito de P5 se aplica dentro de los handlers | `PLAN_INICIAL.md` (cuatro políticas, incluida `CambioContrasenaPendiente`, que aquí es un requisito de `Autenticado`) | #8 |
| USU-D14 | `ErrorType` agrega `Unauthorized` (401) y `Unavailable` (503). `Normalizacion` sale del dominio de Institucional y pasa a SharedKernel, pública, para que la usen ambos módulos | `ESTANDAR_MODULOS.md` §2 y §9 ("Errores") | #8 |

## OfertaEducativa (OFE)

| # | Decisión | Desviación respecto a | PR |
|---|---|---|---|
| OFE-D1 | `sistema_educativo`, `nivel_formacion` y `area_formacion` son **catálogos fijos de solo lectura** con los valores de la semilla, y viven en **Catalogos** para reutilizar `CatalogoFijo`. Se quita `fecha_eliminacion` de las tres tablas **editando `baseline.sql`**, como excepción documentada (igual que INS-D1) | `DATABASE.md` §5, §6.6, §6.7, §6.10, §9.2 y §10. `PLAN_INICIAL.md` (tabla de módulos) | #11 |
| OFE-D2 | La semilla de `area_formacion` se rehace con las claves que usa la UV en sus planes (`CODE_AREA_F`): `111` Área de Formación Básica, `112` Área de Formación Disciplinaria y `113` Área de Formación Terminal | `Baseline/seed.sql` (AFBG, AID, AFD, AFT y AFEL) | #11 |
| OFE-D3 | El grafo pasa a **OfertaEducativa → Institucional, Catalogos**. Catalogos expone el contrato `IClasificacionesAcademicas` | `PLAN_INICIAL.md` (grafo) | #11 |
| OFE-D4 | **Sin restauración** en todo el módulo, como en Institucional y Usuarios. Un registro dado de baja no existe para la API: `GET`, `PUT` y `DELETE` sobre él responden 404, y no hay `incluirEliminados`. `DELETE` sobre un registro ya dado de baja responde 404 | `DATABASE.md` §9.3 y §14 ("Baja y restauración"). `ESTANDAR_MODULOS.md` §6 y §9 | #11 |
| OFE-D5 | **El plan no tiene archivo**. Se eliminan la tabla `archivo_plan_estudios` y la columna `plan_estudios.archivo_plan_estudios_id`, con su FK y su UNIQUE, **editando `baseline.sql`**. La base es la única fuente de verdad: el Excel solo sirve para importar, y la descarga se genera desde la base (OFE-D7) | `DATABASE.md` §5, §6.9, §7, §8, §10, §11, §14 y §17. `PLAN_INICIAL.md` y `DATABASE_DIAGRAM.md` | #11 |
| OFE-D6 | **Importación del plan en JSON**. El front abre el Excel de la UV, permite corregirlo y envía `POST /planes-estudio` con el programa, el código y de 1 a 300 EE. La API no lee Excel. Todo o nada: si una EE falla, no se crea nada y se responde el **primer** error, con el índice de la EE en el campo (`experienciasEducativas[3].creditos`). El anexo A documenta cómo mapea el front cada columna | — | #11 |
| OFE-D7 | **Exportación a Excel**. `GET /planes-estudio/{id}/excel` genera un `.xlsx` con los **14 encabezados** exactos del formato de la UV y las EE activas, con ClosedXML (licencia MIT) | — | #11 |
| OFE-D8 | El plan **no tiene campos editables**: el código y el programa son inmutables. "Modificar un plan" es crear, modificar o dar de baja sus EE una por una | `DATABASE.md` §6.9 (archivo reemplazable) | #11 |
| OFE-D9 | **Solo el DGAA escribe** programas, planes y EE, y solo en las entidades de su área. El Superusuario **lee todo y no escribe** en la estructura curricular. La Entidad Académica solo lee lo de su entidad. Nace la política `Dgaa` (USU-D13) | `DATABASE.md` §13.1 y §15.10 (acceso administrativo global del Superusuario) | #11 |
| OFE-D10 | **Sin cascada: la baja se bloquea con hijos activos** (409). La entidad académica no se da de baja con programas activos (P1). El programa no se da de baja con planes activos. La EE no se da de baja con programaciones activas. **Excepción:** `DELETE` de un plan da de baja el plan y todas sus EE activas con el mismo instante UTC, y se bloquea si alguna EE tiene programaciones activas | `DATABASE.md` §9.1 (cascada entidad → programa → plan → EE → programación). `pendientes.md` P1 (resolución prevista original) | #11 |
| OFE-D11 | **Periodo escolar:** el Superusuario lo crea, modifica sus fechas **en cualquier momento** y lo da de baja. La baja se **bloquea** si el periodo tiene cualquier programación (incluidas las dadas de baja) o cualquier referencia de otro módulo (P8). No hay cascada. La clave es inmutable | `DATABASE.md` §9.1 (periodo → programación en cascada) | #11 |
| OFE-D12 | **Programación académica y horarios son de solo lectura** en OfertaEducativa. Los crea y los da de baja la sincronización con PLANEA (Integracion), que se especifica después (P9) | `DATABASE.md` §12.1 (presuponía programaciones cargadas antes de sincronizar) | #11 |
| OFE-D13 | **Congelamiento de una EE:** horas teóricas, horas prácticas, créditos y área de formación solo cambian si la EE **nunca** tuvo una programación, incluidas las dadas de baja (409 si cambian). Nombre, perfil docente y cupos cambian siempre | Aclara `DATABASE.md` §10 ("antes de la primera programación") | #11 |
| OFE-D14 | P2 se mantiene: el área académica de una entidad se congela en cuanto la entidad tiene **cualquier** programa, incluidos los dados de baja | — (`DATABASE.md` §6.5 y §10) | #11 |
| OFE-D15 | Una referencia del cuerpo a un registro inexistente, dado de baja o **fuera del ámbito** del usuario responde 400 con su campo, sin revelar si existe (como INS-D5). Un recurso de la ruta fuera del ámbito responde 404 | `ESTANDAR_MODULOS.md` §9 (409 para "padres inactivos") | #11 |
| OFE-D16 | `EntidadAcademicaResumen` (contrato de Institucional) agrega `AreaAcademicaId`, para resolver el ámbito del DGAA y el área de la exportación sin otro contrato | — | #11 |

## SolicitudesApertura (SOL)

| # | Decisión | Desviación respecto a | PR |
|---|---|---|---|
| SOL-D1 | **No hay vinculación con la programación académica.** Se quitan de `solicitud_apertura` las columnas `programacion_academica_id`, `vinculada_en` y `vinculada_por_usuario_id`, con sus FK, CHECK e índices, **editando `baseline.sql`** como excepción documentada (igual que INS-D1 y OFE-D5). No existe el comando vincular ni el contrato `IReferenciasProgramacionAcademica` | `DATABASE.md` §2, §16.2, §16.4, §16.5 y §16.6. `DATABASE_DIAGRAM.md`. `PLAN_INICIAL.md` (tabla de módulos). `AGENTS.md` regla 13 y `ESTANDAR_MODULOS.md` §15 (el baseline no se edita) | #16 |
| SOL-D2 | **Concurrencia optimista con `rowversion`.** `solicitud_apertura` agrega la columna `version rowversion NOT NULL`, **editando `baseline.sql`** con la misma excepción de SOL-D1, mapeada con `IsRowVersion()`. Si dos operaciones chocan (una edición mientras otro acepta), la segunda responde 409 `Persistencia.ModificacionConcurrente`, que traduce el manejador global `ConcurrenciaExceptionHandler` de `DbUpdateConcurrencyException` | `DATABASE.md` §16.2 (columna nueva). `AGENTS.md` regla 13 y `ESTANDAR_MODULOS.md` §15. `ESTANDAR_MODULOS.md` §2 (pieza compartida nueva) | #16, #17 |
| SOL-D3 | **Almacenamiento de archivos compartido.** `IAlmacenamientoArchivos` se declara en `BuildingBlocks.Application` (lo usan los handlers de comando) y se implementa en `BuildingBlocks.Infrastructure` sobre el sistema de archivos local, con `AlmacenamientoOptions` (ruta base y tamaño máximo) validadas al arrancar. En `docker compose` los archivos viven en un volumen propio. El proveedor definitivo sigue pendiente y se cambia sin tocar los módulos | `PLAN_INICIAL.md` (la interfaz en `BuildingBlocks.Infrastructure`). `ESTANDAR_MODULOS.md` §2 (piezas compartidas nuevas) | #16 |
| SOL-D4 | **Carga con compensación.** Primero se guarda el binario con una clave nueva y después se confirma la base. Si `SaveChangesAsync` falla, se elimina el binario recién guardado y la excepción sigue su curso. Al reemplazar un oficio, el binario anterior se elimina **después** de confirmar. Un binario que no se puede eliminar queda huérfano y solo se registra en el log (SOL-D19) | Resuelve para este módulo el "proceso seguro para confirmar o compensar cargas" de `DATABASE.md` §17 | #16 |
| SOL-D5 | **Tamaño máximo configurable**, 10 MiB (10 485 760 bytes) por omisión: `Almacenamiento:TamanoMaximoBytes`, entre 1 y 26 214 400 (25 MiB) para que el límite de 30 MB del cuerpo en Kestrel nunca corte antes el multipart. Un oficio mayor responde 400 en el campo `oficio`. El esquema sigue sin máximo | Aclara `DATABASE.md` §16.3 ("sin máximo fijo") | #16 |
| SOL-D6 | **El oficio debe ser un PDF de verdad:** la parte del multipart declara `application/pdf` y su contenido empieza con los bytes `%PDF-`. Si no, 400 en el campo `oficio` | — | #16 |
| SOL-D7 | **Carga multipart.** `POST /solicitudes` recibe `multipart/form-data` con los datos y el oficio en una sola petición. `PUT /solicitudes/{id}` (PR 2) es multipart con el oficio opcional: si no llega, se conserva. `GET /solicitudes/{id}/oficio` descarga el PDF. Las rutas multipart desactivan la validación antiforgery, porque la API se autentica con token bearer y no con cookies | `ESTANDAR_MODULOS.md` §9 (no tenía reglas para archivos; se agrega la subsección "Archivos") | #16 |
| SOL-D8 | **Contratos nuevos de OfertaEducativa**, que ella declara e implementa: `IExperienciasEducativas` (resúmenes de EE con su cadena, su entidad, su área y sus cupos, y los ids de las EE visibles como consulta componible `IQueryable<int>`) e `IPeriodosEscolares`. El área la resuelve OfertaEducativa con el contrato de Institucional que ya usa. El grafo no cambia | `ESTANDAR_MODULOS.md` §5 (primer contrato que devuelve una consulta componible) | #16 |
| SOL-D9 | **Periodos configurados:** `PeriodosOptions` (sección `SolicitudesApertura`, claves `PeriodoActual` y `PeriodoSiguiente`, de seis dígitos y distintas) validadas al arrancar. El cliente envía `periodoEscolarId`; crear exige que sea el siguiente configurado y que el actual y el siguiente existan activos. Un cambio posterior de la configuración no afecta a las solicitudes existentes: editar, aceptar, rechazar y cancelar no vuelven a mirar los periodos. `GET /periodos` devuelve la pareja vigente | — (`DATABASE.md` §16.1 y `PLAN_INICIAL.md`) | #16 |
| SOL-D10 | **Referencias inválidas al crear:** una EE inexistente, dada de baja, con su plan o su programa dados de baja o de otra entidad responde 400 en `experienciaEducativaId`; un periodo inexistente o dado de baja, 400 en `periodoEscolarId` (como OFE-D15). Un periodo válido que no es el siguiente configurado responde 409 `SolicitudApertura.PeriodoNoAbierto`, y una pareja configurada sin sus dos periodos activos, 409 `SolicitudApertura.PeriodosNoDisponibles` | `ESTANDAR_MODULOS.md` §9 (409 para "padres inactivos") | #16 |
| SOL-D11 | **Cupos:** al crear y al modificar, la cantidad no puede ser menor que el cupo mínimo **si existe** ni mayor que el máximo **si existe** (409 `SolicitudApertura.CantidadFueraDeCupos`); una EE sin cupos admite la solicitud. Aceptar exige los dos cupos (409 `SolicitudApertura.CuposIncompletos`) y la cantidad dentro del rango inclusivo (409 `SolicitudApertura.CantidadFueraDeCupos`). Siempre se usan los cupos vigentes de la EE en ese momento | Aclara `DATABASE.md` §16.4 | #16, #17 |
| SOL-D12 | **Autorización:** la Entidad Académica crea, modifica y cancela las solicitudes de su entidad; la DGAA acepta y rechaza las de las entidades de su área. Las tres cuentas leen: la Entidad Académica, las de su entidad; la DGAA, las de su área; el **Superusuario, todas, en solo lectura**. El oficio solo lo descargan la Entidad Académica y la DGAA (403 para el Superusuario). Nacen las políticas `EntidadAcademica` y `DgaaOEntidadAcademica` (USU-D13) | `DATABASE.md` §16.4 ("El Superusuario no tiene acceso a este proceso"). `DATABASE_DIAGRAM.md` (reglas) | #16, #17 |
| SOL-D13 | **Ámbito en la ruta:** una solicitud fuera del ámbito del usuario responde 404 en toda ruta `/{id}`, de lectura o de comando, sin revelar si existe (como OFE-D15). Un filtro del listado fuera del ámbito devuelve una página vacía, como en OfertaEducativa | — | #16, #17 |
| SOL-D14 | **Baja de padres:** la EE y el plan no se dan de baja con solicitudes PENDIENTE o ACEPTADA de sus EE (P7); las RECHAZADAS y CANCELADAS no bloquean. El periodo no se da de baja con **cualquier** solicitud, en cualquier estado: prevalece OFE-D11 sobre `DATABASE.md` §16.6, que se corrige | `DATABASE.md` §16.6 (solo PENDIENTE y ACEPTADA bloqueaban también al periodo) | #16 |
| SOL-D15 | **Longitudes de aplicación:** la justificación y los comentarios de resolución admiten hasta **2000** caracteres, aunque las columnas son `nvarchar(max)`; el motivo de cancelación, hasta 1000, como su columna | Aclara `DATABASE.md` §16.2 | #16, #17 |
| SOL-D16 | **Modificar** (PR 2) exige en cada petición la cantidad y la justificación completas; el oficio es opcional. Solo en PENDIENTE (409 `SolicitudApertura.NoPendiente`). Toda modificación actualiza `actualizada_en` y `actualizada_por_usuario_id`. Un oficio reemplazado elimina su fila de `archivo_solicitud_apertura` en la misma transacción y su binario después de confirmar (SOL-D4) | — (`DATABASE.md` §16.3 y §16.4) | #17 |
| SOL-D17 | **Acciones de estado** (PR 2): `POST /solicitudes/{id}/aceptar`, `/rechazar` y `/cancelar`, con cuerpo JSON, responden **204** sin cuerpo. Solo desde PENDIENTE (409 `SolicitudApertura.NoPendiente`). Aceptar admite comentarios opcionales; rechazar los exige; cancelar exige un motivo | — (`ESTANDAR_MODULOS.md` §9, acción de estado) | #17 |
| SOL-D18 | **Listado paginado** con filtros opcionales `estado`, `periodoEscolarId`, `experienciaEducativaId` y `entidadAcademicaId`, en orden de `creada_en` descendente con el `id` descendente como desempate. El `Response` incluye los datos derivados para mostrar (EE, plan, programa, entidad, modalidad y periodo), que **no** se guardan en la tabla | — (`DATABASE.md` §16.1) | #16 |
| SOL-D19 | **Auditoría:** solo se registra el binario que no se pudo eliminar (huérfano), con su clave. Las transiciones no se registran: quedan en la fila con su actor y su fecha, y el cliente ya ve el resultado | — (`ESTANDAR_MODULOS.md` §11) | #16 |
| SOL-D20 | **Rutas:** el recurso vive en `/api/v1/solicitudes-apertura/solicitudes` y la pareja de periodos en `/api/v1/solicitudes-apertura/periodos`, con el patrón `/api/v1/<modulo>/<recurso-en-plural>` del estándar | — (`ESTANDAR_MODULOS.md` §4) | #16 |
| SOL-D21 | **Estados en mayúsculas.** El estado es el enum `EstadoSolicitudApertura` y se guarda como `PENDIENTE`, `ACEPTADA`, `RECHAZADA` o `CANCELADA` (lo exige `ck_solicitud_apertura__estado`) con un convertidor explícito, no con `HasConversion<string>()`, que guardaría `Pendiente` | `ESTANDAR_MODULOS.md` §8 ("Los estados se guardan como texto con `HasConversion<string>()`") | #16 |

## Anexo A. Contrato de importación del Excel del plan (OFE-D6)

La API no lee Excel (OFE-D6); estas reglas son el contrato con el front y quedan documentadas aquí para que la exportación (OFE-D7) sea su inverso. El formato de la UV tiene una hoja con estos encabezados en la fila 1:

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
