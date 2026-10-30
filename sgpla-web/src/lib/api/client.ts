import createClient, { type Middleware } from "openapi-fetch";
import type { paths } from "@/lib/api/schema";

export function createAuthorizationMiddleware(
  readToken: () => string | null,
): Middleware {
  return {
    onRequest({ request }) {
      const token = readToken();
      if (token) {
        request.headers.set("Authorization", `Bearer ${token}`);
      }
      return request;
    },
  };
}

// Un 401 a una petición con token significa sesión vencida o revocada; el inicio de sesión no envía token.
export function createExpiredSesionMiddleware(
  onExpired: () => void,
): Middleware {
  return {
    onResponse({ request, response }) {
      if (response.status === 401 && request.headers.has("Authorization")) {
        onExpired();
      }
      return response;
    },
  };
}

export const client = createClient<paths>({
  baseUrl: import.meta.env.VITE_API_URL,
});
