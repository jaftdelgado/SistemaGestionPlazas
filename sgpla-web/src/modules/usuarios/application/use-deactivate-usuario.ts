import { useQueryClient } from "@tanstack/react-query";
import * as React from "react";
import { toast } from "@/components/ui/toast-stack";
import { $api } from "@/lib/api/query";
import { getProblemMessage } from "@/lib/api/problem";
import type { UsuarioRow } from "./usuario-row";

function isNotFound(error: unknown) {
  return (
    typeof error === "object" &&
    error !== null &&
    "status" in error &&
    Number(error.status) === 404
  );
}

// La baja es irreversible en la API, así que "Deshacer" ocurre antes de enviarla: el usuario se oculta de la tabla
// sin tocar la caché y la solicitud sale cuando el aviso se cierra sin deshacer.
export function useDeactivateUsuario() {
  const queryClient = useQueryClient();
  const [hiddenIds, setHiddenIds] = React.useState<ReadonlySet<string>>(
    () => new Set(),
  );
  const { mutateAsync } = $api.useMutation(
    "delete",
    "/api/v1/usuarios/cuentas/{id}",
  );

  function setHidden(id: string, hidden: boolean) {
    setHiddenIds((current) => {
      const next = new Set(current);
      if (hidden) next.add(id);
      else next.delete(id);
      return next;
    });
  }

  async function commit(usuario: UsuarioRow) {
    try {
      await mutateAsync({ params: { path: { id: Number(usuario.id) } } });
      toast.success("Usuario dado de baja", usuario.name);
    } catch (error) {
      // Un 404 significa que la cuenta ya no existe: el resultado buscado.
      if (isNotFound(error)) {
        toast.success("Usuario dado de baja", usuario.name);
      } else {
        toast.error("No se pudo dar de baja", getProblemMessage(error));
      }
    }
    // La API manda: se espera el refetch antes de soltar el id para que la fila no parpadee. Si el usuario sigue
    // existiendo (error del DELETE), reaparece sola.
    try {
      await queryClient.invalidateQueries({
        queryKey: ["get", "/api/v1/usuarios/cuentas"],
      });
    } finally {
      setHidden(usuario.id, false);
    }
  }

  function deactivate(usuario: UsuarioRow) {
    setHidden(usuario.id, true);
    toast.undoable({
      title: "Se dará de baja al usuario",
      description: usuario.name,
      onUndo: () => setHidden(usuario.id, false),
      onCommit: () => void commit(usuario),
    });
  }

  return { hiddenIds, deactivate };
}
