import {
  ChevronRightRegular,
  MoreHorizontalRegular,
} from "@fluentui/react-icons";
import {
  AnimatePresence,
  LayoutGroup,
  motion,
  useIsPresent,
  useReducedMotion,
  type HTMLMotionProps,
} from "motion/react";
import {
  Children,
  useEffect,
  useLayoutEffect,
  useRef,
  useState,
  type ReactNode,
  useId,
  type ComponentPropsWithRef,
  type ReactElement,
  type Ref,
  type RefObject,
} from "react";
import {
  MorphPopover,
  MorphPopoverContent,
  MorphPopoverTrigger,
} from "@/components/ui/popover-morph";
import { useHoverCapable } from "@/hooks/use-hover-capable";
import { EASE_OUT, SPRING_LAYOUT } from "@/lib/ease";
import { cn } from "@/lib/utils";

const ETIQUETA_OCULTAS = "Mostrar rutas ocultas";

export type BreadcrumbProps = ComponentPropsWithRef<"nav">;

/** Landmark de navegación. Debe permanecer montado mientras cambia la ruta. */
export function Breadcrumb({ className, children, ...props }: BreadcrumbProps) {
  const id = useId();
  return (
    <nav
      aria-label="Ruta de navegación"
      data-slot="breadcrumb"
      {...props}
      className={cn("min-w-0", className)}
    >
      <LayoutGroup id={id}>{children}</LayoutGroup>
    </nav>
  );
}

export type BreadcrumbListProps = ComponentPropsWithRef<"ol"> & {
  /** Máximo de espacios visibles, incluida la elipsis. Mínimo 3; Infinity desactiva el colapso. */
  maxItems?: number;
  /** Etiqueta accesible del menú con los ancestros ocultos. */
  overflowLabel?: string;
};

/** Pasa BreadcrumbItem con `key` directamente para que las rutas que entran y salen se animen. */
export function BreadcrumbList({
  className,
  children,
  maxItems = 4,
  overflowLabel = ETIQUETA_OCULTAS,
  ...props
}: BreadcrumbListProps) {
  const items = Children.toArray(children);
  const limit = Number.isFinite(maxItems)
    ? Math.max(3, Math.floor(maxItems))
    : 4;
  const collapse = maxItems !== Infinity && items.length > limit;
  const tailCount = limit - 2;
  const visible = collapse
    ? [
        items[0],
        <BreadcrumbItem key="breadcrumb-overflow">
          <BreadcrumbSeparator />
          <BreadcrumbEllipsis label={overflowLabel}>
            {items.slice(1, -tailCount)}
          </BreadcrumbEllipsis>
        </BreadcrumbItem>,
        ...items.slice(-tailCount),
      ]
    : items;
  return (
    <ol
      data-slot="breadcrumb-list"
      {...props}
      className={cn(
        "relative flex flex-wrap items-center gap-x-1 gap-y-1 text-sm",
        className,
      )}
    >
      <AnimatePresence initial={false} mode="popLayout">
        {visible}
      </AnimatePresence>
    </ol>
  );
}

export type BreadcrumbItemProps = HTMLMotionProps<"li"> & {
  ref?: Ref<HTMLLIElement>;
};

/** Usa una `key` de ruta estable; el separador opcional va dentro de este elemento. */
export function BreadcrumbItem({
  className,
  style,
  children,
  ref,
  ...props
}: BreadcrumbItemProps) {
  const reduce = useReducedMotion();
  const present = useIsPresent();
  const itemRef = useRef<HTMLLIElement>(null);
  useLayoutEffect(() => {
    const item = itemRef.current;
    if (!item || !present) return;
    const measure = () => {
      // popLayout toma el offsetWidth (píxeles enteros). Se conserva el ancho exacto para que la
      // pérdida de una fracción de píxel no envuelva el último carácter.
      item.style.setProperty(
        "--breadcrumb-exit-width",
        `${item.getBoundingClientRect().width}px`,
      );
    };
    measure();
    const observer = new ResizeObserver(measure);
    observer.observe(item);
    return () => observer.disconnect();
  }, [present]);
  const hidden = { opacity: 0, y: reduce ? 0 : 6 };
  return (
    <motion.li
      data-slot="breadcrumb-item"
      ref={(node) => {
        itemRef.current = node;
        if (typeof ref === "function") return ref(node);
        if (ref) ref.current = node;
      }}
      layout={reduce ? false : "position"}
      initial={hidden}
      animate={{ opacity: 1, y: 0 }}
      exit={hidden}
      transition={{ duration: 0.2, ease: EASE_OUT, layout: SPRING_LAYOUT }}
      {...props}
      inert={!present}
      aria-hidden={!present || undefined}
      style={{
        ...style,
        minWidth: present ? style?.minWidth : "var(--breadcrumb-exit-width)",
        pointerEvents: present ? style?.pointerEvents : "none",
      }}
      className={cn(
        "relative inline-flex max-w-full min-w-0 items-center gap-1",
        className,
      )}
    >
      {children}
    </motion.li>
  );
}

export type BreadcrumbLinkProps = ComponentPropsWithRef<"a"> & {
  /** Renderiza el Link de tu enrutador, esparciendo estas props sobre él. */
  render?: (props: ComponentPropsWithRef<"a">) => ReactElement;
};

export function BreadcrumbLink({
  className,
  render,
  ...props
}: BreadcrumbLinkProps) {
  const linkProps = {
    "data-slot": "breadcrumb-link",
    ...props,
    className: cn(
      "inline-flex min-h-8 min-w-0 items-center gap-1.5 rounded-lg border border-transparent px-2 font-medium text-muted-foreground transition-colors duration-150 outline-none hover:bg-muted hover:text-foreground focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 [&>svg]:size-3.5 [&>svg]:shrink-0",
      className,
    ),
  };
  return render ? render(linkProps) : <a {...linkProps} />;
}

export type BreadcrumbPageProps = ComponentPropsWithRef<"span">;

export function BreadcrumbPage({
  className,
  children,
  ...props
}: BreadcrumbPageProps) {
  return (
    <span
      data-slot="breadcrumb-page"
      {...props}
      aria-current="page"
      className={cn(
        "relative isolate inline-flex min-h-8 min-w-0 items-center gap-1.5 rounded-lg border border-transparent px-2 font-medium [overflow-wrap:anywhere] text-foreground outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 [&>svg]:size-3.5 [&>svg]:shrink-0",
        className,
      )}
    >
      {children}
    </span>
  );
}

export type BreadcrumbSeparatorProps = ComponentPropsWithRef<"span">;

/** Separador decorativo, colocado dentro del BreadcrumbItem que le sigue. */
export function BreadcrumbSeparator({
  className,
  children,
  ...props
}: BreadcrumbSeparatorProps) {
  return (
    <span
      data-slot="breadcrumb-separator"
      {...props}
      aria-hidden="true"
      data-breadcrumb-separator=""
      className={cn(
        "inline-flex shrink-0 items-center text-muted-foreground/50 rtl:rotate-180 [&>svg]:size-3.5",
        className,
      )}
    >
      {children ?? <ChevronRightRegular />}
    </span>
  );
}

export interface BreadcrumbEllipsisProps {
  /** BreadcrumbItem ocultos, en orden de ruta. */
  children: ReactNode;
  className?: string;
  label?: string;
}

/** Menú al pasar el cursor, con alternancia por clic o toque y acceso con teclado a los enlaces ancestros. */
export function BreadcrumbEllipsis({
  children,
  className,
  label = ETIQUETA_OCULTAS,
}: BreadcrumbEllipsisProps) {
  const [open, setOpen] = useState(false);
  const [placement, setPlacement] = useState<{
    align: "start" | "end";
    side: "top" | "bottom";
    width: number;
  }>({ align: "start", side: "bottom", width: 224 });
  const canHover = useHoverCapable();
  const present = useIsPresent();
  const trigger = useRef<HTMLButtonElement>(null);
  const panel = useRef<HTMLOListElement>(null);
  const focusOnOpen = useRef(false);
  const closeTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  useLayoutEffect(() => {
    if (!open) return;
    const update = () => {
      const rect = trigger.current?.getBoundingClientRect();
      if (!rect) return;
      const right = window.innerWidth - rect.left - 8;
      const left = rect.right - 8;
      const align = right < 224 && left > right ? "end" : "start";
      const below = window.innerHeight - rect.bottom;
      setPlacement({
        align,
        side: below < 280 && rect.top > below ? "top" : "bottom",
        width: Math.max(32, Math.min(224, align === "start" ? right : left)),
      });
    };
    update();
    window.addEventListener("resize", update);
    return () => window.removeEventListener("resize", update);
  }, [open]);

  const cancelClose = () => {
    if (closeTimer.current !== null) clearTimeout(closeTimer.current);
    closeTimer.current = null;
  };
  const leave = () => {
    cancelClose();
    // Permite que el puntero cruce el espacio entre el disparador y el portal.
    closeTimer.current = setTimeout(() => {
      if (
        !panel.current?.contains(document.activeElement) &&
        document.activeElement !== trigger.current
      )
        setOpen(false);
    }, 160);
  };
  useEffect(
    () => () => {
      if (closeTimer.current !== null) clearTimeout(closeTimer.current);
    },
    [],
  );

  useEffect(() => {
    if (!open) return;
    const onFocus = (event: FocusEvent) => {
      if (
        event.target instanceof Node &&
        event.target !== trigger.current &&
        !panel.current?.contains(event.target)
      )
        setOpen(false);
    };
    document.addEventListener("focusin", onFocus);
    return () => document.removeEventListener("focusin", onFocus);
  }, [open]);

  return (
    <MorphPopover
      open={open && present}
      onOpenChange={setOpen}
      className={cn(className)}
    >
      <span
        onPointerEnter={(event) => {
          cancelClose();
          if (canHover && event.pointerType === "mouse") {
            focusOnOpen.current = false;
            setOpen(true);
          }
        }}
        onPointerLeave={leave}
      >
        <MorphPopoverTrigger>
          <button
            data-slot="breadcrumb-ellipsis"
            ref={trigger}
            type="button"
            aria-label={label}
            className="inline-flex size-8 items-center justify-center rounded-lg border border-transparent text-muted-foreground transition-colors outline-none hover:bg-muted hover:text-foreground focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
            onClick={(event) => {
              focusOnOpen.current = event.detail === 0;
            }}
            onKeyDown={(event) => {
              if (event.key === "ArrowDown") {
                event.preventDefault();
                focusOnOpen.current = true;
                setOpen(true);
                panel.current
                  ?.querySelector<HTMLElement>("a[href],button")
                  ?.focus();
              }
            }}
          >
            <MoreHorizontalRegular aria-hidden="true" className="size-4" />
          </button>
        </MorphPopoverTrigger>
        <MorphPopoverContent
          align={placement.align}
          side={placement.side}
          radius={10}
          sideOffset={6}
          className="p-1.5"
        >
          <BreadcrumbOverflowPaths
            ref={panel}
            style={{ width: placement.width - 14 }}
            focusOnOpen={focusOnOpen}
            onPointerEnter={() => {
              cancelClose();
              setOpen(true);
            }}
            onPointerLeave={leave}
            onClick={(event) => {
              if ((event.target as Element).closest("a[href]")) setOpen(false);
            }}
          >
            {children}
          </BreadcrumbOverflowPaths>
        </MorphPopoverContent>
      </span>
    </MorphPopover>
  );
}

function BreadcrumbOverflowPaths({
  focusOnOpen,
  ref,
  ...props
}: ComponentPropsWithRef<"ol"> & { focusOnOpen: RefObject<boolean> }) {
  const localRef = useRef<HTMLOListElement>(null);
  useLayoutEffect(() => {
    if (!focusOnOpen.current) return;
    const focus = () =>
      localRef.current?.querySelector<HTMLElement>("a[href],button")?.focus();
    focus();
    // El portal se vuelve visible después de la medición de layout de su padre.
    const frame = requestAnimationFrame(() => {
      focus();
      focusOnOpen.current = false;
    });
    return () => cancelAnimationFrame(frame);
  }, [focusOnOpen]);
  return (
    <ol
      {...props}
      ref={(node) => {
        localRef.current = node;
        if (typeof ref === "function") return ref(node);
        if (ref) ref.current = node;
      }}
      className="flex max-h-64 flex-col gap-0.5 overflow-y-auto [&_[data-breadcrumb-separator]]:hidden [&_a]:w-full [&_a]:py-1 [&_a]:[overflow-wrap:anywhere] [&>li]:w-full"
    />
  );
}
