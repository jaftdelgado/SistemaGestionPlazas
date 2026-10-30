import { useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { $api } from "@/lib/api/query";
import { safeDestination } from "../domain/safe-destination";
import { establishSesion } from "./active-sesion";

export function useChangePassword(redirect: string | undefined) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  return $api.useMutation(
    "post",
    "/api/v1/usuarios/sesion/cambiar-contrasena",
    {
      onSuccess: (data) => {
        establishSesion(queryClient, data);
        void navigate({ to: safeDestination(redirect) });
      },
    },
  );
}
