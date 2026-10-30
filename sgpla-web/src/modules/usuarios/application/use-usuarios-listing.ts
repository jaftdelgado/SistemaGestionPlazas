import * as React from "react";
import type { FilterChip } from "@/components/ui/filter-toolbar";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { useCuentas } from "./usuarios-queries";
import { toUsuarioRow, type UsuarioRow } from "./usuario-row";

const INITIAL_PAGE_SIZE = 10;
const SEARCH_DEBOUNCE_MS = 300;

export function getFilterValue(filters: FilterChip[], id: string) {
  const value = filters.find((filter) => filter.id === id)?.value;
  return value === undefined ? undefined : Number(value);
}

// Estado del listado (página, filtros y búsqueda) y los usuarios que devuelve la consulta.
export function useUsuariosListing() {
  const [page, setPage] = React.useState(1);
  const [pageSize, setPageSize] = React.useState(INITIAL_PAGE_SIZE);
  const [filters, setFilters] = React.useState<FilterChip[]>([]);
  const [searchText, setSearchText] = React.useState("");

  const search = useDebouncedValue(searchText.trim(), SEARCH_DEBOUNCE_MS);

  const { data, isPending, isError, error, refetch } = useCuentas({
    pagina: page,
    tamanoPagina: pageSize,
    rolId: getFilterValue(filters, "rol"),
    areaAcademicaId: getFilterValue(filters, "area"),
    entidadAcademicaId: getFilterValue(filters, "entidad"),
    busqueda: search === "" ? undefined : search,
  });

  const usuarios = React.useMemo<UsuarioRow[]>(
    () => (data?.elementos ?? []).map(toUsuarioRow),
    [data],
  );
  const total = Number(data?.total ?? 0);

  // Al cambiar los criterios, la página actual podría no existir; se vuelve a la primera.
  function changeSearch(text: string) {
    setSearchText(text);
    setPage(1);
  }

  function changeFilters(next: FilterChip[]) {
    setFilters(next);
    setPage(1);
  }

  function changePageSize(size: number) {
    setPageSize(size);
    setPage(1);
  }

  function clearCriteria() {
    setSearchText("");
    changeFilters([]);
  }

  return {
    usuarios,
    total,
    totalPages: Math.ceil(total / pageSize),
    page,
    setPage,
    pageSize,
    changePageSize,
    filters,
    changeFilters,
    searchText,
    changeSearch,
    clearCriteria,
    hasCriteria: filters.length > 0 || search !== "",
    isPending,
    isError,
    error,
    refetch,
  };
}
