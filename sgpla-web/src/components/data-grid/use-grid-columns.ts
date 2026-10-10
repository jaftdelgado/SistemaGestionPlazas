import * as React from "react";
import { type DataGridColumn } from "@/components/data-grid/types";

type UseGridColumnsParams<ColumnId extends string> = {
  columns: DataGridColumn<ColumnId>[];
  minColumnWidth: number;
  controlColumnWidth: number;
};

export function useGridColumns<ColumnId extends string>({
  columns,
  minColumnWidth,
  controlColumnWidth,
}: UseGridColumnsParams<ColumnId>) {
  // Solo guarda los anchos que el usuario cambió; el resto sale de defaultWidth.
  const [resizedWidths, setResizedWidths] = React.useState<
    Partial<Record<ColumnId, number>>
  >({});

  const resizingRef = React.useRef<{
    columnId: ColumnId;
    startX: number;
    startWidth: number;
  } | null>(null);

  const columnWidths = React.useMemo(() => {
    return Object.fromEntries(
      columns.map((column) => [
        column.id,
        resizedWidths[column.id] ?? column.defaultWidth,
      ]),
    ) as Record<ColumnId, number>;
  }, [columns, resizedWidths]);

  React.useEffect(() => {
    const handlePointerMove = (event: PointerEvent) => {
      const activeResize = resizingRef.current;
      if (!activeResize) return;

      const column = columns.find((item) => item.id === activeResize.columnId);
      const resolvedMinWidth = column?.minWidth ?? minColumnWidth;
      const deltaX = event.clientX - activeResize.startX;
      const nextWidth = Math.max(
        resolvedMinWidth,
        activeResize.startWidth + deltaX,
      );

      setResizedWidths((currentWidths) => ({
        ...currentWidths,
        [activeResize.columnId]: nextWidth,
      }));
    };

    const handlePointerUp = () => {
      if (!resizingRef.current) return;
      resizingRef.current = null;
      document.body.style.userSelect = "";
      document.body.style.cursor = "";
    };

    window.addEventListener("pointermove", handlePointerMove);
    window.addEventListener("pointerup", handlePointerUp);

    return () => {
      window.removeEventListener("pointermove", handlePointerMove);
      window.removeEventListener("pointerup", handlePointerUp);
      document.body.style.userSelect = "";
      document.body.style.cursor = "";
    };
  }, [columns, minColumnWidth]);

  const gridMinWidth = React.useMemo(() => {
    const contentWidth = columns.reduce((sum, column) => {
      return sum + columnWidths[column.id];
    }, 0);

    return controlColumnWidth + contentWidth;
  }, [columnWidths, columns, controlColumnWidth]);

  const beginResize = React.useCallback(
    (event: React.PointerEvent<HTMLButtonElement>, columnId: ColumnId) => {
      event.preventDefault();
      event.stopPropagation();

      resizingRef.current = {
        columnId,
        startX: event.clientX,
        startWidth: columnWidths[columnId],
      };

      document.body.style.userSelect = "none";
      document.body.style.cursor = "col-resize";
    },
    [columnWidths],
  );

  return {
    columnWidths,
    gridMinWidth,
    beginResize,
  };
}
