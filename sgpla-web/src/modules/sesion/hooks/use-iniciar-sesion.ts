import { useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { $api } from "@/lib/api/query";
import { guardarSesion } from "@/lib/api/sesion-token";
import {
  destinoSeguro,
  sesionQueryOptions,
} from "@/modules/sesion/lib/sesion-queries";

export function useIniciarSesion(redirect: string | undefined) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  return $api.useMutation("post", "/api/v1/usuarios/iniciar-sesion", {
    onSuccess: (data) => {
      guardarSesion({ token: data.token, expiraEn: data.expiraEn });
      queryClient.setQueryData(sesionQueryOptions.queryKey, data.usuario);
      if (data.usuario.cambioContrasenaPendiente) {
        void navigate({ to: "/cambiar-contrasena", search: { redirect } });
      } else {
        void navigate({ to: destinoSeguro(redirect) });
      }
    },
  });
}
