import {
  CheckmarkCircleRegular,
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

// Datos de ejemplo para verificar el DataGrid a mano; se sustituyen por la consulta real de usuarios.
type UsuarioEjemplo = {
  id: string;
  nombre: string;
  correo: string;
  rol: string;
  estado: string;
};

type ColumnaUsuario = "nombre" | "correo" | "rol" | "estado";

const COLUMNAS: DataGridColumn<ColumnaUsuario>[] = [
  { id: "nombre", label: "Nombre", icon: PersonRegular, defaultWidth: 260 },
  { id: "correo", label: "Correo", icon: MailRegular, defaultWidth: 280 },
  { id: "rol", label: "Rol", icon: ShieldRegular, defaultWidth: 200 },
  {
    id: "estado",
    label: "Estado",
    icon: CheckmarkCircleRegular,
    defaultWidth: 140,
    minWidth: 120,
  },
];

const USUARIOS_EJEMPLO: UsuarioEjemplo[] = [
  {
    id: "1",
    nombre: "María Fernanda López Hernández",
    correo: "mflopez@uv.mx",
    rol: "Superusuario",
    estado: "Activo",
  },
  {
    id: "2",
    nombre: "José Antonio Ramírez Cruz",
    correo: "joramirez@uv.mx",
    rol: "Coordinador",
    estado: "Activo",
  },
  {
    id: "3",
    nombre: "Ana Karen Morales Vázquez",
    correo: "anmorales@uv.mx",
    rol: "Académico",
    estado: "Inactivo",
  },
  {
    id: "4",
    nombre: "Luis Enrique Domínguez Soto",
    correo: "ludominguez@uv.mx",
    rol: "Académico",
    estado: "Activo",
  },
  {
    id: "5",
    nombre: "Gabriela Sánchez Ortiz",
    correo: "gasanchez@uv.mx",
    rol: "Coordinador",
    estado: "Activo",
  },
  {
    id: "6",
    nombre: "Ricardo Méndez Aguilar",
    correo: "rimendez@uv.mx",
    rol: "Académico",
    estado: "Inactivo",
  },
];

// Relleno temporal para probar la paginación con muchas páginas.
const USUARIOS_RELLENO: UsuarioEjemplo[] = Array.from(
  { length: 94 },
  (_, i) => i,
).flatMap((i) => {
  const base = USUARIOS_EJEMPLO[i % USUARIOS_EJEMPLO.length];
  if (!base) return [];
  const numero = i + 7;
  return [
    {
      ...base,
      id: String(numero),
      nombre: `${base.nombre} ${numero}`,
      correo: `usuario${numero}@uv.mx`,
    },
  ];
});

const TODOS_LOS_USUARIOS = [...USUARIOS_EJEMPLO, ...USUARIOS_RELLENO];

const CAMPOS_FILTRO: FilterField[] = [
  {
    id: "rol",
    label: "Rol",
    options: ["Superusuario", "Coordinador", "Académico"].map((rol) => ({
      value: rol,
      label: rol,
    })),
  },
  {
    id: "estado",
    label: "Estado",
    options: ["Activo", "Inactivo"].map((estado) => ({
      value: estado,
      label: estado,
    })),
  },
];

const TAMANO_PAGINA_INICIAL = 10;

function esColumnaEditable(columnId: ColumnaUsuario) {
  return columnId === "nombre";
}

function abreDetalle(columnId: ColumnaUsuario) {
  return columnId === "nombre" || columnId === "correo";
}

function valorDeCelda(usuario: UsuarioEjemplo, columnId: ColumnaUsuario) {
  return usuario[columnId];
}

function aplicarEdicion(
  usuario: UsuarioEjemplo,
  columnId: ColumnaUsuario,
  valor: string,
) {
  return { ...usuario, [columnId]: valor };
}

export function UsuariosPage() {
  const [usuarios, setUsuarios] = React.useState(TODOS_LOS_USUARIOS);
  const [seleccionados, setSeleccionados] = React.useState<string[]>([]);
  const [pagina, setPagina] = React.useState(1);
  const [tamanoPagina, setTamanoPagina] = React.useState(TAMANO_PAGINA_INICIAL);

  const [filtros, setFiltros] = React.useState<FilterChip[]>([]);

  const usuariosFiltrados = usuarios.filter((usuario) =>
    filtros.every(
      (filtro) => usuario[filtro.id as ColumnaUsuario] === filtro.value,
    ),
  );
  const totalPaginas = Math.ceil(usuariosFiltrados.length / tamanoPagina);
  const inicio = (pagina - 1) * tamanoPagina;
  const usuariosDePagina = usuariosFiltrados.slice(
    inicio,
    inicio + tamanoPagina,
  );

  // Al cambiar los filtros, la página actual podría no existir; se vuelve a la primera.
  function cambiarFiltros(siguientes: FilterChip[]) {
    setFiltros(siguientes);
    setPagina(1);
  }

  // Al cambiar el tamaño, la página actual podría no existir; se vuelve a la primera.
  function cambiarTamanoPagina(tamano: number) {
    setTamanoPagina(tamano);
    setPagina(1);
  }

  // El grid devuelve solo las filas de la página; se reemplazan por id.
  function aplicarFilasDePagina(filas: UsuarioEjemplo[]) {
    setUsuarios((actuales) =>
      actuales.map(
        (usuario) => filas.find((f) => f.id === usuario.id) ?? usuario,
      ),
    );
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
      <DataGrid
        rows={usuariosDePagina}
        columns={COLUMNAS}
        getRowLabel={(usuario) => usuario.nombre}
        renderCell={(usuario, columna) => usuario[columna.id]}
        selectedRowIds={seleccionados}
        onSelectedRowIdsChange={setSeleccionados}
        isEditableColumn={esColumnaEditable}
        getCellEditValue={valorDeCelda}
        applyCellEdit={aplicarEdicion}
        onRowsChange={aplicarFilasDePagina}
        getDrawerCellValue={valorDeCelda}
        canOpenDrawer={abreDetalle}
        emptyState={
          <EmptyState
            label="Sin usuarios"
            title={
              filtros.length > 0 ? "Sin resultados" : "Aún no hay usuarios"
            }
            description={
              filtros.length > 0
                ? "Ningún usuario coincide con los filtros aplicados."
                : "Cuando registres usuarios, aparecerán aquí."
            }
            action={
              filtros.length > 0 ? (
                <Button variant="secondary" onClick={() => cambiarFiltros([])}>
                  Limpiar filtros
                </Button>
              ) : undefined
            }
          />
        }
      />
      {/* Fijo al borde inferior; los márgenes negativos compensan el p-6 del layout. */}
      <div className="sticky bottom-0 z-10 -mx-6 mt-auto -mb-6 flex flex-wrap items-center justify-between gap-4 px-6 pt-14 pb-6 before:absolute before:inset-0 before:-z-10 before:bg-background before:[mask-image:linear-gradient(to_top,black_55%,transparent)]">
        <PaginationSummary
          pageSize={tamanoPagina}
          total={usuariosFiltrados.length}
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
