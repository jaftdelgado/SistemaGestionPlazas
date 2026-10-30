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
import { useNavigation } from "./use-navigation";

export function PanelSidebar() {
  const { items, active } = useNavigation();

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
              {items.map((item) => {
                const { id, title, icon } = item;
                const to = "to" in item ? item.to : undefined;
                return (
                  <SidebarMenuItem key={id}>
                    <SidebarMenuButton
                      isActive={id === active}
                      icon={<HugeiconsIcon icon={icon} />}
                      render={
                        to ? (props) => <Link to={to} {...props} /> : undefined
                      }
                    >
                      {title}
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
