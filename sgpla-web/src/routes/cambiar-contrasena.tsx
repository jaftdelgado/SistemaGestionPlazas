import { createFileRoute, redirect } from "@tanstack/react-router";
import {
  CambiarContrasenaPage,
  destinoSeguro,
  exigirSesion,
  validarBusquedaConRedirect,
} from "@/modules/sesion";

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
