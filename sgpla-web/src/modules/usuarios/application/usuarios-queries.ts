import { keepPreviousData } from "@tanstack/react-query";
import { $api } from "@/lib/api/query";

export type ConsultaCuentas = {
  pagina: number;
  tamanoPagina: number;
  rolId?: number;
  busqueda?: string;
};

// Se conserva la página anterior mientras llega la siguiente, para que la tabla no parpadee al paginar o filtrar.
export function useCuentas({
  pagina,
  tamanoPagina,
  rolId,
  busqueda,
}: ConsultaCuentas) {
  return $api.useQuery(
    "get",
    "/api/v1/usuarios/cuentas",
    { params: { query: { pagina, tamanoPagina, rolId, busqueda } } },
    { placeholderData: keepPreviousData },
  );
}
