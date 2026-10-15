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
  const [usuarios, setUsuarios] = React.useState(USUARIOS_EJEMPLO);
  const [seleccionados, setSeleccionados] = React.useState<string[]>([]);

  return (
    <section className="flex flex-col gap-4">
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
        rows={usuarios}
        columns={COLUMNAS}
        getRowLabel={(usuario) => usuario.nombre}
        renderCell={(usuario, columna) => usuario[columna.id]}
        selectedRowIds={seleccionados}
        onSelectedRowIdsChange={setSeleccionados}
        isEditableColumn={esColumnaEditable}
        getCellEditValue={valorDeCelda}
        applyCellEdit={aplicarEdicion}
        onRowsChange={setUsuarios}
        getDrawerCellValue={valorDeCelda}
        canOpenDrawer={abreDetalle}
      />
    </section>
  );
}
