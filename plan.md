# PR: login de sgpla-web conectado a la API

Implementa el inicio de sesión del frontend contra la API REST: el formulario conectado, el cambio obligatorio de la contraseña temporal del Superusuario, la protección del panel y el manejo global de la sesión vencida. No necesitas el historial de la conversación: todo lo que debes saber está aquí o en las rutas citadas. **Si algo es ambiguo o no coincide con lo que encuentres en el código, DETENTE y pregunta.**

## 1. Rama

Trabaja sobre la rama existente `feat/web-login-components`, que ya contiene la maqueta del login (commit `79762d8`). No crees una rama nueva. Solo commits nuevos: sin `amend`, sin `rebase`, sin `push`.

```bash
git switch feat/web-login-components
git status   # debe estar limpio
```

## 2. Análisis previo (lee antes de escribir código)

Reglas y convenciones:
- `AGENTS.md`: todas las reglas, en especial la 1 (español), la 4 (Docker), la 5 (Git), la 6 (sin atribución de IA), la 8 (sin atajos) y la 11 (alcance).
- `sgpla-web/README.md`: estructura, nombres y comandos.
- `sgpla-web/.oxlintrc.json`: reglas activas, entre ellas `@tanstack/router/create-route-property-order` y `@tanstack/query/exhaustive-deps`.
- `.agents/skills/git-workflow/SKILL.md`: formato de los commits.

Frontend (los patrones que vas a seguir):
- `sgpla-web/src/lib/api/client.ts`, `query.ts` y `client.test.ts`: el cliente `openapi-fetch`, `$api` (`openapi-react-query`) y el middleware de autorización.
- `sgpla-web/src/lib/api/schema.d.ts`: busca `"/api/v1/usuarios/iniciar-sesion"`, `operations["IniciarSesion"]`, `operations["CambiarContrasena"]`, `SesionResponse`, `UsuarioSesionResponse`, `ProblemDetails` y `HttpValidationProblemDetails`. Es un archivo generado: **no se edita**.
- `sgpla-web/src/main.tsx`, `routes/__root.tsx`, `routes/_panel.tsx`, `routes/_panel/index.tsx`, `routes/_panel/usuarios.tsx` y `routes/login.tsx`: el router con contexto `{ queryClient }` y las rutas, que solo montan páginas de `src/modules/<modulo>/pages/`.
- `sgpla-web/src/modules/panel-principal/`: la estructura de un módulo (`components/`, `hooks/`, `layouts/`, `pages/`).
- `sgpla-web/src/components/ui/field.tsx` (`Field`, `FieldLabel`, `FieldDescription`, `FieldError`, `FieldGroup`), `password-input.tsx`, `input.tsx` y `button.tsx`.
- `sgpla-web/src/routes/index.test.tsx`: la prueba de rutas que vas a ajustar.

Backend (el contrato; solo lectura, **no lo modifiques**):
- `sgpla-backend/src/Modules/Usuarios/Sgpla.Modules.Usuarios/Endpoints/Sesion/SesionEndpoints.cs`
- `.../Application/Sesion/IniciarSesion.cs`, `CambiarContrasena.cs` y `SesionConsultas.cs`
- `.../Domain/Cuentas/UsuarioErrors.cs` y `PoliticaContrasena.cs`
- `.../Infrastructure/Autenticacion/VerificacionSesion.cs` y `PoliticasAutorizacion.cs`
- `sgpla-backend/src/BuildingBlocks/Sgpla.BuildingBlocks.Infrastructure/Http/ErrorHttpExtensions.cs`
- `DECISIONES.md`, sección "Usuarios (USU)", en particular USU-D1, D4, D5 y D6.

### Contrato de la API (resumen verificado contra el código)

| Endpoint | Auth | Cuerpo | Éxito | Fallas |
|---|---|---|---|---|
| `POST /api/v1/usuarios/iniciar-sesion` | anónimo | `{ correo, contrasena }` | 200 `SesionResponse { token, expiraEn, usuario }` | 400 con `errors.correo` o `errors.contrasena`; 401 `Usuario.CuentaNoRegistrada`, `Usuario.AmbitoInactivo` o `Usuario.CredencialesInvalidas`; 503 `Usuario.LdapNoDisponible` |
| `GET /api/v1/usuarios/sesion` | token | — | 200 `UsuarioSesionResponse` (incluye `cambioContrasenaPendiente`) | 401 `Autenticacion.NoAutenticado` |
| `POST /api/v1/usuarios/sesion/cambiar-contrasena` | token | `{ contrasenaActual, contrasenaNueva }` | 200 `SesionResponse` con un **token nuevo** | 400 con `errors.contrasenaActual` o `errors.contrasenaNueva`; 409 `Usuario.CambioContrasenaNoAplica` |

- Todos los errores son `application/problem+json`, con `detail` en español y la extensión `codigo`. **`codigo` no está en el esquema OpenAPI**: hay que tiparlo a mano.
- Si `correo` no trae `@`, la API le agrega `@uv.mx`. El campo acepta el usuario UV o el correo.
- El token dura 8 horas, sin refresh; cerrar sesión solo tiene efecto en el cliente.
- Mientras `cambioContrasenaPendiente` es `true`, todo endpoint, salvo `/sesion` y `/sesion/cambiar-contrasena`, responde 403 con `codigo` `Autenticacion.CambioContrasenaPendiente`.
- Los enteros del esquema aparecen como `number | string` (así los genera .NET 10): no conviertas tipos, úsalos tal cual.

## 3. Alcance exacto

### Decisiones ya tomadas (no las cambies)
1. El token se guarda en **`localStorage`**, bajo una sola clave `sgpla.sesion`, como JSON `{ token, expiraEn }`. Todo acceso a `localStorage` va en `try/catch` y devuelve `null` si falla. El `catch` no queda vacío: devuelve `null` o no hace nada, con un comentario que diga por qué.
2. **Sin paquetes nuevos**: el formulario usa estado de React y validación nativa (`required`).
3. **Sin pruebas nuevas.** Solo se ajusta la prueba existente que deja de ser válida (punto 3.6).
4. El backend no se toca.
5. La salida y los comentarios van en español; los identificadores de dominio, en español sin acentos.

### 3.1 `src/lib/api/sesion-token.ts` (nuevo)
Exporta:
- `type SesionGuardada = { token: string; expiraEn: string }`.
- `guardarSesion(sesion: SesionGuardada): void`.
- `leerToken(): string | null`: lee la clave y, si no existe, el JSON es inválido o `Date.parse(expiraEn) <= Date.now()`, borra la clave y devuelve `null`.
- `borrarSesion(): void`.

### 3.2 `src/lib/api/client.ts` (modificar)
- Elimina la función `obtenerToken` y su comentario "Cómo se guarda el token se decide en el PR del login: por ahora no hay sesión."; en su lugar, usa `leerToken` de `sesion-token.ts`.
- Conserva `crearMiddlewareAutorizacion` con la **misma firma**: `client.test.ts` la usa y debe seguir pasando sin cambios.
- Agrega y exporta `registrarAlVencerSesion(fn: () => void): void`, que guarda el callback en una variable del módulo.
- Agrega un segundo middleware con `onResponse({ request, response })`: si `response.status === 401` y `request.headers.has("Authorization")`, llama al callback registrado. Devuelve siempre `response`. `iniciar-sesion` no envía token, así que una contraseña incorrecta no lo dispara.
- Regístralo con `client.use(...)` después del de autorización.

### 3.3 `src/lib/api/problema.ts` (nuevo)
```ts
import type { components } from "@/lib/api/schema";

export type ProblemaApi = components["schemas"]["HttpValidationProblemDetails"] & {
  codigo?: string;
};
```
Exporta también:
- `mensajeDeProblema(error: unknown): string`: si `error` es un objeto con `detail` de tipo string no vacío, lo devuelve. Si no (por ejemplo, un `TypeError` de `fetch` cuando la red falla), devuelve exactamente `No se pudo conectar con el servidor. Intenta de nuevo.`
- `erroresDeCampo(error: unknown): Record<string, string[]>`: los `errors` del problema, o `{}`.

### 3.4 Módulo `src/modules/sesion/` (nuevo)
- `lib/sesion-queries.ts`:
  - `export const sesionQueryOptions = $api.queryOptions("get", "/api/v1/usuarios/sesion");`, con `$api` de `@/lib/api/query`.
  - `export function destinoSeguro(redirect: string | undefined): string`: devuelve `redirect` solo si empieza con `/` y no con `//`; si no, `/`.
- `hooks/use-iniciar-sesion.ts`: envuelve `$api.useMutation("post", "/api/v1/usuarios/iniciar-sesion")`. En `onSuccess(data)`:
  1. `guardarSesion({ token: data.token, expiraEn: data.expiraEn })`;
  2. `queryClient.setQueryData(sesionQueryOptions.queryKey, data.usuario)`;
  3. si `data.usuario.cambioContrasenaPendiente` es `true`, navega a `/cambiar-contrasena` (conservando `redirect` en el search); si no, navega a `destinoSeguro(redirect)`. Usa `useNavigate` y `useQueryClient`.
- `hooks/use-cambiar-contrasena.ts`: lo mismo con `"/api/v1/usuarios/sesion/cambiar-contrasena"`. Guarda el token nuevo, actualiza la sesión en caché y navega a `destinoSeguro(redirect)`.
- `pages/login-page.tsx`: mueve aquí el JSX actual de `routes/login.tsx`, conservando textos y estructura, con estos cambios:
  - campos controlados (`useState`) `usuario` y `contrasena`, ambos `required`;
  - `onSubmit`: `preventDefault()` y `mutate({ body: { correo: usuario, contrasena } })`;
  - mientras `isPending`, el botón queda `disabled` y su texto pasa a `Entrando…`;
  - si hay error: un `FieldError` bajo cada campo con `erroresDeCampo(error)` (`correo` para el usuario y `contrasena` para la contraseña: la API agrupa por el nombre de la propiedad y lo pasa a camelCase) y, sobre el botón, un `<p role="alert" className="text-sm text-destructive">` con `mensajeDeProblema(error)`;
  - en el `Field` con error, `data-invalid="true"`, y en el input, `aria-invalid`;
  - elimina el comentario "Maqueta conceptual: todavía no envía las credenciales ni valida el formulario."
- `pages/cambiar-contrasena-page.tsx`: el mismo layout que el login (un `main` centrado y un `form` `max-w-sm`), con el título `Cambia tu contraseña` y la descripción `Tu contraseña es temporal. Elige una nueva para continuar.` Tiene tres `PasswordInput`, todos `required`:
  - `contrasenaActual`, con la etiqueta `Contraseña actual` y `autoComplete="current-password"`;
  - `contrasenaNueva`, con la etiqueta `Contraseña nueva`, `autoComplete="new-password"` y el `FieldDescription` `De 8 a 128 caracteres, con al menos una mayúscula, una minúscula, un número y un símbolo.`;
  - `confirmacion`, con la etiqueta `Confirma la contraseña nueva` y `autoComplete="new-password"`.

  Si la confirmación no coincide con la nueva, no envía y muestra el `FieldError` `Las contraseñas no coinciden.` Los errores de la API se muestran igual que en el login (`contrasenaActual` y `contrasenaNueva`). El botón dice `Cambiar contraseña`, o `Guardando…` mientras está pendiente. La política la valida la API: no la dupliques en el cliente.

### 3.5 Rutas
- `routes/login.tsx`: solo monta `LoginPage`. Agrega `validateSearch`, que acepta `redirect?: string` (si no es string, `undefined`), y `beforeLoad`: si `leerToken()` no es `null`, `throw redirect({ to: destinoSeguro(search.redirect) })`. Respeta el orden de propiedades que exige `create-route-property-order` (`validateSearch` antes de `beforeLoad` y `component` al final; si el lint pide otro orden, sigue al lint).
- `routes/_panel.tsx`: agrega `beforeLoad: async ({ context, location }) => { … }` con estos pasos:
  1. si `leerToken()` es `null`: `throw redirect({ to: "/login", search: { redirect: location.href } })`;
  2. `const sesion = await context.queryClient.ensureQueryData(sesionQueryOptions)`, dentro de `try/catch`; en el `catch`: `borrarSesion()` y la misma redirección a `/login`;
  3. si `sesion.cambioContrasenaPendiente`: `throw redirect({ to: "/cambiar-contrasena", search: { redirect: location.href } })`;
  4. `return { sesion }`.
- `routes/cambiar-contrasena.tsx` (nuevo, en la raíz de `routes/`, **fuera** de `_panel`): `validateSearch` igual que el login. En `beforeLoad`: sin token, redirige a `/login`; con token, `ensureQueryData(sesionQueryOptions)` (si falla, borra la sesión y redirige a `/login`); si `cambioContrasenaPendiente` es `false`, redirige a `destinoSeguro(search.redirect)`. `component: CambiarContrasenaPage`.
- `main.tsx`: después de crear `router`, registra:
  ```ts
  registrarAlVencerSesion(() => {
    borrarSesion();
    queryClient.clear();
    void router.navigate({ to: "/login", search: { redirect: router.state.location.href } });
  });
  ```
- `routeTree.gen.ts` no se edita a mano. Para regenerarlo con la ruta nueva, ejecuta el plugin del router en el contenedor del servicio `web`, sin levantar el servidor de desarrollo:
  ```bash
  docker compose run --rm --no-deps web sh -c "corepack enable && pnpm install --frozen-lockfile && pnpm exec vite build"
  ```
  `dist/` está en `.gitignore`. Versiona solo `src/routeTree.gen.ts`.

### 3.6 Prueba que se ajusta: `src/routes/index.test.tsx`
Con el panel protegido, la prueba "muestra el panel principal en la ruta raíz" deja de ser válida: sin token, `/` redirige al login. Cámbiala para comprobar ese comportamiento, sin red, porque sin token no se consulta `/sesion`:
- nombre de la prueba: `redirige al inicio de sesión cuando no hay sesión en la ruta raíz`;
- aserción: `await screen.findByRole("heading", { name: "Iniciar sesión" })`;
- `localStorage` arranca vacío en jsdom; si otra prueba lo llenara, límpialo en la propia prueba.

La prueba del 404 no cambia. No agregues pruebas nuevas.

### 3.7 Documentación: `sgpla-web/README.md`
La línea 3 actual:
```text
Frontend de SGPLa. Esta carpeta contiene solo la configuración inicial: sin pantallas de negocio ni inicio de sesión.
```
debe quedar exactamente así:
```text
Frontend de SGPLa: el panel principal y el inicio de sesión contra la API REST de `../sgpla-backend`.
```
Agrega además, antes de la sección `## Variables`, esta sección exactamente así:
```markdown
## Sesión

El token que devuelve `POST /api/v1/usuarios/iniciar-sesion` se guarda en `localStorage`, bajo la clave `sgpla.sesion`, junto con su vencimiento (`expiraEn`), y se descarta al vencer. El cliente lo envía como `Authorization: Bearer` en cada petición. Un 401 con token borra la sesión y lleva al inicio de sesión. Las rutas del panel exigen una sesión vigente, y una cuenta con contraseña temporal solo puede abrir `/cambiar-contrasena`.
```

### Fuera de alcance (repórtalo como nota si lo detectas; no lo implementes)
- El botón "Cerrar sesión" (`POST /cerrar-sesion`) y los datos del usuario en `PanelAvatar`.
- La redirección global ante un 403 `Autenticacion.CambioContrasenaPendiente` en peticiones sueltas.
- Cualquier cambio en `sgpla-backend/`, en `schema.d.ts` o en `package.json`.

## 4. Commits sugeridos

Formato de `git-workflow`: título `tipo(scope): participio` y de 4 a 5 viñetas en participio, en minúscula y sin punto final. Sin `Co-Authored-By`.

1. `feat(web): agregada la sesión del cliente de la API`: `sesion-token.ts`, `problema.ts` y los middlewares de `client.ts`.
2. `feat(web): conectado el inicio de sesión con la API`: el módulo `sesion`, `routes/login.tsx` y `main.tsx`.
3. `feat(web): agregado el cambio de la contraseña temporal`: la página y la ruta `/cambiar-contrasena`, más `routeTree.gen.ts`.
4. `feat(web): protegido el panel con la sesión vigente`: `routes/_panel.tsx` y el ajuste de `index.test.tsx`.
5. `docs(web): documentada la sesión del frontend`: `README.md`.

`git add` siempre por ruta explícita. No agregues `plan.md`.

## 5. Verificación final (una sola vez, al terminar)

```bash
sgpla-web/scripts/verify.sh
```
Debe terminar con `Verificación: OK` y `Atajos nuevos … ninguno`. Si falla, la corrección va en un commit nuevo y se repite la verificación. No levantes el servidor de Vite (`pnpm dev`): la prueba manual la hace el responsable.

Lista para la prueba manual del responsable (inclúyela en tu entrega; no la ejecutes):
1. `/` sin sesión redirige a `/login?redirect=…`.
2. Un correo inexistente muestra `La cuenta no está registrada en el sistema.`
3. Un Superusuario creado con `docker compose run --rm api bootstrap-superusuario` entra, va a `/cambiar-contrasena`, la cambia, llega al panel y una recarga (F5) conserva la sesión.
4. Borrar `sgpla.sesion` en DevTools y navegar dentro del panel devuelve al login.
5. Una cuenta DGAA o Entidad Académica sin red hacia la UV muestra el mensaje del 503 `LdapNoDisponible`.

## 6. Reglas
- Si algo es ambiguo, DETENTE y pregunta.
- `git add` por ruta explícita, nunca `-A` ni `.`.
- Sin push ni PR: el revisor revisa la rama local.
- Sin atribución de IA en commits.
- Docker como único entorno: no uses `node` ni `pnpm` locales.
- Sin atajos (`AGENTS.md`, regla 8): ni `@ts-ignore`, ni `oxlint-disable` sin `-- motivo`, ni `catch` vacíos.
- No amplíes el alcance: lo que encuentres fuera se reporta como nota.

## 7. Formato de entrega
1. Tabla de commits (hash corto y título).
2. Tabla de verificación: cada paso de `verify.sh` (install, format:check, lint, typecheck, test con el conteo de archivos y pruebas, build) y los atajos nuevos.
3. Cumplimiento punto por punto de la sección 3 (3.1 a 3.7).
4. Decisiones que tomaste y no estaban escritas, o desviaciones (si las hubo, explica por qué no te detuviste).
5. Notas de lo que detectaste fuera de alcance.
6. La lista de la prueba manual para el responsable.
7. Propuesta de título y descripción del PR (en español, sin emojis, sin diffs ni rutas).
