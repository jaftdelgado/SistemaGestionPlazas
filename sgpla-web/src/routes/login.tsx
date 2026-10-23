import { createFileRoute, redirect } from "@tanstack/react-router";
import { leerToken } from "@/lib/api/sesion-token";
import {
  destinoSeguro,
  validarBusquedaConRedirect,
} from "@/modules/sesion/lib/sesion-queries";
import { LoginPage } from "@/modules/sesion/pages/login-page";

export const Route = createFileRoute("/login")({
  validateSearch: validarBusquedaConRedirect,
  beforeLoad: ({ search }) => {
    if (leerToken() !== null) {
      throw redirect({ to: destinoSeguro(search.redirect) });
    }
  },
  component: LoginPage,
});
