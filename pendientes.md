# Pendientes

Reglas del modelo que todavía no se implementan porque dependen de un módulo que aún no existe. Cada entrada dice de dónde sale la regla, qué la desbloquea, cómo se resolverá y qué hace el sistema mientras tanto.

Cuando se implemente el módulo que la desbloquea, la entrada se resuelve en el mismo PR y se borra de este archivo.

## OfertaEducativa

Contexto: `Modulo_OfertaEducativa.md`, sección 12. OfertaEducativa declara en su `Application.Contracts` los contratos de referencias; los implementan los módulos que dependen de él, y los handlers los consultan como `IEnumerable<...>`, vacío hasta entonces.

### P7. Las Solicitudes de Apertura bloquean la baja de EE y planes

- **Origen:** `DATABASE.md` §16.6. La baja de una EE, un plan, un programa, una entidad o un periodo se bloquea mientras exista una Solicitud de Apertura PENDIENTE o ACEPTADA. Sin cascada (D10), basta con bloquear en la EE y en el plan: programa y entidad ya exigen hijos dados de baja. El periodo lo cubre P8.
- **Lo desbloquea:** SolicitudesApertura.
- **Resolución prevista:** SolicitudesApertura implementa `IReferenciasExperienciaEducativa.TieneReferenciasAsync` (declarado en `OfertaEducativa.Application.Contracts` desde el PR 3 de `Modulo_OfertaEducativa.md`; sin implementaciones) con sus solicitudes PENDIENTE o ACEPTADA de esas EE. Las bajas de EE y de plan responden 409 `ExperienciaEducativa.TieneReferencias`.
- **Mientras tanto:** las bajas de EE y plan solo se bloquean por programaciones activas.

### P8. Un periodo escolar referenciado no se da de baja

- **Origen:** `Modulo_OfertaEducativa.md` D11: la baja del periodo se bloquea con cualquier referencia. Hoy lo referencian `integracion.sincronizacion_planea`, `academico.solicitud_apertura` y `plazas.aviso`.
- **Lo desbloquea:** Integracion, SolicitudesApertura y Publicacion, cada uno por sus tablas.
- **Resolución prevista:** cada módulo implementa `IReferenciasPeriodoEscolar.TieneReferenciasAsync` (declarado en `OfertaEducativa.Application.Contracts` desde el PR 2 de `Modulo_OfertaEducativa.md`; sin implementaciones) y la registra en su composición. `DarDeBajaPeriodoEscolarHandler` responde 409 `PeriodoEscolar.TieneReferencias`.
- **Mientras tanto:** la baja de un periodo solo se bloquea por sus programaciones.
- **Se resuelve:** por partes; la entrada se borra cuando el último de los tres módulos lo implemente.

### P9. Alta y baja de programaciones y horarios desde PLANEA

- **Origen:** `Modulo_OfertaEducativa.md` D12. Las programaciones académicas no se capturan en SGPLa: las trae PLANEA. El payload real (`/apiroladoovr/periodo/{PERIODO}`) trae `sec_programa` (código del plan), `sec_campus`, `radoc_periodo`, `radoc_nrc`, `radoc_materia`, `radoc_curso` y datos del docente, pero **no** días, horas, edificio, aula ni fechas de sesión. `DATABASE.md` §12 no coincide con él.
- **Lo desbloquea:** Integracion.
- **Resolución prevista, a detallar en la especificación de Integracion:**
  - contrato de OfertaEducativa para crear y dar de baja programaciones, y para reemplazar sus horarios (borrado físico);
  - resolución de la EE de un NRC por campus de la entidad (`sec_campus`), código de plan, materia y curso, porque el código de plan se repite entre campus;
  - de dónde salen los horarios, si no vienen en este endpoint;
  - corrección de `DATABASE.md` §12.
- **Mientras tanto:** OfertaEducativa ya expone la lectura de programaciones y horarios (`GET /api/v1/oferta-educativa/programaciones-academicas`, su detalle y sus horarios; `Modulo_OfertaEducativa.md`, PR 4), pero nada los crea todavía. Una EE con programaciones activas no se da de baja.
