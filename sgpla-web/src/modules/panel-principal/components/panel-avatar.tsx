import { Logout01Icon } from "@hugeicons/core-free-icons";
import { HugeiconsIcon } from "@hugeicons/react";
import { useSuspenseQuery } from "@tanstack/react-query";
import { AvatarBadge } from "@/components/ui/avatar";
import { UsuarioAvatar } from "@/components/ui/usuario-avatar";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { sesionQueryOptions, useLogout } from "@/modules/sesion";

export function PanelAvatar() {
  const { data: usuario } = useSuspenseQuery(sesionQueryOptions);
  const cerrarSesion = useLogout();
  return (
    <div className="flex items-center gap-3">
      <div className="hidden flex-col items-end leading-tight sm:flex">
        <span className="text-sm font-medium">{usuario.nombre}</span>
        <span className="text-xs text-muted-foreground">
          {usuario.rol.nombre}
        </span>
      </div>
      <UsuarioAvatar
        name={usuario.nombre}
        email={usuario.correo}
        className="size-9"
      >
        <AvatarBadge
          role="img"
          aria-label="En línea"
          className="bg-green-600 dark:bg-green-800"
        />
      </UsuarioAvatar>
      <Separator orientation="vertical" className="h-5 self-auto" />
      <Tooltip>
        <TooltipTrigger
          render={
            <Button
              variant="ghost"
              size="icon-sm"
              aria-label="Cerrar sesión"
              disabled={cerrarSesion.isPending}
              onClick={() => cerrarSesion.mutate({})}
            />
          }
        >
          <HugeiconsIcon icon={Logout01Icon} aria-hidden="true" />
        </TooltipTrigger>
        <TooltipContent side="bottom">Cerrar sesión</TooltipContent>
      </Tooltip>
    </div>
  );
}
