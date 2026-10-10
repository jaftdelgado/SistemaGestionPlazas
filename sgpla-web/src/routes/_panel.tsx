import { createFileRoute } from "@tanstack/react-router";
import { PanelLayout } from "@/modules/panel-principal/layouts/panel-layout";

export const Route = createFileRoute("/_panel")({
  component: PanelLayout,
});
