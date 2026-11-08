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
} from "@/shared/ui/data-grid/data-grid";
import { skeletonWidth } from "@/shared/ui/data-grid/loading-width";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/shared/ui/alert-dialog";
import { Button } from "@/shared/ui/button";
import { EmptyState } from "@/shared/ui/empty-state";
import { Skeleton } from "@/shared/ui/skeleton";
import { UsuarioAvatar } from "@/shared/ui/usuario-avatar";

type UsuarioRow = {
  id: string;
  name: string;
  email: string;
  rol: string;
  // Entidad o área de la cuenta; el Superusuario no tiene ámbito.
  ambito: string;
  ambitoType: "Entidad" | "Área" | null;
};

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

function neverOpenDrawer() {
  return false;
}

function getCellValue(usuario: UsuarioRow, columnId: UsuarioColumn) {
  return usuario[columnId];
}

function renderUsuarioCell(
  usuario: UsuarioRow,
  column: DataGridColumn<UsuarioColumn>,
  ownId: string,
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
      {usuario.id === ownId && (
        <span className="shrink-0 text-muted-foreground">(Tú)</span>
      )}
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
  rows: UsuarioRow[];
  // Id del usuario de la sesión, que no puede darse de baja a sí mismo.
  ownId: string;
  loading: boolean;
  // Filas de esqueleto mientras carga.
  loadingRows: number;
  // Presente solo si la carga falló.
  errorMessage: string | undefined;
  onRetry: () => void;
  hasCriteria: boolean;
  onClearCriteria: () => void;
  onDeactivate: (usuario: UsuarioRow) => void;
};

export function UsuariosDataGrid({
  rows,
  ownId,
  loading,
  loadingRows,
  errorMessage,
  onRetry,
  hasCriteria,
  onClearCriteria,
  onDeactivate,
}: UsuariosDataGridProps) {
  // El usuario se conserva al cerrar para que el texto no cambie durante la animación de salida.
  const [usuarioToDeactivate, setUsuarioToDeactivate] =
    useState<UsuarioRow | null>(null);
  const [deactivateOpen, setDeactivateOpen] = useState(false);

  const renderCell = useCallback(
    (usuario: UsuarioRow, column: DataGridColumn<UsuarioColumn>) =>
      renderUsuarioCell(usuario, column, ownId),
    [ownId],
  );

  // Acciones provisionales: ver detalle y editar aún no hacen nada hasta que existan sus flujos.
  const getUsuarioActions = useCallback(
    (usuario: UsuarioRow): DataGridRowAction[] => {
      const actions: DataGridRowAction[] = [
        { id: "detail", label: "Ver detalle", icon: ViewIcon },
        { id: "edit", label: "Editar", icon: Edit02Icon },
      ];
      // Nadie puede darse de baja a sí mismo: la API respondería 409.
      if (usuario.id !== ownId) {
        actions.push({
          id: "deactivate",
          label: "Dar de baja",
          icon: Delete02Icon,
          destructive: true,
          onSelect: () => {
            setUsuarioToDeactivate(usuario);
            setDeactivateOpen(true);
          },
        });
      }
      return actions;
    },
    [ownId],
  );

  if (errorMessage !== undefined) {
    return (
      <EmptyState
        label="Error al cargar usuarios"
        title="No se pudieron cargar los usuarios"
        description={errorMessage}
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
        rows={rows}
        columns={COLUMNS}
        getRowLabel={(usuario) => usuario.name}
        renderCell={renderCell}
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
                  onDeactivate(usuarioToDeactivate);
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
