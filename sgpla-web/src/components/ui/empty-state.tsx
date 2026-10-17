import { FolderRegular } from "@fluentui/react-icons";
import type * as React from "react";
import { cn } from "@/lib/utils";

export type EmptyStateProps = {
  title: string;
  description: string;
  action?: React.ReactNode;
  /** Icono del estado; por defecto, una carpeta. */
  icon?: React.ReactNode;
  className?: string;
  /** Nombre accesible de la región. */
  label?: string;
};

function EmptyState({
  title,
  description,
  action,
  icon,
  className,
  label,
}: EmptyStateProps) {
  return (
    <section
      data-slot="empty-state"
      aria-label={label}
      className={cn(
        "flex flex-col items-center gap-4 px-6 py-12 text-center",
        className,
      )}
    >
      <div
        aria-hidden="true"
        className="flex size-12 items-center justify-center rounded-full bg-muted text-muted-foreground [&_svg]:size-6"
      >
        {icon ?? <FolderRegular />}
      </div>
      <div className="flex flex-col items-center gap-1">
        <h3 className="text-base font-medium">{title}</h3>
        <p className="max-w-sm text-sm text-muted-foreground">{description}</p>
      </div>
      {action && <div className="flex items-center gap-2">{action}</div>}
    </section>
  );
}

export { EmptyState };
