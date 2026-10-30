import type { components } from "@/lib/api/schema";

export type ApiProblem =
  components["schemas"]["HttpValidationProblemDetails"] & {
    codigo?: string;
  };

const NO_CONNECTION_MESSAGE =
  "No se pudo conectar con el servidor. Intenta de nuevo.";

export function getProblemMessage(error: unknown): string {
  if (
    typeof error === "object" &&
    error !== null &&
    "detail" in error &&
    typeof error.detail === "string" &&
    error.detail !== ""
  ) {
    return error.detail;
  }
  return NO_CONNECTION_MESSAGE;
}

export function isUnauthenticated(error: unknown): boolean {
  return (
    typeof error === "object" &&
    error !== null &&
    "status" in error &&
    Number(error.status) === 401
  );
}

export function getFieldErrors(error: unknown): Record<string, string[]> {
  if (typeof error === "object" && error !== null && "errors" in error) {
    const { errors } = error as ApiProblem;
    return errors ?? {};
  }
  return {};
}

// Un error que ya se muestra bajo su campo no se repite en el aviso general.
export function getGeneralMessage(error: unknown): string | undefined {
  return error && Object.keys(getFieldErrors(error)).length === 0
    ? getProblemMessage(error)
    : undefined;
}
