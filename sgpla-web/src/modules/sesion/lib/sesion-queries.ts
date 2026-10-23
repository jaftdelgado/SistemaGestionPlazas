import type { QueryClient } from "@tanstack/react-query";
import { redirect } from "@tanstack/react-router";
import { esNoAutenticado } from "@/lib/api/problema";
import { $api } from "@/lib/api/query";
import { borrarSesion, leerToken } from "@/lib/api/sesion-token";

// Un 401 no mejora al reintentar: la sesión ya no es válida.
export const sesionQueryOptions = $api.queryOptions(
  "get",
  "/api/v1/usuarios/sesion",
  undefined,
  { retry: false },
);

// Solo se aceptan rutas internas: una dirección absoluta o que empieza con // o /\ llevaría a otro sitio.
export function destinoSeguro(redirect: string | undefined): string {
  return redirect?.startsWith("/") &&
    !redirect.startsWith("//") &&
    !redirect.startsWith("/\\")
    ? redirect
    : "/";
}

export function validarBusquedaConRedirect(search: Record<string, unknown>): {
  redirect?: string;
} {
  return {
    redirect: typeof search.redirect === "string" ? search.redirect : undefined,
  };
}

// Solo un 401 descarta la sesión; cualquier otra falla (red, 5xx) se propaga sin borrar un token que puede seguir vigente.
export async function exigirSesion(queryClient: QueryClient, destino?: string) {
  if (leerToken() === null) {
    throw redirect({ to: "/login", search: { redirect: destino } });
  }
  try {
    return await queryClient.ensureQueryData(sesionQueryOptions);
  } catch (error) {
    if (esNoAutenticado(error)) {
      borrarSesion();
      queryClient.clear();
      throw redirect({ to: "/login", search: { redirect: destino } });
    }
    throw error;
  }
}
