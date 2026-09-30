---
name: module-spec
description: Especificación de un módulo del backend de SGPLa en un Modulo_<X>.md, y su cierre. USE FOR empezar un módulo nuevo, decidir su alcance, redactar la tabla de decisiones D#, partir el trabajo en PR, corregir una especificación que una revisión encontró incompleta, y cerrar un módulo terminado (pasar sus decisiones a DECISIONES.md y borrar su documento). DO NOT USE FOR redactar el prompt de implementación de un PR (agent-prompt), implementar código (backend-resource) ni revisar una entrega (merge-review).
---

# Especificar un módulo

Produce un `Modulo_<X>.md` en la raíz del repositorio, completo al punto de que un agente pueda implementar cada PR sin decidir nada por su cuenta. La plantilla está en [assets/modulo-template.md](assets/modulo-template.md).

## Cuándo usarla

- Antes de escribir código de un módulo nuevo (por ejemplo, el siguiente de la tabla de módulos de `PLAN_INICIAL.md`).
- Cuando una revisión detecta que la especificación quedó mal o incompleta: la corrección va dentro del PR en curso, normalmente como primer commit de docs.
- Cuando se fusiona el último PR del módulo: toca el cierre.

## Entradas (descúbrelas, no las pidas)

- `PLAN_INICIAL.md`: tabla de módulos, grafo de dependencias y alcance previsto.
- `DATABASE.md`: tablas, reglas y casos de aceptación del módulo.
- `ESTANDAR_MODULOS.md`: cómo se construye cualquier módulo.
- `DECISIONES.md`: decisiones previas que el módulo hereda o debe respetar.
- `pendientes.md`: entradas `P#` que este módulo desbloquea.
- El código de los módulos de los que depende, sobre todo sus `Application/Contracts`.

## Flujo

1. **Lee** las entradas y lista lo que ya está decidido por escrito.
2. **Pregunta todo lo demás** antes de redactar. Cada pregunta lleva de 2 a 4 opciones con sus consecuencias y una recomendación primero, marcada "(Recomendado)". Agrupa en rondas de hasta 4 preguntas. Si te preguntan "¿cuál conviene?", recomienda con razones. Temas que casi siempre quedan abiertos:
   - quién lee y quién escribe cada recurso (roles y ámbito);
   - baja lógica, restauración y bloqueos por hijos activos;
   - qué pasa con referencias a registros dados de baja o fuera del ámbito (400 o 404);
   - campos inmutables y congelamientos;
   - contratos con otros módulos: quién los declara y quién los implementa;
   - desviaciones respecto a `DATABASE.md`, `PLAN_INICIAL.md` o el estándar.
3. **Redacta desde la plantilla.** Cada decisión va en la tabla D# con la columna "Desviación respecto a" (documento y sección) o "—".
4. **Parte el trabajo en PR** que dejen el CI en verde por sí solos, en orden de dependencia. Cada PR dice su código, sus pruebas (archivo y casos) y los documentos que actualiza.
5. **Escribe los criterios de terminado**: `verify.sh`, `smoke.sh` y los `curl` concretos de la prueba de humo de cada PR.
6. **Registra los pendientes**: lo que depende de un módulo futuro va a `pendientes.md` con origen, qué lo desbloquea, resolución prevista y qué hace el sistema mientras tanto.
7. **Revisa contra el código real**: firmas, nombres de contratos y rutas que la especificación cita deben existir o estar marcados como nuevos.

## Reglas de redacción

| Cuando | Haz | Nunca |
|---|---|---|
| Un texto debe copiarse literal (un `<summary>`, un mensaje de error, un encabezado de Excel) | Escríbelo en un bloque de código exacto, con sus signos | Parafrasearlo: el agente se detendrá por una diferencia |
| Citas otro documento | Léelo del archivo y cita documento y sección | Citar de memoria |
| Una regla ya está en el estándar | Remite a la sección | Copiarla: dos fuentes divergen |
| Un comportamiento no está decidido | Pregunta | Elegir y seguir |
| Una decisión cambia una regla del estándar | Actualiza `ESTANDAR_MODULOS.md` en el mismo PR | Dejar la desviación solo en el módulo |
| Las pruebas de integración comparten la base | Pide aserciones acotadas a los datos de cada prueba | Aserciones sobre listados completos |

## Cierre del módulo

Cuando el último PR del módulo está fusionado:

1. Copia literalmente la tabla D# a `DECISIONES.md`, en una sección nueva con un prefijo de tres letras (`INS`, `USU`, `OFE`…), IDs `<PREFIJO>-Dn` y la columna **PR** con el número del PR en que cada decisión entró a `develop`.
2. Reescribe las referencias internas de las filas: `D7` → `<PREFIJO>-D7`, "D5 de Institucional" → `INS-D5`, "la sección 9" → un anexo en `DECISIONES.md` si el contenido no está en otro lado.
3. Actualiza las referencias de los demás documentos (`DATABASE.md`, `PLAN_INICIAL.md`, `ESTANDAR_MODULOS.md`, `pendientes.md`) a `DECISIONES.md` y el ID.
4. Conserva como anexo lo que no se puede derivar del código, por ejemplo un contrato con el frontend.
5. Elimina el `Modulo_<X>.md` y comprueba que ningún archivo lo cite.

## Validación

- Ninguna sección de la plantilla quedó con marcadores sin reemplazar.
- Cada D# tiene su desviación o "—", y cada PR tiene código, pruebas, documentos y criterio de humo.
- Todo nombre de contrato, ruta o tipo existente que se cite coincide con el código.

## Contrato de salida

- Las rondas de preguntas hechas y sus respuestas.
- El `Modulo_<X>.md` generado o el diff de la corrección.
- La lista de PR con su rama sugerida y sus commits sugeridos.
- Las entradas nuevas o resueltas de `pendientes.md`.
