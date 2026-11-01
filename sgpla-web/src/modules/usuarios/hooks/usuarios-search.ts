import { isPageSize } from "@/shared/lib/pagination";

// Criterios del listado que viven en la dirección; uno ausente toma su valor por omisión.
export type UsuariosSearch = {
  pagina?: number;
  tamanoPagina?: number;
  busqueda?: string;
  rolId?: number;
  areaAcademicaId?: number;
  entidadAcademicaId?: number;
};

function toPositiveInteger(value: unknown): number | undefined {
  const number =
    typeof value === "number"
      ? value
      : typeof value === "string"
        ? Number(value)
        : Number.NaN;
  return Number.isInteger(number) && number > 0 ? number : undefined;
}

// El router convierte en número una búsqueda que solo tiene dígitos; se devuelve a texto.
function toSearchText(value: unknown): string | undefined {
  const text =
    typeof value === "string"
      ? value.trim()
      : typeof value === "number"
        ? String(value)
        : "";
  return text === "" ? undefined : text;
}

// Un valor inválido en la dirección se descarta en lugar de fallar: la página usa el valor por omisión.
export function validateUsuariosSearch(
  search: Record<string, unknown>,
): UsuariosSearch {
  const tamanoPagina = toPositiveInteger(search.tamanoPagina);
  return {
    pagina: toPositiveInteger(search.pagina),
    tamanoPagina:
      tamanoPagina !== undefined && isPageSize(tamanoPagina)
        ? tamanoPagina
        : undefined,
    busqueda: toSearchText(search.busqueda),
    rolId: toPositiveInteger(search.rolId),
    areaAcademicaId: toPositiveInteger(search.areaAcademicaId),
    entidadAcademicaId: toPositiveInteger(search.entidadAcademicaId),
  };
}
