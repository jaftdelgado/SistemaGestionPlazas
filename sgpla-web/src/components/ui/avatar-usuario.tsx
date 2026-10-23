import * as React from "react";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { colorAvatar } from "@/lib/color-avatar";
import { iniciales } from "@/lib/iniciales";
import { cn } from "@/lib/utils";

type AvatarUsuarioProps = {
  nombre: string;
  correo: string;
  className?: string;
  fallbackClassName?: string;
  children?: React.ReactNode;
};

// La sesión no trae foto de perfil: el avatar muestra siempre las iniciales sobre un color estable por usuario.
export function AvatarUsuario({
  nombre,
  correo,
  className,
  fallbackClassName,
  children,
}: AvatarUsuarioProps) {
  return (
    <Avatar className={className}>
      {/* text-box recorta el interlineado a la altura de mayúsculas para centrarlas ópticamente; leading-none es el respaldo donde no se soporta. */}
      <AvatarFallback
        aria-hidden="true"
        className={cn(
          colorAvatar(correo),
          "leading-none text-white [text-box:trim-both_cap_alphabetic]",
          fallbackClassName,
        )}
      >
        {iniciales(nombre)}
      </AvatarFallback>
      {children}
    </Avatar>
  );
}
