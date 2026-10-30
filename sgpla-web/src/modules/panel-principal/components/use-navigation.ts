import { useRouterState } from "@tanstack/react-router";
import {
  BookOpen01Icon,
  Home01Icon,
  Settings01Icon,
  UserGroupIcon,
} from "@hugeicons/core-free-icons";

export const navigationItems = [
  { id: "inicio", title: "Inicio", icon: Home01Icon, to: "/" },
  { id: "ofertas", title: "Ofertas educativas", icon: BookOpen01Icon },
  { id: "usuarios", title: "Usuarios", icon: UserGroupIcon, to: "/usuarios" },
  { id: "configuracion", title: "Configuración", icon: Settings01Icon },
] as const;

// Los elementos sin `to` esperan a que exista su módulo.
export function useNavigation() {
  const pathname = useRouterState({ select: (s) => s.location.pathname });
  const active =
    navigationItems.find(
      (e) =>
        "to" in e &&
        (e.to === "/" ? pathname === "/" : pathname.startsWith(e.to)),
    )?.id ?? "inicio";
  return { items: navigationItems, active };
}
