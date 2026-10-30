import {
  Building01Icon,
  Building03Icon,
  Delete02Icon,
  Edit02Icon,
  GraduationScrollIcon,
  Mail01Icon,
  Shield01Icon,
  UserIcon,
  ViewIcon,
} from "@hugeicons/core-free-icons";
import * as React from "react";
import {
  DataGrid,
  type DataGridColumn,
  type DataGridRowAction,
} from "@/components/data-grid/data-grid";
import { anchoDeEsqueleto } from "@/components/data-grid/loading-width";
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
import { Skeleton } from "@/components/ui/skeleton";
import { useValorConRetardo } from "@/hooks/use-valor-con-retardo";
import { mensajeDeProblema } from "@/lib/api/problema";
import type { components } from "@/lib/api/schema";
import {
  useAreasAcademicas,
  useCuentas,
  useEntidadesAcademicas,
} from "../application/usuarios-queries";
import { ROLES } from "../domain/roles";

type CuentaListada = components["schemas"]["CuentaResponse"];

type UsuarioFila = {
  id: string;
  nombre: string;
  correo: string;
  rol: string;
  // Entidad o área de la cuenta; el Superusuario no tiene ámbito.
  ambito: string;
  tipoAmbito: "Entidad" | "Área" | null;
};

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

const CAMPO_ROL: FilterField = {
  id: "rol",
  label: "Rol",
  icon: Shield01Icon,
  options: ROLES.map((rol) => ({
    value: String(rol.id),
    label: rol.nombre,
  })),
};

const SEMILLA_CARGA = "usuarios";
const TAMANO_PAGINA_INICIAL = 10;
const RETARDO_BUSQUEDA_MS = 300;

function noAbreDetalle() {
  return false;
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

// Acciones provisionales: aún no hacen nada hasta que existan sus flujos.
const ACCIONES_USUARIO: DataGridRowAction[] = [
  { id: "detalle", label: "Ver detalle", icon: ViewIcon },
  { id: "editar", label: "Editar", icon: Edit02Icon },
  { id: "baja", label: "Dar de baja", icon: Delete02Icon, destructive: true },
];

function accionesDeUsuario() {
  return ACCIONES_USUARIO;
}

function valorDeFiltro(filtros: FilterChip[], id: string) {
  const valor = filtros.find((filtro) => filtro.id === id)?.value;
  return valor === undefined ? undefined : Number(valor);
}

// "Facultad de Estadística e Informática" se muestra como "Fac. de Estadística e Informática".
function abreviarFacultad(nombre: string) {
  return nombre.replace(/\bFacultad\b/g, "Fac.");
}

// Las cuentas DGAA traen área y las de Entidad Académica, entidad; el Superusuario, ninguna.
function ambitoDeCuenta(
  cuenta: CuentaListada,
): Pick<UsuarioFila, "ambito" | "tipoAmbito"> {
  if (cuenta.entidadAcademica) {
    return {
      ambito: abreviarFacultad(cuenta.entidadAcademica.nombre),
      tipoAmbito: "Entidad",
    };
  }
  if (cuenta.areaAcademica) {
    return { ambito: cuenta.areaAcademica.nombre, tipoAmbito: "Área" };
  }
  return { ambito: "", tipoAmbito: null };
}

function valorDeCelda(usuario: UsuarioFila, columnId: ColumnaUsuario) {
  return usuario[columnId];
}

export function UsuariosPage() {
  const [pagina, setPagina] = React.useState(1);
  const [tamanoPagina, setTamanoPagina] = React.useState(TAMANO_PAGINA_INICIAL);
  const [filtros, setFiltros] = React.useState<FilterChip[]>([]);
  const [textoBusqueda, setTextoBusqueda] = React.useState("");

  const busqueda = useValorConRetardo(
    textoBusqueda.trim(),
    RETARDO_BUSQUEDA_MS,
  );
  const rolFiltro = valorDeFiltro(filtros, "rol");
  const areaFiltro = valorDeFiltro(filtros, "area");
  const entidadFiltro = valorDeFiltro(filtros, "entidad");

  const { data, isPending, isError, error, refetch } = useCuentas({
    pagina,
    tamanoPagina,
    rolId: rolFiltro,
    areaAcademicaId: areaFiltro,
    entidadAcademicaId: entidadFiltro,
    busqueda: busqueda === "" ? undefined : busqueda,
  });

  const { data: areas } = useAreasAcademicas();
  const { data: entidades } = useEntidadesAcademicas(areaFiltro);

  // Con un área elegida, el filtro de entidad solo ofrece las de esa área.
  const camposFiltro = React.useMemo<FilterField[]>(
    () => [
      CAMPO_ROL,
      {
        id: "area",
        label: "Área académica",
        icon: GraduationScrollIcon,
        options: (areas ?? []).map((area) => ({
          value: String(area.id),
          label: area.nombre,
        })),
      },
      {
        id: "entidad",
        label: "Entidad académica",
        icon: Building03Icon,
        options: (entidades?.elementos ?? []).map((entidad) => ({
          value: String(entidad.id),
          label: abreviarFacultad(entidad.nombre),
        })),
      },
    ],
    [areas, entidades],
  );

  const usuarios = React.useMemo<UsuarioFila[]>(
    () =>
      (data?.elementos ?? []).map((cuenta) => ({
        id: String(cuenta.id),
        nombre: cuenta.nombre,
        correo: cuenta.correo,
        rol: cuenta.rol.nombre,
        ...ambitoDeCuenta(cuenta),
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
          fields={camposFiltro}
          onAdd={(filtro) =>
            cambiarFiltros([
              // Cambiar el área invalida la entidad elegida, que podría ser de otra área.
              ...filtros.filter(
                (f) =>
                  f.id !== filtro.id &&
                  !(filtro.id === "area" && f.id === "entidad"),
              ),
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
          getRowActions={accionesDeUsuario}
          loading={isPending}
          loadingSeed={SEMILLA_CARGA}
          renderLoadingCell={renderizarCargaCelda}
          loadingRowCount={tamanoPagina}
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
                  <Button variant="secondary" onClick={limpiarCriterios}>
                    Limpiar búsqueda y filtros
                  </Button>
                ) : undefined
              }
            />
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
