import { useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { $api } from "@/lib/api/query";
import { descartarSesion } from "./sesion-activa";

export function useCerrarSesion() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  return $api.useMutation("post", "/api/v1/usuarios/cerrar-sesion", {
    // La sesión local se descarta aunque la API falle: un 401 ya significa que no era válida y, ante otro error, el usuario no debe quedar atrapado.
    onSettled: () => {
      descartarSesion(queryClient);
      void navigate({ to: "/login" });
    },
  });
}
