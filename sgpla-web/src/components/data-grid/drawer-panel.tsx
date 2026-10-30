import { Dialog as DialogPrimitive } from "@base-ui/react/dialog";
import { Cancel01Icon } from "@hugeicons/core-free-icons";
import { HugeiconsIcon } from "@hugeicons/react";
import type * as React from "react";
import { Button } from "@/components/ui/button";
import { type DataGridColumn } from "@/components/data-grid/types";

type DrawerPanelProps<Row, ColumnId extends string> = {
  drawerRow: Row | null;
  drawerColumn: DataGridColumn<ColumnId> | null;
  drawerCellValue: React.ReactNode;
  getRowLabel: (row: Row) => string;
  isEditableColumn: (columnId: ColumnId) => boolean;
  isEmptyValue: (value: React.ReactNode) => boolean;
};

export function DrawerPanel<Row, ColumnId extends string>({
  drawerRow,
  drawerColumn,
  drawerCellValue,
  getRowLabel,
  isEditableColumn,
  isEmptyValue,
}: DrawerPanelProps<Row, ColumnId>) {
  return (
    <DialogPrimitive.Portal>
      <DialogPrimitive.Popup
        data-slot="data-grid-drawer"
        className="fixed top-0 right-0 z-50 flex h-dvh w-full max-w-md translate-x-full flex-col border-l bg-popover text-popover-foreground shadow-xl ring-1 ring-foreground/10 transition-transform duration-200 outline-none data-open:translate-x-0 data-closed:translate-x-full"
      >
        <div className="flex items-center justify-between gap-3 border-b px-4 py-3">
          <div className="min-w-0">
            <DialogPrimitive.Title className="truncate text-sm font-medium">
              {drawerRow ? getRowLabel(drawerRow) : "Registro"} ·{" "}
              {drawerColumn?.label ?? "Detalle"}
            </DialogPrimitive.Title>
            <DialogPrimitive.Description className="text-xs text-muted-foreground">
              Panel de contexto de la celda seleccionada.
            </DialogPrimitive.Description>
          </div>
          <DialogPrimitive.Close
            render={
              <Button
                variant="ghost"
                size="icon-sm"
                className="-mr-1"
                aria-label="Cerrar el panel"
              />
            }
          >
            <HugeiconsIcon icon={Cancel01Icon} aria-hidden="true" />
          </DialogPrimitive.Close>
        </div>

        <div className="flex-1 space-y-4 overflow-y-auto p-4">
          <div className="rounded-lg border bg-muted/30 p-3">
            <div className="text-xs text-muted-foreground">
              Valor de la celda
            </div>
            <div className="mt-1 text-sm wrap-break-word">
              {!isEmptyValue(drawerCellValue) ? drawerCellValue : "Vacío"}
            </div>
          </div>

          <dl className="grid gap-3 text-sm">
            <div className="flex items-center justify-between gap-3">
              <dt className="text-muted-foreground">Registro</dt>
              <dd className="text-right font-medium">
                {drawerRow ? getRowLabel(drawerRow) : "-"}
              </dd>
            </div>
            <div className="flex items-center justify-between gap-3">
              <dt className="text-muted-foreground">Campo</dt>
              <dd className="text-right font-medium">
                {drawerColumn?.label ?? "-"}
              </dd>
            </div>
            <div className="flex items-center justify-between gap-3">
              <dt className="text-muted-foreground">Editable</dt>
              <dd className="text-right font-medium">
                {drawerColumn && isEditableColumn(drawerColumn.id)
                  ? "Sí"
                  : "No"}
              </dd>
            </div>
          </dl>
        </div>

        <div className="flex items-center justify-end gap-2 border-t p-3">
          <DialogPrimitive.Close render={<Button variant="secondary" />}>
            Cerrar
          </DialogPrimitive.Close>
        </div>
      </DialogPrimitive.Popup>
    </DialogPrimitive.Portal>
  );
}
