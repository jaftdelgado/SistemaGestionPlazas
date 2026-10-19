import * as React from "react";
import {
  type DataGridRowBase,
  type EditingCell,
} from "@/components/data-grid/types";

type UseGridEditingParams<
  Row extends DataGridRowBase,
  ColumnId extends string,
> = {
  inputRef: React.RefObject<HTMLInputElement | null>;
  isEditableColumn: (columnId: ColumnId) => boolean;
  getCellEditValue?: (row: Row, columnId: ColumnId) => string;
  applyCellEdit?: (row: Row, columnId: ColumnId, nextValue: string) => Row;
  commitRows: (updater: (currentRows: Row[]) => Row[]) => void;
};

export function useGridEditing<
  Row extends DataGridRowBase,
  ColumnId extends string,
>({
  inputRef,
  isEditableColumn,
  getCellEditValue,
  applyCellEdit,
  commitRows,
}: UseGridEditingParams<Row, ColumnId>) {
  const [editingCell, setEditingCell] =
    React.useState<EditingCell<ColumnId> | null>(null);
  const [draftValue, setDraftValue] = React.useState("");

  React.useEffect(() => {
    if (!editingCell) return;
    inputRef.current?.focus();
    inputRef.current?.select();
  }, [editingCell, inputRef]);

  const canEditColumn = React.useCallback(
    (columnId: ColumnId) => {
      return (
        getCellEditValue !== undefined &&
        applyCellEdit !== undefined &&
        isEditableColumn(columnId)
      );
    },
    [applyCellEdit, getCellEditValue, isEditableColumn],
  );

  const startEditing = React.useCallback(
    (row: Row, columnId: ColumnId) => {
      if (!getCellEditValue || !canEditColumn(columnId)) return;
      setEditingCell({ rowId: row.id, columnId });
      setDraftValue(getCellEditValue(row, columnId));
    },
    [canEditColumn, getCellEditValue],
  );

  const commitEdit = React.useCallback(() => {
    if (!editingCell || !applyCellEdit) return;

    commitRows((currentRows) =>
      currentRows.map((row) => {
        if (row.id !== editingCell.rowId) return row;
        return applyCellEdit(row, editingCell.columnId, draftValue);
      }),
    );
    setEditingCell(null);
    setDraftValue("");
  }, [applyCellEdit, commitRows, draftValue, editingCell]);

  const cancelEdit = React.useCallback(() => {
    setEditingCell(null);
    setDraftValue("");
  }, []);

  return {
    editingCell,
    draftValue,
    setDraftValue,
    canEditColumn,
    startEditing,
    commitEdit,
    cancelEdit,
  };
}
