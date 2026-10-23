import type { components } from "@/lib/api/schema";

export type ProblemaApi =
  components["schemas"]["HttpValidationProblemDetails"] & {
    codigo?: string;
  };

const MENSAJE_SIN_CONEXION =
  "No se pudo conectar con el servidor. Intenta de nuevo.";

export function mensajeDeProblema(error: unknown): string {
  if (
    typeof error === "object" &&
    error !== null &&
    "detail" in error &&
    typeof error.detail === "string" &&
    error.detail !== ""
  ) {
    return error.detail;
  }
  return MENSAJE_SIN_CONEXION;
}

export function erroresDeCampo(error: unknown): Record<string, string[]> {
  if (typeof error === "object" && error !== null && "errors" in error) {
    const { errors } = error as ProblemaApi;
    return errors ?? {};
  }
  return {};
}
