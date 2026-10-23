import { createFileRoute, redirect } from "@tanstack/react-router";
import { borrarSesion, leerToken } from "@/lib/api/sesion-token";
import {
  destinoSeguro,
  sesionQueryOptions,
} from "@/modules/sesion/lib/sesion-queries";
import { CambiarContrasenaPage } from "@/modules/sesion/pages/cambiar-contrasena-page";

export const Route = createFileRoute("/cambiar-contrasena")({
  validateSearch: (search: Record<string, unknown>): { redirect?: string } => ({
    redirect: typeof search.redirect === "string" ? search.redirect : undefined,
  }),
  beforeLoad: async ({ context, search }) => {
    if (leerToken() === null) {
      throw redirect({ to: "/login" });
    }
    let sesion;
    try {
      sesion = await context.queryClient.ensureQueryData(sesionQueryOptions);
    } catch {
      borrarSesion();
      throw redirect({ to: "/login" });
    }
    if (!sesion.cambioContrasenaPendiente) {
      throw redirect({ to: destinoSeguro(search.redirect) });
    }
  },
  component: CambiarContrasenaPage,
});
