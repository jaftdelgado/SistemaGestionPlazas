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

export function esNoAutenticado(error: unknown): boolean {
  return (
    typeof error === "object" &&
    error !== null &&
    "status" in error &&
    Number(error.status) === 401
  );
}

export function erroresDeCampo(error: unknown): Record<string, string[]> {
  if (typeof error === "object" && error !== null && "errors" in error) {
    const { errors } = error as ProblemaApi;
    return errors ?? {};
  }
  return {};
}

// Un error que ya se muestra bajo su campo no se repite en el aviso general.
export function mensajeGeneral(error: unknown): string | undefined {
  return error && Object.keys(erroresDeCampo(error)).length === 0
    ? mensajeDeProblema(error)
    : undefined;
}
