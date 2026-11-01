import { QueryClient } from "@tanstack/react-query";

// Los catálogos cambian poco: se reutilizan sin volver a pedirlos en cada visita.
export const CATALOG_STALE_TIME = 5 * 60_000;

// Tamaño máximo de página que acepta la API.
export const MAX_PAGE_SIZE = 100;

export function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: 1,
        staleTime: 30_000,
      },
    },
  });
}
