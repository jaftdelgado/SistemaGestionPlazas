# Configuración inicial del frontend

Especificación del PR que deja lista la configuración de `sgpla-web/`. Es normativa: quien lo implemente no decide nada que no esté escrito aquí. Si algo falta o se contradice, se detiene la implementación y se aclara en este documento antes de programar. Se elimina al fusionar el PR, cuando su contenido ya viva en `sgpla-web/README.md` y en `AGENTS.md`.

## Contexto

`sgpla-web/` está reservada desde el esqueleto y solo tiene un README. El backend ya cubre cinco módulos, y la API está lista para que un cliente la consuma:
- CORS admite `http://localhost:5173` (`SGPLA_WEB_ORIGIN` en `docker-compose.yml`);
- publica OpenAPI en `/openapi/v1.json` (solo en Development, en `sgpla-backend/src/Sgpla.Api/Program.cs`);
- usa JWT bearer.

Este PR deja **solo la configuración** del frontend: sin pantallas de negocio ni login. Debe cumplir las reglas de `AGENTS.md`: Docker como único entorno (regla 4), sin atajos (regla 8) y paquetes con licencia revisada (regla 10).

## Decisiones aprobadas

| Tema | Decisión |
|---|---|
| Base | React + Vite + TypeScript 7.0.2 estricto |
| Gestor | pnpm, con su versión fijada en `packageManager`; `pnpm-lock.yaml` versionado |
| Versiones | Exactas, sin `^` ni `~`: `saveExact: true` en `pnpm-workspace.yaml`. No hay `.npmrc`, porque pnpm 12 ignora en él esa clave |
| Node | Imagen `node:24.21.0-alpine` y `.nvmrc` con `24.21.0`, iguales en compose, `verify.sh` y el CI. `engines.node` conserva el rango `>=24 <25` |
| `node_modules` | En la carpeta del host (`sgpla-web/node_modules`), instalado desde el contenedor, con `nodeLinker: hoisted`: el diseño con enlaces simbólicos de pnpm, creado en el contenedor sobre NTFS, no se puede leer desde Windows, y VS Code no vería los tipos |
| Router | TanStack Router con rutas por archivos (plugin de Vite) |
| Datos | TanStack Query |
| API | `openapi-fetch` y `openapi-react-query`. Los tipos los genera `openapi-typescript` en el paquete aislado `tools/openapi/`, porque necesita la API clásica del compilador, que TypeScript 7 ya no expone |
| UI | Tailwind v4 y shadcn/ui con Base UI |
| Calidad | Oxlint con `oxlint-tsgolint` (análisis con tipos, `--type-aware`), Prettier, Vitest y Testing Library. Sin ESLint: `typescript-eslint` no admite TypeScript 7 |
| Editor | `.vscode/extensions.json` en la raíz, con la extensión de oxc y la extensión nativa de TypeScript. Sin `typescript.tsdk`, porque el paquete `typescript@7.0.2` no trae `tsserver` |
| Estructura | `src/modules/<modulo-kebab>/` (nombres de módulo en español); las demás carpetas en inglés |
| Identificadores | Dominio en español sin acentos; términos técnicos en inglés; UI y comentarios en español |
| Producción | Fuera de este PR: sin imagen nginx |
| Gobernanza | `AGENTS.md`, `frontend-ci.yml` y los README; la skill del frontend llega después |

## Versiones fijadas

| Pieza | Versión | Dónde |
|---|---|---|
| Node | 24.21.0 (`node:24.21.0-alpine`) | compose, `verify.sh`, `.nvmrc` y el CI |
| pnpm | 12.8.1 | `packageManager` de la app y de `tools/openapi` |
| TypeScript (app) | 7.0.2 | `sgpla-web/package.json` |
| TypeScript (generador) | 5.9.3 | `sgpla-web/tools/openapi/package.json` |
| `openapi-typescript` | 7.13.0 | `sgpla-web/tools/openapi/package.json` |
| `@types/node` | La última de la serie 24, exacta | `sgpla-web/package.json` |
| Los demás paquetes | La última estable, resuelta en el contenedor al implementar y fijada exacta | `sgpla-web/package.json` |

## Rama y commits

Rama `chore/web-configuracion`, desde `origin/develop` con `--no-track`. Commits (skill `git-workflow`, scope `web`):
1. `chore(web): agregado el esqueleto de React con Vite y TypeScript`: `package.json`, `pnpm-workspace.yaml`, tsconfig, `vite.config.ts`, Oxlint, Prettier, Vitest y la prueba mínima.
2. `chore(web): configurados TanStack Router, TanStack Query y el cliente tipado de la API`, con el generador de `tools/openapi/`.
3. `chore(web): configurados Tailwind y shadcn/ui con Base UI`
4. `chore(web): agregados el servicio de desarrollo en Docker y el script de verificación`
5. `ci(web): agregado el workflow del frontend`
6. `docs(agents): extendidas al frontend las reglas de entorno, atajos y licencias`, junto con los README y `.vscode/extensions.json`.

Título del PR: `chore(web): agregada la configuración inicial del frontend`.

## Contenido de `sgpla-web/`

**Raíz:**
- `package.json`: `"type": "module"`, `packageManager: pnpm@12.8.1` y `engines.node: ">=24 <25"`. Todas las versiones exactas.
- `pnpm-workspace.yaml`:
  - `saveExact: true` y `nodeLinker: hoisted`;
  - `allowBuilds` solo con los paquetes que `pnpm install` reporte como bloqueados (`ERR_PNPM_IGNORED_BUILDS`) y que de verdad necesiten su script, cada uno con un comentario de una línea que justifique el permiso. Si ninguno lo necesita, `allowBuilds` queda vacío o no se declara.
- `.nvmrc` con `24.21.0`.
- `tsconfig.json`, `tsconfig.app.json` y `tsconfig.node.json`, como la plantilla react-ts:
  - `strict`, `noUncheckedIndexedAccess` y `noUnusedLocals/Parameters`;
  - alias `"paths": { "@/*": ["./src/*"] }` **sin `baseUrl`**, tanto en `tsconfig.json` como en `tsconfig.app.json`;
  - `include` solo de `src`: `tools/` queda fuera.
- `vite.config.ts`:
  - `tanstackRouter({ target: 'react', autoCodeSplitting: true })` antes de `react()`, y `tailwindcss()`;
  - alias `@`, igual que en tsconfig;
  - `server: { host: true, port: 5173, strictPort: true, watch: { usePolling: env SGPLA_WEB_POLLING === 'true' } }`. El *polling* hace falta porque los eventos de archivo no cruzan el montaje desde NTFS.
  - Bloque `test` de Vitest: `jsdom`, `setupFiles: src/test/setup.ts`, `globals: false`, `include` solo de `src`.
- `.oxlintrc.json`:
  - plugins `typescript`, `react` y `oxc`;
  - reglas `react/rules-of-hooks` y `react/exhaustive-deps` como error;
  - `jsPlugins` con `@tanstack/eslint-plugin-query`, y `@tanstack/eslint-plugin-router` solo si pasa su comprobación (ver "Comprobaciones en contenedor");
  - `ignorePatterns`: `dist`, `src/routeTree.gen.ts`, `src/lib/api/schema.d.ts` y `tools/`.
- `.prettierrc.json` con `prettier-plugin-tailwindcss`, y `.prettierignore` con los generados, `pnpm-lock.yaml` (cubre también el de `tools/openapi`), `dist` y `tools/openapi/node_modules`.
- `components.json` de shadcn, con la variante Base UI. El CLI se consulta en el contenedor al implementar para usar su forma vigente.
- `index.html` con `lang="es"` y título SGPLa.

**`src/`:**
- `main.tsx`: crea `QueryClient` y el router con `context: { queryClient }`, y monta `QueryClientProvider` y `RouterProvider`.
- `routes/__root.tsx`: `createRootRouteWithContext<{ queryClient: QueryClient }>()`, con un layout mínimo con `<Outlet/>`, un `notFoundComponent` en español y las devtools de Router y Query solo con `import.meta.env.DEV`.
- `routes/index.tsx`: página de inicio de marcador, con un `Button` de shadcn para comprobar Tailwind.
- `routeTree.gen.ts`: generado por el plugin y versionado. Se marca `linguist-generated` en `.gitattributes`.
- `lib/query-client.ts`: fábrica del `QueryClient` con sus opciones por omisión (`retry` y `staleTime`).
- `lib/api/`:
  - `schema.d.ts`: generado por `tools/openapi` (ver la sección "Generador de tipos") y versionado.
  - `client.ts`: `createClient<paths>({ baseUrl: import.meta.env.VITE_API_URL })`, con un *middleware* que agrega `Authorization: Bearer` cuando `obtenerToken()` devuelve un valor. Hoy `obtenerToken()` devuelve siempre `null`: cómo se guarda el token se decide en el PR del login.
  - `query.ts`: `export const $api = createQueryClient(client)` (`openapi-react-query`).
- `lib/utils.ts`: `cn()` de shadcn.
- `components/ui/button.tsx`: el único componente de shadcn en este PR.
- `index.css`: `@import "tailwindcss"` y las variables del tema de shadcn.
- `vite-env.d.ts`: tipa `ImportMetaEnv` con `VITE_API_URL: string`.
- `modules/.gitkeep`: carpeta de los módulos (`solicitudes-apertura/`, `oferta-educativa/`…), vacía en este PR.
- `test/setup.ts`: `@testing-library/jest-dom/vitest` y `cleanup`.

**Pruebas:**
- `routes/index.test.tsx`: renderiza el router con `createMemoryHistory` en `/` y comprueba el encabezado; con una ruta inexistente, el texto del 404.
- `lib/api/client.test.ts`: con un `fetch` falso, sin token no hay `Authorization`; con token, se envía `Bearer <token>`.

**Scripts de `package.json`:**
- `dev`, `build` (`tsc -b && vite build`), `preview` y `typecheck` (`tsc -b`; los tsconfig ya llevan `noEmit`);
- `lint`: `oxlint --type-aware`, sin admitir advertencias (la opción exacta de oxlint para eso se toma de su `--help` en el contenedor);
- `format`, `format:check` y `test` (`vitest run`).

La app no tiene script de generación de la API ni depende de `openapi-typescript`.

## Generador de tipos (`tools/openapi`)

`openapi-typescript` usa la API clásica del compilador (`ts.factory`), que TypeScript 7 ya no exporta: instalado en la app, falla. Por eso vive en un paquete propio con TypeScript 5.9.3, que solo carga el generador. Lo que genera (`schema.d.ts`) lo compila TypeScript 7.

`sgpla-web/tools/openapi/`:
- `package.json`: `"private": true`, `"type": "module"`, `packageManager: pnpm@12.8.1`, y como únicas dependencias de desarrollo exactas `openapi-typescript@7.13.0` y `typescript@5.9.3`. Script `generar`: `openapi-typescript $SGPLA_OPENAPI_URL -o ../../src/lib/api/schema.d.ts`.
- `pnpm-workspace.yaml` propio, para que sea la raíz de su propio workspace: `saveExact: true`, `nodeLinker: hoisted` y `allowBuilds` con la misma regla que la app.
- `pnpm-lock.yaml` propio, versionado.

Reglas:
- No se ejecuta con `dlx`: siempre `pnpm install --frozen-lockfile` dentro de `tools/openapi` y luego `pnpm generar`.
- Oxlint, los tsconfig y Vitest de la app no lo incluyen. Prettier solo formatea su `package.json` y su `pnpm-workspace.yaml`.
- Sus licencias se revisan como las de la app (sección "Paquetes y licencias").

## Docker

- **`docker-compose.yml`: servicio nuevo `web`.**
  - Imagen `node:24.21.0-alpine`, `working_dir: /app` y montaje `./sgpla-web:/app`, con `node_modules` en el host.
  - Volumen nombrado `pnpm-store` en `/pnpm/store`, con `pnpm_config_store_dir=/pnpm/store` (pnpm 12 ignora `npm_config_store_dir`). pnpm copia en lugar de enlazar porque son sistemas de archivos distintos, y es aceptable.
  - `command`: activa pnpm 12.8.1 (con `corepack enable` si pasa su comprobación; si no, se detiene), y ejecuta `pnpm install --frozen-lockfile && pnpm dev`.
  - Variables:
    - `VITE_API_URL: "http://localhost:${SGPLA_API_PORT:-8180}"`: el navegador llama al puerto del host y usa el CORS existente;
    - `SGPLA_WEB_POLLING: "${SGPLA_WEB_POLLING:-true}"`;
    - `SGPLA_OPENAPI_URL: "http://api:8080/openapi/v1.json"`.
  - Puerto `${SGPLA_WEB_PORT:-5173}:5173`. Sin `depends_on` del api: el frontend arranca aunque la API no esté.
- **`.env.example`:** `SGPLA_WEB_PORT=5173` y `SGPLA_WEB_POLLING=true`, con comentarios, junto a `SGPLA_WEB_ORIGIN`; se indica que el puerto y el origen deben coincidir.
- **`sgpla-web/scripts/verify.sh`:** mismo patrón que `sgpla-backend/scripts/verify.sh`.
  - Conserva el manejo de MSYS (`MSYS_NO_PATHCONV` y `pwd -W`) y la copia a `/w` sin `node_modules`, `dist` ni `TestResults`, tampoco los de `tools/openapi`.
  - Antes de ejecutar nada, falla si `.nvmrc` no coincide con la versión de la etiqueta de la imagen que usa el script.
  - En la copia:
    - `pnpm install --frozen-lockfile` en la app y en `tools/openapi`; el segundo valida que su lockfile corresponde a su `package.json`;
    - `pnpm format:check && pnpm lint && pnpm typecheck && pnpm test && pnpm build` en la app.
  - Usa el volumen `sgpla-pnpm-store`; el log va a `sgpla-web/TestResults/verify.log`.
  - Al final avisa de los atajos nuevos respecto a `origin/develop`:
    - `oxlint-disable` y `eslint-disable` sin `--` de justificación;
    - `@ts-ignore`, `@ts-expect-error` sin descripción y `@ts-nocheck`;
    - `.skip(` y `.only(` en pruebas, y `setTimeout` en pruebas;
    - versiones con `^` o `~` en `sgpla-web/package.json` y en `sgpla-web/tools/openapi/package.json`.
- **`sgpla-web/scripts/generar-api.sh`:** `docker compose run --rm web` activa pnpm y ejecuta, dentro de `tools/openapi`, `pnpm install --frozen-lockfile && pnpm generar`. Requiere la API levantada en Development.

## CI: `.github/workflows/frontend-ci.yml`

- Se ejecuta con `push`, `pull_request` (hacia `main` y `develop`) y `workflow_dispatch`, solo con cambios en `sgpla-web/**` o en el propio workflow.
- `concurrency` y `working-directory: sgpla-web`, como `backend-ci.yml`.
- Pasos:
  - `actions/checkout@v5`, `pnpm/action-setup` (lee `packageManager`) y `actions/setup-node` con `node-version-file: sgpla-web/.nvmrc` y `cache: pnpm`;
  - `pnpm install --frozen-lockfile` en la app y en `tools/openapi`;
  - `format:check`, `lint`, `typecheck`, `test` y `build` en la app.
- El CI no regenera `schema.d.ts`, porque no tiene la API; queda como nota del PR.
- El CI corre en glibc y el contenedor en musl: el resultado real del CI solo se conoce después del push, que decide el responsable.

## Configuración de la raíz

- `.gitattributes`: `sgpla-web/src/routeTree.gen.ts` y `sgpla-web/src/lib/api/schema.d.ts` con `linguist-generated=true`.
- `.gitignore`: ya cubre `node_modules/` (también el de `tools/openapi`), `dist/`, `.vite/`, `coverage/` y `TestResults/`. Se agrega **solo** `*.tsbuildinfo`; los encabezados y el resto del archivo no cambian.
- `.vscode/extensions.json`: recomienda la extensión de oxc y la extensión nativa de TypeScript, con sus identificadores copiados del Marketplace al implementar, nunca de memoria. El `.gitignore` ya admite este archivo (`!.vscode/extensions.json`). Sin `settings.json` ni `typescript.tsdk`.

## Documentos

- **`AGENTS.md`:**
  - el párrafo inicial pasa a decir que `sgpla-web/` es React + Vite;
  - **regla 4:** el frontend también se ejecuta solo con Docker, con `sgpla-web/scripts/verify.sh` y el servicio `web`, sin Node ni pnpm locales;
  - **regla 8:** agrega los atajos del frontend que avisa `verify.sh`, con la lista exacta de la sección "Docker";
  - **regla 10:** pasa a "paquetes NuGet o npm", e incluye el lockfile de `tools/openapi`;
  - en "Puertas de calidad": antes de entregar un PR del frontend, `sgpla-web/scripts/verify.sh`;
  - la tabla de skills no cambia.
- **`sgpla-web/README.md`:** se reescribe con:
  - stack y versiones fijadas;
  - estructura (`modules/` en kebab-case y en español; lo demás en inglés), y `tools/openapi` con su motivo;
  - comandos con Docker (levantar, verificar, generar la API y agregar un paquete con `docker compose run --rm web pnpm add <pkg>` revisando la licencia);
  - variables, la nota del *polling* en Windows y las extensiones recomendadas de VS Code.
- **`README.md` (raíz):** la fila de `sgpla-web/`, la sección de levantar el entorno (el frontend en `http://localhost:5173`) y CI (`frontend-ci.yml` ya existe).
- **`PLAN_INICIAL.md`:** la línea "`frontend-ci.yml` se agregará…" pasa a reflejar que ya existe. Es una corrección de una contradicción, pedida por la regla del repositorio.

## Paquetes y licencias (regla 10)

Las versiones son las de "Versiones fijadas"; las demás se instalan en el contenedor con la última estable al implementar. Antes del commit, `pnpm licenses list` (producción y desarrollo) se ejecuta en la app **y** en `tools/openapi`, y el resultado va a una tabla en la descripción del PR.

| Grupo | Paquetes | Licencia esperada |
|---|---|---|
| Runtime | `react`, `react-dom`, `@tanstack/react-router`, `@tanstack/react-query`, `openapi-fetch`, `openapi-react-query`, `@base-ui/react`, `class-variance-authority`, `clsx`, `tailwind-merge`, `lucide-react` | MIT, Apache-2.0 o ISC |
| Desarrollo | `vite`, `@vitejs/plugin-react`, `typescript`, `tailwindcss`, `@tailwindcss/vite`, `tw-animate-css`, `@tanstack/router-plugin`, `@tanstack/react-router-devtools`, `@tanstack/react-query-devtools`, `oxlint`, `oxlint-tsgolint`, `@tanstack/eslint-plugin-query`, `@tanstack/eslint-plugin-router` (si pasa su comprobación), `prettier`, `prettier-plugin-tailwindcss`, `vitest`, `jsdom`, `@testing-library/react`, `@testing-library/jest-dom`, `@testing-library/user-event`, `@types/react`, `@types/react-dom`, `@types/node` | MIT, Apache-2.0 o BSD |
| Generador (`tools/openapi`) | `openapi-typescript`, `typescript` | MIT y Apache-2.0 |

La app no incluye `eslint`, `@eslint/js`, `typescript-eslint`, `eslint-config-prettier`, `eslint-plugin-react-hooks`, `eslint-plugin-react-refresh`, `globals` ni `openapi-typescript`.

## Comprobaciones en contenedor al implementar

Se hacen en contenedores desechables antes de confirmar lo que dependa de ellas. Cada resultado se reporta en la entrega.

1. **Aislamiento de `tools/openapi`:**
   - `pnpm ls -r --depth -1` desde `sgpla-web` lista solo `sgpla-web`;
   - instalar en `tools/openapi` no cambia `sgpla-web/pnpm-lock.yaml` (`git diff --exit-code`);
   - `pnpm why typescript` da 7.0.2 en la app y 5.9.3 en `tools/openapi`.
2. **Activación de pnpm:** `corepack enable` en `node:24.21.0-alpine` deja `pnpm --version` en 12.8.1 con el `packageManager` del proyecto.
3. **TypeScript 7:** `tsc -b` acepta todas las opciones de los tsconfig y compila con los tipos de TanStack Router, `openapi-fetch` y `openapi-react-query`.
4. **CLI de shadcn:** funciona con el alias sin `baseUrl` y con Base UI, sin interacción.
5. **Plugin de TanStack Router en oxlint:** con un archivo desechable, no versionado, que declara las propiedades de una ruta en un orden incorrecto, oxlint carga el plugin y lo reporta. Si no, el plugin se excluye y se anota en el PR.
6. **Oxlint en musl:** `oxlint --type-aware` con `oxlint-tsgolint` corre en `node:24.21.0-alpine`. Además, el lockfile registra las variantes gnu y musl de los binarios nativos (oxlint, `oxlint-tsgolint`, TypeScript 7 y `@tailwindcss/oxide`), que necesita el CI.
7. **Documento OpenAPI,** con la API levantada por `sgpla-backend/scripts/smoke.sh`, leído desde el contenedor `web` en `http://api:8080/openapi/v1.json`:
   - el valor de `openapi`;
   - el tipo de `id` en `SolicitudAperturaResponse`;
   - el `requestBody` de `CrearSolicitudApertura` (`multipart/form-data`, con `oficio` como binario);
   - el contenido de la respuesta 200 de `DescargarOficioSolicitudApertura` (`application/pdf`) y de `ExportarPlanEstudios` (el tipo de contenido del Excel).

## Condiciones de parada

El agente se detiene, no improvisa, y pregunta si:
- `tools/openapi` no queda aislado del workspace de la app (comprobación 1);
- aparece un `^` o un `~` en `sgpla-web/package.json` o en `sgpla-web/tools/openapi/package.json`;
- `pnpm licenses list` muestra una licencia no permisiva (GPL, AGPL, LGPL, SSPL, BUSL o similar) o desconocida en cualquiera de los dos lockfiles;
- `pnpm install` falla por un *peer* incumplido o por un *build* bloqueado que no está justificado en `allowBuilds`. Un aviso de *peer* que no impide instalar se reporta en el PR y no se silencia;
- TypeScript 7 rechaza una opción de tsconfig o `tsc -b` falla con los tipos de una librería (comprobación 3). No se quita una opción en silencio;
- oxlint o `oxlint-tsgolint` no corren en musl (comprobación 6);
- `corepack` no activa pnpm 12.8.1 (comprobación 2);
- el CLI de shadcn exige `baseUrl` o no ofrece Base UI sin interacción (comprobación 4);
- `generar-api.sh` no es reproducible (verificación 3);
- `.nvmrc` no coincide con la etiqueta de la imagen.

**Excepciones que no detienen el PR:**
- **Ids como `number | string` en `schema.d.ts`.** Se reportan en la descripción del PR con la versión de OpenAPI, un ejemplo del tipo generado y el endpoint de origen. La corrección va en un PR del backend, que debe fusionarse antes del primer PR del frontend que consuma ids.
- **El plugin de TanStack Router** que no pasa la comprobación 5: se excluye y se anota.

## Fuera de alcance (notas para el PR)

- Login y guardado del token (el PR del login); pantallas de los módulos.
- Librería de formularios.
- Imagen de producción y origen de la API en producción.
- Skill del frontend y estándar del frontend: llegan cuando haya patrones reales.
- Que el CI detecte un `schema.d.ts` desactualizado.
- La corrección del tipo de los ids en el backend, si hace falta.

## Verificación

1. `sgpla-web/scripts/verify.sh` termina en 0, con los conteos de las pruebas (4 casos), la comprobación de `.nvmrc` contra la imagen y sin avisos de atajos.
2. `docker compose up -d` y luego:
   - `http://localhost:5173` muestra la página de inicio con el botón estilizado;
   - al editar `routes/index.tsx`, la recarga en caliente lo refleja;
   - una ruta inexistente muestra el 404.
3. **Reproducibilidad del generador**, con la API arriba: `sgpla-web/scripts/generar-api.sh` se ejecuta dos veces seguidas.
   - `sha256sum src/lib/api/schema.d.ts` es idéntico tras cada ejecución;
   - `git diff --exit-code -- src/lib/api/schema.d.ts pnpm-lock.yaml tools/openapi/pnpm-lock.yaml` termina en 0;
   - `pnpm typecheck` pasa.
4. `git diff --stat origin/develop -- sgpla-backend` sale vacío: el PR no toca el backend y no hace falta `sgpla-backend/scripts/verify.sh`.
5. `bash .github/scripts/check-skills.sh` pasa tras editar `AGENTS.md`.
6. Las comprobaciones en contenedor y su resultado, una por una.
7. Sin push ni PR hasta que se pida. Los resultados se reportan tal como salgan; el CI queda pendiente hasta el push.
