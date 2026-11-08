import { Logout01Icon } from "@hugeicons/core-free-icons";
import { HugeiconsIcon } from "@hugeicons/react";
import { AvatarBadge } from "@/shared/ui/avatar";
import { Button } from "@/shared/ui/button";
import { Separator } from "@/shared/ui/separator";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/shared/ui/tooltip";
import { UsuarioAvatar } from "@/shared/ui/usuario-avatar";

type PanelAvatarProps = {
  usuario: { name: string; email: string; rol: string };
  onLogout: () => void;
  isLoggingOut: boolean;
};

export function PanelAvatar({
  usuario,
  onLogout,
  isLoggingOut,
}: PanelAvatarProps) {
  return (
    <div className="flex items-center gap-3">
      <div className="hidden flex-col items-end leading-tight sm:flex">
        <span className="text-sm font-medium">{usuario.name}</span>
        <span className="text-xs text-muted-foreground">{usuario.rol}</span>
      </div>
      <UsuarioAvatar
        name={usuario.name}
        email={usuario.email}
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
              disabled={isLoggingOut}
              onClick={onLogout}
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
