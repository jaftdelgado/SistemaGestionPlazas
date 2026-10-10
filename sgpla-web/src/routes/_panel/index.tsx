import { createFileRoute } from "@tanstack/react-router";
import { InicioPage } from "@/modules/panel-principal/pages/inicio-page";

export const Route = createFileRoute("/_panel/")({
  component: InicioPage,
});
