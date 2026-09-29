# Pendientes

Reglas del modelo que todavía no se implementan porque dependen de un módulo que aún no existe. Cada entrada dice de dónde sale la regla, qué la desbloquea, cómo se resolverá y qué hace el sistema mientras tanto.

Cuando se implemente el módulo que la desbloquea, la entrada se resuelve en el mismo PR y se borra de este archivo.

## Institucional

Contexto: `Modulo_Institucional.md`, decisión D6. Institucional no puede depender de OfertaEducativa, porque este depende de él. Las reglas que cruzan hacia él se resuelven con contratos en `Institucional.Application.Contracts` que implementa el módulo dependiente (inversión de dependencias, como `IReferenciasArticulo` en Catalogos; `ESTANDAR_MODULOS.md` §5).

### P1. La baja de una entidad académica se propaga a sus programas

- **Origen:** `DATABASE.md` §9.1. La baja sigue la jerarquía entidad → programa → plan → EE → programación, en una sola transacción, con el mismo instante UTC y sin reemplazar fechas previas; los horarios afectados se eliminan físicamente.
- **Lo desbloquea:** OfertaEducativa.
- **Resolución prevista:** contrato `IBajaEnCascadaEntidadAcademica` con `Task DarDeBajaDescendientesAsync(int entidadAcademicaId, DateTime instante, CancellationToken)`, que implementa OfertaEducativa. `DarDeBajaEntidadAcademicaHandler` lo invoca con todas sus implementaciones antes de `SaveChangesAsync`. Todos los módulos comparten `SgplaDbContext`, así que el guardado es atómico.
- **Mientras tanto:** la baja de una entidad solo marca la entidad.

### P2. El área académica de una entidad se congela cuando tiene programas

- **Origen:** `DATABASE.md` §6.5 y §10. `entidad_academica.area_academica_id` puede cambiar solo mientras la entidad no tenga programas educativos, incluidos los dados de baja.
- **Lo desbloquea:** OfertaEducativa.
- **Resolución prevista:** contrato `IProgramasDeEntidadAcademica` con `Task<bool> TieneProgramasAsync(int entidadAcademicaId, CancellationToken)`, que implementa OfertaEducativa contando también los programas dados de baja. `ModificarEntidadAcademicaHandler` lo consulta solo si cambia el área, y responde 409 `EntidadAcademica.AreaAcademicaInmutable`.
- **Mientras tanto:** el área siempre puede cambiarse.
