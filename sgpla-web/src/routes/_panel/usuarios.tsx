import { createFileRoute } from "@tanstack/react-router";
import { UsuariosPage } from "@/modules/usuarios/pages/usuarios-page";

export const Route = createFileRoute("/_panel/usuarios")({
  component: UsuariosPage,
});
