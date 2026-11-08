// Tamaños de página que ofrece la interfaz y que acepta la búsqueda de una ruta.
export const PAGE_SIZE_OPTIONS = [5, 10, 25, 50] as const;

export const DEFAULT_PAGE_SIZE = 10;

export function isPageSize(value: number): boolean {
  return PAGE_SIZE_OPTIONS.some((option) => option === value);
}
