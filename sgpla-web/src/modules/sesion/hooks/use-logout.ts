import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { cerrarSesion } from "../api/sesion";
import { discardSesion } from "./active-sesion";

export function useLogout() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const { mutate, isPending } = useMutation({
    mutationFn: cerrarSesion,
    // La sesión local se descarta aunque la API falle: un 401 ya significa que no era válida y, ante otro error, el usuario no debe quedar atrapado.
    onSettled: () => {
      discardSesion(queryClient);
      void navigate({ to: "/login" });
    },
  });

  return { logout: () => mutate(), isLoggingOut: isPending };
}
