import type { QueryClient } from "@tanstack/react-query";
import { redirect } from "@tanstack/react-router";
import { hasStatus } from "@/shared/api/errors";
import { sesionQuery } from "../api/sesion";
import { readToken } from "../storage/sesion-token";
import { discardSesion } from "./active-sesion";

// Solo un 401 descarta la sesión; cualquier otra falla (red, 5xx) se propaga sin borrar un token que puede seguir vigente.
export async function requireSesion(
  queryClient: QueryClient,
  destination?: string,
) {
  if (readToken() === null) {
    throw redirect({ to: "/login", search: { redirect: destination } });
  }
  try {
    return await queryClient.ensureQueryData(sesionQuery);
  } catch (error) {
    if (hasStatus(error, 401)) {
      discardSesion(queryClient);
      throw redirect({ to: "/login", search: { redirect: destination } });
    }
    throw error;
  }
}

export function hasSavedSesion(): boolean {
  return readToken() !== null;
}
