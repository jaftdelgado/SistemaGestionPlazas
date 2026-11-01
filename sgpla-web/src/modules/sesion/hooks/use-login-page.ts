import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useNavigate, useSearch } from "@tanstack/react-router";
import { getFieldErrors, getGeneralMessage } from "@/shared/api/errors";
import { notify } from "@/shared/lib/notifications";
import { iniciarSesion } from "../api/sesion";
import { safeDestination } from "../domain/safe-destination";
import type { Credenciales } from "../domain/sesion";
import { establishSesion } from "./active-sesion";

export function useLoginPage() {
  const { redirect } = useSearch({ from: "/login" });
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const { mutate, isPending, error } = useMutation({
    mutationFn: iniciarSesion,
    onSuccess: (sesion) => {
      establishSesion(queryClient, sesion);
      if (sesion.usuario.cambioContrasenaPendiente) {
        void navigate({ to: "/cambiar-contrasena", search: { redirect } });
      } else {
        notify.success(
          "Sesión iniciada",
          `Bienvenido, ${sesion.usuario.nombre}`,
        );
        void navigate({ to: safeDestination(redirect) });
      }
    },
  });

  const fieldErrors = getFieldErrors(error);

  return {
    submit: (credenciales: Credenciales) => mutate(credenciales),
    isPending,
    usernameError: fieldErrors["correo"]?.join(" "),
    passwordError: fieldErrors["contrasena"]?.join(" "),
    generalError: getGeneralMessage(error),
  };
}
