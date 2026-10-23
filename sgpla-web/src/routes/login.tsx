import { createFileRoute, redirect } from "@tanstack/react-router";
import {
  destinoSeguro,
  haySesionGuardada,
  LoginPage,
  validarBusquedaConRedirect,
} from "@/modules/sesion";

export const Route = createFileRoute("/login")({
  validateSearch: validarBusquedaConRedirect,
  beforeLoad: ({ search }) => {
    if (haySesionGuardada()) {
      throw redirect({ to: destinoSeguro(search.redirect) });
    }
  },
  component: LoginPage,
});
