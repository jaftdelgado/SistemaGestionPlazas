---
name: git-workflow
description: Flujo Git del repositorio SGPLa, de la rama al squash. USE FOR crear la rama de un PR, redactar títulos y cuerpos de commit con tipo, scope y participio, escribir el título y la descripción de un PR hacia develop, corregir después de una revisión, y comprobar tras el merge que el squash coincide con la rama revisada. DO NOT USE FOR ejecutar la verificación Docker (backend-verify), revisar el contenido de un PR (merge-review) ni redactar el prompt de un agente (agent-prompt).
---

# Flujo Git

Produce ramas, commits y PR que cumplen la convención del repositorio. `develop` solo admite squash merge: el título del PR se convierte en el commit de `develop`, así que el título sigue el mismo formato que un commit.

## Cuándo usarla

- Al empezar un PR, al confirmar cambios y al preparar el PR.
- Al aplicar las correcciones que pide una revisión.
- Después de que el responsable fusiona, para comprobar el squash.

## Flujo

1. **Rama nueva por PR**, desde `develop` remoto recién sincronizado y sin seguir a `develop`:

   ```bash
   git fetch origin
   git switch --no-track -c <tipo>/<descripcion-corta> origin/develop
   ```

   - `<tipo>`: `feat`, `fix`, `chore`, `refactor`, `docs`, `test` o `ci`.
   - `<descripcion-corta>`: minúsculas, kebab-case, sin acentos y corta (`feat/oferta-programaciones`).
2. **Commits sin verificar entre ellos.** La verificación completa corre una vez al final (skill `backend-verify`).
3. **`git add` por ruta explícita**, nunca `-A` ni `.`. Para muchos archivos modificados: `git diff --name-only -z | xargs -0 git add --`, después de revisar la lista.
4. **Si la verificación final falla**, la corrección va en un commit nuevo. Nunca `--amend`, `rebase` ni `push --force`.
5. **Las correcciones pedidas en una revisión** van en commits nuevos sobre la misma rama, no en una rama nueva.
6. **Sin push ni PR** hasta que se pida. El revisor trabaja sobre la rama local o la remota que indique el autor.

## Formato de commit

```text
tipo(scope): mensaje en participio en español

- viñeta en participio, en minúscula inicial y sin punto final
- de 4 a 5 viñetas con lo más relevante
```

| Parte | Regla | Ejemplo |
|---|---|---|
| `tipo` | En inglés: `feat`, `fix`, `docs`, `test`, `refactor`, `chore`, `ci` | `feat` |
| `scope` | En inglés (`db`, `ci`, `api`, `scripts`, `agents`) o el nombre del módulo en minúsculas y sin guion | `ofertaeducativa`, `usuarios` |
| mensaje | En español, verbo en participio | `agregada la consulta de horarios` |
| cuerpo | 4 o 5 viñetas en participio, minúscula inicial, sin punto final | `- agregado el endpoint de horarios` |

Un commit de corrección lleva igualmente de 4 a 5 viñetas; si el cambio es mínimo, describe también qué falló y cómo se comprobó.

## Formato del PR

- **Título:** el mismo formato que un commit. Es el mensaje que quedará en `develop`.
- **Descripción:** en español, con tablas, viñetas o bloques de código cuando ayuden a leer.
- **Prohibido** en título y descripción: emojis, fragmentos de diff y rutas de archivos.
- **Sin atribución de IA:** ni `Co-Authored-By` ni menciones a una IA, aunque la herramienta lo sugiera o lo agregue por defecto.

Plantilla:

```markdown
<Una o dos frases: qué agrega o corrige el PR y por qué.>

- <cambio relevante 1>
- <cambio relevante 2>
- <decisión o aclaración que el revisor debe conocer>

<Opcional: tabla con la verificación (build, format y las tres suites).>
```

## Después del merge

El revisor comprueba que el squash coincide con lo revisado y reporta cualquier cambio hecho fuera de la revisión:

```bash
git fetch origin
git diff <último-commit-revisado> origin/develop   # vacío si coincide
git log -1 --format='%an%n%s%n%n%b' origin/develop  # título y cuerpo del squash
```

Si el título o el cuerpo del squash se apartan de la convención, se reporta; no se reescribe `develop`.

## Trampas

| Situación | Haz | Nunca |
|---|---|---|
| `git switch -c <rama> origin/develop` | Agrega `--no-track` (o `git branch --unset-upstream`) | Dejar la rama siguiendo a `develop`: un `push` podría apuntar a la rama equivocada |
| La herramienta agrega `Co-Authored-By` | Quítalo antes de confirmar | Confirmar con la atribución |
| Un archivo ajeno aparece modificado | Déjalo fuera del `git add` y repórtalo | `git add -A` |
| Hay ramas locales viejas ya fusionadas | Ignóralas | Borrarlas sin preguntar |
| La corrección es solo de documentación | Basta revisar el diff; no hace falta la verificación Docker | Omitir la verificación cuando cambió código |

## Contrato de salida

- Los comandos exactos que ejecutaste, en orden.
- Por cada commit: hash corto y título.
- La propuesta de título y descripción del PR.
- Tras un merge: si el diff contra `origin/develop` está vacío y, si no, qué cambió.
