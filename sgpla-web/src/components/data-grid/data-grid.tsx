import { Dialog as DialogPrimitive } from "@base-ui/react/dialog";
import * as React from "react";
import { DrawerPanel } from "@/components/data-grid/drawer-panel";
import { DataGridTableBody } from "@/components/data-grid/table-body";
import { DataGridTableHeader } from "@/components/data-grid/table-header";
import {
  type DataGridDrawerPanelProps,
  type DataGridProps,
  type DataGridRowBase,
  type EditingCell,
} from "@/components/data-grid/types";
import { useGridColumns } from "@/components/data-grid/use-grid-columns";
import { useDragSelection } from "@/components/data-grid/use-drag-selection";
import { useGridEditing } from "@/components/data-grid/use-grid-editing";
import { useStableCallback } from "@/components/data-grid/use-stable-callback";
import { Table } from "@/components/ui/table";
import { cn } from "@/lib/utils";

export type {
  DataGridColumn,
  DataGridDrawerPanelProps,
  DataGridIcon,
  DataGridProps,
  DataGridRowBase,
  DataGridSort,
  EditingCell,
  SortDirection,
} from "@/components/data-grid/types";

const MIN_COLUMN_WIDTH = 96;
const CONTROL_COLUMN_WIDTH = 40;

function isEmptyValue(value: React.ReactNode) {
  if (value === null || value === undefined) return true;
  if (typeof value === "string") return value.trim().length === 0;
  return false;
}

function neverEditable() {
  return false;
}

function alwaysOpenDrawer() {
  return true;
}

export function DataGrid<Row extends DataGridRowBase, ColumnId extends string>({
  rows,
  columns,
  sort = null,
  onSortChange,
  getRowLabel,
  renderCell,
  selectedRowIds,
  onSelectedRowIdsChange,
  isEditableColumn = neverEditable,
  getCellEditValue,
  applyCellEdit,
  onRowsChange,
  getDrawerCellValue,
  canOpenDrawer = alwaysOpenDrawer,
  renderDrawerPanel,
  onOpenDrawerCell,
  drawerModal = false,
  disablePointerDismissal = true,
  emptyState,
  fillAvailableHeight = false,
  tableContainerClassName,
}: DataGridProps<Row, ColumnId>) {
  const [drawerCell, setDrawerCell] =
    React.useState<EditingCell<ColumnId> | null>(null);
  const [focusedRowId, setFocusedRowId] = React.useState<string | null>(null);

  const gridRef = React.useRef<HTMLDivElement>(null);
  const inputRef = React.useRef<HTMLInputElement>(null);

  const { columnWidths, gridMinWidth, beginResize, resetWidth } =
    useGridColumns({
      columns,
      minColumnWidth: MIN_COLUMN_WIDTH,
      controlColumnWidth: CONTROL_COLUMN_WIDTH,
    });

  const drawerRow = React.useMemo(() => {
    if (!drawerCell) return null;
    return rows.find((row) => row.id === drawerCell.rowId) ?? null;
  }, [drawerCell, rows]);

  const drawerRowIndex = React.useMemo(() => {
    if (!drawerRow) return -1;
    return rows.findIndex((row) => row.id === drawerRow.id);
  }, [drawerRow, rows]);

  const drawerColumn = React.useMemo(() => {
    if (!drawerCell) return null;
    return columns.find((column) => column.id === drawerCell.columnId) ?? null;
  }, [columns, drawerCell]);

  const drawerCellValue = React.useMemo(() => {
    if (!drawerCell || !drawerRow) return null;
    return getDrawerCellValue(drawerRow, drawerCell.columnId);
  }, [drawerCell, drawerRow, getDrawerCellValue]);

  const commitRows = React.useCallback(
    (updater: (currentRows: Row[]) => Row[]) => {
      const nextRows = updater(rows);
      onRowsChange?.(nextRows);
    },
    [onRowsChange, rows],
  );

  const {
    editingCell,
    draftValue,
    setDraftValue,
    canEditColumn,
    startEditing,
    commitEdit,
    cancelEdit,
  } = useGridEditing<Row, ColumnId>({
    inputRef,
    isEditableColumn,
    getCellEditValue,
    applyCellEdit,
    commitRows,
  });

  // Cada clic avanza el ciclo de la columna: ascendente, descendente y sin orden.
  function toggleSort(columnId: ColumnId) {
    if (sort?.columnId !== columnId) {
      onSortChange?.({ columnId, direction: "asc" });
      return;
    }

    onSortChange?.(
      sort.direction === "asc" ? { columnId, direction: "desc" } : null,
    );
  }

  const closeDrawer = React.useCallback(() => {
    setDrawerCell(null);
  }, []);

  const selectedRowIdSet = React.useMemo(
    () => new Set(selectedRowIds),
    [selectedRowIds],
  );

  const toggleRowSelection = React.useCallback(
    (rowId: string, checked: boolean) => {
      if (checked) {
        if (selectedRowIds.includes(rowId)) return;
        onSelectedRowIdsChange([...selectedRowIds, rowId]);
        return;
      }

      onSelectedRowIdsChange(
        selectedRowIds.filter((currentRowId) => currentRowId !== rowId),
      );
    },
    [onSelectedRowIdsChange, selectedRowIds],
  );

  const startSelectionDrag = useDragSelection({
    gridRef,
    rows,
    selectedRowIds,
    onSelectedRowIdsChange,
  });

  const toggleAllRows = React.useCallback(
    (checked: boolean) => {
      onSelectedRowIdsChange(checked ? rows.map((row) => row.id) : []);
    },
    [onSelectedRowIdsChange, rows],
  );

  // La selección es del consumidor y puede incluir filas que ya no están: el encabezado solo cuenta las visibles.
  const selectedVisibleRowCount = React.useMemo(
    () => rows.filter((row) => selectedRowIdSet.has(row.id)).length,
    [rows, selectedRowIdSet],
  );
  const allRowsSelected =
    rows.length > 0 && selectedVisibleRowCount === rows.length;
  const someRowsSelected = selectedVisibleRowCount > 0 && !allRowsSelected;

  // Siempre hay una fila en el orden de tabulación: la última enfocada o, si ya no existe, la primera.
  const activeRowId = React.useMemo(
    () =>
      focusedRowId !== null && rows.some((row) => row.id === focusedRowId)
        ? focusedRowId
        : (rows[0]?.id ?? null),
    [focusedRowId, rows],
  );

  const focusRow = React.useCallback((rowIndex: number) => {
    const targetRow = gridRef.current?.querySelector<HTMLElement>(
      `[data-grid-row="true"][data-row-index="${rowIndex}"]`,
    );
    targetRow?.focus();
  }, []);

  const handleRowKeyDown = React.useCallback(
    (
      event: React.KeyboardEvent<HTMLTableRowElement>,
      row: Row,
      rowIndex: number,
    ) => {
      // Las teclas de la fila no aplican cuando el foco está en un control interno (casilla, botón o input).
      if (event.target !== event.currentTarget) return;

      if (event.key === "ArrowDown") {
        event.preventDefault();
        focusRow(Math.min(rowIndex + 1, rows.length - 1));
        return;
      }

      if (event.key === "ArrowUp") {
        event.preventDefault();
        focusRow(Math.max(rowIndex - 1, 0));
        return;
      }

      if (event.key === " ") {
        event.preventDefault();
        toggleRowSelection(row.id, !selectedRowIdSet.has(row.id));
      }
    },
    [focusRow, rows.length, selectedRowIdSet, toggleRowSelection],
  );

  const updateRow = React.useCallback(
    (rowId: string, updater: (row: Row) => Row) => {
      commitRows((currentRows) =>
        currentRows.map((row) => (row.id === rowId ? updater(row) : row)),
      );
    },
    [commitRows],
  );

  const openDrawerRowAt = React.useCallback(
    (rowIndex: number) => {
      if (!drawerCell) return;

      const nextRow = rows[rowIndex];
      if (!nextRow) return;

      setDrawerCell({
        rowId: nextRow.id,
        columnId: drawerCell.columnId,
      });
    },
    [drawerCell, rows],
  );

  const openPreviousRow = React.useCallback(() => {
    openDrawerRowAt(drawerRowIndex - 1);
  }, [drawerRowIndex, openDrawerRowAt]);

  const openNextRow = React.useCallback(() => {
    openDrawerRowAt(drawerRowIndex + 1);
  }, [drawerRowIndex, openDrawerRowAt]);

  const drawerPanelProps: DataGridDrawerPanelProps<Row, ColumnId> = {
    drawerRow,
    drawerRowIndex,
    drawerRowCount: rows.length,
    drawerColumn,
    drawerCellValue,
    getRowLabel,
    isEditableColumn: canEditColumn,
    isEmptyValue,
    updateRow,
    openPreviousRow,
    openNextRow,
    closeDrawer,
  };

  // Las filas están memoizadas: estos manejadores mantienen su identidad para que un cambio de selección,
  // foco o edición solo renderice las filas afectadas. `renderCell` debe depender únicamente de `row` y `column`.
  const stableGetRowLabel = useStableCallback(getRowLabel);
  const stableRenderCell = useStableCallback(renderCell);
  const stableRowKeyDown = useStableCallback(handleRowKeyDown);
  const stableToggleRowSelection = useStableCallback(toggleRowSelection);
  const stableStartSelectionDrag = useStableCallback(startSelectionDrag);
  const stableCanEditColumn = useStableCallback(canEditColumn);
  const stableStartEditing = useStableCallback(startEditing);
  const stableSetDraftValue = useStableCallback(setDraftValue);
  const stableCommitEdit = useStableCallback(commitEdit);
  const stableCancelEdit = useStableCallback(cancelEdit);
  const stableCanOpenDrawer = useStableCallback(canOpenDrawer);
  const stableOpenDrawer = useStableCallback((cell: EditingCell<ColumnId>) => {
    if (onOpenDrawerCell) {
      onOpenDrawerCell(cell);
      return;
    }

    setDrawerCell(cell);
  });

  return (
    <DialogPrimitive.Root
      open={drawerCell !== null}
      modal={drawerModal}
      disablePointerDismissal={disablePointerDismissal}
      onOpenChange={(open) => {
        if (!open) closeDrawer();
      }}
    >
      <div
        data-slot="data-grid"
        className={cn("min-h-0", fillAvailableHeight && "flex h-full flex-col")}
        ref={gridRef}
      >
        <div className={cn(fillAvailableHeight && "min-h-0 flex-1")}>
          <Table
            className="table-fixed"
            containerClassName={cn(
              "rounded-2xl border border-transparent bg-card shadow-border",
              fillAvailableHeight && "h-full",
              tableContainerClassName,
            )}
            style={{ width: `max(100%, ${gridMinWidth}px)` }}
          >
            <DataGridTableHeader
              columns={columns}
              columnWidths={columnWidths}
              sort={sort}
              onSortToggle={onSortChange ? toggleSort : undefined}
              controlColumnWidth={CONTROL_COLUMN_WIDTH}
              allRowsSelected={allRowsSelected}
              someRowsSelected={someRowsSelected}
              onToggleAllRows={toggleAllRows}
              onResizeStart={beginResize}
              onResizeReset={resetWidth}
            />

            <DataGridTableBody
              rows={rows}
              emptyState={emptyState}
              columns={columns}
              getRowLabel={stableGetRowLabel}
              renderCell={stableRenderCell}
              activeRowId={activeRowId}
              onActiveRowChange={setFocusedRowId}
              onRowKeyDown={stableRowKeyDown}
              selectedRowIds={selectedRowIdSet}
              onToggleRowSelection={stableToggleRowSelection}
              onSelectionDragStart={stableStartSelectionDrag}
              editingCell={editingCell}
              canEditColumn={stableCanEditColumn}
              startEditing={stableStartEditing}
              inputRef={inputRef}
              draftValue={draftValue}
              setDraftValue={stableSetDraftValue}
              commitEdit={stableCommitEdit}
              cancelEdit={stableCancelEdit}
              canOpenDrawer={stableCanOpenDrawer}
              onOpenDrawer={stableOpenDrawer}
            />
          </Table>
        </div>
      </div>

      {renderDrawerPanel ? (
        renderDrawerPanel(drawerPanelProps)
      ) : (
        <DrawerPanel
          drawerRow={drawerRow}
          drawerColumn={drawerColumn}
          drawerCellValue={drawerCellValue}
          getRowLabel={getRowLabel}
          isEditableColumn={canEditColumn}
          isEmptyValue={isEmptyValue}
        />
      )}
    </DialogPrimitive.Root>
  );
}
