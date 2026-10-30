import {
  BookOpen01Icon,
  Delete01Icon,
  Edit01Icon,
  Home01Icon,
  MoreHorizontalIcon,
  Mortarboard01Icon,
  Notification01Icon,
  Settings01Icon,
} from "@hugeicons/core-free-icons";
import { HugeiconsIcon } from "@hugeicons/react";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarInset,
  SidebarMenu,
  SidebarMenuAction,
  SidebarMenuActions,
  SidebarMenuBadge,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarMenuSkeleton,
  SidebarMenuSub,
  SidebarMenuSubButton,
  SidebarMenuSubItem,
  SidebarProvider,
  SidebarTrigger,
} from "@/components/ui/sidebar";

type Opciones = {
  side: "left" | "right";
  variant: "sidebar" | "floating" | "inset";
  peek: "none" | "hover" | "click";
  collapsible: "offcanvas" | "none";
};

const options = {
  side: ["left", "right"],
  variant: ["sidebar", "floating", "inset"],
  peek: ["none", "hover", "click"],
  collapsible: ["offcanvas", "none"],
} as const;

export function SidebarDemo() {
  const [config, setConfig] = useState<Opciones>({
    side: "left",
    variant: "sidebar",
    peek: "hover",
    collapsible: "offcanvas",
  });
  const [offersOpen, setOffersOpen] = useState(true);

  return (
    <div className="flex w-full flex-col gap-3">
      <div className="flex flex-wrap gap-x-6 gap-y-2">
        {(Object.keys(options) as (keyof Opciones)[]).map((itemKey) => (
          <div key={itemKey} className="flex items-center gap-1">
            <span className="mr-1 text-xs text-muted-foreground">
              {itemKey}
            </span>
            {options[itemKey].map((value) => (
              <Button
                key={value}
                size="sm"
                variant={config[itemKey] === value ? "default" : "secondary"}
                onClick={() =>
                  setConfig((actual) => ({ ...actual, [itemKey]: value }))
                }
              >
                {value}
              </Button>
            ))}
          </div>
        ))}
      </div>
      {/* El transform crea el bloque contenedor de los elementos fixed del Sidebar. */}
      <div className="h-[28rem] w-full transform-gpu overflow-hidden rounded-xl border">
        <SidebarProvider
          key={`${config.side}-${config.peek}`}
          persist={false}
          side={config.side}
          peek={config.peek}
          className="h-full min-h-0"
        >
          {config.side === "right" && <Contenido />}
          <Sidebar
            variant={config.variant}
            collapsible={config.collapsible}
            side={config.side}
          >
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
                    <SidebarMenuItem>
                      <SidebarMenuButton
                        isActive
                        icon={<HugeiconsIcon icon={Home01Icon} />}
                      >
                        Inicio
                      </SidebarMenuButton>
                    </SidebarMenuItem>
                    <SidebarMenuItem>
                      <SidebarMenuButton
                        icon={<HugeiconsIcon icon={Notification01Icon} />}
                        status="unread"
                      >
                        Notificaciones
                      </SidebarMenuButton>
                      <SidebarMenuBadge>12</SidebarMenuBadge>
                    </SidebarMenuItem>
                    <SidebarMenuItem>
                      <SidebarMenuButton
                        icon={<HugeiconsIcon icon={BookOpen01Icon} />}
                        aria-expanded={offersOpen}
                        onClick={() => setOffersOpen((value) => !value)}
                      >
                        Ofertas educativas
                      </SidebarMenuButton>
                      <SidebarMenuActions showOnHover>
                        <SidebarMenuAction aria-label="Editar">
                          <HugeiconsIcon icon={Edit01Icon} />
                        </SidebarMenuAction>
                        <SidebarMenuAction aria-label="Eliminar">
                          <HugeiconsIcon icon={Delete01Icon} />
                        </SidebarMenuAction>
                        <SidebarMenuAction aria-label="Más">
                          <HugeiconsIcon icon={MoreHorizontalIcon} />
                        </SidebarMenuAction>
                      </SidebarMenuActions>
                      <SidebarMenuSub open={offersOpen}>
                        <SidebarMenuSubItem>
                          <SidebarMenuSubButton isActive={false}>
                            Licenciaturas
                          </SidebarMenuSubButton>
                        </SidebarMenuSubItem>
                        <SidebarMenuSubItem>
                          <SidebarMenuSubButton status="active">
                            Posgrados
                          </SidebarMenuSubButton>
                        </SidebarMenuSubItem>
                      </SidebarMenuSub>
                    </SidebarMenuItem>
                  </SidebarMenu>
                </SidebarGroupContent>
              </SidebarGroup>
              <SidebarGroup collapsible>
                <SidebarGroupLabel>Cargando</SidebarGroupLabel>
                <SidebarGroupContent>
                  <SidebarMenu>
                    {[0, 1, 2].map((index) => (
                      <SidebarMenuItem key={index}>
                        <SidebarMenuSkeleton showIcon />
                      </SidebarMenuItem>
                    ))}
                  </SidebarMenu>
                </SidebarGroupContent>
              </SidebarGroup>
            </SidebarContent>
            <SidebarFooter>
              <SidebarMenu>
                <SidebarMenuItem>
                  <SidebarMenuButton
                    icon={<HugeiconsIcon icon={Settings01Icon} />}
                    status="idle"
                  >
                    Configuración
                  </SidebarMenuButton>
                </SidebarMenuItem>
              </SidebarMenu>
            </SidebarFooter>
          </Sidebar>
          {config.side === "left" && <Contenido />}
        </SidebarProvider>
      </div>
    </div>
  );
}

function Contenido() {
  return (
    <SidebarInset>
      <header className="flex h-12 items-center gap-2 border-b px-3">
        <SidebarTrigger />
        <span className="text-sm text-muted-foreground">
          Atajo: [ o ] con el foco dentro
        </span>
      </header>
      <div className="p-4 text-sm text-muted-foreground">
        Arrastra el borde de la barra para redimensionar; un clic la colapsa.
      </div>
    </SidebarInset>
  );
}
