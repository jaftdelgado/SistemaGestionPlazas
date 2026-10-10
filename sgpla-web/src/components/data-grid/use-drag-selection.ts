import * as React from "react";

interface UseDragSelectionOptions {
  gridRef: React.RefObject<HTMLElement | null>;
  rows: readonly { id: string }[];
  selectedRowIds: string[];
  onSelectedRowIdsChange: (rowIds: string[]) => void;
}

// Arrastrar desde la celda de selección marca o desmarca el rango de filas recorrido.
// La acción la define la fila inicial: si estaba sin marcar el arrastre marca, y si no, desmarca.
function useDragSelection({
  gridRef,
  rows,
  selectedRowIds,
  onSelectedRowIdsChange,
}: UseDragSelectionOptions) {
  const cleanupRef = React.useRef<(() => void) | null>(null);

  React.useEffect(() => () => cleanupRef.current?.(), []);

  return React.useCallback(
    (anchorIndex: number) => {
      const anchorRow = rows[anchorIndex];
      if (!anchorRow) return;

      cleanupRef.current?.();

      const initialSelection = selectedRowIds;
      const shouldSelect = !initialSelection.includes(anchorRow.id);
      let lastIndex = anchorIndex;
      let moved = false;

      const previousUserSelect = document.body.style.userSelect;
      document.body.style.userSelect = "none";

      // Un evento por frame; mientras el puntero siga dentro de la fila actual no se consulta el DOM.
      let frame = 0;
      let pointerY = 0;
      let pointerX = 0;
      let currentRowBounds: { top: number; bottom: number } | null = null;

      const updateSelection = () => {
        frame = 0;
        if (
          currentRowBounds &&
          pointerY >= currentRowBounds.top &&
          pointerY < currentRowBounds.bottom
        ) {
          return;
        }

        const rowElement = document
          .elementFromPoint(pointerX, pointerY)
          ?.closest<HTMLElement>('[data-grid-row="true"]');
        if (!rowElement || !gridRef.current?.contains(rowElement)) return;

        const rowIndex = Number(rowElement.dataset.rowIndex);
        if (Number.isNaN(rowIndex)) return;

        const bounds = rowElement.getBoundingClientRect();
        currentRowBounds = { top: bounds.top, bottom: bounds.bottom };
        if (rowIndex === lastIndex) return;
        lastIndex = rowIndex;
        moved = true;

        const rangeIds = new Set(
          rows
            .slice(
              Math.min(anchorIndex, rowIndex),
              Math.max(anchorIndex, rowIndex) + 1,
            )
            .map((row) => row.id),
        );
        const untouched = initialSelection.filter((id) => !rangeIds.has(id));
        onSelectedRowIdsChange(
          shouldSelect ? [...untouched, ...rangeIds] : untouched,
        );
      };

      const handlePointerMove = (event: PointerEvent) => {
        pointerX = event.clientX;
        pointerY = event.clientY;
        if (frame === 0) frame = requestAnimationFrame(updateSelection);
      };

      // Al desplazarse la página los límites guardados dejan de ser válidos.
      const invalidateBounds = () => {
        currentRowBounds = null;
      };

      const blockClick = (event: MouseEvent) => {
        event.stopPropagation();
        event.preventDefault();
      };

      const cleanup = () => {
        window.removeEventListener("pointermove", handlePointerMove);
        window.removeEventListener("pointerup", finish);
        window.removeEventListener("pointercancel", finish);
        window.removeEventListener("scroll", invalidateBounds, true);
        cancelAnimationFrame(frame);
        document.body.style.userSelect = previousUserSelect;
        cleanupRef.current = null;
      };

      function finish() {
        cleanup();
        if (!moved) return;
        // El clic posterior al arrastre no debe alternar de nuevo la fila inicial.
        window.addEventListener("click", blockClick, {
          capture: true,
          once: true,
        });
        setTimeout(
          () => window.removeEventListener("click", blockClick, true),
          0,
        );
      }

      window.addEventListener("pointermove", handlePointerMove);
      window.addEventListener("pointerup", finish);
      window.addEventListener("pointercancel", finish);
      window.addEventListener("scroll", invalidateBounds, true);
      cleanupRef.current = cleanup;
    },
    [gridRef, onSelectedRowIdsChange, rows, selectedRowIds],
  );
}

export { useDragSelection };
