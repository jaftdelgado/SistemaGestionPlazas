import { createFileRoute } from "@tanstack/react-router";
import { HomePage } from "@/modules/panel-principal";

export const Route = createFileRoute("/_panel/")({
  component: HomePage,
});
