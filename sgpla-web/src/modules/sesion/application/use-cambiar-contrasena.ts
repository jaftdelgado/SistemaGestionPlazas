import { useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { $api } from "@/lib/api/query";
import { destinoSeguro } from "../domain/destino-seguro";
import { establecerSesion } from "./sesion-activa";

export function useCambiarContrasena(redirect: string | undefined) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  return $api.useMutation(
    "post",
    "/api/v1/usuarios/sesion/cambiar-contrasena",
    {
      onSuccess: (data) => {
        establecerSesion(queryClient, data);
        void navigate({ to: destinoSeguro(redirect) });
      },
    },
  );
}
