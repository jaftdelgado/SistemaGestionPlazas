---
name: skill-authoring
description: Autoría de las skills del proyecto SGPLa en .agents/skills y de sus envoltorios en .claude/skills. USE FOR crear una skill nueva, modificar la descripción o el contenido de una existente, dividir una skill que creció, retirar una que ya no aporta, o arreglar el paso de CI agents-ci cuando falla. DO NOT USE FOR cambiar el estándar de implementación (ESTANDAR_MODULOS.md) ni especificar un módulo (module-spec).
---

# Autoría de skills

Produce skills que el agente carga en el momento justo y que cambian su comportamiento respecto a no tenerlas. Una skill que solo repite lo que el modelo ya sabe no aporta y gasta contexto.

## Principios

- **Solo metodología.** Una skill describe cómo se trabaja: patrones, pasos, comandos y trampas. Nunca reglas de negocio ni conceptos del dominio; esos viven en `DATABASE.md`, `DECISIONES.md` y los `Modulo_<X>.md`.
- **Procedimental.** `ESTANDAR_MODULOS.md` es la única fuente normativa: la skill remite a sus secciones en lugar de copiarlas.
- **Ejemplos con marcadores** (`<Modulo>`, `<Entidad>`, `<Accion>`) y remisión al código real del repositorio como ejemplo vivo.
- **Escribe lo que el modelo haría mal.** Borra lo que ya produce sin ayuda.

## Estructura

```text
.agents/skills/<nombre>/SKILL.md          la skill (canónica)
.agents/skills/<nombre>/references/*.md   casos raros o extensos, opcionales
.agents/skills/<nombre>/assets/*          plantillas, opcionales
.claude/skills/<nombre>/SKILL.md          envoltorio para Claude Code
```

Nombre: en inglés, kebab-case, igual al nombre de la carpeta. Prefijo `backend-` o `frontend-` para las de un stack; sin prefijo para las compartidas. Evita nombres genéricos que choquen con skills de las herramientas (`code-review`, `security-review`).

Secciones del `SKILL.md`, en este orden: propósito, cuándo usarla, entradas (que el agente descubre, no que pide), flujo con puntos de control, tablas de trampas, validación y contrato de salida. Menos de 500 líneas; lo raro o largo va en `references/` y se lee solo cuando hace falta.

## La descripción decide si se carga

Es el único texto que el agente ve al elegir una skill. Una sola línea en el frontmatter, de 1 024 caracteres como máximo:

```yaml
description: <Qué hace, en una frase>. USE FOR <tareas, síntomas, artefactos y frases como las diría alguien del equipo>. DO NOT USE FOR <tareas vecinas> (<skill que las atiende>).
```

- Distingue a las skills hermanas por lo que realmente las separa (por ejemplo, escribir pruebas frente a ejecutarlas) y escribe la exclusión en **las dos** descripciones.
- Relee cada "DO NOT USE FOR": una exclusión mal escrita puede bloquear el propósito de la propia skill.

## Contenido que funciona

| En lugar de | Escribe |
|---|---|
| Prosa de referencia o listas de alternativas | Tablas "cuando A, haz B, nunca C" |
| "Verifica que todo funcione" | El comando exacto y cómo leer su resultado |
| Un informe de muchas secciones para cualquier tamaño | Una salida proporcional a la tarea |
| Pedir rutas al usuario | Decirle al agente dónde encontrarlas |
| Confiar en que el agente reporte bien | Exigir el resultado tal como salió, incluidos los pasos omitidos |

Cierra siempre con un **contrato de salida**: el comando, la tabla o el veredicto exacto que la skill entrega. Agrega condiciones de parada ("si algo es ambiguo, DETENTE") donde una skill fuerte podría aplicarse de más.

## Envoltorio para Claude Code

Claude Code descubre las skills en `.claude/skills/`. El envoltorio repite `name` y `description` exactamente y remite a la skill canónica:

```markdown
---
name: <nombre>
description: <la misma descripción, idéntica>
---

Esta skill vive en `.agents/skills/<nombre>/SKILL.md`. Lee ese archivo completo, junto con los archivos que cite, y sigue sus instrucciones.
```

## Flujo

1. Confirma que la tarea no la cubre ya otra skill; si se solapan, ajusta los límites en ambas.
2. Escribe o modifica `.agents/skills/<nombre>/SKILL.md`.
3. Crea o actualiza su envoltorio en `.claude/skills/<nombre>/SKILL.md`.
4. Agrega o actualiza la fila de la skill en la tabla de skills de `AGENTS.md`.
5. Ejecuta la validación local de `agents-ci`: `bash .github/scripts/check-skills.sh`.
6. Confirma con `docs(agents): …` (ver `git-workflow`).

## Validación

`.github/scripts/check-skills.sh` falla si:
- una skill no tiene envoltorio o un envoltorio no tiene skill;
- `name` no coincide con la carpeta o difiere del envoltorio;
- la descripción difiere del envoltorio, está vacía o supera 1 024 caracteres;
- la skill no aparece en la tabla de `AGENTS.md`.

## Contrato de salida

- La lista de archivos creados o modificados (skill, envoltorio y `AGENTS.md`).
- La descripción final y su longitud.
- El resultado de `check-skills.sh`.
