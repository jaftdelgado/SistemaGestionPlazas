import { MoreHorizontalRegular } from "@fluentui/react-icons";
import { Menu as MenuPrimitive } from "@base-ui/react/menu";
import type * as React from "react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

export type DropdownMenuItem = {
  id: string;
  label: string;
  icon?: React.ComponentType<{ className?: string }>;
  /** Resalta la acción como destructiva. */
  destructive?: boolean;
  disabled?: boolean;
  onSelect?: () => void;
};

export type DropdownMenuProps = {
  items: DropdownMenuItem[];
  /** Nombre accesible del botón que abre el menú. */
  label: string;
  /** Orden de tabulación del botón; -1 lo saca del recorrido con Tab. */
  triggerTabIndex?: number;
  className?: string;
};

function DropdownMenu({
  items,
  label,
  triggerTabIndex,
  className,
}: DropdownMenuProps) {
  return (
    <MenuPrimitive.Root>
      <MenuPrimitive.Trigger
        data-slot="dropdown-menu-trigger"
        tabIndex={triggerTabIndex}
        aria-label={label}
        render={<Button variant="ghost" size="icon-sm" className={className} />}
      >
        <MoreHorizontalRegular aria-hidden="true" />
      </MenuPrimitive.Trigger>
      <MenuPrimitive.Portal>
        <MenuPrimitive.Positioner
          sideOffset={4}
          align="end"
          className="isolate z-50"
        >
          <MenuPrimitive.Popup
            data-slot="dropdown-menu-content"
            className="min-w-44 origin-(--transform-origin) rounded-2xl bg-popover p-1 text-popover-foreground shadow-border outline-none data-open:animate-in data-open:fade-in-0 data-open:zoom-in-95 data-closed:animate-out data-closed:fade-out-0 data-closed:zoom-out-95"
          >
            {items.map(
              ({ id, label: etiqueta, icon: Icon, destructive, ...item }) => (
                <MenuPrimitive.Item
                  key={id}
                  disabled={item.disabled}
                  onClick={item.onSelect}
                  className={cn(
                    "flex min-h-8 w-full cursor-default items-center gap-2 rounded-full py-1 pr-3 pl-3 text-left text-base outline-none select-none data-highlighted:bg-muted md:text-sm data-disabled:pointer-events-none data-disabled:opacity-50 [&_svg]:size-4 [&_svg]:shrink-0",
                    destructive && "text-destructive",
                  )}
                >
                  {Icon ? <Icon aria-hidden="true" /> : null}
                  {etiqueta}
                </MenuPrimitive.Item>
              ),
            )}
          </MenuPrimitive.Popup>
        </MenuPrimitive.Positioner>
      </MenuPrimitive.Portal>
    </MenuPrimitive.Root>
  );
}

export { DropdownMenu };
