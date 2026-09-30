---
name: backend-resource
description: Implementación de código del backend de SGPLa (.NET 10, monolito modular, Clean Architecture por módulo). USE FOR agregar o modificar un recurso (entidad, comandos, consultas, endpoints), un catálogo fijo, un contrato entre módulos, una migración de DbUp o un módulo nuevo, y resolver errores de capas, de visibilidad o de filtros de EF Core. DO NOT USE FOR escribir o corregir pruebas (backend-testing), ejecutar la verificación (backend-verify) ni especificar el módulo (module-spec).
---

# Recurso del backend

Implementa código que cumple `ESTANDAR_MODULOS.md` a la primera, siguiendo el patrón que ya usan los módulos existentes. El estándar es la norma; esta skill da el orden de trabajo y las trampas que más se repiten.

## Entradas (descúbrelas)

- La sección del recurso en el `Modulo_<X>.md` vivo, con sus decisiones D#.
- `ESTANDAR_MODULOS.md`: §2 (capas), §3 (carpetas), §4 (nombres), §5 (visibilidad y contratos), §6 a §10 (cada capa y la composición) y §14 (receta).
- Un recurso ya implementado del mismo tipo en `sgpla-backend/src/Modules/`: úsalo como plantilla viva en lugar de inventar la forma.

## Flujo (receta del estándar §14)

1. **Domain** — `Domain/<Recursos>/<Entidad>.cs`: `internal sealed class <Entidad> : Entity` (más `IEliminable` si tiene baja lógica), constantes de longitud, fábrica `Crear` y métodos de comportamiento que validan. `<Entidad>Errors` con el campo en cada error de validación.
2. **Application** — `Application/<Recursos>/<Entidad>Consultas.cs` con los `Response`, queries y filtros; un archivo `<Accion><Entidad>.cs` por comando, con su handler y validator solo para lo que el dominio no valida; el puerto `I<Entidad>Repository`.
3. **Infrastructure** — `<Entidad>Configuration.cs` (tabla, esquema, longitudes y filtro de baja lógica), `<Entidad>Repository.cs` y `<Entidad>Consultas.cs` con los handlers de consulta.
4. **Endpoints** — `Endpoints/<Recursos>/<Entidad>Endpoints.cs` con `MapGroup`, `WithName`, `WithSummary`, errores declarados y `TypedResults` (`ToOk`, `ToProblem`); un `Request` solo donde difiere del comando.
5. **Composición** — registra repositorios y contratos en `<Modulo>Module` y mapea el grupo en `Map<Modulo>Endpoints`. Los handlers y validators se registran solos por ensamblado.
6. **Esquema** — si hace falta un cambio, crea `src/Sgpla.Database/Scripts/####__descripcion.sql`. Nunca edites `baseline.sql` ni `seed.sql`, ni declares índices o restricciones en EF.

## Dónde va cada cosa

| Pieza | Capa | Regla |
|---|---|---|
| Invariantes y normalización de una entidad | Domain | La entidad nunca queda inválida |
| Comando (crear, modificar, dar de baja) | Application, con puertos | Nunca `SgplaDbContext` en un handler de comando |
| Consulta (listar, obtener) | Infrastructure, con `AsNoTracking` | Sin puerto ni handler intermedio |
| Tipo que comparten un request y un comando | Application (`<Entidad>Entrada`) | Endpoints no puede depender de Domain ni de Infrastructure |
| Lo que otro módulo necesita | `Application/Contracts` del módulo que lo expone | Nunca tipos internos de otro módulo |
| Reglas de negocio fallidas | `Result` con `Error` | Nunca excepciones |
| Hora actual | `TimeProvider` inyectado | Nunca `DateTime.UtcNow` |
| Unicidad de textos | La compara SQL Server con la intercalación de la columna | Nunca `ToLower` o `ToUpper` en memoria para decidir unicidad |

## Trampas conocidas

| Situación | Haz | Nunca |
|---|---|---|
| Necesitas incluir registros dados de baja (por ejemplo, para saber si algo tuvo hijos alguna vez) | Una consulta aparte con `IgnoreQueryFilters([FiltrosConsulta.BajaLogica])` | Ponerla dentro de la consulta principal: se extiende a **toda** la consulta y apaga también el filtro de las demás entidades |
| Consulta con ámbito por rol | Aplica el ámbito **antes** de los demás filtros | Filtrar primero y restringir después |
| Datos de otro módulo para una página de resultados | **Una** llamada al contrato con todos los ids de la página | Una llamada por fila |
| Un handler devuelve dos tipos de resultado | Declara `Task<Results<Ok<T>, ProblemHttpResult>>` explícito | `TypedResults` en un ternario sin tipo de retorno |
| Error de validación en una colección | El campo sale como `items[3].campo`: `ErrorHttpExtensions` pasa cada segmento a camelCase | Construir el nombre del campo a mano en el endpoint |
| Recurso de solo lectura | Solo `MapGet`; el enrutamiento ya responde 405 al resto | Endpoints que devuelvan 405 a mano |
| Referencia del cuerpo a un registro inexistente, dado de baja o fuera del ámbito | 400 con el campo, sin revelar si existe | 409 o 404 |
| Recurso de la ruta fuera del ámbito | 404 con el `NoEncontrado` del recurso | 403 |
| Comentario que justifica una regla | Enuncia la regla | Citar un documento markdown |
| El build falla por un analizador (`TreatWarningsAsErrors`) | Corrige el código | `#pragma`, `SuppressMessage` sin justificación o `<NoWarn>` |

## Primer recurso de un módulo

Los módulos de `PLAN_INICIAL.md` ya existen como esqueleto: proyecto `Sgpla.Modules.<Modulo>`, `<Modulo>Module` público registrado en `Sgpla.Api/Program.cs` e `InternalsVisibleTo` para `Sgpla.UnitTests`. Al implementar el primer recurso:
- agrega a `<Modulo>Module` las llamadas `AddPersistenciaModulo` y `AddHandlersModulo` y el grupo de endpoints, siguiendo un módulo ya implementado;
- si el módulo necesita otro, agrega el `ProjectReference` en su `.csproj` y la dependencia en `tests/Sgpla.ArchitectureTests/Modulos.cs`, igual al grafo de `PLAN_INICIAL.md`. Un cambio de grafo es una decisión del módulo y va en su tabla D#.

## Validación

- Compila sin advertencias y pasa `dotnet format` (se comprueba con `backend-verify`, una vez al final).
- Cada endpoint aparece en `/openapi/v1.json` con su nombre, resumen y errores declarados.
- Las pruebas del recurso existen (skill `backend-testing`).

## Contrato de salida

- La lista de archivos creados o modificados, agrupados por capa.
- Las decisiones no escritas que tomaste, si hubo alguna, para que el responsable las apruebe. Si algo era ambiguo, debiste detenerte y preguntar.
