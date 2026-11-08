import { keepPreviousData, queryOptions } from "@tanstack/react-query";
import { client } from "@/shared/api/client";
import { unwrap, unwrapEmpty } from "@/shared/api/errors";
import { toPagina } from "@/shared/api/pagination";
import type { components } from "@/shared/api/schema";
import type { Cuenta, CuentasCriteria } from "../domain/cuenta";

type CuentaResponse = components["schemas"]["CuentaResponse"];

export const cuentasKeys = {
  all: ["usuarios", "cuentas"] as const,
  list: (criteria: CuentasCriteria) =>
    [...cuentasKeys.all, "list", criteria] as const,
};

// Se conserva la página anterior mientras llega la siguiente, para que la tabla no parpadee al paginar o filtrar.
export function cuentasQuery(criteria: CuentasCriteria) {
  return queryOptions({
    queryKey: cuentasKeys.list(criteria),
    queryFn: async () => {
      const page = await unwrap(
        client.GET("/api/v1/usuarios/cuentas", {
          params: { query: criteria },
        }),
      );
      return toPagina(page, toCuenta);
    },
    placeholderData: keepPreviousData,
  });
}

export function deleteCuenta(id: number): Promise<void> {
  return unwrapEmpty(
    client.DELETE("/api/v1/usuarios/cuentas/{id}", {
      params: { path: { id } },
    }),
  );
}

function toCuenta(dto: CuentaResponse): Cuenta {
  return {
    id: Number(dto.id),
    nombre: dto.nombre,
    correo: dto.correo,
    rol: { id: Number(dto.rol.id), nombre: dto.rol.nombre },
    areaAcademica: dto.areaAcademica
      ? { id: Number(dto.areaAcademica.id), nombre: dto.areaAcademica.nombre }
      : null,
    entidadAcademica: dto.entidadAcademica
      ? {
          id: Number(dto.entidadAcademica.id),
          nombre: dto.entidadAcademica.nombre,
        }
      : null,
  };
}
