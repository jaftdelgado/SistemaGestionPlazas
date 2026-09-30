---
name: backend-testing
description: Pruebas del backend de SGPLa con xUnit v3, Shouldly, Testcontainers (SQL Server) y NetArchTest. USE FOR escribir o corregir pruebas unitarias de entidades y handlers con fakes escritos a mano, pruebas de integración de endpoints contra la base compartida, pruebas de arquitectura de un módulo nuevo, helpers SQL para datos sin endpoint, y diagnosticar pruebas inestables o que fallan solo en paralelo. DO NOT USE FOR ejecutar la verificación completa o la prueba de humo (backend-verify) ni implementar código de producción (backend-resource).
---

# Pruebas del backend

Produce pruebas que fallan por la razón correcta y que no se interfieren entre sí en una base compartida y en paralelo. La norma está en `ESTANDAR_MODULOS.md` §12; esta skill da el orden de trabajo y las trampas.

## Entradas (descúbrelas)

- La sección "Pruebas" del PR en el `Modulo_<X>.md`: qué archivo y qué casos.
- `tests/Sgpla.IntegrationTests/Infraestructura/`: `SgplaApiFactory` (clientes por rol), `SqlServerFixture` (una base por ensamblado) y `DatosUnicos`.
- Las pruebas del mismo patrón en `tests/Sgpla.UnitTests/<Modulo>/` y `tests/Sgpla.IntegrationTests/<Modulo>/`, incluidos sus escenarios y helpers SQL.

## Qué prueba cada proyecto

| Proyecto | Qué cubre | Qué no |
|---|---|---|
| `Sgpla.UnitTests` | Fábricas, normalización e invariantes de cada entidad; ramas de un handler de comando con lógica propia, con fakes | Handlers de consulta (los cubre integración) |
| `Sgpla.IntegrationTests` | Cada endpoint de punta a punta: éxito y **cada** error que declara (400, 404, 409), formatos de serialización y ámbito por rol | Reglas que ya prueba una unitaria, repetidas sin necesidad |
| `Sgpla.ArchitectureTests` | Capas, convenciones, grafo y observabilidad | Se edita solo para agregar un módulo o una regla nueva |

## Flujo

1. **Unitarias primero:** una clase `<Entidad>Tests` por entidad, con cada error y su campo, y cada normalización con una entrada que **deba cambiar** (`"  a1b2 "` → `"A1B2"`), no una ya normalizada.
2. **Fakes a mano:** puertos falsos pequeños en memoria, en un `Fakes.cs` del módulo. Sin bibliotecas de mocks.
3. **Integración:** una clase `<Entidad>EndpointsTests(SqlServerFixture sqlServer)` con `SgplaApiFactory` e `IAsyncDisposable`. Clientes por rol: `CrearClienteSuperusuarioAsync`, `CrearClienteDgaaAsync(areaId)` y `CrearClienteEntidadAcademicaAsync(entidadId)`.
4. **Datos propios en cada prueba:** valores únicos con `DatosUnicos`; lo que no tiene endpoint se inserta con un helper SQL de la carpeta del módulo, que respeta los CHECK del esquema.
5. **Aserciones acotadas:** todo listado se filtra por lo que creó la prueba (su entidad, su periodo, su plan…) y se compara con los ids esperados, en orden.
6. **Formatos:** si un endpoint devuelve horas o fechas, prueba el texto exacto (`"08:00:00"`, `"2026-08-10"`).

## Trampas conocidas

| Situación | Haz | Nunca |
|---|---|---|
| Aserción sobre un listado | Filtra por los datos de la prueba y compara ids exactos | Contar filas globales o suponer una tabla vacía: otras pruebas escriben en paralelo |
| Necesitas un valor único | `DatosUnicos` (su aleatoriedad es intencional) | Valores fijos que choquen entre pruebas; también fallan con un índice UNIQUE |
| Necesitas una clave que además ordene | Un generador monótono como `DatosUnicos.ClavePeriodo()` | Un valor aleatorio cuando la prueba verifica un orden |
| `ShouldBe` sobre una colección de enteros con mensaje | `ShouldBe(esperados)` sin mensaje, o compara con `ToList()` | Buscar una sobrecarga con `customMessage` que no existe para esa combinación |
| Un catálogo fijo | Afírmalo completo: la semilla lo define y nadie lo modifica | Tratarlo como datos compartidos variables |
| Una fila de la semilla con un valor que otra prueba puede cambiar | No la uses como dato estable | Afirmarla (en el pasado resultó inestable) |
| Prueba de un 404 por ámbito | Crea el recurso en otra área o entidad y consúltalo con el cliente del rol | Suponer que un id alto no existe |
| La prueba falla por una razón ajena al PR | Repórtala | `Skip=`, borrarla o debilitar la aserción |
| Esperar un estado asíncrono | Espera por la condición | `Task.Delay` o `Thread.Sleep` |
| Salto de línea en un texto esperado | `"\n"` explícito (la verificación corre en Linux) | `Environment.NewLine` |

## Validación

- Cada endpoint del PR tiene su éxito y cada error declarado cubierto.
- Ninguna aserción depende de datos que la prueba no creó.
- Los conteos de las suites suben lo esperado (`backend-verify` los reporta con su diferencia).

## Contrato de salida

- Tabla endpoint → prueba de éxito → pruebas de cada error.
- Los helpers o escenarios nuevos y por qué hacían falta.
- La diferencia esperada en los conteos de cada suite.
