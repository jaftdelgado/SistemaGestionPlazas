import { createFileRoute, redirect } from "@tanstack/react-router";
import { PanelLayout } from "@/modules/panel-principal/layouts/panel-layout";
import { exigirSesion } from "@/modules/sesion/lib/sesion-queries";

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
