import { Outlet } from "@tanstack/react-router";
import {
  SidebarInset,
  SidebarProvider,
  SidebarTrigger,
} from "@/shared/ui/sidebar";
import { usePanelLayout } from "../../hooks/use-panel-layout";
import { PanelAvatar } from "../components/panel-avatar";
import { PanelBreadcrumb } from "../components/panel-breadcrumb";
import { PanelSidebar } from "../components/panel-sidebar";

export function PanelLayout() {
  const panel = usePanelLayout();

  return (
    <SidebarProvider>
      <PanelSidebar items={panel.navigationItems} activeId={panel.activeId} />
      <SidebarInset>
        <header className="flex h-12 items-center gap-2 border-b px-4">
          <SidebarTrigger />
          <PanelBreadcrumb activeId={panel.activeId} title={panel.pageTitle} />
          <div className="ml-auto">
            <PanelAvatar
              usuario={panel.usuario}
              onLogout={panel.onLogout}
              isLoggingOut={panel.isLoggingOut}
            />
          </div>
        </header>
        <div className="flex flex-1 flex-col p-6">
          <Outlet />
        </div>
      </SidebarInset>
    </SidebarProvider>
  );
}
