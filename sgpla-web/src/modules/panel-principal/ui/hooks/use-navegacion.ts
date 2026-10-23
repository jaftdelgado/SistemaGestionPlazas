import { useRouterState } from "@tanstack/react-router";
import {
  BookOpenRegular,
  HomeRegular,
  SettingsRegular,
  PeopleRegular,
} from "@fluentui/react-icons";

export const elementosNavegacion = [
  { id: "inicio", titulo: "Inicio", icono: HomeRegular, to: "/" },
  { id: "ofertas", titulo: "Ofertas educativas", icono: BookOpenRegular },
  { id: "usuarios", titulo: "Usuarios", icono: PeopleRegular, to: "/usuarios" },
  { id: "configuracion", titulo: "Configuración", icono: SettingsRegular },
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
