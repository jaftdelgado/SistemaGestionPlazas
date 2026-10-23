import type { QueryClient } from "@tanstack/react-query";
import {
  client,
  crearMiddlewareAutorizacion,
  crearMiddlewareSesionVencida,
} from "@/lib/api/client";
import { descartarSesion } from "./application/sesion-activa";
import { leerToken } from "./infrastructure/sesion-token";

// Raíz de composición del módulo: conecta la sesión guardada con el cliente de la API. La navegación al vencer la decide quien registra, porque solo él conoce el router.
export function registrarSesion(
  queryClient: QueryClient,
  alVencer: () => void,
): void {
  client.use(
    crearMiddlewareAutorizacion(leerToken),
    crearMiddlewareSesionVencida(() => {
      descartarSesion(queryClient);
      alVencer();
    }),
  );
}
