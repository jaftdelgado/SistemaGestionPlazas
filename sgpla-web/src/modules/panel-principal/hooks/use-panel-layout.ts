import { useRouterState } from "@tanstack/react-router";
import { useLogout, useSesionUsuario } from "@/modules/sesion";
import {
  getActiveItemId,
  getPageTitle,
  NAVIGATION_ITEMS,
} from "../domain/navigation";

export function usePanelLayout() {
  const pathname = useRouterState({ select: (s) => s.location.pathname });
  const usuario = useSesionUsuario();
  const { logout, isLoggingOut } = useLogout();
  const activeId = getActiveItemId(pathname);

  return {
    navigationItems: NAVIGATION_ITEMS,
    activeId,
    pageTitle: getPageTitle(activeId),
    usuario: {
      name: usuario.nombre,
      email: usuario.correo,
      rol: usuario.rol.nombre,
    },
    onLogout: logout,
    isLoggingOut,
  };
}
