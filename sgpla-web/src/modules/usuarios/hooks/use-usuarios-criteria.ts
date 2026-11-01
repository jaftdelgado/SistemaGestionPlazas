import { useNavigate, useSearch } from "@tanstack/react-router";
import * as React from "react";
import { useDebouncedValue } from "@/shared/hooks/use-debounced-value";
import { DEFAULT_PAGE_SIZE } from "@/shared/lib/pagination";
import {
  applyFilters,
  type CuentasCriteria,
  type CuentasFilters,
} from "../domain/cuenta";

const SEARCH_DEBOUNCE_MS = 300;

// Los criterios del listado viven en la dirección, así sobreviven a recargar y se pueden compartir. El texto de búsqueda
// se edita en local y llega a la dirección con retraso, para no navegar ni consultar en cada tecla.
export function useUsuariosCriteria() {
  const search = useSearch({ from: "/_panel/usuarios" });
  const navigate = useNavigate({ from: "/usuarios" });

  const page = search.pagina ?? 1;
  const pageSize = search.tamanoPagina ?? DEFAULT_PAGE_SIZE;
  const filters: CuentasFilters = {
    rolId: search.rolId,
    areaAcademicaId: search.areaAcademicaId,
    entidadAcademicaId: search.entidadAcademicaId,
  };

  const [searchText, setSearchText] = React.useState(search.busqueda ?? "");
  const [syncedBusqueda, setSyncedBusqueda] = React.useState(search.busqueda);
  // Si la dirección cambia por fuera (un enlace o Atrás), el campo la sigue; si es el eco de lo escrito, se deja igual.
  if (search.busqueda !== syncedBusqueda) {
    setSyncedBusqueda(search.busqueda);
    if ((search.busqueda ?? "") !== searchText.trim()) {
      setSearchText(search.busqueda ?? "");
    }
  }

  const debouncedText = useDebouncedValue(
    searchText.trim(),
    SEARCH_DEBOUNCE_MS,
  );
  const settled = debouncedText === searchText.trim();
  React.useEffect(() => {
    const busqueda = debouncedText === "" ? undefined : debouncedText;
    // Mientras el retraso no termina, el valor anterior no debe pisar uno que llegó por la dirección.
    if (!settled || busqueda === search.busqueda) return;
    void navigate({
      search: (prev) => ({ ...prev, busqueda, pagina: undefined }),
      replace: true,
    });
  }, [debouncedText, settled, search.busqueda, navigate]);

  // Al cambiar los criterios, la página actual podría no existir; se vuelve a la primera.
  function changeFilters(next: CuentasFilters) {
    const applied = applyFilters(filters, next);
    void navigate({
      search: (prev) => ({
        ...prev,
        rolId: applied.rolId,
        areaAcademicaId: applied.areaAcademicaId,
        entidadAcademicaId: applied.entidadAcademicaId,
        pagina: undefined,
      }),
    });
  }

  const changePage = React.useCallback(
    (pagina: number, options?: { replace?: boolean }) => {
      void navigate({
        search: (prev) => ({
          ...prev,
          pagina: pagina > 1 ? pagina : undefined,
        }),
        replace: options?.replace,
      });
    },
    [navigate],
  );

  function changePageSize(size: number) {
    void navigate({
      search: (prev) => ({
        ...prev,
        tamanoPagina: size === DEFAULT_PAGE_SIZE ? undefined : size,
        pagina: undefined,
      }),
    });
  }

  function clearCriteria() {
    setSearchText("");
    void navigate({
      search: (prev) => ({
        ...prev,
        busqueda: undefined,
        rolId: undefined,
        areaAcademicaId: undefined,
        entidadAcademicaId: undefined,
        pagina: undefined,
      }),
    });
  }

  const query: CuentasCriteria = {
    ...filters,
    pagina: page,
    tamanoPagina: pageSize,
    busqueda: search.busqueda,
  };

  return {
    query,
    page,
    pageSize,
    filters,
    searchText,
    hasCriteria:
      search.busqueda !== undefined ||
      Object.values(filters).some((value) => value !== undefined),
    changeSearch: setSearchText,
    changeFilters,
    changePage,
    changePageSize,
    clearCriteria,
  };
}
