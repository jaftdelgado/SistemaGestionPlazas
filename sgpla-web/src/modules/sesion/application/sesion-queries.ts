import { $api } from "@/lib/api/query";

// Un 401 no mejora al reintentar: la sesión ya no es válida.
export const sesionQueryOptions = $api.queryOptions(
  "get",
  "/api/v1/usuarios/sesion",
  undefined,
  { retry: false },
);
