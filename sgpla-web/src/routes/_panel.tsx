import { createFileRoute, redirect } from "@tanstack/react-router";
import { PanelLayout } from "@/modules/panel-principal";
import { exigirSesion } from "@/modules/sesion";

export const Route = createFileRoute("/_panel")({
  beforeLoad: async ({ context, location }) => {
    const sesion = await exigirSesion(context.queryClient, location.href);
    if (sesion.cambioContrasenaPendiente) {
      throw redirect({
        to: "/cambiar-contrasena",
        search: { redirect: location.href },
      });
    }
    return { sesion };
  },
  component: PanelLayout,
});
