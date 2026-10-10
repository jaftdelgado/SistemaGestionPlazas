import {
  AlertRegular,
  BookOpenRegular,
  HatGraduationRegular,
  HomeRegular,
  MoreHorizontalRegular,
  EditRegular,
  SettingsRegular,
  DeleteRegular,
} from "@fluentui/react-icons";
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

const opciones = {
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
  const [ofertasAbierto, setOfertasAbierto] = useState(true);

  return (
    <div className="flex w-full flex-col gap-3">
      <div className="flex flex-wrap gap-x-6 gap-y-2">
        {(Object.keys(opciones) as (keyof Opciones)[]).map((clave) => (
          <div key={clave} className="flex items-center gap-1">
            <span className="mr-1 text-xs text-muted-foreground">{clave}</span>
            {opciones[clave].map((valor) => (
              <Button
                key={valor}
                size="sm"
                variant={config[clave] === valor ? "default" : "secondary"}
                onClick={() =>
                  setConfig((actual) => ({ ...actual, [clave]: valor }))
                }
              >
                {valor}
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
                    <SidebarMenuItem>
                      <SidebarMenuButton isActive icon={<HomeRegular />}>
                        Inicio
                      </SidebarMenuButton>
                    </SidebarMenuItem>
                    <SidebarMenuItem>
                      <SidebarMenuButton
                        icon={<AlertRegular />}
                        status="unread"
                      >
                        Notificaciones
                      </SidebarMenuButton>
                      <SidebarMenuBadge>12</SidebarMenuBadge>
                    </SidebarMenuItem>
                    <SidebarMenuItem>
                      <SidebarMenuButton
                        icon={<BookOpenRegular />}
                        aria-expanded={ofertasAbierto}
                        onClick={() => setOfertasAbierto((valor) => !valor)}
                      >
                        Ofertas educativas
                      </SidebarMenuButton>
                      <SidebarMenuActions showOnHover>
                        <SidebarMenuAction aria-label="Editar">
                          <EditRegular />
                        </SidebarMenuAction>
                        <SidebarMenuAction aria-label="Eliminar">
                          <DeleteRegular />
                        </SidebarMenuAction>
                        <SidebarMenuAction aria-label="Más">
                          <MoreHorizontalRegular />
                        </SidebarMenuAction>
                      </SidebarMenuActions>
                      <SidebarMenuSub open={ofertasAbierto}>
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
                    {[0, 1, 2].map((indice) => (
                      <SidebarMenuItem key={indice}>
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
                  <SidebarMenuButton icon={<SettingsRegular />} status="idle">
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
