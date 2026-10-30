import type { QueryClient } from "@tanstack/react-query";
import {
  client,
  createAuthorizationMiddleware,
  createExpiredSesionMiddleware,
} from "@/lib/api/client";
import { discardSesion } from "./application/active-sesion";
import { readToken } from "./infrastructure/sesion-token";

// Raíz de composición del módulo: conecta la sesión guardada con el cliente de la API. La navegación al vencer la decide quien registra, porque solo él conoce el router.
export function registerSesion(
  queryClient: QueryClient,
  onExpired: () => void,
): void {
  client.use(
    createAuthorizationMiddleware(readToken),
    createExpiredSesionMiddleware(() => {
      discardSesion(queryClient);
      onExpired();
    }),
  );
}
