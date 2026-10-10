import type { ComponentPropsWithRef } from "react";
import { cn } from "@/lib/utils";

export type PageHeaderProps = ComponentPropsWithRef<"header">;

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
        "min-w-0 text-3xl font-medium tracking-tight wrap-anywhere text-foreground",
        className,
      )}
    />
  );
}

export type PageHeaderActionsProps = ComponentPropsWithRef<"div">;

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
