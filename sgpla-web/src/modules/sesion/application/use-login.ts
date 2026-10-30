import { useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { toast } from "@/components/ui/toast-stack";
import { $api } from "@/lib/api/query";
import { safeDestination } from "../domain/safe-destination";
import { establishSesion } from "./active-sesion";

export function useLogin(redirect: string | undefined) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  return $api.useMutation("post", "/api/v1/usuarios/iniciar-sesion", {
    onSuccess: (data) => {
      establishSesion(queryClient, data);
      if (data.usuario.cambioContrasenaPendiente) {
        void navigate({ to: "/cambiar-contrasena", search: { redirect } });
      } else {
        toast.success("Sesión iniciada", `Bienvenido, ${data.usuario.nombre}`);
        void navigate({ to: safeDestination(redirect) });
      }
    },
  });
}
