import { keepPreviousData } from "@tanstack/react-query";
import { $api } from "@/lib/api/query";

export type CuentasQuery = {
  pagina: number;
  tamanoPagina: number;
  rolId?: number;
  areaAcademicaId?: number;
  entidadAcademicaId?: number;
  busqueda?: string;
};

// Se conserva la página anterior mientras llega la siguiente, para que la tabla no parpadee al paginar o filtrar.
export function useCuentas(query: CuentasQuery) {
  return $api.useQuery(
    "get",
    "/api/v1/usuarios/cuentas",
    { params: { query: query } },
    { placeholderData: keepPreviousData },
  );
}

// Los catálogos cambian poco: se reutilizan sin volver a pedirlos en cada visita.
const CATALOG_STALE_TIME = 5 * 60_000;
const MAX_PAGE_SIZE = 100;

export function useAreasAcademicas() {
  return $api.useQuery(
    "get",
    "/api/v1/institucional/areas-academicas",
    undefined,
    { staleTime: CATALOG_STALE_TIME },
  );
}

// Con un área elegida solo se traen sus entidades; sin ella, las primeras de la página máxima.
export function useEntidadesAcademicas(areaAcademicaId: number | undefined) {
  return $api.useQuery(
    "get",
    "/api/v1/institucional/entidades-academicas",
    {
      params: {
        query: { areaAcademicaId, tamanoPagina: MAX_PAGE_SIZE },
      },
    },
    { staleTime: CATALOG_STALE_TIME },
  );
}
