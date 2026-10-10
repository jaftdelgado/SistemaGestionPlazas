import type * as React from "react";
import { DataGridRow } from "@/components/data-grid/table-row";
import { TableBody } from "@/components/ui/table";
import {
  type DataGridColumn,
  type DataGridRowBase,
  type EditingCell,
} from "@/components/data-grid/types";

type DataGridTableBodyProps<
  Row extends DataGridRowBase,
  ColumnId extends string,
> = {
  rows: Row[];
  columns: DataGridColumn<ColumnId>[];
  columnWidths: Record<ColumnId, number>;
  getRowLabel: (row: Row) => string;
  renderCell: (row: Row, column: DataGridColumn<ColumnId>) => React.ReactNode;
  activeRowId: string | null;
  onActiveRowChange: (rowId: string) => void;
  onRowKeyDown: (
    event: React.KeyboardEvent<HTMLTableRowElement>,
    row: Row,
    rowIndex: number,
  ) => void;
  selectedRowIds: ReadonlySet<string>;
  onToggleRowSelection: (rowId: string, checked: boolean) => void;
  onSelectionDragStart: (rowIndex: number) => void;
  editingCell: EditingCell<ColumnId> | null;
  canEditColumn: (columnId: ColumnId) => boolean;
  startEditing: (row: Row, columnId: ColumnId) => void;
  inputRef: React.RefObject<HTMLInputElement | null>;
  draftValue: string;
  setDraftValue: (value: string) => void;
  commitEdit: () => void;
  cancelEdit: () => void;
  canOpenDrawer: (columnId: ColumnId) => boolean;
  onOpenDrawer: (cell: EditingCell<ColumnId>) => void;
};

export function DataGridTableBody<
  Row extends DataGridRowBase,
  ColumnId extends string,
>({
  rows,
  columns,
  columnWidths,
  getRowLabel,
  renderCell,
  activeRowId,
  onActiveRowChange,
  onRowKeyDown,
  selectedRowIds,
  onToggleRowSelection,
  onSelectionDragStart,
  editingCell,
  canEditColumn,
  startEditing,
  inputRef,
  draftValue,
  setDraftValue,
  commitEdit,
  cancelEdit,
  canOpenDrawer,
  onOpenDrawer,
}: DataGridTableBodyProps<Row, ColumnId>) {
  return (
    <TableBody>
      {rows.map((row, rowIndex) => {
        const isEditingRow = editingCell?.rowId === row.id;

        return (
          <DataGridRow
            key={row.id}
            row={row}
            rowIndex={rowIndex}
            columns={columns}
            columnWidths={columnWidths}
            isSelected={selectedRowIds.has(row.id)}
            isActive={row.id === activeRowId}
            editingColumnId={isEditingRow ? editingCell.columnId : null}
            draftValue={isEditingRow ? draftValue : ""}
            getRowLabel={getRowLabel}
            renderCell={renderCell}
            onActiveRowChange={onActiveRowChange}
            onRowKeyDown={onRowKeyDown}
            onToggleRowSelection={onToggleRowSelection}
            onSelectionDragStart={onSelectionDragStart}
            canEditColumn={canEditColumn}
            startEditing={startEditing}
            inputRef={inputRef}
            setDraftValue={setDraftValue}
            commitEdit={commitEdit}
            cancelEdit={cancelEdit}
            canOpenDrawer={canOpenDrawer}
            onOpenDrawer={onOpenDrawer}
          />
        );
      })}
    </TableBody>
  );
}
