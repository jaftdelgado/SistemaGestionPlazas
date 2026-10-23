import { createFileRoute, redirect } from "@tanstack/react-router";
import {
  destinoSeguro,
  exigirSesion,
  validarBusquedaConRedirect,
} from "@/modules/sesion/lib/sesion-queries";
import { CambiarContrasenaPage } from "@/modules/sesion/pages/cambiar-contrasena-page";

export const Route = createFileRoute("/cambiar-contrasena")({
  validateSearch: validarBusquedaConRedirect,
  beforeLoad: async ({ context, search }) => {
    const sesion = await exigirSesion(context.queryClient);
    if (!sesion.cambioContrasenaPendiente) {
      throw redirect({ to: destinoSeguro(search.redirect) });
    }
  },
  component: CambiarContrasenaPage,
});
