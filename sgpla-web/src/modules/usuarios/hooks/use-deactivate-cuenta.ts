import { useMutation, useQueryClient } from "@tanstack/react-query";
import * as React from "react";
import { getProblemMessage, hasStatus } from "@/shared/api/errors";
import { notify } from "@/shared/lib/notifications";
import { cuentasKeys, deleteCuenta } from "../api/cuentas";

type CuentaToDeactivate = { id: number; nombre: string };

// La baja es irreversible en la API, así que "Deshacer" ocurre antes de enviarla: la cuenta se oculta de la tabla
// sin tocar la caché y la solicitud sale cuando el aviso se cierra sin deshacer.
export function useDeactivateCuenta() {
  const queryClient = useQueryClient();
  const [hiddenIds, setHiddenIds] = React.useState<ReadonlySet<number>>(
    () => new Set(),
  );
  const { mutateAsync } = useMutation({ mutationFn: deleteCuenta });

  function setHidden(id: number, hidden: boolean) {
    setHiddenIds((current) => {
      const next = new Set(current);
      if (hidden) next.add(id);
      else next.delete(id);
      return next;
    });
  }

  async function commit(cuenta: CuentaToDeactivate) {
    try {
      await mutateAsync(cuenta.id);
      notify.success("Usuario dado de baja", cuenta.nombre);
    } catch (error) {
      // Un 404 significa que la cuenta ya no existe: el resultado buscado.
      if (hasStatus(error, 404)) {
        notify.success("Usuario dado de baja", cuenta.nombre);
      } else {
        notify.error("No se pudo dar de baja", getProblemMessage(error));
      }
    }
    // La API manda: se espera el refetch antes de soltar el id para que la fila no parpadee. Si la cuenta sigue
    // existiendo (error del DELETE), reaparece sola.
    try {
      await queryClient.invalidateQueries({ queryKey: cuentasKeys.all });
    } finally {
      setHidden(cuenta.id, false);
    }
  }

  function deactivate(cuenta: CuentaToDeactivate) {
    setHidden(cuenta.id, true);
    notify.undoable({
      title: "Se dará de baja al usuario",
      description: cuenta.nombre,
      onUndo: () => setHidden(cuenta.id, false),
      onCommit: () => void commit(cuenta),
    });
  }

  return { hiddenIds, deactivate };
}
