import { queryOptions } from "@tanstack/react-query";
import { client } from "@/shared/api/client";
import { unwrap } from "@/shared/api/errors";
import { CATALOG_STALE_TIME } from "@/shared/api/query-client";
import type { components } from "@/shared/api/schema";
import type { AreaAcademica } from "../domain/area-academica";

type AreaAcademicaResponse = components["schemas"]["AreaAcademicaResponse"];

export const areasAcademicasKeys = {
  all: ["institucional", "areas-academicas"] as const,
};

export const areasAcademicasQuery = queryOptions({
  queryKey: areasAcademicasKeys.all,
  queryFn: async () => {
    const areas = await unwrap(
      client.GET("/api/v1/institucional/areas-academicas"),
    );
    return areas.map(toAreaAcademica);
  },
  staleTime: CATALOG_STALE_TIME,
});

function toAreaAcademica(dto: AreaAcademicaResponse): AreaAcademica {
  return { id: Number(dto.id), nombre: dto.nombre };
}
