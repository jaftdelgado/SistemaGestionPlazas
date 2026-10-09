import type { ComponentPropsWithRef } from "react";
import { cn } from "@/lib/utils";

export type PageHeaderProps = ComponentPropsWithRef<"header">;

/** Encabezado de página: el título a la izquierda y las acciones a la derecha. */
export function PageHeader({ className, ...props }: PageHeaderProps) {
  return (
    <header
      data-slot="page-header"
      {...props}
      className={cn(
        "flex flex-wrap items-center justify-between gap-x-4 gap-y-2",
        className,
      )}
    />
  );
}

export type PageHeaderTitleProps = ComponentPropsWithRef<"h1">;

export function PageHeaderTitle({ className, ...props }: PageHeaderTitleProps) {
  return (
    <h1
      data-slot="page-header-title"
      {...props}
      className={cn(
        "min-w-0 text-2xl font-semibold tracking-tight [overflow-wrap:anywhere] text-foreground",
        className,
      )}
    />
  );
}

export type PageHeaderActionsProps = ComponentPropsWithRef<"div">;

/** Contenedor de las acciones de la página (botones, menús…), alineado a la derecha. */
export function PageHeaderActions({
  className,
  ...props
}: PageHeaderActionsProps) {
  return (
    <div
      data-slot="page-header-actions"
      {...props}
      className={cn("ml-auto flex shrink-0 items-center gap-2", className)}
    />
  );
}
