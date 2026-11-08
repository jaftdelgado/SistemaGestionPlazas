import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useNavigate, useSearch } from "@tanstack/react-router";
import * as React from "react";
import { getFieldErrors, getGeneralMessage } from "@/shared/api/errors";
import { cambiarContrasena } from "../api/sesion";
import { safeDestination } from "../domain/safe-destination";
import { establishSesion } from "./active-sesion";

type ChangePasswordForm = {
  currentPassword: string;
  newPassword: string;
  confirmation: string;
};

export function useChangePasswordPage() {
  const { redirect } = useSearch({ from: "/cambiar-contrasena" });
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [mismatch, setMismatch] = React.useState(false);

  const { mutate, isPending, error } = useMutation({
    mutationFn: cambiarContrasena,
    onSuccess: (sesion) => {
      establishSesion(queryClient, sesion);
      void navigate({ to: safeDestination(redirect) });
    },
  });

  // La confirmación solo existe en el formulario: se compara antes de enviar.
  function submit(form: ChangePasswordForm) {
    if (form.newPassword !== form.confirmation) {
      setMismatch(true);
      return;
    }
    setMismatch(false);
    mutate({
      contrasenaActual: form.currentPassword,
      contrasenaNueva: form.newPassword,
    });
  }

  const fieldErrors = getFieldErrors(error);

  return {
    submit,
    isPending,
    mismatch,
    currentError: fieldErrors["contrasenaActual"]?.join(" "),
    newError: fieldErrors["contrasenaNueva"]?.join(" "),
    generalError: getGeneralMessage(error),
  };
}
