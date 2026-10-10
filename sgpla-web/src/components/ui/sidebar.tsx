import { Dialog } from "@base-ui/react/dialog";
import { Tooltip } from "@base-ui/react/tooltip";
import { cva } from "class-variance-authority";
import {
  ChevronRightRegular,
  PanelLeftRegular,
  PanelRightRegular,
} from "@fluentui/react-icons";
import {
  Children,
  createContext,
  use,
  useCallback,
  useEffect,
  useId,
  useMemo,
  useRef,
  useState,
  type ComponentProps,
  type ComponentPropsWithRef,
  type CSSProperties,
  type KeyboardEvent,
  type ReactElement,
  type ReactNode,
} from "react";
import { Button } from "@/components/ui/button";
import { useMediaQuery } from "@/hooks/use-media-query";
import { cn } from "@/lib/utils";

export const SIDEBAR_WIDTH_MIN = 160;
export const SIDEBAR_WIDTH_MAX = 360;
const SIDEBAR_WIDTH_STEP = 16;
const COOKIE_OPEN = "sidebar_state";
const COOKIE_WIDTH = "sidebar_width";
const COOKIE_MAX_AGE = 60 * 60 * 24 * 7;
const EASE = "ease-[cubic-bezier(0.16,1,0.3,1)]";

function clampWidth(value: number) {
  return Math.min(SIDEBAR_WIDTH_MAX, Math.max(SIDEBAR_WIDTH_MIN, value));
}

function readCookie(name: string) {
  if (typeof document === "undefined") return undefined;
  const entry = document.cookie
    .split("; ")
    .find((cookie) => cookie.startsWith(`${name}=`));
  return entry ? decodeURIComponent(entry.slice(name.length + 1)) : undefined;
}

function writeCookie(name: string, value: string) {
  document.cookie = `${name}=${encodeURIComponent(value)}; path=/; max-age=${COOKIE_MAX_AGE}; samesite=lax`;
}

function isEditable(target: EventTarget | null) {
  return (
    target instanceof HTMLElement &&
    (target.isContentEditable ||
      ["INPUT", "TEXTAREA", "SELECT"].includes(target.tagName))
  );
}

type SidebarSide = "left" | "right";
type SidebarPeek = "none" | "hover" | "click";

interface SidebarContextValue {
  open: boolean;
  setOpen: (value: boolean | ((current: boolean) => boolean)) => void;
  openMobile: boolean;
  setOpenMobile: (value: boolean | ((current: boolean) => boolean)) => void;
  isMobile: boolean;
  toggle: () => void;
  side: SidebarSide;
  peek: SidebarPeek;
  peeking: boolean;
  setPeeking: (value: boolean | ((current: boolean) => boolean)) => void;
  width: number;
  setWidth: (value: number) => void;
  widthMobile: string;
  resizing: boolean;
  setResizing: (value: boolean) => void;
}

const SidebarContext = createContext<SidebarContextValue | null>(null);

export function useSidebar() {
  const context = use(SidebarContext);
  if (!context)
    throw new Error("useSidebar debe usarse dentro de un SidebarProvider.");
  return context;
}

export type SidebarProviderProps = Omit<ComponentPropsWithRef<"div">, "dir"> & {
  open?: boolean;
  onOpenChange?: (open: boolean) => void;
  /** Estado inicial cuando no es controlado y no hay cookie guardada. */
  defaultOpen?: boolean;
  /** Guarda el estado y el ancho en cookies de 7 días. */
  persist?: boolean;
  /** Interacción del borde cuando la barra está colapsada: superpone la barra sin cambiar el estado. */
  peek?: SidebarPeek;
  /** Lado predeterminado de los Sidebar hijos; también define el atajo predeterminado. */
  side?: SidebarSide;
  /** Tecla del atajo (con el foco dentro del provider). `false` lo desactiva. Predeterminado: "[" izquierda, "]" derecha. */
  shortcut?: string | false;
  /** Ancho en píxeles bajo el cual la barra pasa a ser un panel modal. */
  mobileBreakpoint?: number;
  /** Ancho inicial en píxeles, entre 160 y 360. Se exporta como `--sidebar-width`. */
  width?: number;
  /** Ancho del panel móvil. Se exporta como `--sidebar-width-mobile`. */
  widthMobile?: number | string;
};

export function SidebarProvider({
  open: openProp,
  onOpenChange,
  defaultOpen = true,
  persist = true,
  peek = "none",
  side = "left",
  shortcut,
  mobileBreakpoint = 768,
  width: initialWidth = 256,
  widthMobile = "18rem",
  className,
  style,
  children,
  onKeyDown,
  ...props
}: SidebarProviderProps) {
  const [openState, setOpenState] = useState(() => {
    const saved = persist ? readCookie(COOKIE_OPEN) : undefined;
    return saved === undefined ? defaultOpen : saved === "true";
  });
  const [openMobile, setOpenMobile] = useState(false);
  const [peekingState, setPeeking] = useState(false);
  const [resizing, setResizing] = useState(false);
  const [width, setWidthState] = useState(() => {
    const saved = persist ? Number(readCookie(COOKIE_WIDTH)) : Number.NaN;
    return clampWidth(
      Number.isFinite(saved) && saved > 0 ? saved : initialWidth,
    );
  });
  const isMobile = useMediaQuery(`(max-width: ${mobileBreakpoint - 1}px)`);
  const open = openProp ?? openState;
  const peeking = peekingState && peek !== "none";

  const setOpen = useCallback(
    (value: boolean | ((current: boolean) => boolean)) => {
      const next = typeof value === "function" ? value(open) : value;
      if (openProp === undefined) setOpenState(next);
      onOpenChange?.(next);
      if (persist) writeCookie(COOKIE_OPEN, String(next));
      if (next) setPeeking(false);
    },
    [open, openProp, onOpenChange, persist],
  );

  const setWidth = useCallback(
    (value: number) => {
      const next = clampWidth(value);
      setWidthState(next);
      if (persist) writeCookie(COOKIE_WIDTH, String(next));
    },
    [persist],
  );

  const toggle = useCallback(() => {
    if (isMobile) setOpenMobile((current) => !current);
    else setOpen((current) => !current);
  }, [isMobile, setOpen]);

  const key = shortcut ?? (side === "right" ? "]" : "[");

  const context = useMemo<SidebarContextValue>(
    () => ({
      open,
      setOpen,
      openMobile,
      setOpenMobile,
      isMobile,
      toggle,
      side,
      peek,
      peeking,
      setPeeking,
      width,
      setWidth,
      widthMobile:
        typeof widthMobile === "number" ? `${widthMobile}px` : widthMobile,
      resizing,
      setResizing,
    }),
    [
      open,
      setOpen,
      openMobile,
      isMobile,
      toggle,
      side,
      peek,
      peeking,
      width,
      setWidth,
      widthMobile,
      resizing,
    ],
  );

  return (
    <SidebarContext value={context}>
      <div
        data-slot="sidebar-wrapper"
        {...props}
        onKeyDown={(event) => {
          onKeyDown?.(event);
          if (
            shortcut === false ||
            event.defaultPrevented ||
            event.metaKey ||
            event.ctrlKey ||
            event.altKey ||
            isEditable(event.target) ||
            event.key !== key
          )
            return;
          event.preventDefault();
          toggle();
        }}
        style={
          {
            "--sidebar-width": `${width}px`,
            "--sidebar-width-mobile": context.widthMobile,
            ...style,
          } as CSSProperties
        }
        className={cn(
          "group/sidebar-wrapper flex min-h-svh w-full has-data-[variant=inset]:bg-sidebar",
          className,
        )}
      >
        {children}
      </div>
    </SidebarContext>
  );
}

export type SidebarProps = Omit<ComponentPropsWithRef<"div">, "dir"> & {
  /** Predeterminado: el `side` del provider. */
  side?: SidebarSide;
  variant?: "sidebar" | "floating" | "inset";
  /** `offcanvas` se desliza fuera de la vista; `none` queda siempre abierta y estática. */
  collapsible?: "offcanvas" | "none";
  /** Muestra el borde de arrastre para redimensionar y colapsar. */
  rail?: boolean;
  /** `true` fija el tooltip del borde; `false` lo desactiva. Sin valor aparece al pasar el cursor. */
  railTooltipOpen?: boolean;
  /** Borde en el canto interior (solo variante `sidebar`). */
  bordered?: boolean;
};

export function Sidebar({
  side: sideProp,
  variant = "sidebar",
  collapsible = "offcanvas",
  rail = true,
  railTooltipOpen,
  bordered = true,
  className,
  children,
  ...props
}: SidebarProps) {
  const {
    side: defaultSide,
    open,
    openMobile,
    setOpenMobile,
    isMobile,
    peek,
    peeking,
    setPeeking,
    widthMobile,
    resizing,
  } = useSidebar();
  const side = sideProp ?? defaultSide;
  const panel = useRef<HTMLDivElement>(null);
  const edge = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    if (!peeking || peek !== "click") return;
    const close = (event: PointerEvent) => {
      const target = event.target;
      if (
        target instanceof Node &&
        !panel.current?.contains(target) &&
        !edge.current?.contains(target)
      )
        setPeeking(false);
    };
    document.addEventListener("pointerdown", close);
    return () => document.removeEventListener("pointerdown", close);
  }, [peeking, peek, setPeeking]);

  const borderClass = bordered
    ? side === "left"
      ? "border-r"
      : "border-l"
    : "";

  if (collapsible === "none") {
    return (
      <div
        data-slot="sidebar"
        data-side={side}
        data-variant={variant}
        data-state="expanded"
        {...props}
        className={cn(
          "flex h-full w-(--sidebar-width) shrink-0 flex-col bg-sidebar text-sidebar-foreground",
          variant === "sidebar" && borderClass,
          className,
        )}
      >
        {children}
      </div>
    );
  }

  if (isMobile) {
    return (
      <Dialog.Root open={openMobile} onOpenChange={setOpenMobile}>
        <Dialog.Portal>
          <Dialog.Backdrop
            className={cn(
              "fixed inset-0 z-50 bg-black/40 transition-opacity duration-200 data-ending-style:opacity-0 data-starting-style:opacity-0 motion-reduce:transition-none",
            )}
          />
          <Dialog.Popup
            data-slot="sidebar"
            data-mobile="true"
            data-side={side}
            style={{ width: widthMobile }}
            className={cn(
              "fixed inset-y-0 z-50 flex max-w-[85vw] flex-col bg-sidebar text-sidebar-foreground shadow-lg transition-transform duration-300 ease-[cubic-bezier(0.32,0.72,0,1)] outline-none motion-reduce:transition-none",
              side === "left"
                ? "left-0 border-r data-ending-style:-translate-x-full data-starting-style:-translate-x-full"
                : "right-0 border-l data-ending-style:translate-x-full data-starting-style:translate-x-full",
              className,
            )}
          >
            <Dialog.Title className="sr-only">Navegación</Dialog.Title>
            {children}
          </Dialog.Popup>
        </Dialog.Portal>
      </Dialog.Root>
    );
  }

  const hidden = !open && !peeking;
  const padded = variant !== "sidebar" || (!open && peeking);
  const width = padded
    ? "w-[calc(var(--sidebar-width)+1rem)]"
    : "w-(--sidebar-width)";
  const floating = variant === "floating" || (!open && peeking);

  return (
    <div
      data-slot="sidebar"
      data-state={open ? "expanded" : "collapsed"}
      data-side={side}
      data-variant={variant}
      data-peeking={peeking}
      data-resizing={resizing}
      className="group peer text-sidebar-foreground"
    >
      <div
        aria-hidden="true"
        data-slot="sidebar-gap"
        className={cn(
          "relative shrink-0 bg-transparent transition-[width] duration-300 group-data-[resizing=true]:transition-none motion-reduce:transition-none",
          EASE,
          variant === "sidebar"
            ? "w-(--sidebar-width)"
            : "w-[calc(var(--sidebar-width)+1rem)]",
          !open && "w-0",
        )}
      />
      {!open && peek !== "none" && !peeking && (
        <button
          ref={edge}
          type="button"
          data-slot="sidebar-edge"
          aria-label="Mostrar barra lateral"
          tabIndex={peek === "click" ? 0 : -1}
          aria-hidden={peek === "hover" || undefined}
          onPointerEnter={peek === "hover" ? () => setPeeking(true) : undefined}
          onClick={peek === "click" ? () => setPeeking(true) : undefined}
          className={cn(
            "fixed inset-y-0 z-10 w-2 cursor-pointer outline-none",
            side === "left" ? "left-0" : "right-0",
          )}
        />
      )}
      <div
        ref={panel}
        data-slot="sidebar-panel"
        data-hidden={hidden}
        inert={hidden}
        {...props}
        onPointerLeave={(event) => {
          props.onPointerLeave?.(event);
          if (peek === "hover" && peeking) setPeeking(false);
        }}
        onKeyDown={(event: KeyboardEvent<HTMLDivElement>) => {
          props.onKeyDown?.(event);
          if (event.key === "Escape" && peeking) setPeeking(false);
        }}
        className={cn(
          "fixed inset-y-0 z-10 flex h-svh transition-[transform,visibility] duration-300 group-data-[resizing=true]:transition-none data-[hidden=true]:invisible motion-reduce:transition-none",
          EASE,
          width,
          padded && "p-2",
          side === "left" ? "left-0" : "right-0",
          hidden &&
            (side === "left" ? "-translate-x-full" : "translate-x-full"),
          className,
        )}
      >
        <div
          data-slot="sidebar-inner"
          className={cn(
            "relative flex h-full w-full min-w-0 flex-col bg-sidebar",
            variant === "inset" && open && "bg-transparent",
            variant === "sidebar" && open && borderClass,
            floating && "rounded-xl border shadow-sm",
            !open && peeking && "shadow-lg",
          )}
        >
          {children}
          {rail && <SidebarRail side={side} tooltipOpen={railTooltipOpen} />}
        </div>
      </div>
    </div>
  );
}

function SidebarRail({
  side,
  tooltipOpen,
}: {
  side: SidebarSide;
  tooltipOpen?: boolean;
}) {
  const { width, setWidth, setOpen, setResizing } = useSidebar();
  const drag = useRef<{
    startX: number;
    startWidth: number;
    moved: boolean;
  } | null>(null);
  const direction = side === "left" ? 1 : -1;

  const finish = (commit: boolean) => {
    const current = drag.current;
    drag.current = null;
    setResizing(false);
    if (commit && current && !current.moved) setOpen((value) => !value);
  };

  return (
    <Tooltip.Root
      open={tooltipOpen === true ? true : undefined}
      disabled={tooltipOpen === false}
    >
      <Tooltip.Trigger
        data-slot="sidebar-rail"
        aria-label="Redimensionar o colapsar la barra lateral"
        onPointerDown={(event) => {
          if (event.button !== 0) return;
          event.currentTarget.setPointerCapture(event.pointerId);
          drag.current = {
            startX: event.clientX,
            startWidth: width,
            moved: false,
          };
        }}
        onPointerMove={(event) => {
          const current = drag.current;
          if (!current) return;
          const delta = (event.clientX - current.startX) * direction;
          if (!current.moved && Math.abs(delta) < 3) return;
          if (!current.moved) setResizing(true);
          current.moved = true;
          setWidth(current.startWidth + delta);
        }}
        onPointerUp={() => finish(true)}
        onPointerCancel={() => finish(false)}
        onClick={(event) => {
          // Un clic con teclado (detail 0); el puntero se resuelve en pointerup.
          if (event.detail === 0) setOpen((value) => !value);
        }}
        onKeyDown={(event) => {
          if (event.key !== "ArrowLeft" && event.key !== "ArrowRight") return;
          event.preventDefault();
          const sign = event.key === "ArrowRight" ? 1 : -1;
          setWidth(width + sign * direction * SIDEBAR_WIDTH_STEP);
        }}
        className={cn(
          "absolute inset-y-0 z-20 w-3 cursor-col-resize touch-none outline-none after:absolute after:inset-y-0 after:left-1/2 after:w-0.5 after:-translate-x-1/2 after:rounded-full after:transition-colors hover:after:bg-border focus-visible:after:bg-ring",
          side === "left" ? "-right-1.5" : "-left-1.5",
        )}
      />
      <Tooltip.Portal>
        <Tooltip.Positioner
          side={side === "left" ? "right" : "left"}
          sideOffset={8}
          className="z-50"
        >
          <Tooltip.Popup className="rounded-md bg-foreground px-2 py-1 text-xs text-background transition-opacity duration-150 data-ending-style:opacity-0 data-starting-style:opacity-0">
            Arrastra para ajustar · Clic para colapsar
          </Tooltip.Popup>
        </Tooltip.Positioner>
      </Tooltip.Portal>
    </Tooltip.Root>
  );
}

export function SidebarTrigger({
  className,
  onClick,
  ...props
}: ComponentProps<typeof Button>) {
  const { toggle, open, openMobile, isMobile, side } = useSidebar();
  const Icon = side === "right" ? PanelRightRegular : PanelLeftRegular;
  return (
    <Button
      data-slot="sidebar-trigger"
      variant="ghost"
      size="icon-sm"
      aria-label="Alternar barra lateral"
      aria-expanded={isMobile ? openMobile : open}
      {...props}
      className={className}
      onClick={(event) => {
        onClick?.(event);
        if (!event.defaultPrevented) toggle();
      }}
    >
      <Icon aria-hidden="true" />
    </Button>
  );
}

export function SidebarInset({
  className,
  ...props
}: ComponentPropsWithRef<"main">) {
  return (
    <main
      data-slot="sidebar-inset"
      {...props}
      className={cn(
        "relative flex min-w-0 flex-1 flex-col bg-background group-has-data-[variant=inset]/sidebar-wrapper:m-2 group-has-data-[variant=inset]/sidebar-wrapper:rounded-xl group-has-data-[variant=inset]/sidebar-wrapper:shadow-sm group-has-[[data-variant=inset][data-side=left][data-state=expanded]]/sidebar-wrapper:ml-0 group-has-[[data-variant=inset][data-side=right][data-state=expanded]]/sidebar-wrapper:mr-0",
        className,
      )}
    />
  );
}

export function SidebarHeader({
  className,
  ...props
}: ComponentPropsWithRef<"div">) {
  return (
    <div
      data-slot="sidebar-header"
      {...props}
      className={cn("flex flex-col gap-2 p-2", className)}
    />
  );
}

export function SidebarFooter({
  className,
  ...props
}: ComponentPropsWithRef<"div">) {
  return (
    <div
      data-slot="sidebar-footer"
      {...props}
      className={cn("flex flex-col gap-2 p-2", className)}
    />
  );
}

export function SidebarContent({
  className,
  ...props
}: ComponentPropsWithRef<"div">) {
  return (
    <div
      data-slot="sidebar-content"
      {...props}
      className={cn(
        "flex min-h-0 flex-1 flex-col gap-2 overflow-x-hidden overflow-y-auto p-2",
        className,
      )}
    />
  );
}

/** Colapso por altura medida: la fila de la cuadrícula pasa de 0fr a 1fr, sin animar `height: auto`. */
function Collapse({
  open,
  className,
  children,
}: {
  open: boolean;
  className?: string;
  children: ReactNode;
}) {
  return (
    <div
      data-state={open ? "open" : "closed"}
      className={cn(
        "grid transition-[grid-template-rows] duration-200 motion-reduce:transition-none",
        EASE,
        open ? "grid-rows-[1fr]" : "grid-rows-[0fr]",
        className,
      )}
    >
      <div inert={!open} className="min-h-0 overflow-hidden">
        {children}
      </div>
    </div>
  );
}

interface SidebarGroupContextValue {
  collapsible: boolean;
  open: boolean;
  toggle: () => void;
}

const SidebarGroupContext = createContext<SidebarGroupContextValue>({
  collapsible: false,
  open: true,
  toggle: () => {},
});

export type SidebarGroupProps = Omit<
  ComponentPropsWithRef<"div">,
  "defaultValue"
> & {
  /** Convierte la sección en un acordeón con su etiqueta como control. */
  collapsible?: boolean;
  open?: boolean;
  onOpenChange?: (open: boolean) => void;
  defaultOpen?: boolean;
};

export function SidebarGroup({
  collapsible = false,
  open: openProp,
  onOpenChange,
  defaultOpen = true,
  className,
  ...props
}: SidebarGroupProps) {
  const [openState, setOpenState] = useState(defaultOpen);
  const open = !collapsible || (openProp ?? openState);
  const value = useMemo(
    () => ({
      collapsible,
      open,
      toggle: () => {
        if (openProp === undefined) setOpenState(!open);
        onOpenChange?.(!open);
      },
    }),
    [collapsible, open, openProp, onOpenChange],
  );
  return (
    <SidebarGroupContext value={value}>
      <div
        data-slot="sidebar-group"
        {...props}
        className={cn("relative flex w-full min-w-0 flex-col", className)}
      />
    </SidebarGroupContext>
  );
}

const groupLabelClass =
  "group/group-label flex h-8 w-full shrink-0 items-center gap-1 rounded-md px-2 text-xs font-medium text-sidebar-foreground/70 outline-none";

export function SidebarGroupLabel({
  className,
  children,
  ...props
}: ComponentPropsWithRef<"button">) {
  const { collapsible, open, toggle } = use(SidebarGroupContext);
  if (!collapsible)
    return (
      <div
        data-slot="sidebar-group-label"
        className={cn(groupLabelClass, className)}
      >
        {children}
      </div>
    );
  return (
    <button
      type="button"
      data-slot="sidebar-group-label"
      data-state={open ? "open" : "closed"}
      aria-expanded={open}
      {...props}
      onClick={(event) => {
        props.onClick?.(event);
        if (!event.defaultPrevented) toggle();
      }}
      className={cn(
        groupLabelClass,
        "cursor-pointer hover:text-sidebar-foreground focus-visible:ring-2 focus-visible:ring-sidebar-ring",
        className,
      )}
    >
      <span className="min-w-0 flex-1 truncate text-start">{children}</span>
      <ChevronRightRegular
        aria-hidden="true"
        className={cn(
          "size-3.5 shrink-0 transition-[transform,opacity] duration-200 group-hover/group-label:opacity-100 group-focus-visible/group-label:opacity-100 motion-reduce:transition-none",
          open ? "rotate-90 opacity-0" : "opacity-100",
        )}
      />
    </button>
  );
}

export function SidebarGroupContent({
  className,
  ...props
}: ComponentPropsWithRef<"div">) {
  const { collapsible, open } = use(SidebarGroupContext);
  const content = (
    <div
      data-slot="sidebar-group-content"
      {...props}
      className={cn("w-full text-sm", className)}
    />
  );
  return collapsible ? <Collapse open={open}>{content}</Collapse> : content;
}

export function SidebarMenu({
  className,
  ...props
}: ComponentPropsWithRef<"ul">) {
  return (
    <ul
      data-slot="sidebar-menu"
      {...props}
      className={cn("flex w-full min-w-0 flex-col gap-0.5", className)}
    />
  );
}

export function SidebarMenuItem({
  className,
  ...props
}: ComponentPropsWithRef<"li">) {
  return (
    <li
      data-slot="sidebar-menu-item"
      {...props}
      className={cn(
        "group/menu-item relative has-data-[actions='1']:[&>[data-slot=sidebar-menu-button]]:pr-8 has-data-[actions='2']:[&>[data-slot=sidebar-menu-button]]:pr-14 has-data-[actions='3']:[&>[data-slot=sidebar-menu-button]]:pr-20 has-data-[slot=sidebar-menu-badge]:[&>[data-slot=sidebar-menu-button]]:pr-9",
        className,
      )}
    />
  );
}

const menuButtonVariants = cva(
  "group/menu-button flex w-full min-w-0 items-center gap-2 rounded-md px-2 text-start text-sm outline-none transition-[background-color,color] duration-150 hover:bg-sidebar-accent hover:text-sidebar-accent-foreground focus-visible:ring-2 focus-visible:ring-sidebar-ring data-[active=true]:bg-sidebar-accent data-[active=true]:font-semibold data-[active=true]:text-sidebar-accent-foreground [&_svg]:size-4 [&_svg]:shrink-0 [&_svg]:stroke-[1.5] [&_svg]:transition-[stroke-width] data-[active=true]:[&_svg]:stroke-2",
  {
    variants: {
      variant: {
        default: "",
        outline:
          "bg-background shadow-[0_0_0_1px_var(--sidebar-border)] hover:shadow-[0_0_0_1px_var(--sidebar-accent)]",
      },
      size: {
        default: "h-8",
        sm: "h-7 text-xs",
        lg: "h-10",
        sub: "h-7 text-[0.8rem]",
      },
    },
    defaultVariants: { variant: "default", size: "default" },
  },
);

type SidebarStatus = "active" | "unread" | "idle";
type SidebarDot = "filled" | "ring";

const STATUS_LABEL: Record<SidebarStatus, string> = {
  active: "Activo",
  unread: "Sin leer",
  idle: "Inactivo",
};

const STATUS_COLOR: Record<SidebarStatus, string> = {
  active: "text-emerald-500",
  unread: "text-primary",
  idle: "text-muted-foreground",
};

function SidebarStatusDot({
  status,
  dot,
}: {
  status: SidebarStatus;
  dot?: SidebarDot;
}) {
  const shape = dot ?? (status === "idle" ? "ring" : "filled");
  return (
    <span
      data-slot="sidebar-menu-status"
      data-status={status}
      className={cn("ml-auto flex shrink-0 items-center", STATUS_COLOR[status])}
    >
      <span
        aria-hidden="true"
        className={cn(
          "size-1.5 rounded-full",
          shape === "filled" ? "bg-current" : "border border-current",
        )}
      />
      <span className="sr-only">{STATUS_LABEL[status]}</span>
    </span>
  );
}

export type SidebarMenuButtonProps = Omit<
  ComponentPropsWithRef<"button">,
  "children"
> & {
  children?: ReactNode;
  isActive?: boolean;
  /** Ícono inicial; el trazo se engrosa al activarse la fila. */
  icon?: ReactNode;
  status?: SidebarStatus;
  /** Forma del indicador de `status`; por omisión, `ring` para idle y `filled` para el resto. */
  dot?: SidebarDot;
  size?: "default" | "sm" | "lg";
  variant?: "default" | "outline";
  /** Renderiza el Link de tu enrutador, esparciendo estas props sobre él. */
  render?: (props: ComponentPropsWithRef<"a">) => ReactElement;
};

function buildMenuButton(
  {
    isActive = false,
    icon,
    status,
    dot,
    variant,
    className,
    children,
    render,
    ...props
  }: Omit<SidebarMenuButtonProps, "size">,
  size: "default" | "sm" | "lg" | "sub",
) {
  const shared = {
    "data-slot": "sidebar-menu-button",
    "data-size": size,
    "data-active": isActive,
    "aria-current": isActive ? ("page" as const) : undefined,
    className: cn(menuButtonVariants({ variant, size }), className),
    children: (
      <>
        {icon && (
          <span
            data-slot="sidebar-menu-icon"
            aria-hidden="true"
            className="flex shrink-0 items-center"
          >
            {icon}
          </span>
        )}
        <span className="min-w-0 flex-1 truncate">{children}</span>
        {status && <SidebarStatusDot status={status} dot={dot} />}
      </>
    ),
  };
  if (render)
    return render({
      ...(props as object),
      ...shared,
    } as ComponentPropsWithRef<"a">);
  return <button type="button" {...props} {...shared} />;
}

export function SidebarMenuButton({
  size = "default",
  ...props
}: SidebarMenuButtonProps) {
  return buildMenuButton(props, size);
}

export type SidebarMenuSubProps = ComponentPropsWithRef<"ul"> & {
  /** Muestra u oculta el submenú con una animación de altura medida. */
  open?: boolean;
};

export function SidebarMenuSub({
  open = true,
  className,
  ...props
}: SidebarMenuSubProps) {
  return (
    <Collapse open={open}>
      <ul
        data-slot="sidebar-menu-sub"
        {...props}
        className={cn(
          "mx-3.5 flex min-w-0 flex-col gap-0.5 border-l border-sidebar-border px-2 py-0.5",
          className,
        )}
      />
    </Collapse>
  );
}

export function SidebarMenuSubItem({
  className,
  ...props
}: ComponentPropsWithRef<"li">) {
  return (
    <li
      data-slot="sidebar-menu-sub-item"
      {...props}
      className={cn("group/menu-sub-item relative", className)}
    />
  );
}

export function SidebarMenuSubButton(
  props: Omit<SidebarMenuButtonProps, "size" | "icon" | "variant">,
) {
  return buildMenuButton(props, "sub");
}

export function SidebarMenuBadge({
  className,
  ...props
}: ComponentPropsWithRef<"div">) {
  return (
    <div
      data-slot="sidebar-menu-badge"
      {...props}
      className={cn(
        "pointer-events-none absolute top-1/2 right-2 flex h-5 min-w-5 -translate-y-1/2 items-center justify-center rounded-md px-1 text-xs font-medium text-sidebar-foreground/70 tabular-nums select-none group-has-data-[slot=sidebar-menu-actions]/menu-item:group-focus-within/menu-item:opacity-0 group-has-data-[slot=sidebar-menu-actions]/menu-item:group-hover/menu-item:opacity-0",
        className,
      )}
    />
  );
}

export type SidebarMenuActionsProps = ComponentPropsWithRef<"div"> & {
  /** Muestra las acciones solo al pasar el cursor o enfocar la fila. */
  showOnHover?: boolean;
};

/** Contenedor de hasta 3 acciones contextuales al final de la fila. */
export function SidebarMenuActions({
  showOnHover = false,
  className,
  children,
  ...props
}: SidebarMenuActionsProps) {
  const actions = Children.toArray(children).slice(0, 3);
  return (
    <div
      data-slot="sidebar-menu-actions"
      data-actions={actions.length}
      {...props}
      className={cn(
        "absolute top-1/2 right-1.5 flex -translate-y-1/2 items-center gap-0.5",
        showOnHover &&
          "opacity-0 group-focus-within/menu-item:opacity-100 group-hover/menu-item:opacity-100 pointer-coarse:opacity-100",
        className,
      )}
    >
      {actions}
    </div>
  );
}

export function SidebarMenuAction({
  className,
  ...props
}: ComponentPropsWithRef<"button">) {
  return (
    <button
      type="button"
      data-slot="sidebar-menu-action"
      {...props}
      className={cn(
        "flex size-6 items-center justify-center rounded-md text-sidebar-foreground/70 transition-colors outline-none hover:bg-sidebar-accent hover:text-sidebar-accent-foreground focus-visible:ring-2 focus-visible:ring-sidebar-ring [&_svg]:size-4 [&_svg]:shrink-0",
        className,
      )}
    />
  );
}

export type SidebarMenuSkeletonProps = ComponentPropsWithRef<"div"> & {
  showIcon?: boolean;
};

/** El ancho sale del id estable del componente, así coincide entre el servidor y el cliente. */
export function SidebarMenuSkeleton({
  showIcon = false,
  className,
  ...props
}: SidebarMenuSkeletonProps) {
  const id = useId();
  let hash = 0;
  for (const char of id) hash = (hash + char.charCodeAt(0)) % 41;
  return (
    <div
      data-slot="sidebar-menu-skeleton"
      aria-hidden="true"
      {...props}
      className={cn("flex h-8 items-center gap-2 rounded-md px-2", className)}
    >
      {showIcon && (
        <div className="size-4 shrink-0 animate-pulse rounded-md bg-sidebar-accent" />
      )}
      <div
        className="h-4 flex-1 animate-pulse rounded-md bg-sidebar-accent"
        style={{ maxWidth: `${50 + hash}%` }}
      />
    </div>
  );
}
