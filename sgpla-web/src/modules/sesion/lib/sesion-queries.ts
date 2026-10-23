import { $api } from "@/lib/api/query";

export const sesionQueryOptions = $api.queryOptions(
  "get",
  "/api/v1/usuarios/sesion",
);

// Solo se aceptan rutas internas: una dirección absoluta o que empieza con // llevaría a otro sitio.
export function destinoSeguro(redirect: string | undefined): string {
  return redirect?.startsWith("/") && !redirect.startsWith("//")
    ? redirect
    : "/";
}
