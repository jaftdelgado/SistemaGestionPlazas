import { createFileRoute, redirect } from "@tanstack/react-router";
import { borrarSesion, leerToken } from "@/lib/api/sesion-token";
import { PanelLayout } from "@/modules/panel-principal/layouts/panel-layout";
import { sesionQueryOptions } from "@/modules/sesion/lib/sesion-queries";

export const Route = createFileRoute("/_panel")({
  beforeLoad: async ({ context, location }) => {
    if (leerToken() === null) {
      throw redirect({ to: "/login", search: { redirect: location.href } });
    }
    let sesion;
    try {
      sesion = await context.queryClient.ensureQueryData(sesionQueryOptions);
    } catch {
      borrarSesion();
      throw redirect({ to: "/login", search: { redirect: location.href } });
    }
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
