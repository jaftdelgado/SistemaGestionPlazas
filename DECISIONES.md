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
