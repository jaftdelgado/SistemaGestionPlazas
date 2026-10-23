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
| `openapi-fetch`, `openapi-react-query`    | Ver `package.json`              | Cliente tipado de la API                                                      |
| Tailwind v4, shadcn/ui con Base UI        | Ver `package.json`              | Estilos y componentes                                                         |
| Oxlint, Prettier, Vitest, Testing Library | Ver `package.json`              | Lint con tipos, formato y pruebas                                             |

Todas las versiones son exactas: sin `^` ni `~`. `saveExact: true` en `pnpm-workspace.yaml` lo mantiene al agregar paquetes, y `nodeLinker: hoisted` deja `node_modules` legible desde Windows para que VS Code vea los tipos.

## Estructura

```text
sgpla-web/
  src/
    modules/        un directorio por módulo, en kebab-case y en español (solicitudes-apertura/, oferta-educativa/…)
    routes/         rutas por archivos de TanStack Router
    components/ui/  componentes de shadcn/ui
    lib/            utilidades, QueryClient y cliente de la API (lib/api/)
    test/           preparación de las pruebas
  tools/openapi/    generador de tipos de la API, aislado
  scripts/          verify.sh y generar-api.sh
```

Los nombres de módulo van en español; el resto de las carpetas, en inglés. Los identificadores de dominio van en español sin acentos, los términos técnicos en inglés, y la interfaz y los comentarios en español.

### Arquitectura de un módulo

Cada módulo replica las capas de los módulos del backend, con la ceremonia proporcional a su lógica: una capa sin contenido no se crea.

```text
modules/<modulo>/
  domain/           reglas en TypeScript puro: sin React, TanStack ni la API
  application/      casos de uso: queryOptions, hooks de mutación y guardas de ruta
  infrastructure/   adaptadores del navegador (localStorage, por ejemplo)
  ui/               pages/, components/, layouts/ y hooks/ de presentación
  <modulo>-module.ts  raíz de composición, si el módulo se conecta al cliente de la API u otra pieza global
  index.ts          contrato: lo único que importan las rutas y los demás módulos
```

| Capa             | Equivale en el backend  | Puede depender de                                          | Nunca depende de                                          |
| ---------------- | ----------------------- | ---------------------------------------------------------- | --------------------------------------------------------- |
| `domain`         | `Domain`                | Nada fuera del módulo                                      | React, TanStack, `lib/api`, `components`, las otras capas |
| `application`    | `Application`           | `domain`, `infrastructure`, `lib/`, toasts                 | `ui`                                                      |
| `infrastructure` | `Infrastructure`        | `domain`, `application`, `lib/`                            | `ui`                                                      |
| `ui`             | `Endpoints`             | `application`, `domain`, `components/`, `lib/api/problema` | `infrastructure`, `lib/api/client`, `lib/api/query`       |
| `index.ts`       | `Application/Contracts` | Todas las capas                                            | —                                                         |

Las rutas de `routes/` son adaptadores delgados, como los `Map…Endpoints`: validan la búsqueda, ejecutan la guarda en `beforeLoad` y montan la página. `main.tsx` es la raíz de composición de la aplicación. Dentro de un módulo se importa con rutas relativas; desde fuera, solo `@/modules/<modulo>`.

`.oxlintrc.json` verifica estas reglas en el lint, como `Sgpla.ArchitectureTests` en el backend: los imports profundos entre módulos, los imports prohibidos por capa y el uso de `localStorage` o `sessionStorage` fuera de `infrastructure`. No detecta imports relativos que crucen de un módulo a otro; esos se revisan en el PR.

`routeTree.gen.ts` y `lib/api/schema.d.ts` son archivos generados que se versionan: no se editan a mano.

### `tools/openapi`: el generador de tipos

`openapi-typescript` usa la API clásica del compilador de TypeScript, que TypeScript 7 ya no expone. Por eso vive en un paquete propio, con su `package.json`, su `pnpm-workspace.yaml` y su `pnpm-lock.yaml`, y con TypeScript 5.9.3. La aplicación no depende de él y solo compila con TypeScript 7 el archivo que genera.

## Comandos

Todo se ejecuta con Docker; no se usa Node ni pnpm locales.

```bash
# Levantar el entorno completo (frontend en http://localhost:5173, con recarga en caliente)
docker compose up -d

# Verificar: instalación, formato, lint, tipos, pruebas y build, más el aviso de atajos
sgpla-web/scripts/verify.sh

# Regenerar src/lib/api/schema.d.ts (requiere la API levantada en Development)
sgpla-web/scripts/generar-api.sh

# Agregar un paquete (revisa antes la licencia del paquete y de sus dependencias)
docker compose run --rm web sh -c "corepack enable && pnpm add <paquete>"
```

Un paquete nuevo o una actualización mayor necesita revisión de licencia y aprobación. Después de agregarlo se ejecuta `pnpm licenses list` en la aplicación y, si cambió, en `tools/openapi`.

El CI no regenera `schema.d.ts`, porque no tiene la API: tras un cambio de contratos en el backend, hay que ejecutar `generar-api.sh` y confirmar el resultado.

## Sesión

El token que devuelve `POST /api/v1/usuarios/iniciar-sesion` se guarda en `localStorage`, bajo la clave `sgpla.sesion`, junto con su vencimiento (`expiraEn`), y se descarta al vencer. El módulo `sesion` conecta el token con el cliente de la API desde `main.tsx` (`registrarSesion`), y el cliente lo envía como `Authorization: Bearer` en cada petición. Un 401 con token borra la sesión y lleva al inicio de sesión. Las rutas del panel exigen una sesión vigente, y una cuenta con contraseña temporal solo puede abrir `/cambiar-contrasena`.

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
