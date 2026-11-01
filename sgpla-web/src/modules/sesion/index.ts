// Contrato del módulo: lo único que las rutas y los demás módulos pueden importar.
export type { SesionUsuario } from "./domain/sesion";
export { safeDestination } from "./domain/safe-destination";
export { requireSesion, hasSavedSesion } from "./hooks/guards";
export { validateRedirectSearch } from "./hooks/redirect-search";
export { useLogout } from "./hooks/use-logout";
export { useSesionUsuario } from "./hooks/use-sesion-usuario";
export { ChangePasswordPage } from "./ui/pages/change-password-page";
export { LoginPage } from "./ui/pages/login-page";
export { registerSesion } from "./sesion-module";
