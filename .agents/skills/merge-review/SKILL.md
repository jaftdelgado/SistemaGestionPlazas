---
name: merge-review
description: Revisión de la rama de un PR de SGPLa y veredicto de merge. USE FOR revisar lo que entregó un agente o un integrante, contrastar el diff con su Modulo_<X>.md y el estándar, verificar con Docker, revisar commits y atribuciones, y redactar el prompt de corrección si hay hallazgos. DO NOT USE FOR implementar las correcciones (backend-resource o backend-testing), redactar el prompt inicial de un PR (agent-prompt) ni la comprobación del squash después del merge (git-workflow).
---

# Veredicto de merge

Produce un veredicto sustentado: se puede fusionar, o no, con los hallazgos concretos que lo impiden. Revisa un integrante distinto del autor; el responsable del proyecto hace el squash.

## Cuándo usarla

- Cuando el autor avisa que la rama está lista, con su reporte de entrega.
- Cuando el autor agregó commits de corrección tras un veredicto anterior.

## Entradas (descúbrelas)

- La rama y su base: `git log --oneline origin/develop..<rama>`.
- La especificación: la sección del PR en el `Modulo_<X>.md` y las aclaraciones del prompt del agente.
- `ESTANDAR_MODULOS.md` §15 (lista de verificación) y §16 (lo que no se hace).
- El reporte del autor: sus conteos y decisiones no escritas.

## Flujo

1. **Prepara el árbol.** `git fetch origin`, cambia a la rama del PR y confirma que el árbol está limpio (`git status --short` vacío). `verify.sh` verifica la copia de trabajo: en la rama equivocada, el veredicto no vale.
2. **Lanza la verificación en segundo plano:** `sgpla-backend/scripts/verify.sh` (unos 4 minutos). Mientras corre, revisa el diff.
3. **Revisa el diff contra la especificación,** punto por punto: `git diff origin/develop...<rama>`. Para cada punto del alcance, localiza su implementación y su prueba.
4. **Revisa el estándar:**
   - capas, visibilidad (`internal` salvo el módulo y los contratos) y contratos entre módulos solo en `Application/Contracts`;
   - `TypedResults`, errores declarados en los endpoints y validación donde corresponde;
   - consultas con `AsNoTracking` y ámbito antes de los filtros;
   - `IgnoreQueryFilters` solo en una consulta aparte, porque se extiende a toda la consulta;
   - ningún comentario, `Justification` ni script SQL cita un documento markdown.
5. **Revisa las pruebas** con `backend-testing`: éxito y cada error documentado, aserciones acotadas a los datos de cada prueba, formatos de serialización probados, fakes escritos a mano.
6. **Revisa los atajos** que avisa `verify.sh`: cada uno necesita justificación explícita. Un `Skip=`, un `catch` vacío o un `<NoWarn>` para ocultar un fallo bloquean el merge.
7. **Revisa los commits:** títulos y cuerpos según `git-workflow`, ningún `Co-Authored-By` ni mención a una IA (`git log --format='%an <%ae>%n%(trailers)' origin/develop..<rama>`), y ninguna ruta, diff o emoji en la propuesta de PR.
8. **Contrasta los conteos** con los del reporte del autor y con los de `develop`. Una diferencia no explicada es un hallazgo.
9. **Emite el veredicto.**

## Criterios

| Hallazgo | Bloquea | Va como nota |
|---|---|---|
| Build con advertencias, format con cambios o una suite fallida | Sí | |
| Un punto del alcance sin implementar o sin probar | Sí | |
| Una decisión no escrita que cambia el comportamiento | Sí, hasta que el responsable la apruebe | |
| Atribución de IA en un commit | Sí | |
| Atajo sin justificación | Sí | |
| Cita a un documento markdown en el código | Sí | |
| Aserción sobre un listado sin acotar | Sí: es inestable en la base compartida | |
| Cuerpo de commit con menos de 4 viñetas, en un commit que el squash descartará | | Sí |
| Estilo o nombres mejorables sin efecto en el contrato | | Sí |
| Algo fuera del alcance del PR | | Sí; no se pide corregirlo aquí |

## Contrato de salida

```markdown
# Veredicto de merge: <PR> (<rama>, hasta <hash>)

**<Se puede fusionar. / No se puede fusionar todavía.>** <Una frase con el motivo principal.>

| Comprobación | Resultado | Diferencia contra develop |
|---|---|---|
| Build | <n advertencias, n errores> | — |
| Format | <sin cambios / con cambios> | — |
| Arquitectura | <superadas / total> | <+n> |
| Unitarias | <superadas / total> | <+n> |
| Integración | <superadas / total> | <+n> |
| Atajos nuevos | <ninguno / lista> | — |

## Cumplimiento punto por punto
- <punto de la especificación>: <dónde se implementa y qué prueba lo cubre> ✔ / ✘

## Notas que no bloquean
1. <nota>

## Prompt de corrección
<Solo si hay hallazgos: el prompt breve de la skill agent-prompt.>
```

Reporta los resultados tal como salieron. Si no pudiste ejecutar algo (por ejemplo, un login que necesita el LDAP de la UV), dilo; no lo des por bueno.
