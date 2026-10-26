import { keepPreviousData } from "@tanstack/react-query";
import { $api } from "@/lib/api/query";

export type ConsultaCuentas = {
  pagina: number;
  tamanoPagina: number;
  rolId?: number;
  areaAcademicaId?: number;
  entidadAcademicaId?: number;
  busqueda?: string;
};

// Se conserva la página anterior mientras llega la siguiente, para que la tabla no parpadee al paginar o filtrar.
export function useCuentas(consulta: ConsultaCuentas) {
  return $api.useQuery(
    "get",
    "/api/v1/usuarios/cuentas",
    { params: { query: consulta } },
    { placeholderData: keepPreviousData },
  );
}

// Los catálogos cambian poco: se reutilizan sin volver a pedirlos en cada visita.
const CATALOGO_STALE_TIME = 5 * 60_000;
const TAMANO_MAXIMO_PAGINA = 100;

export function useAreasAcademicas() {
  return $api.useQuery(
    "get",
    "/api/v1/institucional/areas-academicas",
    undefined,
    { staleTime: CATALOGO_STALE_TIME },
  );
}

// Con un área elegida solo se traen sus entidades; sin ella, las primeras de la página máxima.
export function useEntidadesAcademicas(areaAcademicaId: number | undefined) {
  return $api.useQuery(
    "get",
    "/api/v1/institucional/entidades-academicas",
    {
      params: {
        query: { areaAcademicaId, tamanoPagina: TAMANO_MAXIMO_PAGINA },
      },
    },
    { staleTime: CATALOGO_STALE_TIME },
  );
}
