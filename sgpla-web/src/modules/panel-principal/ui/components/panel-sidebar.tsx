import {
  BookOpen01Icon,
  Home01Icon,
  Mortarboard01Icon,
  Settings01Icon,
  UserGroupIcon,
} from "@hugeicons/core-free-icons";
import { HugeiconsIcon, type IconSvgElement } from "@hugeicons/react";
import { Link, type LinkProps } from "@tanstack/react-router";
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
} from "@/shared/ui/sidebar";

type PanelSectionId = "inicio" | "ofertas" | "usuarios" | "configuracion";

// Una sección nueva no compila hasta que tenga su ícono.
const ICONS: Record<PanelSectionId, IconSvgElement> = {
  inicio: Home01Icon,
  ofertas: BookOpen01Icon,
  usuarios: UserGroupIcon,
  configuracion: Settings01Icon,
};

type PanelSidebarItem = {
  id: PanelSectionId;
  title: string;
  to?: LinkProps["to"];
};

type PanelSidebarProps = {
  items: readonly PanelSidebarItem[];
  activeId: PanelSectionId;
};

export function PanelSidebar({ items, activeId }: PanelSidebarProps) {
  return (
    <Sidebar>
      <SidebarHeader>
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton
              size="lg"
              icon={<HugeiconsIcon icon={Mortarboard01Icon} />}
            >
              SGPLa
            </SidebarMenuButton>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarHeader>
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupLabel>General</SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              {items.map(({ id, title, to }) => (
                <SidebarMenuItem key={id}>
                  <SidebarMenuButton
                    isActive={id === activeId}
                    icon={<HugeiconsIcon icon={ICONS[id]} />}
                    render={
                      to ? (props) => <Link to={to} {...props} /> : undefined
                    }
                  >
                    {title}
                  </SidebarMenuButton>
                </SidebarMenuItem>
              ))}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>
      <SidebarFooter>
        <p className="px-2 text-xs text-muted-foreground">
          Usuario (placeholder)
        </p>
      </SidebarFooter>
    </Sidebar>
  );
}
