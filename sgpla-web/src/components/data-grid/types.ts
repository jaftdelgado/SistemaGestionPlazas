import type { IconSvgElement } from "@hugeicons/react";
import type * as React from "react";
import type { DropdownMenuItem } from "@/components/ui/dropdown-menu";

export type DataGridRowAction = DropdownMenuItem;

export type DataGridIcon = IconSvgElement;

export type DataGridRowBase = {
  id: string;
};

export type DataGridColumn<ColumnId extends string> = {
  id: ColumnId;
  label: string;
  icon: DataGridIcon;
  defaultWidth: number;
  minWidth?: number;
  // Permite ordenar por la columna al hacer clic en su encabezado; requiere onSortChange en el grid.
  sortable?: boolean;
};

export type SortDirection = "asc" | "desc";

type ActiveSort<ColumnId extends string> = {
  columnId: ColumnId;
  direction: SortDirection;
};

// Solo se ordena por una columna a la vez; null es sin orden.
export type DataGridSort<ColumnId extends string> = ActiveSort<ColumnId> | null;

export type EditingCell<ColumnId extends string> = {
  rowId: string;
  columnId: ColumnId;
  originRect?: {
    x: number;
    y: number;
    width: number;
    height: number;
  };
};

export type DataGridDrawerPanelProps<Row, ColumnId extends string> = {
  drawerRow: Row | null;
  drawerRowIndex: number;
  drawerRowCount: number;
  drawerColumn: DataGridColumn<ColumnId> | null;
  drawerCellValue: React.ReactNode;
  getRowLabel: (row: Row) => string;
  isEditableColumn: (columnId: ColumnId) => boolean;
  isEmptyValue: (value: React.ReactNode) => boolean;
  updateRow: (rowId: string, updater: (row: Row) => Row) => void;
  openPreviousRow: () => void;
  openNextRow: () => void;
  closeDrawer: () => void;
};

export type DataGridProps<
  Row extends DataGridRowBase,
  ColumnId extends string,
> = {
  rows: Row[];
  columns: DataGridColumn<ColumnId>[];
  // El orden es del consumidor: el grid solo lo dibuja y avisa del siguiente (asc, desc, sin orden) en onSortChange.
  sort?: DataGridSort<ColumnId>;
  onSortChange?: (sort: DataGridSort<ColumnId>) => void;
  getRowLabel: (row: Row) => string;
  renderCell: (row: Row, column: DataGridColumn<ColumnId>) => React.ReactNode;
  // Con él, el grid agrega una columna fija al final con un menú de acciones por fila; sin él, no hay columna.
  getRowActions?: (row: Row) => DataGridRowAction[];
  // Mientras carga, el cuerpo muestra filas de esqueleto en lugar de las filas y del estado vacío.
  loading?: boolean;
  loadingRowCount?: number;
  // Varía el ancho de los esqueletos por celda: es estable entre cargas y distinto entre grids con otra semilla.
  loadingSeed?: string;
  // Contenido de esqueleto de una celda mientras carga; por defecto, una barra.
  renderLoadingCell?: (
    column: DataGridColumn<ColumnId>,
    rowIndex: number,
  ) => React.ReactNode;
  // La selección es opcional: con onSelectedRowIdsChange el grid agrega la columna de casillas; sin él, no hay columna.
  selectedRowIds?: string[];
  onSelectedRowIdsChange?: (rowIds: string[]) => void;
  // La edición en celda es opcional: solo se activa en las columnas que isEditableColumn acepta y si se pasan
  // getCellEditValue y applyCellEdit.
  isEditableColumn?: (columnId: ColumnId) => boolean;
  getCellEditValue?: (row: Row, columnId: ColumnId) => string;
  applyCellEdit?: (row: Row, columnId: ColumnId, nextValue: string) => Row;
  onRowsChange?: (rows: Row[]) => void;
  getDrawerCellValue: (row: Row, columnId: ColumnId) => React.ReactNode;
  canOpenDrawer?: (columnId: ColumnId) => boolean;
  renderDrawerPanel?: (
    props: DataGridDrawerPanelProps<Row, ColumnId>,
  ) => React.ReactNode;
  onOpenDrawerCell?: (cell: EditingCell<ColumnId>) => void;
  drawerModal?: boolean;
  disablePointerDismissal?: boolean;
  // Se muestra en lugar de las filas cuando no hay ninguna; sin él, la tabla queda solo con el encabezado.
  emptyState?: React.ReactNode;
  fillAvailableHeight?: boolean;
  tableContainerClassName?: string;
};
