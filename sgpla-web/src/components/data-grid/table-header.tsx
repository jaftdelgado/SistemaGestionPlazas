import { ArrowUp01Icon } from "@hugeicons/core-free-icons";
import { HugeiconsIcon } from "@hugeicons/react";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { SelectionCheckbox } from "@/components/data-grid/selection-checkbox";
import { TableHead, TableHeader, TableRow } from "@/components/ui/table";
import {
  type DataGridColumn,
  type DataGridIcon,
  type DataGridSort,
  type SortDirection,
} from "@/components/data-grid/types";
import { type GridResizeEvent } from "@/components/data-grid/use-grid-columns";
import { EASE_OUT, SPRING_SWAP } from "@/lib/ease";
import { cn } from "@/lib/utils";
import { DESVANECIDO_ACCIONES } from "@/components/data-grid/actions-column";

const EASE = [...EASE_OUT] as [number, number, number, number];

function ariaSort(direction: SortDirection | null) {
  if (direction === "asc") return "ascending";
  if (direction === "desc") return "descending";
  return "none";
}

// El icono de la columna cede su lugar a una flecha mientras la columna está ordenada; la flecha gira al
// invertir el sentido.
function HeaderIcon({
  icon,
  direction,
}: {
  icon: DataGridIcon;
  direction: SortDirection | null;
}) {
  const reducir = useReducedMotion() ?? false;
  const transicion = reducir ? { duration: 0 } : { duration: 0.12, ease: EASE };

  return (
    <span className="relative flex size-3.5 shrink-0 items-center justify-center text-muted-foreground">
      <AnimatePresence mode="wait" initial={false}>
        {direction === null ? (
          <motion.span
            key="icono"
            className="flex"
            initial={{ opacity: 0, scale: 0.6 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0, scale: 0.6 }}
            transition={transicion}
          >
            <HugeiconsIcon icon={icon} className="size-3.5" />
          </motion.span>
        ) : (
          <motion.span
            key="flecha"
            className="flex text-foreground"
            initial={{ opacity: 0, scale: 0.6 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0, scale: 0.6 }}
            transition={transicion}
          >
            <motion.span
              className="flex"
              initial={false}
              animate={{ rotate: direction === "desc" ? 180 : 0 }}
              transition={reducir ? { duration: 0 } : SPRING_SWAP}
            >
              <HugeiconsIcon icon={ArrowUp01Icon} className="size-3.5" />
            </motion.span>
          </motion.span>
        )}
      </AnimatePresence>
    </span>
  );
}

type DataGridTableHeaderProps<ColumnId extends string> = {
  columns: DataGridColumn<ColumnId>[];
  columnWidths: Record<ColumnId, number>;
  sort: DataGridSort<ColumnId>;
  // Sin él, ninguna columna es ordenable aunque lo declare.
  onSortToggle?: (columnId: ColumnId) => void;
  // Sin selección no se dibuja la columna de casillas.
  selectable: boolean;
  controlColumnWidth: number;
  // Ancho de la columna de acciones, fija y no redimensionable; sin él, no se dibuja.
  actionsColumnWidth?: number;
  allRowsSelected: boolean;
  someRowsSelected: boolean;
  onToggleAllRows: (checked: boolean) => void;
  onResizeStart: (event: GridResizeEvent, columnId: ColumnId) => void;
  onResizeReset: (columnId: ColumnId) => void;
};

export function DataGridTableHeader<ColumnId extends string>({
  columns,
  columnWidths,
  sort,
  onSortToggle,
  selectable,
  controlColumnWidth,
  actionsColumnWidth,
  allRowsSelected,
  someRowsSelected,
  onToggleAllRows,
  onResizeStart,
  onResizeReset,
}: DataGridTableHeaderProps<ColumnId>) {
  return (
    <>
      <colgroup>
        {selectable ? <col style={{ width: controlColumnWidth }} /> : null}
        {columns.map((column, index) => (
          // La última columna no fija su ancho: es la única que absorbe el sobrante de la tabla.
          <col
            key={column.id}
            style={
              index === columns.length - 1
                ? undefined
                : { width: columnWidths[column.id] }
            }
          />
        ))}
        {actionsColumnWidth !== undefined ? (
          <col style={{ width: actionsColumnWidth }} />
        ) : null}
      </colgroup>

      <TableHeader>
        <TableRow>
          {selectable ? (
            <TableHead className="h-10 w-10 border-r bg-table-header px-0 text-center">
              <SelectionCheckbox
                aria-label="Seleccionar todas las filas"
                checked={allRowsSelected}
                indeterminate={someRowsSelected}
                onToggle={(checked) => onToggleAllRows(checked)}
              />
            </TableHead>
          ) : null}
          {columns.map((column, index) => {
            const icon = column.icon;
            const width = columnWidths[column.id];
            const sortable =
              (column.sortable ?? false) && onSortToggle !== undefined;
            const direction =
              sort?.columnId === column.id ? sort.direction : null;

            return (
              <TableHead
                key={column.id}
                aria-sort={sortable ? ariaSort(direction) : undefined}
                className={cn(
                  "relative h-10 overflow-hidden border-r bg-table-header px-2 text-sm",
                  // La columna de acciones no lleva divisoria a su izquierda.
                  actionsColumnWidth !== undefined &&
                    index === columns.length - 1 &&
                    "border-r-0",
                )}
                style={
                  index === columns.length - 1
                    ? undefined
                    : { width, minWidth: width }
                }
              >
                {sortable ? (
                  <button
                    type="button"
                    className="absolute inset-0 flex min-w-0 items-center gap-1.5 px-2 pr-3 text-left leading-none outline-none hover:bg-foreground/5 focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-inset"
                    onClick={() => onSortToggle?.(column.id)}
                  >
                    <HeaderIcon icon={icon} direction={direction} />
                    <span className="truncate">{column.label}</span>
                  </button>
                ) : (
                  <span className="flex min-w-0 items-center gap-1.5 leading-none">
                    <HugeiconsIcon
                      icon={icon}
                      className="size-3.5 shrink-0 text-muted-foreground"
                    />
                    <span className="truncate">{column.label}</span>
                  </span>
                )}
                <button
                  type="button"
                  tabIndex={-1}
                  aria-label={`Redimensionar la columna ${column.label}`}
                  className="absolute top-0 right-0 h-full w-3 cursor-col-resize touch-none"
                  onMouseDown={(event) => onResizeStart(event, column.id)}
                  onTouchStart={(event) => onResizeStart(event, column.id)}
                  onDoubleClick={() => onResizeReset(column.id)}
                />
              </TableHead>
            );
          })}
          {actionsColumnWidth !== undefined ? (
            <TableHead
              className={cn(
                "sticky right-0 z-10 h-10 bg-table-header px-0",
                DESVANECIDO_ACCIONES,
                "before:from-table-header",
              )}
            >
              <span className="sr-only">Acciones</span>
            </TableHead>
          ) : null}
        </TableRow>
      </TableHeader>
    </>
  );
}
