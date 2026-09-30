---
name: agent-prompt
description: Redacción de prompts autocontenidos para que otro agente implemente un PR de SGPLa o corrija una rama revisada. USE FOR escribir el prompt de implementación de un PR a partir de su Modulo_<X>.md, el prompt breve de corrección tras un veredicto de merge, o responder a un agente que se detuvo por una ambigüedad. DO NOT USE FOR decidir el alcance o las decisiones del módulo (module-spec), revisar la entrega (merge-review) ni implementar tú mismo (backend-resource).
---

# Prompt para un agente

Produce un prompt que otro agente, sin el historial de la conversación, pueda ejecutar de principio a fin sin decidir nada por su cuenta. Todo lo que el agente necesite saber va escrito o referenciado por ruta exacta.

## Cuándo usarla

- Para cada PR de la sección "Entregas" de un `Modulo_<X>.md`.
- Tras un veredicto con hallazgos: prompt breve de corrección sobre la misma rama.
- Cuando un agente se detiene y pregunta: la respuesta también es un fragmento de prompt, igual de exacto.

## Estructura obligatoria (en este orden)

1. **Rama.** Nombre y comando de creación desde `origin/develop` recién sincronizado (ver `git-workflow`). En una corrección: la rama existente, con commits nuevos.
2. **Análisis previo.** Lista de lecturas con rutas exactas: la sección del `Modulo_<X>.md`, `ESTANDAR_MODULOS.md` (secciones concretas), el código de referencia del mismo patrón y las pruebas de referencia.
3. **Alcance exacto:**
   - qué se implementa, remitiendo a secciones del `Modulo_<X>.md`;
   - decisiones y aclaraciones ya tomadas que no están en los documentos;
   - correcciones de documentación, cada una con el texto actual y el texto nuevo en bloques de código;
   - lo que queda fuera.
4. **Commits sugeridos,** con título y tipo; el primero suele ser el de documentación.
5. **Verificación final, no por commit:** `sgpla-backend/scripts/verify.sh`, `sgpla-backend/scripts/smoke.sh` y los `curl` concretos con sus códigos esperados. Si falla, la corrección va en un commit separado y se repite.
6. **Reglas:**
   - "Si algo es ambiguo, DETENTE y pregunta."
   - `git add` por ruta explícita, nunca `-A` ni `.`.
   - Sin push ni PR: el revisor revisa la rama local.
   - Sin atribución de IA en commits.
   - Docker como único entorno.
   - No ampliar el alcance: lo que encuentre fuera se reporta como nota.
7. **Formato de entrega:** tabla de commits (hash y título), tabla de verificación (build, format y las tres suites con su diferencia), resultados de la prueba de humo, cumplimiento punto por punto, decisiones no escritas o desviaciones, commits de corrección y propuesta de título y descripción del PR.

## Reglas de redacción

| Cuando | Haz | Nunca |
|---|---|---|
| Citas un texto de un documento | Cópialo leyendo el archivo, carácter por carácter | Citarlo de memoria: un agente se detiene ante cualquier diferencia |
| Una decisión no está escrita | Pregúntala al responsable antes de redactar | Resolverla dentro del prompt como si estuviera decidida |
| El agente debe copiar un texto literal | Ponlo en un bloque de código y di "debe quedar exactamente así" | Describirlo con palabras |
| Pides pruebas de integración | Indica qué datos crea cada prueba y que toda aserción sobre un listado se acote a ellos | Dejar el aislamiento implícito |
| Pides datos que solo un rol sin acceso de prueba puede crear | Indica cómo insertarlos (por ejemplo, helpers SQL de las pruebas o `sqlcmd` en el contenedor) | Suponer que el agente lo resolverá |
| La especificación resultó incompleta | Incluye su corrección como commit de docs en el mismo PR | Dejar el documento desactualizado |

## Prompt de corrección

Breve y sobre la misma rama:

```markdown
Rama: <rama existente> (commits nuevos; sin amend ni rebase).

Hallazgos a corregir:
1. <archivo o área>: <qué está mal> → <qué debe quedar>.

Commits sugeridos:
- <tipo(scope): corregido …>

Verificación: la completa de backend-verify, una vez al final (solo revisar el diff si la corrección es únicamente de documentación).

Reglas: si algo es ambiguo, DETENTE y pregunta; git add por ruta; sin push ni PR.
```

## Validación

- Las 7 secciones están presentes y en orden.
- Cada texto citado coincide con el archivo, comprobado leyéndolo.
- Nada en el alcance depende de una decisión que no esté escrita o respondida.

## Contrato de salida

El prompt completo en un bloque listo para copiar, precedido de una lista de las decisiones que el prompt fija y de dónde salió cada una.
