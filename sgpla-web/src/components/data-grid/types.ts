import type * as React from "react";

export type DataGridIcon = React.ComponentType<{
  className?: string;
}>;

export type DataGridRowBase = {
  id: string;
};

export type DataGridColumn<ColumnId extends string> = {
  id: ColumnId;
  label: string;
  icon: DataGridIcon;
  defaultWidth: number;
  minWidth?: number;
};

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
  getRowLabel: (row: Row) => string;
  renderCell: (row: Row, column: DataGridColumn<ColumnId>) => React.ReactNode;
  selectedRowIds: string[];
  onSelectedRowIdsChange: (rowIds: string[]) => void;
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
  fillAvailableHeight?: boolean;
  tableContainerClassName?: string;
};
