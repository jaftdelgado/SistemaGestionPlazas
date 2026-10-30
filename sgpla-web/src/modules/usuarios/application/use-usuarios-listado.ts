import * as React from "react";
import type { FilterChip } from "@/components/ui/filter-toolbar";
import { useValorConRetardo } from "@/hooks/use-valor-con-retardo";
import { useCuentas } from "./usuarios-queries";
import { aFilaUsuario, type UsuarioFila } from "./usuario-fila";

const TAMANO_PAGINA_INICIAL = 10;
const RETARDO_BUSQUEDA_MS = 300;

export function valorDeFiltro(filtros: FilterChip[], id: string) {
  const valor = filtros.find((filtro) => filtro.id === id)?.value;
  return valor === undefined ? undefined : Number(valor);
}

// Estado del listado (página, filtros y búsqueda) y los usuarios que devuelve la consulta.
export function useUsuariosListado() {
  const [pagina, setPagina] = React.useState(1);
  const [tamanoPagina, setTamanoPagina] = React.useState(TAMANO_PAGINA_INICIAL);
  const [filtros, setFiltros] = React.useState<FilterChip[]>([]);
  const [textoBusqueda, setTextoBusqueda] = React.useState("");

  const busqueda = useValorConRetardo(
    textoBusqueda.trim(),
    RETARDO_BUSQUEDA_MS,
  );

  const { data, isPending, isError, error, refetch } = useCuentas({
    pagina,
    tamanoPagina,
    rolId: valorDeFiltro(filtros, "rol"),
    areaAcademicaId: valorDeFiltro(filtros, "area"),
    entidadAcademicaId: valorDeFiltro(filtros, "entidad"),
    busqueda: busqueda === "" ? undefined : busqueda,
  });

  const usuarios = React.useMemo<UsuarioFila[]>(
    () => (data?.elementos ?? []).map(aFilaUsuario),
    [data],
  );
  const total = Number(data?.total ?? 0);

  // Al cambiar los criterios, la página actual podría no existir; se vuelve a la primera.
  function cambiarBusqueda(texto: string) {
    setTextoBusqueda(texto);
    setPagina(1);
  }

  function cambiarFiltros(siguientes: FilterChip[]) {
    setFiltros(siguientes);
    setPagina(1);
  }

  function cambiarTamanoPagina(tamano: number) {
    setTamanoPagina(tamano);
    setPagina(1);
  }

  function limpiarCriterios() {
    setTextoBusqueda("");
    cambiarFiltros([]);
  }

  return {
    usuarios,
    total,
    totalPaginas: Math.ceil(total / tamanoPagina),
    pagina,
    setPagina,
    tamanoPagina,
    cambiarTamanoPagina,
    filtros,
    cambiarFiltros,
    textoBusqueda,
    cambiarBusqueda,
    limpiarCriterios,
    hayCriterios: filtros.length > 0 || busqueda !== "",
    isPending,
    isError,
    error,
    refetch,
  };
}
