import { useCallback, useState } from "react";
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
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { Skeleton } from "@/components/ui/skeleton";
import { toast } from "@/components/ui/toast-stack";
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

// Maqueta: la baja y su reversión están simuladas hasta que existan los endpoints.
function simulateRequest() {
  return new Promise<void>((resolve) => {
    setTimeout(resolve, 1500);
  });
}

function deactivateUsuario(usuario: UsuarioRow) {
  toast.undoable({
    title: "Se dará de baja al usuario",
    description: usuario.name,
    onCommit: () => {
      simulateRequest().then(
        () => toast.success("Usuario dado de baja", usuario.name),
        () => toast.error("No se pudo dar de baja", usuario.name),
      );
    },
  });
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
  // El usuario se conserva al cerrar para que el texto no cambie durante la animación de salida.
  const [usuarioToDeactivate, setUsuarioToDeactivate] =
    useState<UsuarioRow | null>(null);
  const [deactivateOpen, setDeactivateOpen] = useState(false);

  // Acciones provisionales: ver detalle y editar aún no hacen nada hasta que existan sus flujos.
  const getUsuarioActions = useCallback(
    (usuario: UsuarioRow): DataGridRowAction[] => [
      { id: "detail", label: "Ver detalle", icon: ViewIcon },
      { id: "edit", label: "Editar", icon: Edit02Icon },
      {
        id: "deactivate",
        label: "Dar de baja",
        icon: Delete02Icon,
        destructive: true,
        onSelect: () => {
          setUsuarioToDeactivate(usuario);
          setDeactivateOpen(true);
        },
      },
    ],
    [],
  );

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
    <>
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
      <AlertDialog open={deactivateOpen} onOpenChange={setDeactivateOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>¿Deseas dar de baja al usuario?</AlertDialogTitle>
            <AlertDialogDescription>
              Al dar de baja a {usuarioToDeactivate?.name}, perderá el acceso al
              sistema.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancelar</AlertDialogCancel>
            <AlertDialogAction
              variant="destructive"
              onClick={() => {
                setDeactivateOpen(false);
                if (usuarioToDeactivate) {
                  deactivateUsuario(usuarioToDeactivate);
                }
              }}
            >
              Dar de baja
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}
