---
name: backend-verify
description: Verificación del backend de SGPLa solo con Docker, mediante sgpla-backend/scripts/verify.sh y smoke.sh. USE FOR compilar, comprobar el formato y correr las tres suites antes de entregar un PR, leer el resumen y los avisos de atajos, hacer la prueba de humo con token de Superusuario y curl, consultar la base con sqlcmd, y diagnosticar fallas de build, format, Testcontainers o CRLF. DO NOT USE FOR escribir pruebas (backend-testing), implementar código (backend-resource) ni revisar un PR ajeno (merge-review, que usa esta verificación).
---

# Verificación del backend

Produce un resultado de verificación fiable y reportado tal como salió. Todo corre en contenedores con la imagen `mcr.microsoft.com/dotnet/sdk:10.0.401`; no se usa `dotnet` local.

## Cuándo usarla

- **Una sola vez, al terminar la implementación de un PR**, no entre commits.
- Otra vez después de cada commit de corrección.
- Una corrección que solo toca documentación no la necesita: basta revisar el diff.

## Flujo

1. Confirma la rama y el árbol: `git status --short` y `git branch --show-current`. El script verifica la copia de trabajo, incluidos los cambios sin confirmar. Si el entorno de `docker compose` está levantado, detenlo con `docker compose stop` para que no compita por memoria con Testcontainers.
2. Ejecuta la verificación (unos 4 minutos; en segundo plano si la herramienta lo permite):

   ```bash
   sgpla-backend/scripts/verify.sh
   ```

3. Lee el resumen del final:
   - `Warning(s)` y `Error(s)` deben ser 0;
   - un `Test run summary` por suite, en orden: arquitectura, unitarias e integración, con `total`, `failed` y `succeeded`;
   - la sección de atajos nuevos respecto a `origin/develop`: cada uno necesita justificación explícita en la entrega.
4. Si algo falla, busca la causa en `sgpla-backend/TestResults/verify.log`, corrige en un **commit nuevo** y repite la verificación completa.
5. Prueba de humo, después de que `verify.sh` pase:

   ```bash
   sgpla-backend/scripts/smoke.sh
   TOKEN=$(cat sgpla-backend/TestResults/smoke-token.txt)
   curl -s -o /dev/null -w '%{http_code}\n' -H "Authorization: Bearer $TOKEN" http://localhost:8180/api/v1/<ruta>
   ```

   `smoke.sh` borra la base local, levanta `docker compose`, crea el primer Superusuario y deja un token. Los `curl` concretos los define el prompt o la especificación del PR.
6. **Datos que solo puede crear un rol sin acceso de prueba** (el login de DGAA y Entidad Académica necesita el LDAP de la UV): se insertan con `sqlcmd` en el contenedor:

   ```bash
   docker compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '<SGPLA_SA_PASSWORD>' -C -d sgpla-bd -Q "<consulta>"
   ```

   Sin red hacia la UV, la parte que necesita LDAP se reporta como **omitida**, nunca como aprobada.

## Diagnóstico

| Síntoma | Causa probable | Qué hacer |
|---|---|---|
| `dotnet format` reporta `WHITESPACE` o finales de línea | La copia de trabajo tiene CRLF | Refresca el clon (sección "Preparar el entorno" del README del backend); el script normaliza igual como defensa |
| `dotnet format` reporta `IMPORTS` o `IDE0055` | Orden de `using` o formato | Corrige el archivo; no desactives la regla |
| Error de compilación por un analizador | `TreatWarningsAsErrors` | Corrige el código; nunca `#pragma`, `SuppressMessage` sin justificación ni `<NoWarn>` |
| Testcontainers no conecta con SQL Server | En Linux falta `host.docker.internal` | El script agrega `--add-host`; revisa que Docker esté en marcha y que el socket esté disponible |
| `SqlException: Execution Timeout Expired` en consultas triviales, o la suite de integración tarda mucho más que de costumbre | Docker sin memoria: otro SQL Server (por ejemplo, el entorno de `docker compose`) compite con el de Testcontainers | `docker compose stop` antes de `verify.sh` y repite; `smoke.sh` vuelve a levantar el entorno después. No toques la prueba |
| Una prueba de integración falla solo a veces | Una aserción no acotada a los datos de la prueba | Ver `backend-testing` |
| `smoke.sh` dice que falta `.env` | No se copió `.env.example` | `cp .env.example .env` en la raíz |
| La sección de atajos dice "omitido" | No existe `origin/develop` localmente | `git fetch origin` |

## Contrato de salida

```markdown
| Comprobación | Resultado | Diferencia contra develop |
|---|---|---|
| Build | <n advertencias, n errores> | — |
| Format | <sin cambios / con cambios> | — |
| Arquitectura | <superadas / total> | <+n> |
| Unitarias | <superadas / total> | <+n> |
| Integración | <superadas / total> | <+n> |
| Atajos nuevos | <ninguno / lista con su justificación> | — |

| Prueba de humo | HTTP |
|---|---|
| <petición> | <código> |
| <paso que requiere LDAP> | omitido (sin red hacia la UV) |
```

Reporta los números exactos del resumen. Si un paso no se ejecutó, dilo.
