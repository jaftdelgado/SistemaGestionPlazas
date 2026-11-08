import { useSuspenseQuery } from "@tanstack/react-query";
import { sesionQuery } from "../api/sesion";

// La guarda del panel ya dejó el usuario en caché, así que la lectura no suspende.
export function useSesionUsuario() {
  return useSuspenseQuery(sesionQuery).data;
}
