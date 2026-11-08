import type { components } from "@/shared/api/schema";

export type ApiProblem =
  components["schemas"]["HttpValidationProblemDetails"] & {
    codigo?: string;
  };

// Respuesta de la API fuera del rango 2xx. Una falla de red no llega aquí: fetch la lanza como TypeError.
export class ApiError extends Error {
  readonly status: number;
  readonly problem: ApiProblem | undefined;

  constructor(status: number, body: unknown) {
    super(`La API respondió con el estado ${status}`);
    this.name = "ApiError";
    this.status = status;
    this.problem =
      typeof body === "object" && body !== null
        ? (body as ApiProblem)
        : undefined;
  }
}

type ApiResult = { data?: unknown; error?: unknown; response: Response };

// El resultado de openapi-fetch es una unión: data existe solo en la rama de éxito, que es la que se devuelve.
type SuccessData<Result extends ApiResult> = NonNullable<Result["data"]>;

// Una respuesta sin cuerpo puede traer error undefined: el estado HTTP es lo que decide si falló.
export async function unwrap<Result extends ApiResult>(
  pending: Promise<Result>,
): Promise<SuccessData<Result>> {
  const { data, error, response } = await pending;
  if (!response.ok) {
    throw new ApiError(response.status, error);
  }
  return data as SuccessData<Result>;
}

export async function unwrapEmpty(
  pending: Promise<{ error?: unknown; response: Response }>,
): Promise<void> {
  const { error, response } = await pending;
  if (!response.ok) {
    throw new ApiError(response.status, error);
  }
}

const NO_CONNECTION_MESSAGE =
  "No se pudo conectar con el servidor. Intenta de nuevo.";

export function hasStatus(error: unknown, status: number): boolean {
  return error instanceof ApiError && error.status === status;
}

export function getProblemMessage(error: unknown): string {
  const detail = error instanceof ApiError ? error.problem?.detail : undefined;
  return detail ? detail : NO_CONNECTION_MESSAGE;
}

export function getFieldErrors(error: unknown): Record<string, string[]> {
  return error instanceof ApiError ? (error.problem?.errors ?? {}) : {};
}

// Un error que ya se muestra bajo su campo no se repite en el aviso general.
export function getGeneralMessage(error: unknown): string | undefined {
  return error && Object.keys(getFieldErrors(error)).length === 0
    ? getProblemMessage(error)
    : undefined;
}
