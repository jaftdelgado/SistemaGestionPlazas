import { Link } from "@tanstack/react-router";
import { HatGraduationRegular } from "@fluentui/react-icons";
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
import { useNavegacion } from "../hooks/use-navegacion";

export function PanelSidebar() {
  const { elementos, activo } = useNavegacion();

  return (
    <Sidebar>
      <SidebarHeader>
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton size="lg" icon={<HatGraduationRegular />}>
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
                const { id, titulo, icono: Icono } = elemento;
                const to = "to" in elemento ? elemento.to : undefined;
                return (
                  <SidebarMenuItem key={id}>
                    <SidebarMenuButton
                      isActive={id === activo}
                      icon={<Icono />}
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
