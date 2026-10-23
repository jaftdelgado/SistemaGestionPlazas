import type { QueryClient } from "@tanstack/react-query";
import { redirect } from "@tanstack/react-router";
import { esNoAutenticado } from "@/lib/api/problema";
import { leerToken } from "../infrastructure/sesion-token";
import { descartarSesion } from "./sesion-activa";
import { sesionQueryOptions } from "./sesion-queries";

// Solo un 401 descarta la sesión; cualquier otra falla (red, 5xx) se propaga sin borrar un token que puede seguir vigente.
export async function exigirSesion(queryClient: QueryClient, destino?: string) {
  if (leerToken() === null) {
    throw redirect({ to: "/login", search: { redirect: destino } });
  }
  try {
    return await queryClient.ensureQueryData(sesionQueryOptions);
  } catch (error) {
    if (esNoAutenticado(error)) {
      descartarSesion(queryClient);
      throw redirect({ to: "/login", search: { redirect: destino } });
    }
    throw error;
  }
}

export function haySesionGuardada(): boolean {
  return leerToken() !== null;
}
