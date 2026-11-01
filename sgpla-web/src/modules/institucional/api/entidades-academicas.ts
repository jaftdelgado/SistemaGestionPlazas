import { queryOptions } from "@tanstack/react-query";
import { client } from "@/shared/api/client";
import { unwrap } from "@/shared/api/errors";
import { toPagina } from "@/shared/api/pagination";
import { CATALOG_STALE_TIME, MAX_PAGE_SIZE } from "@/shared/api/query-client";
import type { components } from "@/shared/api/schema";
import type { EntidadAcademica } from "../domain/entidad-academica";

type EntidadAcademicaResponse =
  components["schemas"]["EntidadAcademicaResponse"];

export const entidadesAcademicasKeys = {
  all: ["institucional", "entidades-academicas"] as const,
  byArea: (areaAcademicaId: number | undefined) =>
    [...entidadesAcademicasKeys.all, { areaAcademicaId }] as const,
};

// Con un área elegida solo se traen sus entidades; sin ella, las primeras de la página máxima.
export function entidadesAcademicasQuery(areaAcademicaId: number | undefined) {
  return queryOptions({
    queryKey: entidadesAcademicasKeys.byArea(areaAcademicaId),
    queryFn: async () => {
      const page = await unwrap(
        client.GET("/api/v1/institucional/entidades-academicas", {
          params: { query: { areaAcademicaId, tamanoPagina: MAX_PAGE_SIZE } },
        }),
      );
      return toPagina(page, toEntidadAcademica);
    },
    staleTime: CATALOG_STALE_TIME,
  });
}

function toEntidadAcademica(dto: EntidadAcademicaResponse): EntidadAcademica {
  return {
    id: Number(dto.id),
    nombre: dto.nombre,
    areaAcademicaId: Number(dto.areaAcademica.id),
  };
}
