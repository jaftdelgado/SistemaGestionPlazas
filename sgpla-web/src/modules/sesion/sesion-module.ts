import type { QueryClient } from "@tanstack/react-query";
import {
  client,
  createAuthorizationMiddleware,
  createExpiredSesionMiddleware,
} from "@/shared/api/client";
import { discardSesion } from "./hooks/active-sesion";
import { readToken } from "./storage/sesion-token";

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
