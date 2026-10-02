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
| Base | React + Vite + TypeScript estricto |
| Gestor | pnpm, con su versión fijada en `packageManager`; `pnpm-lock.yaml` versionado |
| Versiones | Exactas: `.npmrc` con `save-exact=true`, sin `^` |
| Node | 24 LTS, con imagen `node:24.x.y-alpine` exacta (patch vigente al implementar), `.nvmrc` y `engines` |
| Entorno | Servicio `web` en compose (Vite dev con recarga en caliente) más `sgpla-web/scripts/verify.sh`. Sin Node local |
| `node_modules` | En la carpeta del host (`sgpla-web/node_modules`), instalado desde el contenedor, para que VS Code vea los tipos |
| Router | TanStack Router con rutas por archivos (plugin de Vite) |
| Datos | TanStack Query |
| API | Tipos con `openapi-typescript`; `openapi-fetch` y `openapi-react-query` |
| UI | Tailwind v4 y shadcn/ui con Base UI |
| Calidad | ESLint (flat config), Prettier, Vitest y Testing Library |
| Estructura | `src/modules/<modulo-kebab>/` (nombres de módulo en español); las demás carpetas en inglés |
| Identificadores | Dominio en español sin acentos; términos técnicos en inglés; UI y comentarios en español |
| Producción | Fuera de este PR: sin imagen nginx |
| Gobernanza | `AGENTS.md`, `frontend-ci.yml` y los README; la skill del frontend llega después |

## Rama y commits

Rama `chore/web-configuracion`, desde `origin/develop` con `--no-track`. Commits (skill `git-workflow`, scope `web`):
1. `chore(web): agregado el esqueleto de React con Vite y TypeScript`: `package.json`, tsconfig, `vite.config.ts`, ESLint, Prettier, Vitest y la prueba mínima.
2. `chore(web): configurados TanStack Router, TanStack Query y el cliente tipado de la API`
3. `chore(web): configurados Tailwind y shadcn/ui con Base UI`
4. `chore(web): agregados el servicio de desarrollo en Docker y el script de verificación`
5. `ci(web): agregado el workflow del frontend`
6. `docs(agents): extendidas al frontend las reglas de entorno, atajos y licencias`, junto con los README.

Título del PR: `chore(web): agregada la configuración inicial del frontend`.

## Contenido de `sgpla-web/`

**Raíz:**
- `package.json`:
  - `"type": "module"`, `packageManager: pnpm@<x.y.z>` y `engines.node: ">=24 <25"`;
  - `pnpm.onlyBuiltDependencies` solo con lo que necesita compilar (`esbuild`, `@tailwindcss/oxide`), porque pnpm 10 bloquea los *postinstall*.
- `.npmrc` (`save-exact=true`) y `.nvmrc` (`24`).
- `tsconfig.json`, `tsconfig.app.json` y `tsconfig.node.json`, como la plantilla react-ts:
  - `strict`, `noUncheckedIndexedAccess` y `noUnusedLocals/Parameters`;
  - alias `@/*` → `src/*`.
- `vite.config.ts`:
  - `tanstackRouter({ target: 'react', autoCodeSplitting: true })` antes de `react()`, y `tailwindcss()`;
  - alias `@`;
  - `server: { host: true, port: 5173, strictPort: true, watch: { usePolling: env SGPLA_WEB_POLLING === 'true' } }`. El *polling* hace falta porque los eventos de archivo no cruzan el montaje desde NTFS.
  - Bloque `test` de Vitest: `jsdom`, `setupFiles: src/test/setup.ts`, `globals: false`.
- `eslint.config.js`:
  - `@eslint/js`, `typescript-eslint` (`recommendedTypeChecked`), `react-hooks`, `react-refresh` (`allowConstantExport`) y `@tanstack/eslint-plugin-query` y `-router`;
  - `eslint-config-prettier` al final;
  - ignora `dist`, `routeTree.gen.ts` y `schema.d.ts`.
- `.prettierrc.json` con `prettier-plugin-tailwindcss`, y `.prettierignore` con los generados, `pnpm-lock.yaml` y `dist`.
- `components.json` de shadcn, con la variante Base UI. El CLI se consulta en el contenedor al implementar para usar su forma vigente.
- `index.html` con `lang="es"` y título SGPLa.

**`src/`:**
- `main.tsx`: crea `QueryClient` y el router con `context: { queryClient }`, y monta `QueryClientProvider` y `RouterProvider`.
- `routes/__root.tsx`: `createRootRouteWithContext<{ queryClient: QueryClient }>()`, con un layout mínimo con `<Outlet/>`, un `notFoundComponent` en español y las devtools de Router y Query solo con `import.meta.env.DEV`.
- `routes/index.tsx`: página de inicio de marcador, con un `Button` de shadcn para comprobar Tailwind.
- `routeTree.gen.ts`: generado por el plugin y versionado. Se marca `linguist-generated` en `.gitattributes`.
- `lib/query-client.ts`: fábrica del `QueryClient` con sus opciones por omisión (`retry` y `staleTime`).
- `lib/api/`:
  - `schema.d.ts`: generado y versionado. Lo crea el script `api:generar` desde `http://api:8080/openapi/v1.json` dentro de la red de compose.
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
- `dev`, `build` (`tsc -b && vite build`), `preview` y `typecheck` (`tsc -b --noEmit`);
- `lint` (`eslint . --max-warnings 0`), `format` y `format:check`;
- `test` (`vitest run`) y `api:generar` (`openapi-typescript $SGPLA_OPENAPI_URL -o src/lib/api/schema.d.ts`).

## Docker

- **`docker-compose.yml`: servicio nuevo `web`.**
  - Imagen `node:24.x.y-alpine`, `working_dir: /app` y montaje `./sgpla-web:/app`, con `node_modules` en el host.
  - Volumen nombrado `pnpm-store` en `/pnpm/store`, con `npm_config_store_dir=/pnpm/store`. pnpm copia en lugar de enlazar porque son sistemas de archivos distintos, y es aceptable.
  - `command: sh -c "corepack enable && pnpm install --frozen-lockfile && pnpm dev"`.
  - Variables:
    - `VITE_API_URL: "http://localhost:${SGPLA_API_PORT:-8180}"`: el navegador llama al puerto del host y usa el CORS existente;
    - `SGPLA_WEB_POLLING: "${SGPLA_WEB_POLLING:-true}"`;
    - `SGPLA_OPENAPI_URL: "http://api:8080/openapi/v1.json"`.
  - Puerto `${SGPLA_WEB_PORT:-5173}:5173`. Sin `depends_on` del api: el frontend arranca aunque la API no esté.
- **`.env.example`:** `SGPLA_WEB_PORT=5173` y `SGPLA_WEB_POLLING=true`, con comentarios, junto a `SGPLA_WEB_ORIGIN`; se indica que el puerto y el origen deben coincidir.
- **`sgpla-web/scripts/verify.sh`:** mismo patrón que `sgpla-backend/scripts/verify.sh`.
  - Conserva el manejo de MSYS (`MSYS_NO_PATHCONV` y `pwd -W`) y la copia a `/w` sin `node_modules`, `dist` ni `TestResults`.
  - En la copia: `corepack enable && pnpm install --frozen-lockfile && pnpm format:check && pnpm lint && pnpm typecheck && pnpm test && pnpm build`.
  - Usa el volumen `sgpla-pnpm-store`; el log va a `sgpla-web/TestResults/verify.log`.
  - Al final avisa de los atajos nuevos respecto a `origin/develop`: `eslint-disable` sin `--` de justificación, `@ts-ignore`, `@ts-expect-error` sin descripción, `.skip(`/`.only(`, `setTimeout` en pruebas y versiones con `^` o `~` en `package.json`.
- **`sgpla-web/scripts/generar-api.sh`:** `docker compose run --rm web sh -c "corepack enable && pnpm api:generar"`. Requiere la API levantada en Development.

## CI: `.github/workflows/frontend-ci.yml`

- Se ejecuta con `push`, `pull_request` (hacia `main` y `develop`) y `workflow_dispatch`, solo con cambios en `sgpla-web/**` o en el propio workflow.
- `concurrency` y `working-directory: sgpla-web`, como `backend-ci.yml`.
- Pasos:
  - `actions/checkout@v5`, `pnpm/action-setup` (lee `packageManager`) y `actions/setup-node` con `node-version-file: sgpla-web/.nvmrc` y `cache: pnpm`;
  - `pnpm install --frozen-lockfile`, `format:check`, `lint`, `typecheck`, `test` y `build`.
- El CI no regenera `schema.d.ts`, porque no tiene la API; queda como nota del PR.

## Configuración de la raíz

- `.gitattributes`: `sgpla-web/src/routeTree.gen.ts` y `sgpla-web/src/lib/api/schema.d.ts` con `linguist-generated=true`.
- `.gitignore`: ya cubre `node_modules/`, `dist/`, `.vite/`, `coverage/` y `TestResults/`. Se agrega `*.tsbuildinfo`.

## Documentos

- **`AGENTS.md`:**
  - el párrafo inicial pasa a decir que `sgpla-web/` es React + Vite;
  - **regla 4:** el frontend también se ejecuta solo con Docker, con `sgpla-web/scripts/verify.sh` y el servicio `web`, sin Node ni pnpm locales;
  - **regla 8:** agrega los atajos del frontend que avisa el script;
  - **regla 10:** pasa a "paquetes NuGet o npm";
  - en "Puertas de calidad": antes de entregar un PR del frontend, `sgpla-web/scripts/verify.sh`;
  - la tabla de skills no cambia.
- **`sgpla-web/README.md`:** se reescribe con:
  - stack;
  - estructura (`modules/` en kebab-case y en español; lo demás en inglés);
  - comandos con Docker (levantar, verificar, generar la API y agregar un paquete con `docker compose run --rm web pnpm add <pkg>` revisando la licencia);
  - variables y la nota del *polling* en Windows.
- **`README.md` (raíz):** la fila de `sgpla-web/`, la sección de levantar el entorno (el frontend en `http://localhost:5173`) y CI (`frontend-ci.yml` ya existe).
- **`PLAN_INICIAL.md`:** la línea "`frontend-ci.yml` se agregará…" pasa a reflejar que ya existe. Es una corrección de una contradicción, pedida por la regla del repositorio.

## Paquetes y licencias (regla 10)

Se instalan en el contenedor con la última versión estable al implementar. Antes del commit, `pnpm licenses list` (producción y desarrollo) va a una tabla en la descripción del PR. Si aparece una licencia no permisiva (GPL, AGPL, SSPL o similar) en una dependencia transitiva, **se detiene y se pregunta**.

| Grupo | Paquetes | Licencia esperada |
|---|---|---|
| Runtime | `react`, `react-dom`, `@tanstack/react-router`, `@tanstack/react-query`, `openapi-fetch`, `openapi-react-query`, `@base-ui/react` (o el nombre vigente), `class-variance-authority`, `clsx`, `tailwind-merge`, `lucide-react` | MIT, Apache-2.0 o ISC |
| Desarrollo | `vite`, `@vitejs/plugin-react`, `typescript`, `tailwindcss`, `@tailwindcss/vite`, `tw-animate-css`, `@tanstack/router-plugin`, `@tanstack/react-router-devtools`, `@tanstack/react-query-devtools`, `openapi-typescript`, `eslint` y sus plugins, `typescript-eslint`, `prettier`, `prettier-plugin-tailwindcss`, `vitest`, `jsdom`, `@testing-library/react`, `@testing-library/jest-dom`, `@testing-library/user-event`, `@types/react`, `@types/react-dom`, `@types/node` | MIT, Apache-2.0 o BSD |

## Fuera de alcance (notas para el PR)

- Login y guardado del token (el PR del login); pantallas de los módulos.
- Librería de formularios.
- Imagen de producción y origen de la API en producción.
- Skill del frontend y estándar del frontend: llegan cuando haya patrones reales.
- Que el CI detecte un `schema.d.ts` desactualizado.

## Verificación

1. `sgpla-web/scripts/verify.sh` termina en 0, con los conteos de las pruebas (4 casos) y sin avisos de atajos.
2. `docker compose up -d` y luego:
   - `http://localhost:5173` muestra la página de inicio con el botón estilizado;
   - al editar `routes/index.tsx`, la recarga en caliente lo refleja;
   - una ruta inexistente muestra el 404.
3. Con la API arriba, `sgpla-web/scripts/generar-api.sh` regenera `schema.d.ts` sin diferencias respecto al versionado, y `pnpm typecheck` pasa.
4. `sgpla-backend/scripts/verify.sh` no cambia de resultado: el PR no toca el backend.
5. `bash .github/scripts/check-skills.sh` pasa tras editar `AGENTS.md`.
6. Sin push ni PR hasta que se pida. Los resultados se reportan tal como salgan.
