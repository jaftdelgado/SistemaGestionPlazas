import * as React from "react";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { getAvatarColor } from "@/lib/avatar-color";
import { getInitials } from "@/lib/initials";
import { cn } from "@/lib/utils";

type UsuarioAvatarProps = {
  name: string;
  email: string;
  className?: string;
  fallbackClassName?: string;
  children?: React.ReactNode;
};

// La sesión no trae foto de perfil: el avatar muestra siempre las iniciales sobre un color estable por usuario.
export function UsuarioAvatar({
  name,
  email,
  className,
  fallbackClassName,
  children,
}: UsuarioAvatarProps) {
  return (
    <Avatar className={className}>
      {/* text-box recorta el interlineado a la altura de mayúsculas para centrarlas ópticamente; leading-none es el respaldo donde no se soporta. */}
      <AvatarFallback
        aria-hidden="true"
        className={cn(
          getAvatarColor(email),
          "leading-none text-white [text-box:trim-both_cap_alphabetic]",
          fallbackClassName,
        )}
      >
        {getInitials(name)}
      </AvatarFallback>
      {children}
    </Avatar>
  );
}
