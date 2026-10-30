import {
  Building01Icon,
  Delete02Icon,
  Edit02Icon,
  Mail01Icon,
  Shield01Icon,
  UserIcon,
  ViewIcon,
} from "@hugeicons/core-free-icons";
import {
  DataGrid,
  type DataGridColumn,
  type DataGridRowAction,
} from "@/components/data-grid/data-grid";
import { skeletonWidth } from "@/components/data-grid/loading-width";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { Skeleton } from "@/components/ui/skeleton";
import { UsuarioAvatar } from "@/components/ui/usuario-avatar";
import { getProblemMessage } from "@/lib/api/problem";
import type { UsuarioRow } from "../application/usuario-row";

type UsuarioColumn = "name" | "email" | "rol" | "ambito";

const COLUMNS: DataGridColumn<UsuarioColumn>[] = [
  {
    id: "name",
    label: "Usuario",
    icon: UserIcon,
    defaultWidth: 260,
  },
  {
    id: "email",
    label: "Correo",
    icon: Mail01Icon,
    defaultWidth: 280,
  },
  {
    id: "rol",
    label: "Rol",
    icon: Shield01Icon,
    defaultWidth: 200,
  },
  {
    id: "ambito",
    label: "Ámbito",
    icon: Building01Icon,
    defaultWidth: 320,
  },
];

const LOADING_SEED = "usuarios";

// Acciones provisionales: aún no hacen nada hasta que existan sus flujos.
const USUARIO_ACTIONS: DataGridRowAction[] = [
  { id: "detail", label: "Ver detalle", icon: ViewIcon },
  { id: "edit", label: "Editar", icon: Edit02Icon },
  {
    id: "deactivate",
    label: "Dar de baja",
    icon: Delete02Icon,
    destructive: true,
  },
];

function getUsuarioActions() {
  return USUARIO_ACTIONS;
}

function neverOpenDrawer() {
  return false;
}

function getCellValue(usuario: UsuarioRow, columnId: UsuarioColumn) {
  return usuario[columnId];
}

function renderUsuarioCell(
  usuario: UsuarioRow,
  column: DataGridColumn<UsuarioColumn>,
) {
  if (column.id === "ambito") {
    if (usuario.ambitoType === null) return "—";
    return <span className="truncate">{usuario.ambito}</span>;
  }
  if (column.id !== "name") return usuario[column.id];
  return (
    <span className="flex items-center gap-2">
      <UsuarioAvatar
        name={usuario.name}
        email={usuario.email}
        className="size-6"
        fallbackClassName="text-[10px]"
      />
      <span className="truncate">{usuario.name}</span>
    </span>
  );
}

function renderUsuarioLoadingCell(
  column: DataGridColumn<UsuarioColumn>,
  rowIndex: number,
) {
  if (column.id !== "name") return undefined;
  return (
    <span className="flex w-full items-center gap-2">
      <Skeleton className="size-6 shrink-0 rounded-full" />
      <Skeleton
        className="h-4 max-w-[calc(100%-2rem)]"
        style={{
          width: `${skeletonWidth(LOADING_SEED, column.id, rowIndex)}%`,
        }}
      />
    </span>
  );
}

type UsuariosDataGridProps = {
  usuarios: UsuarioRow[];
  loading: boolean;
  // Filas de esqueleto mientras carga.
  loadingRows: number;
  hasError: boolean;
  error: unknown;
  onRetry: () => void;
  hasCriteria: boolean;
  onClearCriteria: () => void;
};

export function UsuariosDataGrid({
  usuarios,
  loading,
  loadingRows,
  hasError,
  error,
  onRetry,
  hasCriteria,
  onClearCriteria,
}: UsuariosDataGridProps) {
  if (hasError) {
    return (
      <EmptyState
        label="Error al cargar usuarios"
        title="No se pudieron cargar los usuarios"
        description={getProblemMessage(error)}
        action={
          <Button variant="secondary" onClick={onRetry}>
            Reintentar
          </Button>
        }
      />
    );
  }

  return (
    <DataGrid
      rows={usuarios}
      columns={COLUMNS}
      getRowLabel={(usuario) => usuario.name}
      renderCell={renderUsuarioCell}
      getRowActions={getUsuarioActions}
      loading={loading}
      loadingSeed={LOADING_SEED}
      renderLoadingCell={renderUsuarioLoadingCell}
      loadingRowCount={loadingRows}
      getDrawerCellValue={getCellValue}
      canOpenDrawer={neverOpenDrawer}
      emptyState={
        <EmptyState
          label="Sin usuarios"
          title={hasCriteria ? "Sin resultados" : "Aún no hay usuarios"}
          description={
            hasCriteria
              ? "Ningún usuario coincide con la búsqueda o los filtros aplicados."
              : "Cuando registres usuarios, aparecerán aquí."
          }
          action={
            hasCriteria ? (
              <Button variant="secondary" onClick={onClearCriteria}>
                Limpiar búsqueda y filtros
              </Button>
            ) : undefined
          }
        />
      }
    />
  );
}
