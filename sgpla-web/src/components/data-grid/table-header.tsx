import type * as React from "react";
import { SelectionCheckbox } from "@/components/data-grid/selection-checkbox";
import { TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { type DataGridColumn } from "@/components/data-grid/types";

type DataGridTableHeaderProps<ColumnId extends string> = {
  columns: DataGridColumn<ColumnId>[];
  columnWidths: Record<ColumnId, number>;
  controlColumnWidth: number;
  allRowsSelected: boolean;
  someRowsSelected: boolean;
  onToggleAllRows: (checked: boolean) => void;
  onResizeStart: (
    event: React.PointerEvent<HTMLButtonElement>,
    columnId: ColumnId,
  ) => void;
};

export function DataGridTableHeader<ColumnId extends string>({
  columns,
  columnWidths,
  controlColumnWidth,
  allRowsSelected,
  someRowsSelected,
  onToggleAllRows,
  onResizeStart,
}: DataGridTableHeaderProps<ColumnId>) {
  return (
    <>
      <colgroup>
        <col style={{ width: controlColumnWidth }} />
        {columns.map((column) => (
          <col key={column.id} style={{ width: columnWidths[column.id] }} />
        ))}
      </colgroup>

      <TableHeader>
        <TableRow>
          <TableHead className="h-10 w-10 border-r px-0 text-center">
            <SelectionCheckbox
              aria-label="Seleccionar todas las filas"
              checked={allRowsSelected}
              indeterminate={someRowsSelected}
              onToggle={(checked) => onToggleAllRows(checked)}
            />
          </TableHead>
          {columns.map((column) => {
            const Icon = column.icon;
            const width = columnWidths[column.id];

            return (
              <TableHead
                key={column.id}
                className="relative h-10 overflow-hidden border-r px-2 text-sm"
                style={{ width, minWidth: width }}
              >
                <span className="flex min-w-0 items-center gap-1.5 leading-none">
                  {Icon ? (
                    <Icon className="size-3.5 shrink-0 text-muted-foreground" />
                  ) : null}
                  <span className="truncate">{column.label}</span>
                </span>
                <button
                  type="button"
                  tabIndex={-1}
                  aria-label={`Redimensionar la columna ${column.label}`}
                  className="absolute top-0 right-0 h-full w-3 cursor-col-resize"
                  onPointerDown={(event) => onResizeStart(event, column.id)}
                />
              </TableHead>
            );
          })}
        </TableRow>
      </TableHeader>
    </>
  );
}
