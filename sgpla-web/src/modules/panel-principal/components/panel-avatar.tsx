import { PersonRegular } from "@fluentui/react-icons";
import { Avatar, AvatarBadge, AvatarFallback } from "@/components/ui/avatar";

// Todavía no hay sesión: el avatar muestra un icono genérico y el estado queda fijo en línea.
export function PanelAvatar() {
  return (
    <Avatar>
      <AvatarFallback>
        <PersonRegular aria-hidden="true" className="size-4" />
      </AvatarFallback>
      <AvatarBadge
        role="img"
        aria-label="En línea"
        className="bg-green-600 dark:bg-green-800"
      />
    </Avatar>
  );
}
