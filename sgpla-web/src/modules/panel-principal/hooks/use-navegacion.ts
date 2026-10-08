import { BookOpen, Home, Settings, Users } from "lucide-react";

export const elementosNavegacion = [
  { id: "inicio", titulo: "Inicio", icono: Home },
  { id: "ofertas", titulo: "Ofertas educativas", icono: BookOpen },
  { id: "usuarios", titulo: "Usuarios", icono: Users },
  { id: "configuracion", titulo: "Configuración", icono: Settings },
] as const;

// Placeholder: la navegación real se definirá con los módulos del sistema.
export function useNavegacion() {
  return { elementos: elementosNavegacion, activo: "inicio" };
}
