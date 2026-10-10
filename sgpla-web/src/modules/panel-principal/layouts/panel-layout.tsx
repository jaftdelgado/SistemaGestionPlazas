import { Outlet } from "@tanstack/react-router";
import {
  SidebarInset,
  SidebarProvider,
  SidebarTrigger,
} from "@/components/ui/sidebar";
import { PanelAvatar } from "../components/panel-avatar";
import { PanelBreadcrumb } from "../components/panel-breadcrumb";
import { PanelSidebar } from "../components/panel-sidebar";

export function PanelLayout() {
  return (
    <SidebarProvider>
      <PanelSidebar />
      <SidebarInset>
        <header className="flex h-12 items-center gap-2 border-b px-4">
          <SidebarTrigger />
          <PanelBreadcrumb />
          <div className="ml-auto">
            <PanelAvatar />
          </div>
        </header>
        <div className="flex-1 p-6">
          <Outlet />
        </div>
      </SidebarInset>
    </SidebarProvider>
  );
}
