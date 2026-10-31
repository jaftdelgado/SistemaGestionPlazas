import * as React from "react";
import type { FilterChip } from "@/components/ui/filter-toolbar";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { useMinimumDuration } from "@/hooks/use-minimum-duration";
import { useDeactivateUsuario } from "./use-deactivate-usuario";
import { useCuentas } from "./usuarios-queries";
import { toUsuarioRow, type UsuarioRow } from "./usuario-row";

const INITIAL_PAGE_SIZE = 10;
const SEARCH_DEBOUNCE_MS = 300;
const MIN_SKELETON_MS = 300;

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

  const { data, isPending, isPlaceholderData, isError, error, refetch } =
    useCuentas({
      pagina: page,
      tamanoPagina: pageSize,
      rolId: getFilterValue(filters, "rol"),
      areaAcademicaId: getFilterValue(filters, "area"),
      entidadAcademicaId: getFilterValue(filters, "entidad"),
      busqueda: search === "" ? undefined : search,
    });

  const showSkeleton = useMinimumDuration(isPending, MIN_SKELETON_MS);

  const { hiddenIds, deactivate } = useDeactivateUsuario();

  const allUsuarios = React.useMemo<UsuarioRow[]>(
    () => (data?.elementos ?? []).map(toUsuarioRow),
    [data],
  );
  const usuarios = React.useMemo(
    () => allUsuarios.filter((usuario) => !hiddenIds.has(usuario.id)),
    [allUsuarios, hiddenIds],
  );
  // El total descuenta los ocultos de esta página, que el servidor aún cuenta.
  const total = Math.max(
    0,
    Number(data?.total ?? 0) - (allUsuarios.length - usuarios.length),
  );

  // Si tras una baja la página ya no existe (era el último usuario de la última página), se retrocede a la última.
  const serverTotal = Number(data?.total ?? 0);
  const lastPage = Math.ceil(serverTotal / pageSize);
  if (
    data !== undefined &&
    !isPlaceholderData &&
    allUsuarios.length === 0 &&
    serverTotal > 0 &&
    page > lastPage
  ) {
    setPage(lastPage);
  }

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
    deactivate,
    hasCriteria: filters.length > 0 || search !== "",
    isPending: showSkeleton,
    isError,
    error,
    refetch,
  };
}
