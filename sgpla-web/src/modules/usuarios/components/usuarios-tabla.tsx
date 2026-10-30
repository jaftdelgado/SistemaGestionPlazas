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
import { anchoDeEsqueleto } from "@/components/data-grid/loading-width";
import { AvatarUsuario } from "@/components/ui/avatar-usuario";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { Skeleton } from "@/components/ui/skeleton";
import { mensajeDeProblema } from "@/lib/api/problema";
import type { UsuarioFila } from "../application/usuario-fila";

type ColumnaUsuario = "nombre" | "correo" | "rol" | "ambito";

const COLUMNAS: DataGridColumn<ColumnaUsuario>[] = [
  {
    id: "nombre",
    label: "Usuario",
    icon: UserIcon,
    defaultWidth: 260,
  },
  {
    id: "correo",
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

const SEMILLA_CARGA = "usuarios";

// Acciones provisionales: aún no hacen nada hasta que existan sus flujos.
const ACCIONES_USUARIO: DataGridRowAction[] = [
  { id: "detalle", label: "Ver detalle", icon: ViewIcon },
  { id: "editar", label: "Editar", icon: Edit02Icon },
  { id: "baja", label: "Dar de baja", icon: Delete02Icon, destructive: true },
];

function accionesDeUsuario() {
  return ACCIONES_USUARIO;
}

function noAbreDetalle() {
  return false;
}

function valorDeCelda(usuario: UsuarioFila, columnId: ColumnaUsuario) {
  return usuario[columnId];
}

function renderizarCelda(
  usuario: UsuarioFila,
  columna: DataGridColumn<ColumnaUsuario>,
) {
  if (columna.id === "ambito") {
    if (usuario.tipoAmbito === null) return "—";
    return <span className="truncate">{usuario.ambito}</span>;
  }
  if (columna.id !== "nombre") return usuario[columna.id];
  return (
    <span className="flex items-center gap-2">
      <AvatarUsuario
        nombre={usuario.nombre}
        correo={usuario.correo}
        className="size-6"
        fallbackClassName="text-[10px]"
      />
      <span className="truncate">{usuario.nombre}</span>
    </span>
  );
}

function renderizarCargaCelda(
  columna: DataGridColumn<ColumnaUsuario>,
  fila: number,
) {
  if (columna.id !== "nombre") return undefined;
  return (
    <span className="flex w-full items-center gap-2">
      <Skeleton className="size-6 shrink-0 rounded-full" />
      <Skeleton
        className="h-4 max-w-[calc(100%-2rem)]"
        style={{
          width: `${anchoDeEsqueleto(SEMILLA_CARGA, columna.id, fila)}%`,
        }}
      />
    </span>
  );
}

type UsuariosTablaProps = {
  usuarios: UsuarioFila[];
  cargando: boolean;
  // Filas de esqueleto mientras carga.
  filasCarga: number;
  hayError: boolean;
  error: unknown;
  onReintentar: () => void;
  hayCriterios: boolean;
  onLimpiarCriterios: () => void;
};

export function UsuariosTabla({
  usuarios,
  cargando,
  filasCarga,
  hayError,
  error,
  onReintentar,
  hayCriterios,
  onLimpiarCriterios,
}: UsuariosTablaProps) {
  if (hayError) {
    return (
      <EmptyState
        label="Error al cargar usuarios"
        title="No se pudieron cargar los usuarios"
        description={mensajeDeProblema(error)}
        action={
          <Button variant="secondary" onClick={onReintentar}>
            Reintentar
          </Button>
        }
      />
    );
  }

  return (
    <DataGrid
      rows={usuarios}
      columns={COLUMNAS}
      getRowLabel={(usuario) => usuario.nombre}
      renderCell={renderizarCelda}
      getRowActions={accionesDeUsuario}
      loading={cargando}
      loadingSeed={SEMILLA_CARGA}
      renderLoadingCell={renderizarCargaCelda}
      loadingRowCount={filasCarga}
      getDrawerCellValue={valorDeCelda}
      canOpenDrawer={noAbreDetalle}
      emptyState={
        <EmptyState
          label="Sin usuarios"
          title={hayCriterios ? "Sin resultados" : "Aún no hay usuarios"}
          description={
            hayCriterios
              ? "Ningún usuario coincide con la búsqueda o los filtros aplicados."
              : "Cuando registres usuarios, aparecerán aquí."
          }
          action={
            hayCriterios ? (
              <Button variant="secondary" onClick={onLimpiarCriterios}>
                Limpiar búsqueda y filtros
              </Button>
            ) : undefined
          }
        />
      }
    />
  );
}
