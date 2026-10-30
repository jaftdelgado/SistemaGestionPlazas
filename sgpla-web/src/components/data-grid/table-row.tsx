import { SquareArrowUpRightIcon } from "@hugeicons/core-free-icons";
import { HugeiconsIcon } from "@hugeicons/react";
import * as React from "react";
import { SelectionCheckbox } from "@/components/data-grid/selection-checkbox";
import { DESVANECIDO_ACCIONES } from "@/components/data-grid/actions-column";
import {
  type DataGridColumn,
  type DataGridRowAction,
  type DataGridRowBase,
  type EditingCell,
} from "@/components/data-grid/types";
import { Button } from "@/components/ui/button";
import { DropdownMenu } from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import { TableCell, TableRow } from "@/components/ui/table";
import { cn } from "@/lib/utils";

type DataGridRowProps<Row extends DataGridRowBase, ColumnId extends string> = {
  row: Row;
  rowIndex: number;
  columns: DataGridColumn<ColumnId>[];
  isSelected: boolean;
  isActive: boolean;
  // Solo la fila en edición recibe la columna y el borrador; el resto mantiene props idénticas y no se renderiza.
  editingColumnId: ColumnId | null;
  draftValue: string;
  getRowLabel: (row: Row) => string;
  renderCell: (row: Row, column: DataGridColumn<ColumnId>) => React.ReactNode;
  getRowActions?: (row: Row) => DataGridRowAction[];
  onActiveRowChange: (rowId: string) => void;
  onRowKeyDown: (
    event: React.KeyboardEvent<HTMLTableRowElement>,
    row: Row,
    rowIndex: number,
  ) => void;
  onToggleRowSelection: (rowId: string, checked: boolean) => void;
  onSelectionDragStart: (rowIndex: number) => void;
  canEditColumn: (columnId: ColumnId) => boolean;
  startEditing: (row: Row, columnId: ColumnId) => void;
  inputRef: React.RefObject<HTMLInputElement | null>;
  setDraftValue: (value: string) => void;
  commitEdit: () => void;
  cancelEdit: () => void;
  canOpenDrawer: (columnId: ColumnId) => boolean;
  onOpenDrawer: (cell: EditingCell<ColumnId>) => void;
};

function DataGridRowInner<
  Row extends DataGridRowBase,
  ColumnId extends string,
>({
  row,
  rowIndex,
  columns,
  isSelected,
  isActive,
  editingColumnId,
  draftValue,
  getRowLabel,
  renderCell,
  getRowActions,
  onActiveRowChange,
  onRowKeyDown,
  onToggleRowSelection,
  onSelectionDragStart,
  canEditColumn,
  startEditing,
  inputRef,
  setDraftValue,
  commitEdit,
  cancelEdit,
  canOpenDrawer,
  onOpenDrawer,
}: DataGridRowProps<Row, ColumnId>) {
  // Solo la fila activa y sus controles entran en el orden de tabulación; entre filas se navega con flechas.
  const controlTabIndex = isActive ? 0 : -1;

  return (
    <TableRow
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
          onDragStart={() => onSelectionDragStart(rowIndex)}
        />
      </TableCell>
      {columns.map((column) => {
        const isEditing = editingColumnId === column.id;
        const editable = canEditColumn(column.id);
        const showDrawerAction = canOpenDrawer(column.id);

        return (
          <TableCell
            key={column.id}
            className={cn(
              "group/cell relative h-10 overflow-hidden border-r px-2 py-1 whitespace-nowrap",
              editable && "cursor-text",
            )}
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
                      <HugeiconsIcon
                        icon={SquareArrowUpRightIcon}
                        aria-hidden="true"
                      />
                      Abrir
                    </Button>
                  </div>
                ) : null}
              </>
            )}
          </TableCell>
        );
      })}
      {getRowActions ? (
        <TableCell
          className={cn(
            "sticky right-0 z-10 h-10 bg-card p-0 text-center in-data-[state=selected]:bg-secondary",
            DESVANECIDO_ACCIONES,
            "before:from-card in-data-[state=selected]:before:from-secondary",
          )}
        >
          <DropdownMenu
            label={`Acciones de ${getRowLabel(row)}`}
            items={getRowActions(row)}
            triggerTabIndex={controlTabIndex}
          />
        </TableCell>
      ) : null}
    </TableRow>
  );
}

// React.memo pierde los genéricos del componente; el cast los conserva.
const DataGridRow = React.memo(DataGridRowInner) as typeof DataGridRowInner;

export { DataGridRow };
