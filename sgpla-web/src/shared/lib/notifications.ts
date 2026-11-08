import { Toast } from "@base-ui/react/toast";

export type NotificationType = "success" | "info" | "error" | "warning";

// Gestor global: permite avisar desde hooks y callbacks sin depender del contexto de React. ToastStack lo muestra.
export const toastManager = Toast.createToastManager();

// Tiempo por defecto antes de que un aviso se cierre solo; la barra de progreso lo usa si el aviso no trae el suyo.
export const DEFAULT_TOAST_TIMEOUT = 4000;
const UNDO_TIMEOUT = 4000;

function add(type: NotificationType) {
  return (title: string, description?: string) =>
    toastManager.add({ type: type, title, description });
}

type UndoableOptions = {
  title: string;
  description?: string;
  // Se ejecuta si la persona deshace dentro del plazo.
  onUndo?: () => void;
  // Se ejecuta cuando el aviso se cierra sin que se haya deshecho (vence el plazo o se descarta).
  onCommit: () => void;
};

// Aviso con barra de progreso y "Deshacer": la acción real se difiere hasta que el aviso se cierra.
function undoable({ title, description, onUndo, onCommit }: UndoableOptions) {
  let undone = false;
  const id = toastManager.add({
    type: "info",
    title,
    description,
    timeout: UNDO_TIMEOUT,
    data: { progress: true },
    actionProps: {
      children: "Deshacer",
      onClick: () => {
        undone = true;
        onUndo?.();
        toastManager.close(id);
      },
    },
    onClose: () => {
      if (!undone) {
        onCommit();
      }
    },
  });
  return id;
}

export const notify = {
  success: add("success"),
  info: add("info"),
  error: add("error"),
  warning: add("warning"),
  undoable,
};
