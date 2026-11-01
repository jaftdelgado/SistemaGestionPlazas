# sgpla-web

Frontend de SGPLa: el panel principal y el inicio de sesión contra la API REST de `../sgpla-backend`.

## Stack

| Pieza                                     | Versión                         | Uso                                                                           |
| ----------------------------------------- | ------------------------------- | ----------------------------------------------------------------------------- |
| Node                                      | 24.21.0 (`node:24.21.0-alpine`) | Entorno de ejecución; igual en compose, `scripts/verify.sh`, `.nvmrc` y el CI |
| pnpm                                      | 12.8.1                          | Gestor de paquetes, fijado en `packageManager`                                |
| TypeScript                                | 7.0.2                           | Estricto, con `noUncheckedIndexedAccess`                                      |
| React, Vite                               | Ver `package.json`              | Aplicación y servidor de desarrollo                                           |
| TanStack Router y Query                   | Ver `package.json`              | Rutas por archivos y datos del servidor                                       |
| `openapi-fetch`                           | Ver `package.json`              | Cliente tipado de la API, envuelto por las consultas de TanStack Query        |
| Tailwind v4, shadcn/ui con Base UI        | Ver `package.json`              | Estilos y componentes                                                         |
| Oxlint, Prettier, Vitest, Testing Library | Ver `package.json`              | Lint con tipos, formato y pruebas                                             |

Todas las versiones son exactas: sin `^` ni `~`. `saveExact: true` en `pnpm-workspace.yaml` lo mantiene al agregar paquetes, y `nodeLinker: hoisted` deja `node_modules` legible desde Windows para que VS Code vea los tipos.

## Estructura

```text
sgpla-web/
  src/
    main.tsx        raíz de composición de la aplicación
    modules/        un directorio por módulo, en kebab-case y en español (solicitudes-apertura/, oferta-educativa/…)
    routes/         rutas por archivos de TanStack Router
    shared/
      api/          cliente openapi-fetch, ApiError y mensajes de error, paginación, QueryClient y el schema generado
      hooks/        hooks visuales genéricos (debounce, media query…)
      lib/          utilidades, tokens de movimiento y el servicio de avisos (notifications)
      ui/           componentes de shadcn/ui y los compartidos propios (data-grid, filter-toolbar, page-header…)
    test/           preparación de las pruebas
  tools/openapi/    generador de tipos de la API, aislado
  scripts/          verify.sh y generar-api.sh
```

Los nombres de módulo van en español; el resto de las carpetas, en inglés. `components.json` apunta los alias de shadcn a `shared/`, así que los componentes que se agregan con su CLI caen en `shared/ui`.

### Nombres en el código

Los archivos, componentes, funciones, hooks, props y variables van en inglés. En español solo queda el sustantivo del dominio, sin acentos: `UsuariosDataGrid`, `UsuariosFilters`, `useUsuariosPage`, `UsuarioRow`, `Sesion`, `Cuenta`, `Rol`, `Ambito`. La interfaz, los `aria-label` y los comentarios van en español.

Dos excepciones: los campos que vienen del contrato generado (`nombre`, `correo`, `pagina`, `tamanoPagina`, `elementos`, `rolId`…) conservan su nombre, y los archivos de `routes/` conservan el suyo en español porque son la URL (`cambiar-contrasena.tsx`).

### Arquitectura de un módulo

Cada módulo separa la presentación, el código de enlace y la conexión con la API. Las dependencias van en una sola dirección:

```text
routes ──► ui/pages ──► ui/components (pasivos)
              │
              ▼
            hooks  (código de enlace)
            │    │
            ▼    ▼
           api   storage
            │    │
            ▼    ▼
           domain
```

```text
modules/<modulo>/
  domain/           tipos del modelo y reglas puras, sin dependencias
  api/              un archivo por recurso de la API: llamadas, claves de caché, queryOptions y mapeo de DTO a modelo
  storage/          adaptadores del navegador (localStorage); solo si el módulo los necesita
  hooks/            código de enlace: un hook por página, los hooks de acción con estado propio, las guardas y la validación de la búsqueda
  ui/pages/         una por ruta: usa su hook y compone componentes
  ui/components/    presentación pasiva: recibe datos y callbacks por props
  <modulo>-module.ts  raíz de composición, si el módulo se conecta al cliente de la API u otra pieza global
  index.ts          contrato: lo único que importan las rutas y los demás módulos
```

| Capa            | Responsabilidad                                                                                                | Puede depender de                                                                                                                  | Nunca depende de                                                          |
| --------------- | -------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------- |
| `domain`        | Modelo del cliente y reglas puras                                                                              | Nada                                                                                                                               | Todo lo demás                                                             |
| `api`           | Llamar a la API, traducir DTO a modelo (incluidos los enteros que el contrato publica como `number \| string`) | `domain`, `shared/api`, `queryOptions` y `keepPreviousData` de TanStack Query                                                      | React, hooks de TanStack, router, `hooks`, `ui`, otros módulos            |
| `storage`       | Adaptar el almacenamiento del navegador                                                                        | `domain`                                                                                                                           | Todo lo demás                                                             |
| `hooks`         | Conectar las capas: consultas, mutaciones, invalidación, estado en la dirección, navegación, avisos y mensajes | `api`, `storage`, `domain`, React, TanStack Query y Router, `shared/api`, `shared/hooks`, `shared/lib`, contratos de otros módulos | `ui`, `shared/ui`, `shared/api/client`                                    |
| `ui/pages`      | Llamar a su hook y componer componentes                                                                        | `hooks` propios, `ui/components`, `shared/ui`, `shared/hooks`, `Link` y `Outlet`                                                   | `api`, `storage`, `domain`, `shared/api`, TanStack Query, otros módulos   |
| `ui/components` | Mostrar datos y emitir eventos                                                                                 | Otros componentes, `shared/ui`, `shared/hooks`, `shared/lib/utils`, `Link`                                                         | `hooks`, `api`, `storage`, `domain`, `pages`, `shared/api`, otros módulos |
| `index.ts`      | Contrato del módulo                                                                                            | `ui/pages`, `hooks`, las consultas de `api` y los tipos de `domain`                                                                | `storage`, `ui/components`                                                |

Reglas que evitan la atomicidad excesiva:

- `api/`: un archivo por recurso, no por endpoint; el mapeo de DTO a modelo vive en el mismo archivo y no se exporta.
- `hooks/`: un hook por página. Un hook de acción va aparte solo si tiene estado propio (la baja con deshacer) o lo usan dos páginas; nunca un hook por consulta, que se escribe con `useQuery(<recurso>Query(...))` dentro del hook de la página.
- Los datos de otro módulo se piden por su contrato: `institucional` exporta las consultas de áreas y entidades, y `sesion` exporta `useSesionUsuario` y `useLogout`.

Los componentes declaran el tipo de sus props sin importar `domain`: el hook entrega objetos con esa forma y TypeScript la valida en la página. Los criterios de un listado (página, tamaño, filtros y búsqueda) viven en la dirección y los valida el `validateSearch` de su ruta.

Las rutas de `routes/` son adaptadores delgados, como los `Map…Endpoints`: validan la búsqueda, ejecutan la guarda en `beforeLoad` y montan la página. Dentro de un módulo se importa con rutas relativas; desde fuera, solo `@/modules/<modulo>`.

`.oxlintrc.json` verifica estas reglas en el lint, como `Sgpla.ArchitectureTests` en el backend: los imports profundos entre módulos, los imports prohibidos por capa (también en `shared/`) y el uso de `localStorage` o `sessionStorage` fuera de `storage`. No detecta imports relativos que crucen de un módulo a otro ni que un componente reciba un hook por props; eso se revisa en el PR.

`routeTree.gen.ts` y `shared/api/schema.d.ts` son archivos generados que se versionan: no se editan a mano.

### `tools/openapi`: el generador de tipos

`openapi-typescript` usa la API clásica del compilador de TypeScript, que TypeScript 7 ya no expone. Por eso vive en un paquete propio, con su `package.json`, su `pnpm-workspace.yaml` y su `pnpm-lock.yaml`, y con TypeScript 5.9.3. La aplicación no depende de él y solo compila con TypeScript 7 el archivo que genera.

## Comandos

Todo se ejecuta con Docker; no se usa Node ni pnpm locales.

```bash
# Levantar el entorno completo (frontend en http://localhost:5173, con recarga en caliente)
docker compose up -d

# Verificar: instalación, formato, lint, tipos, pruebas y build, más el aviso de atajos
sgpla-web/scripts/verify.sh

# Regenerar src/shared/api/schema.d.ts (requiere la API levantada en Development)
sgpla-web/scripts/generar-api.sh

# Agregar un paquete (revisa antes la licencia del paquete y de sus dependencias)
docker compose run --rm web sh -c "corepack enable && pnpm add <paquete>"
```

Un paquete nuevo o una actualización mayor necesita revisión de licencia y aprobación. Después de agregarlo se ejecuta `pnpm licenses list` en la aplicación y, si cambió, en `tools/openapi`.

El CI no regenera `schema.d.ts`, porque no tiene la API: tras un cambio de contratos en el backend, hay que ejecutar `generar-api.sh` y confirmar el resultado.

## Sesión

El token que devuelve `POST /api/v1/usuarios/iniciar-sesion` se guarda en `localStorage`, bajo la clave `sgpla.sesion`, junto con su vencimiento (`expiraEn`), y se descarta al vencer. El módulo `sesion` conecta el token con el cliente de la API desde `main.tsx` (`registerSesion`), y el cliente lo envía como `Authorization: Bearer` en cada petición. Un 401 con token borra la sesión y lleva al inicio de sesión. Las rutas del panel exigen una sesión vigente, y una cuenta con contraseña temporal solo puede abrir `/cambiar-contrasena`.

## Variables

Se leen del `.env` de la raíz (ver `.env.example`) y tienen un valor por omisión para desarrollo.

| Variable            | Por omisión                          | Uso                                                                                              |
| ------------------- | ------------------------------------ | ------------------------------------------------------------------------------------------------ |
| `SGPLA_WEB_PORT`    | `5173`                               | Puerto del frontend en el host. Debe coincidir con `SGPLA_WEB_ORIGIN`, el origen que admite CORS |
| `SGPLA_WEB_POLLING` | `true`                               | Sondeo de archivos de Vite                                                                       |
| `VITE_API_URL`      | `http://localhost:${SGPLA_API_PORT}` | Dirección de la API que usa el navegador                                                         |
| `SGPLA_OPENAPI_URL` | `http://api:8080/openapi/v1.json`    | Documento OpenAPI que lee el generador dentro de la red de compose                               |

En Windows los eventos de archivo no cruzan el montaje desde NTFS hacia el contenedor, así que Vite necesita sondear los archivos (`SGPLA_WEB_POLLING=true`) para refrescar al editar. En Linux puede ponerse en `false`.

## VS Code

`.vscode/extensions.json`, en la raíz, recomienda la extensión de Oxc (`oxc.oxc-vscode`) y la extensión nativa de TypeScript (`TypeScriptTeam.native-preview`). El paquete `typescript@7.0.2` no trae `tsserver`, por eso no se configura `typescript.tsdk`.

La API REST está en `../sgpla-backend`; su configuración de CORS (`Cors:OrigenesPermitidos`) debe incluir el origen del servidor de desarrollo.
