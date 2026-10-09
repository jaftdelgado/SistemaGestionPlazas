import { useRouterState } from "@tanstack/react-router";
import { BookOpen, Home, Settings, Users } from "lucide-react";

export const elementosNavegacion = [
  { id: "inicio", titulo: "Inicio", icono: Home, to: "/" },
  { id: "ofertas", titulo: "Ofertas educativas", icono: BookOpen },
  { id: "usuarios", titulo: "Usuarios", icono: Users, to: "/usuarios" },
  { id: "configuracion", titulo: "Configuración", icono: Settings },
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
