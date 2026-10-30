import { createFileRoute, redirect } from "@tanstack/react-router";
import {
  ChangePasswordPage,
  safeDestination,
  requireSesion,
  validateRedirectSearch,
} from "@/modules/sesion";

export const Route = createFileRoute("/cambiar-contrasena")({
  validateSearch: validateRedirectSearch,
  beforeLoad: async ({ context, search }) => {
    const sesion = await requireSesion(context.queryClient);
    if (!sesion.cambioContrasenaPendiente) {
      throw redirect({ to: safeDestination(search.redirect) });
    }
  },
  component: ChangePasswordPage,
});
