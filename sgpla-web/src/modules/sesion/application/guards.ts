import type { QueryClient } from "@tanstack/react-query";
import { redirect } from "@tanstack/react-router";
import { isUnauthenticated } from "@/lib/api/problem";
import { readToken } from "../infrastructure/sesion-token";
import { discardSesion } from "./active-sesion";
import { sesionQueryOptions } from "./sesion-queries";

// Solo un 401 descarta la sesión; cualquier otra falla (red, 5xx) se propaga sin borrar un token que puede seguir vigente.
export async function requireSesion(
  queryClient: QueryClient,
  destination?: string,
) {
  if (readToken() === null) {
    throw redirect({ to: "/login", search: { redirect: destination } });
  }
  try {
    return await queryClient.ensureQueryData(sesionQueryOptions);
  } catch (error) {
    if (isUnauthenticated(error)) {
      discardSesion(queryClient);
      throw redirect({ to: "/login", search: { redirect: destination } });
    }
    throw error;
  }
}

export function hasSavedSesion(): boolean {
  return readToken() !== null;
}
