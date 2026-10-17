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
  (_, i) => {
    const base = USUARIOS_EJEMPLO[i % USUARIOS_EJEMPLO.length];
    const numero = i + 7;
    return {
      ...base,
      id: String(numero),
      nombre: `${base.nombre} ${numero}`,
      correo: `usuario${numero}@uv.mx`,
    };
  },
);

const TODOS_LOS_USUARIOS = [...USUARIOS_EJEMPLO, ...USUARIOS_RELLENO];

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

  const totalPaginas = Math.ceil(usuarios.length / tamanoPagina);
  const inicio = (pagina - 1) * tamanoPagina;
  const usuariosDePagina = usuarios.slice(inicio, inicio + tamanoPagina);

  // Al cambiar el tamaño, la página actual podría no existir; se vuelve a la primera.
  function cambiarTamanoPagina(tamano: number) {
    setTamanoPagina(tamano);
    setPagina(1);
  }

  // El grid devuelve solo las filas de la página; se reinsertan en su posición.
  function aplicarFilasDePagina(filas: UsuarioEjemplo[]) {
    setUsuarios((actuales) => [
      ...actuales.slice(0, inicio),
      ...filas,
      ...actuales.slice(inicio + tamanoPagina),
    ]);
  }

  return (
    <section className="flex flex-1 flex-col gap-4">
      <PageHeader>
        <PageHeaderTitle>Usuarios</PageHeaderTitle>
        <PageHeaderActions>
          <Button>Registrar usuario</Button>
        </PageHeaderActions>
      </PageHeader>
      <SearchInput
        aria-label="Buscar usuarios"
        placeholder="Buscar usuarios"
        className="max-w-xs"
      />
      <p className="text-sm text-muted-foreground">
        {seleccionados.length} de {usuarios.length} seleccionados
      </p>
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
      />
      {/* Fijo al borde inferior; los márgenes negativos compensan el p-6 del layout. */}
      <div className="sticky bottom-0 z-10 mt-auto -mx-6 -mb-6 flex flex-wrap items-center justify-between gap-4 px-6 pt-14 pb-6 before:absolute before:inset-0 before:-z-10 before:bg-background before:[mask-image:linear-gradient(to_top,black_55%,transparent)]">
        <PaginationSummary
          pageSize={tamanoPagina}
          total={usuarios.length}
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
