import {
  MailRegular,
  PersonRegular,
  ShieldRegular,
} from "@fluentui/react-icons";
import * as React from "react";
import {
  DataGrid,
  type DataGridColumn,
} from "@/components/data-grid/data-grid";
import {
  PageHeader,
  PageHeaderActions,
  PageHeaderTitle,
} from "@/components/page-header/page-header";
import { AvatarUsuario } from "@/components/ui/avatar-usuario";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import {
  FilterToolbar,
  type FilterChip,
  type FilterField,
} from "@/components/ui/filter-toolbar";
import { Pagination } from "@/components/ui/pagination";
import { PaginationSummary } from "@/components/ui/pagination-summary";
import { SearchInput } from "@/components/ui/search-input";
import { useValorConRetardo } from "@/hooks/use-valor-con-retardo";
import { mensajeDeProblema } from "@/lib/api/problema";
import { useCuentas } from "../application/usuarios-queries";
import { ROLES } from "../domain/roles";

type UsuarioFila = {
  id: string;
  nombre: string;
  correo: string;
  rol: string;
};

type ColumnaUsuario = "nombre" | "correo" | "rol";

const COLUMNAS: DataGridColumn<ColumnaUsuario>[] = [
  {
    id: "nombre",
    label: "Nombre",
    icon: PersonRegular,
    defaultWidth: 260,
  },
  {
    id: "correo",
    label: "Correo",
    icon: MailRegular,
    defaultWidth: 280,
  },
  {
    id: "rol",
    label: "Rol",
    icon: ShieldRegular,
    defaultWidth: 200,
  },
];

const CAMPOS_FILTRO: FilterField[] = [
  {
    id: "rol",
    label: "Rol",
    options: ROLES.map((rol) => ({
      value: String(rol.id),
      label: rol.nombre,
    })),
  },
];

const TAMANO_PAGINA_INICIAL = 10;
const RETARDO_BUSQUEDA_MS = 300;

function noAbreDetalle() {
  return false;
}

function renderizarCelda(
  usuario: UsuarioFila,
  columna: DataGridColumn<ColumnaUsuario>,
) {
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

function valorDeCelda(usuario: UsuarioFila, columnId: ColumnaUsuario) {
  return usuario[columnId];
}

export function UsuariosPage() {
  const [seleccionados, setSeleccionados] = React.useState<string[]>([]);
  const [pagina, setPagina] = React.useState(1);
  const [tamanoPagina, setTamanoPagina] = React.useState(TAMANO_PAGINA_INICIAL);
  const [filtros, setFiltros] = React.useState<FilterChip[]>([]);
  const [textoBusqueda, setTextoBusqueda] = React.useState("");

  const busqueda = useValorConRetardo(
    textoBusqueda.trim(),
    RETARDO_BUSQUEDA_MS,
  );
  const rolFiltro = filtros.find((filtro) => filtro.id === "rol")?.value;

  const { data, isPending, isError, error, refetch } = useCuentas({
    pagina,
    tamanoPagina,
    rolId: rolFiltro === undefined ? undefined : Number(rolFiltro),
    busqueda: busqueda === "" ? undefined : busqueda,
  });

  const usuarios = React.useMemo<UsuarioFila[]>(
    () =>
      (data?.elementos ?? []).map((cuenta) => ({
        id: String(cuenta.id),
        nombre: cuenta.nombre,
        correo: cuenta.correo,
        rol: cuenta.rol.nombre,
      })),
    [data],
  );
  const total = Number(data?.total ?? 0);
  const totalPaginas = Math.ceil(total / tamanoPagina);
  const hayCriterios = filtros.length > 0 || busqueda !== "";

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

  return (
    <section className="flex flex-1 flex-col gap-4">
      <PageHeader>
        <PageHeaderTitle>Usuarios</PageHeaderTitle>
        <PageHeaderActions>
          <Button>Registrar usuario</Button>
        </PageHeaderActions>
      </PageHeader>
      <div className="flex flex-wrap items-center justify-between gap-4">
        <SearchInput
          aria-label="Buscar usuarios"
          placeholder="Buscar usuarios"
          className="max-w-xs"
          value={textoBusqueda}
          onChange={(evento) => cambiarBusqueda(evento.target.value)}
        />
        <FilterToolbar
          filters={filtros}
          fields={CAMPOS_FILTRO}
          onAdd={(filtro) =>
            cambiarFiltros([
              ...filtros.filter((f) => f.id !== filtro.id),
              filtro,
            ])
          }
          onRemove={(id) => cambiarFiltros(filtros.filter((f) => f.id !== id))}
          onClearAll={() => cambiarFiltros([])}
        />
      </div>
      {isError ? (
        <EmptyState
          label="Error al cargar usuarios"
          title="No se pudieron cargar los usuarios"
          description={mensajeDeProblema(error)}
          action={
            <Button variant="secondary" onClick={() => void refetch()}>
              Reintentar
            </Button>
          }
        />
      ) : (
        <DataGrid
          rows={usuarios}
          columns={COLUMNAS}
          getRowLabel={(usuario) => usuario.nombre}
          renderCell={renderizarCelda}
          selectedRowIds={seleccionados}
          onSelectedRowIdsChange={setSeleccionados}
          getDrawerCellValue={valorDeCelda}
          canOpenDrawer={noAbreDetalle}
          emptyState={
            isPending ? (
              <EmptyState
                label="Cargando usuarios"
                title="Cargando usuarios"
                description="Un momento, por favor."
              />
            ) : (
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
                    <Button variant="secondary" onClick={limpiarCriterios}>
                      Limpiar búsqueda y filtros
                    </Button>
                  ) : undefined
                }
              />
            )
          }
        />
      )}
      {/* Fijo al borde inferior; los márgenes negativos compensan el p-6 del layout. */}
      <div className="sticky bottom-0 z-10 -mx-6 mt-auto -mb-6 flex flex-wrap items-center justify-between gap-4 px-6 pt-14 pb-6 before:absolute before:inset-0 before:-z-10 before:bg-background before:[mask-image:linear-gradient(to_top,black_55%,transparent)]">
        <PaginationSummary
          pageSize={tamanoPagina}
          total={total}
          onPageSizeChange={cambiarTamanoPagina}
        />
        <Pagination
          page={pagina}
          pageCount={totalPaginas}
          onPageChange={setPagina}
        />
      </div>
    </section>
  );
}
