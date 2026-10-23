import { useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { $api } from "@/lib/api/query";
import { guardarSesion } from "@/lib/api/sesion-token";
import {
  destinoSeguro,
  sesionQueryOptions,
} from "@/modules/sesion/lib/sesion-queries";

export function useCambiarContrasena(redirect: string | undefined) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  return $api.useMutation(
    "post",
    "/api/v1/usuarios/sesion/cambiar-contrasena",
    {
      onSuccess: (data) => {
        guardarSesion({ token: data.token, expiraEn: data.expiraEn });
        queryClient.setQueryData(sesionQueryOptions.queryKey, data.usuario);
        void navigate({ to: destinoSeguro(redirect) });
      },
    },
  );
}
