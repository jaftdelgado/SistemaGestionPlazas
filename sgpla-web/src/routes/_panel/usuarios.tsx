import { createFileRoute } from "@tanstack/react-router";
import { UsuariosPage, validateUsuariosSearch } from "@/modules/usuarios";

export const Route = createFileRoute("/_panel/usuarios")({
  validateSearch: validateUsuariosSearch,
  component: UsuariosPage,
});
