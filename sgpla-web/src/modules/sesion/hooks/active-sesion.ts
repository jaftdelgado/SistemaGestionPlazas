import type { QueryClient } from "@tanstack/react-query";
import { sesionKeys } from "../api/sesion";
import type { SesionIniciada } from "../domain/sesion";
import { clearSesion, saveSesion } from "../storage/sesion-token";

// La sesión se conserva y el usuario queda en caché, así la guarda de ruta no vuelve a pedirlo.
export function establishSesion(
  queryClient: QueryClient,
  sesion: SesionIniciada,
): void {
  saveSesion({ token: sesion.token, expiraEn: sesion.expiraEn });
  queryClient.setQueryData(sesionKeys.current, sesion.usuario);
}

// Se limpia toda la caché: los datos del usuario anterior no deben quedar visibles para el siguiente.
export function discardSesion(queryClient: QueryClient): void {
  clearSesion();
  queryClient.clear();
}
