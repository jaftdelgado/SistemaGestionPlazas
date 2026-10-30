import { useRouterState } from "@tanstack/react-router";
import {
  BookOpen01Icon,
  Home01Icon,
  Settings01Icon,
  UserGroupIcon,
} from "@hugeicons/core-free-icons";

export const elementosNavegacion = [
  { id: "inicio", titulo: "Inicio", icono: Home01Icon, to: "/" },
  { id: "ofertas", titulo: "Ofertas educativas", icono: BookOpen01Icon },
  { id: "usuarios", titulo: "Usuarios", icono: UserGroupIcon, to: "/usuarios" },
  { id: "configuracion", titulo: "Configuración", icono: Settings01Icon },
] as const;

// Los elementos sin `to` esperan a que exista su módulo.
export function useNavegacion() {
  const pathname = useRouterState({ select: (s) => s.location.pathname });
  const activo =
    elementosNavegacion.find(
      (e) =>
        "to" in e &&
        (e.to === "/" ? pathname === "/" : pathname.startsWith(e.to)),
    )?.id ?? "inicio";
  return { elementos: elementosNavegacion, activo };
}
