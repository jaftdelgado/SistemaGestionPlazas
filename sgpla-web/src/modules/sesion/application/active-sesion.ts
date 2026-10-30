import type { QueryClient } from "@tanstack/react-query";
import type { components } from "@/lib/api/schema";
import { clearSesion, saveSesion } from "../infrastructure/sesion-token";
import { sesionQueryOptions } from "./sesion-queries";

type SesionResponse = components["schemas"]["SesionResponse"];

// La sesión se conserva y el usuario queda en caché, así la guarda de ruta no vuelve a pedirlo.
export function establishSesion(
  queryClient: QueryClient,
  sesion: SesionResponse,
): void {
  saveSesion({ token: sesion.token, expiraEn: sesion.expiraEn });
  queryClient.setQueryData(sesionQueryOptions.queryKey, sesion.usuario);
}

// Se limpia toda la caché: los datos del usuario anterior no deben quedar visibles para el siguiente.
export function discardSesion(queryClient: QueryClient): void {
  clearSesion();
  queryClient.clear();
}
