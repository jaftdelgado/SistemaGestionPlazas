import { Link } from "@tanstack/react-router";
import { Mortarboard01Icon } from "@hugeicons/core-free-icons";
import { HugeiconsIcon } from "@hugeicons/react";
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
} from "@/components/ui/sidebar";
import { useNavegacion } from "./use-navegacion";

export function PanelSidebar() {
  const { elementos, activo } = useNavegacion();

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
              {elementos.map((elemento) => {
                const { id, titulo, icono } = elemento;
                const to = "to" in elemento ? elemento.to : undefined;
                return (
                  <SidebarMenuItem key={id}>
                    <SidebarMenuButton
                      isActive={id === activo}
                      icon={<HugeiconsIcon icon={icono} />}
                      render={
                        to ? (props) => <Link to={to} {...props} /> : undefined
                      }
                    >
                      {titulo}
                    </SidebarMenuButton>
                  </SidebarMenuItem>
                );
              })}
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
