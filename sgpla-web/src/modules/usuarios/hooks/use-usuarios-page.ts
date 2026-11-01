import { useQuery } from "@tanstack/react-query";
import * as React from "react";
import {
  abbreviateFacultad,
  areasAcademicasQuery,
  entidadesAcademicasQuery,
} from "@/modules/institucional";
import { useSesionUsuario } from "@/modules/sesion";
import { getProblemMessage } from "@/shared/api/errors";
import { useMinimumDuration } from "@/shared/hooks/use-minimum-duration";
import { cuentasQuery } from "../api/cuentas";
import { getAmbito, type Cuenta } from "../domain/cuenta";
import { ROLES } from "../domain/roles";
import { useDeactivateCuenta } from "./use-deactivate-cuenta";
import { useUsuariosCriteria } from "./use-usuarios-criteria";

const MIN_SKELETON_MS = 300;

const ROL_OPTIONS = ROLES.map((rol) => ({
  value: String(rol.id),
  label: rol.name,
}));

function toUsuarioRow(cuenta: Cuenta) {
  const ambito = getAmbito(cuenta);
  return {
    id: String(cuenta.id),
    name: cuenta.nombre,
    email: cuenta.correo,
    rol: cuenta.rol.nombre,
    ambito:
      ambito === null
        ? ""
        : ambito.type === "Entidad"
          ? abbreviateFacultad(ambito.nombre)
          : ambito.nombre,
    ambitoType: ambito?.type ?? null,
  };
}

// Conecta el listado de usuarios: criterios de la dirección, consultas, baja con deshacer y opciones de los filtros.
export function useUsuariosPage() {
  const criteria = useUsuariosCriteria();
  const cuentas = useQuery(cuentasQuery(criteria.query));
  const areas = useQuery(areasAcademicasQuery);
  const entidades = useQuery(
    entidadesAcademicasQuery(criteria.filters.areaAcademicaId),
  );
  const sesionUsuario = useSesionUsuario();
  const { hiddenIds, deactivate } = useDeactivateCuenta();
  const showSkeleton = useMinimumDuration(cuentas.isPending, MIN_SKELETON_MS);

  const allCuentas = React.useMemo(
    () => cuentas.data?.elementos ?? [],
    [cuentas.data],
  );
  const rows = React.useMemo(
    () =>
      allCuentas
        .filter((cuenta) => !hiddenIds.has(cuenta.id))
        .map(toUsuarioRow),
    [allCuentas, hiddenIds],
  );
  const serverTotal = cuentas.data?.total ?? 0;
  // El total descuenta los ocultos de esta página, que el servidor aún cuenta.
  const total = Math.max(0, serverTotal - (allCuentas.length - rows.length));

  // Si tras una baja la página ya no existe (era el último usuario de la última página), se retrocede a la última.
  const lastPage = Math.ceil(serverTotal / criteria.pageSize);
  const outOfRange =
    cuentas.data !== undefined &&
    !cuentas.isPlaceholderData &&
    allCuentas.length === 0 &&
    serverTotal > 0 &&
    criteria.page > lastPage;
  const { changePage } = criteria;
  React.useEffect(() => {
    if (outOfRange) changePage(lastPage, { replace: true });
  }, [outOfRange, lastPage, changePage]);

  const areaOptions = React.useMemo(
    () =>
      (areas.data ?? []).map((area) => ({
        value: String(area.id),
        label: area.nombre,
      })),
    [areas.data],
  );
  // Con un área elegida, el filtro de entidad solo ofrece las de esa área.
  const entidadOptions = React.useMemo(
    () =>
      (entidades.data?.elementos ?? []).map((entidad) => ({
        value: String(entidad.id),
        label: abbreviateFacultad(entidad.nombre),
      })),
    [entidades.data],
  );

  return {
    filters: {
      searchText: criteria.searchText,
      onSearchChange: criteria.changeSearch,
      values: criteria.filters,
      rolOptions: ROL_OPTIONS,
      areaOptions,
      entidadOptions,
      onFiltersChange: criteria.changeFilters,
    },
    grid: {
      rows,
      ownId: String(sesionUsuario.id),
      loading: showSkeleton,
      loadingRows: criteria.pageSize,
      errorMessage: cuentas.isError
        ? getProblemMessage(cuentas.error)
        : undefined,
      onRetry: () => void cuentas.refetch(),
      hasCriteria: criteria.hasCriteria,
      onClearCriteria: criteria.clearCriteria,
      onDeactivate: (row: { id: string; name: string }) =>
        deactivate({ id: Number(row.id), nombre: row.name }),
    },
    pagination: {
      page: criteria.page,
      totalPages: Math.ceil(total / criteria.pageSize),
      pageSize: criteria.pageSize,
      total,
      onPageChange: changePage,
      onPageSizeChange: criteria.changePageSize,
    },
  };
}
