import createClient, { type Middleware } from "openapi-fetch";
import { leerToken as leerTokenGuardado } from "@/lib/api/sesion-token";
import type { paths } from "@/lib/api/schema";

let alVencerSesion: () => void = () => {};

export function registrarAlVencerSesion(fn: () => void): void {
  alVencerSesion = fn;
}

export function crearMiddlewareAutorizacion(
  leerToken: () => string | null,
): Middleware {
  return {
    onRequest({ request }) {
      const token = leerToken();
      if (token) {
        request.headers.set("Authorization", `Bearer ${token}`);
      }
      return request;
    },
  };
}

// Un 401 a una petición con token significa sesión vencida o revocada; el inicio de sesión no envía token.
const middlewareSesionVencida: Middleware = {
  onResponse({ request, response }) {
    if (response.status === 401 && request.headers.has("Authorization")) {
      alVencerSesion();
    }
    return response;
  },
};

export const client = createClient<paths>({
  baseUrl: import.meta.env.VITE_API_URL,
});

client.use(crearMiddlewareAutorizacion(leerTokenGuardado));
client.use(middlewareSesionVencida);
