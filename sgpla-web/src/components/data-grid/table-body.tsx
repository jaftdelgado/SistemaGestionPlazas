import { OpenRegular } from "@fluentui/react-icons";
import type * as React from "react";
import { Button } from "@/components/ui/button";
import { SelectionCheckbox } from "./selection-checkbox";
import { Input } from "@/components/ui/input";
import { TableBody, TableCell, TableRow } from "@/components/ui/table";
import {
  type DataGridColumn,
  type DataGridRowBase,
  type EditingCell,
} from "@/components/data-grid/types";
import { cn } from "@/lib/utils";

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
        const isSelected = selectedRowIds.has(row.id);
        // Solo la fila activa y sus controles entran en el orden de tabulación; entre filas se navega con flechas.
        const controlTabIndex = row.id === activeRowId ? 0 : -1;

        return (
          <TableRow
            key={row.id}
            data-grid-row="true"
            data-row-index={rowIndex}
            data-state={isSelected ? "selected" : undefined}
            aria-selected={isSelected}
            tabIndex={controlTabIndex}
            className="outline-none focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-ring"
            onFocus={() => onActiveRowChange(row.id)}
            onKeyDown={(event) => onRowKeyDown(event, row, rowIndex)}
          >
            <TableCell className="h-10 border-r px-0 py-0 text-center">
              <SelectionCheckbox
                aria-label={`Seleccionar ${getRowLabel(row)}`}
                checked={isSelected}
                tabIndex={controlTabIndex}
                onToggle={(checked) => onToggleRowSelection(row.id, checked)}
              />
            </TableCell>
            {columns.map((column) => {
              const isEditing =
                editingCell?.rowId === row.id &&
                editingCell.columnId === column.id;
              const editable = canEditColumn(column.id);
              const showDrawerAction = canOpenDrawer(column.id);

              return (
                <TableCell
                  key={column.id}
                  className={cn(
                    "group/cell relative h-10 overflow-hidden border-r px-2 py-1 whitespace-nowrap",
                    editable && "cursor-text",
                  )}
                  style={{
                    width: columnWidths[column.id],
                    minWidth: columnWidths[column.id],
                  }}
                  onDoubleClick={() => {
                    if (editable) startEditing(row, column.id);
                  }}
                >
                  {isEditing ? (
                    <Input
                      ref={inputRef}
                      size="sm"
                      aria-label={`${column.label} de ${getRowLabel(row)}`}
                      value={draftValue}
                      onChange={(event) => setDraftValue(event.target.value)}
                      onBlur={commitEdit}
                      onKeyDown={(event) => {
                        if (event.key === "Enter") {
                          event.preventDefault();
                          commitEdit();
                        }

                        if (event.key === "Escape") {
                          event.preventDefault();
                          cancelEdit();
                        }
                      }}
                      className="h-7 px-2"
                    />
                  ) : (
                    <>
                      <span
                        aria-hidden
                        className="pointer-events-none absolute inset-0 border border-transparent transition-colors duration-150 group-focus-within/cell:border-ring/30 group-hover/cell:border-ring/30"
                      />
                      <div
                        className={cn(
                          "min-w-0 truncate transition-[padding] duration-150",
                          showDrawerAction &&
                            "group-focus-within/cell:pr-20 group-hover/cell:pr-20",
                        )}
                      >
                        {renderCell(row, column)}
                      </div>
                      {showDrawerAction ? (
                        <div className="pointer-events-none absolute top-1/2 right-1.5 z-10 -translate-y-1/2 opacity-0 transition-all group-focus-within/cell:pointer-events-auto group-focus-within/cell:opacity-100 group-hover/cell:pointer-events-auto group-hover/cell:opacity-100">
                          <Button
                            type="button"
                            variant="secondary"
                            size="sm"
                            tabIndex={controlTabIndex}
                            aria-label={`Abrir detalle de ${getRowLabel(row)}, ${column.label}`}
                            className="h-7"
                            onPointerDown={(event) => {
                              event.stopPropagation();
                            }}
                            onDoubleClick={(event) => {
                              event.stopPropagation();
                            }}
                            onClick={(event) => {
                              event.stopPropagation();
                              const originElement =
                                event.currentTarget.closest("td") ??
                                event.currentTarget;
                              const originRect =
                                originElement.getBoundingClientRect();

                              onOpenDrawer({
                                rowId: row.id,
                                columnId: column.id,
                                originRect: {
                                  x: originRect.x,
                                  y: originRect.y,
                                  width: originRect.width,
                                  height: originRect.height,
                                },
                              });
                            }}
                          >
                            <OpenRegular aria-hidden="true" />
                            Abrir
                          </Button>
                        </div>
                      ) : null}
                    </>
                  )}
                </TableCell>
              );
            })}
          </TableRow>
        );
      })}
    </TableBody>
  );
}
