import createClient, { type Middleware } from "openapi-fetch";
import type { paths } from "@/lib/api/schema";

// Cómo se guarda el token se decide en el PR del login: por ahora no hay sesión.
export function obtenerToken(): string | null {
  return null;
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

export const client = createClient<paths>({
  baseUrl: import.meta.env.VITE_API_URL,
});

client.use(crearMiddlewareAutorizacion(obtenerToken));
