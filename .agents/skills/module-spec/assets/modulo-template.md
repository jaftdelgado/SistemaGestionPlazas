# Módulo <Modulo>

<Una o dos frases: qué resuelve el módulo y de qué módulos depende.>

Documentos relacionados: `PLAN_INICIAL.md` (tabla de módulos y grafo), `DATABASE.md` (<secciones del modelo>), `ESTANDAR_MODULOS.md` y `DECISIONES.md`.

## Contenido

1. Alcance
2. Decisiones y desviaciones
3. Piezas compartidas
4. Normalización y validación comunes
5. Autorización y ámbito
6. <Recurso 1>
7. <Recurso 2>
8. Contratos entre módulos
9. Auditoría
10. Composición del módulo
11. Entregas (PR)
12. Criterios de terminado

## 1. Alcance

Incluye:
- <recurso u operación>;

Fuera de alcance:
- <lo que queda para otro módulo o para después, con su entrada de `pendientes.md` si aplica>;

## 2. Decisiones y desviaciones

| # | Decisión | Desviación respecto a |
|---|---|---|
| D1 | <Decisión en una o dos frases, con lo esencial en negritas> | <`DATABASE.md` §n, `PLAN_INICIAL.md` o `ESTANDAR_MODULOS.md` §n; "—" si no se desvía> |

## 3. Piezas compartidas

<Tipos nuevos en SharedKernel o BuildingBlocks, paquetes nuevos (con su licencia revisada) o contratos que otro módulo expone. "Ninguna" si no hay.>

## 4. Normalización y validación comunes

| Campo | Normalización | Validación | Error |
|---|---|---|---|
| `<Campo>` | <recorte, mayúsculas…> | <longitud, formato> | `<Entidad>.<Codigo>` |

## 5. Autorización y ámbito

| Operación | <Rol 1> | <Rol 2> | <Rol 3> | Política de la ruta |
|---|---|---|---|---|
| <Consultar …> | <Sí / los de su ámbito> | | | (grupo) |

<Cómo se filtra por ámbito en consultas y comandos, y qué responde un recurso fuera del ámbito: 404 en la ruta, 400 en una referencia del cuerpo.>

## 6. <Recurso 1>

### Dominio

`Domain/<Recursos>/<Entidad>.cs`: `internal sealed class <Entidad> : Entity<, IEliminable>`.

| Propiedad | Tipo | Constante |
|---|---|---|
| `<Propiedad>` | `<tipo>` | `<LongitudMaxima… = n>` o — |

Fábrica y métodos: <`Crear(...)`, `Modificar(...)`, `DarDeBaja(...)` y las invariantes que protegen>.

`<Entidad>Errors`: <código, tipo y mensaje exacto de cada error>.

### Application

`Application/<Recursos>/<Entidad>Consultas.cs`:

```csharp
<records de respuesta, queries y filtros, con los <summary> exactos que deben copiarse>
```

Comandos (`<Accion><Entidad>.cs`): <comando, validator si el dominio no valida, puertos que usa y errores que devuelve>.

### Infrastructure

- `<Entidad>Configuration.cs`: <tabla, esquema, longitudes y filtro de baja lógica>.
- `<Entidad>Repository.cs`: <métodos del puerto>.
- `<Entidad>Consultas.cs`: <joins, ámbito antes de los filtros, orden y paginación>.

### Endpoints

`Endpoints/<Recursos>/<Entidad>Endpoints.cs`, grupo `/<ruta>` con tag `<Tag>`:

| Método y ruta | `WithName` | `WithSummary` | Entrada | Éxito | Errores declarados |
|---|---|---|---|---|---|
| `GET /` | `<Listar…>` | <Resumen exacto.> | <request> | 200 `<Response>` | 400 |

## 7. <Recurso 2>

<Misma estructura que la sección anterior.>

## 8. Contratos entre módulos

<Cada contrato: quién lo declara (en su `Application/Contracts`), quién lo implementa, firma exacta y qué hace el sistema mientras no hay implementación.>

## 9. Auditoría

<Qué eventos se registran con `[LoggerMessage]` y qué datos nunca se registran. "Sin eventos propios" si no hay.>

## 10. Composición del módulo

<Registros en `<Modulo>Module`: repositorios, contratos que implementa, políticas y mapeo de endpoints.>

## 11. Entregas (PR)

<N> PR en orden, cada uno en una rama nueva desde `origin/develop` y con el CI en verde por sí solo.

### PR 1: <título>

- **Código:** <piezas y secciones de este documento>.
- **Pruebas:**
  - unitarias (`<Modulo>/<Entidad>Tests.cs`): <casos>;
  - integración (`<Modulo>/<Entidad>EndpointsTests.cs`): <éxito y cada error documentado; aserciones acotadas a los datos de cada prueba>.
- **Documentos:** <correcciones o entradas de `pendientes.md` que actualiza>.

## 12. Criterios de terminado

Cada PR cumple la lista de verificación de `ESTANDAR_MODULOS.md` §15. La verificación se ejecuta una vez, al terminar la implementación, con `sgpla-backend/scripts/verify.sh`, seguida de `sgpla-backend/scripts/smoke.sh`. Prueba de humo por PR:
- **PR 1:** <peticiones `curl` con el token y códigos esperados; lo que requiere LDAP de la UV se reporta como omitido si no hay red>.
