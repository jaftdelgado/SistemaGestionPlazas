import {
  columnResizingFeature,
  columnSizingFeature,
  tableFeatures,
  useTable,
  type ColumnDef,
} from "@tanstack/react-table";
import * as React from "react";
import { type DataGridColumn } from "@/components/data-grid/types";

type UseGridColumnsParams<ColumnId extends string> = {
  columns: DataGridColumn<ColumnId>[];
  minColumnWidth: number;
  controlColumnWidth: number;
};

// La tabla de TanStack solo administra los anchos; las filas las renderiza el grid.
const features = tableFeatures({ columnSizingFeature, columnResizingFeature });
type FilaVacia = Record<string, never>;
const SIN_FILAS: FilaVacia[] = [];

export type GridResizeEvent = React.MouseEvent | React.TouchEvent;

export function useGridColumns<ColumnId extends string>({
  columns,
  minColumnWidth,
  controlColumnWidth,
}: UseGridColumnsParams<ColumnId>) {
  const columnDefs = React.useMemo<ColumnDef<typeof features, FilaVacia>[]>(
    () =>
      columns.map((column) => ({
        id: column.id,
        size: column.defaultWidth,
        minSize: column.minWidth ?? minColumnWidth,
      })),
    [columns, minColumnWidth],
  );

  const table = useTable({
    features,
    columns: columnDefs,
    data: SIN_FILAS,
    columnResizeMode: "onChange",
  });

  const columnWidths = Object.fromEntries(
    table.getAllLeafColumns().map((column) => [column.id, column.getSize()]),
  ) as Record<ColumnId, number>;

  const gridMinWidth =
    controlColumnWidth +
    columns.reduce((sum, column) => sum + columnWidths[column.id], 0);

  const resizingColumnId = table.state.columnResizing.isResizingColumn;
  const isResizing = resizingColumnId !== false;

  React.useEffect(() => {
    if (!isResizing) return;
    document.body.style.userSelect = "none";
    document.body.style.cursor = "col-resize";
    return () => {
      document.body.style.userSelect = "";
      document.body.style.cursor = "";
    };
  }, [isResizing]);

  function beginResize(event: GridResizeEvent, columnId: ColumnId) {
    const header = table
      .getFlatHeaders()
      .find((candidate) => candidate.column.id === columnId);
    if (!header) return;
    // Evita que el arrastre inicie una selección de texto o una acción de la celda.
    event.stopPropagation();
    if (event.type === "mousedown") event.preventDefault();
    header.getResizeHandler()(event);
  }

  function resetWidth(columnId: ColumnId) {
    table.getColumn(columnId)?.resetSize();
  }

  return {
    columnWidths,
    gridMinWidth,
    beginResize,
    resetWidth,
  };
}
