import {
  AnimatePresence,
  motion,
  animate,
  useMotionValue,
  usePresence,
  useReducedMotion,
} from "motion/react";
import {
  cloneElement,
  createContext,
  isValidElement,
  type ReactElement,
  type ReactNode,
  type Ref,
  type RefObject,
  useCallback,
  useContext,
  useEffect,
  useId,
  useMemo,
  useRef,
  useSyncExternalStore,
  useState,
} from "react";
import { createPortal } from "react-dom";
import { usePopoverPortalPosition } from "@/components/ui/popover-position";
import { EASE_OUT, SPRING_PANEL } from "@/lib/ease";
import { cn } from "@/lib/utils";

type Side = "top" | "bottom";
type Align = "start" | "end";

type MorphContextValue = {
  open: boolean;
  setOpen: (open: boolean) => void;
  toggle: () => void;
  triggerId: string;
  contentId: string;
  /** Elemento contra el que el panel se mide; ver `registerTrigger`. */
  triggerRef: RefObject<HTMLElement | null>;
  registerTrigger: (node: HTMLElement | null) => void;
  contentRef: RefObject<HTMLDivElement | null>;
};

const MorphContext = createContext<MorphContextValue | null>(null);

function useMorphContext(component: string) {
  const ctx = useContext(MorphContext);
  if (!ctx) throw new Error(`${component} must be used within <MorphPopover>`);
  return ctx;
}

export interface MorphPopoverProps {
  children: ReactNode;
  /** Estado abierto controlado. */
  open?: boolean;
  /** Estado abierto inicial cuando no es controlado. */
  defaultOpen?: boolean;
  onOpenChange?: (open: boolean) => void;
  className?: string;
}

/**
 * Popover cuyo panel se abre desde la esquina del disparador: se distribuye a tamaño completo pero
 * recortado a la esquina más cercana al disparador, y luego se descubre como una sola pieza.
 * Se cierra con un clic fuera o con Escape. Puede ser controlado o no controlado.
 */
export function MorphPopover({
  children,
  open: controlledOpen,
  defaultOpen = false,
  onOpenChange,
  className,
}: MorphPopoverProps) {
  const baseId = useId();
  const [root, setRoot] = useState<HTMLDivElement | null>(null);
  const [trigger, setTrigger] = useState<HTMLElement | null>(null);
  const contentRef = useRef<HTMLDivElement | null>(null);
  const [internalOpen, setInternalOpen] = useState(defaultOpen);
  const controlled = controlledOpen !== undefined;
  const open = controlled ? controlledOpen : internalOpen;

  const setOpen = useCallback(
    (next: boolean) => {
      if (!controlled) setInternalOpen(next);
      onOpenChange?.(next);
    },
    [controlled, onOpenChange],
  );
  const toggle = useCallback(() => setOpen(!open), [setOpen, open]);

  // Un disparador normalmente se registra mediante MorphPopoverTrigger. No puede hacerlo cuando otro
  // componente ya clona el elemento (por ejemplo, un Tooltip que envuelve el botón), y un disparador
  // sin registrar deja al panel sin nada contra qué medirse, así que quedaría invisible. La raíz
  // encuadra exactamente al disparador (el contenido sale de ella por un portal), por lo que lo
  // sustituye hasta que uno real se registre, y vuelve a hacerlo si ese se desmonta. Ambos son
  // estado, de modo que un disparador que llega con el panel abierto lo reancla.
  const anchorRef = useMemo<RefObject<HTMLElement | null>>(
    () => ({ current: trigger ?? root }),
    [root, trigger],
  );

  // El panel es un `role="dialog"` y queda inerte al cerrarse, así que el foco no puede quedarse
  // dentro: al cerrar se devuelve al disparador, como pide el patrón ARIA de diálogo. Un cierre con el
  // puntero ya lleva el foco al elemento enfocable donde cae; esto solo cubre el caso en que el foco
  // quedaría varado. Sin disparador registrado, la raíz sustituye solo si puede recibir foco.
  const close = useCallback(() => {
    setOpen(false);
    const focused = document.activeElement;
    const inPanel =
      focused instanceof HTMLElement && contentRef.current?.contains(focused);
    if (!inPanel) return;
    const restore = trigger ?? (root && root.tabIndex >= 0 ? root : null);
    restore?.focus();
  }, [root, setOpen, trigger]);

  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => e.key === "Escape" && close();
    const onPointer = (e: PointerEvent) => {
      const target = e.target as Node;
      if (
        root &&
        !root.contains(target) &&
        !contentRef.current?.contains(target)
      )
        close();
    };
    window.addEventListener("keydown", onKey);
    window.addEventListener("pointerdown", onPointer);
    return () => {
      window.removeEventListener("keydown", onKey);
      window.removeEventListener("pointerdown", onPointer);
    };
  }, [open, root, close]);

  const ctx = useMemo<MorphContextValue>(
    () => ({
      open,
      setOpen,
      toggle,
      triggerId: `${baseId}-trigger`,
      contentId: `${baseId}-content`,
      triggerRef: anchorRef,
      registerTrigger: setTrigger,
      contentRef,
    }),
    [open, setOpen, toggle, baseId, anchorRef],
  );

  return (
    <MorphContext.Provider value={ctx}>
      <div ref={setRoot} className={cn("relative inline-flex", className)}>
        {children}
      </div>
    </MorphContext.Provider>
  );
}

export interface MorphPopoverTriggerProps {
  children: ReactElement;
}

function mergeRefs<T>(...refs: Array<Ref<T> | undefined>) {
  return (node: T | null) => {
    for (const ref of refs) {
      if (typeof ref === "function") ref(node);
      else if (ref && typeof ref === "object")
        (ref as RefObject<T | null>).current = node;
    }
  };
}

/** Envuelve un solo elemento y alterna el popover al hacer clic. */
export function MorphPopoverTrigger({ children }: MorphPopoverTriggerProps) {
  const ctx = useMorphContext("MorphPopoverTrigger");
  const child = children as ReactElement<Record<string, unknown>>;
  const childOnClick = child?.props?.onClick as
    ((e: unknown) => void) | undefined;
  const childRef = (child?.props as { ref?: Ref<HTMLElement> } | undefined)
    ?.ref;
  // Se registra una vez por cambio real de ref, no en cada render por el estado abierto.
  const mergedRef = useMemo(
    () => mergeRefs(childRef, ctx.registerTrigger),
    [childRef, ctx.registerTrigger],
  );
  if (!isValidElement(children)) return children;

  return cloneElement(child, {
    id: ctx.triggerId,
    ref: mergedRef,
    onClick: (e: unknown) => {
      childOnClick?.(e);
      ctx.toggle();
    },
    "aria-haspopup": "dialog",
    "aria-expanded": ctx.open,
    "aria-controls": ctx.open ? ctx.contentId : undefined,
  });
}

const originFor = (side: Side, align: Align) =>
  `${side === "bottom" ? "top" : "bottom"} ${align === "end" ? "right" : "left"}`;

// Recorte que oculta todo salvo la esquina más cercana al disparador, para que el panel parezca
// crecer desde ella. inset(arriba derecha abajo izquierda).
function clipAt(side: Side, align: Align, radius: number, inset: number) {
  const top = side === "bottom" ? "0%" : `${inset}%`;
  const bottom = side === "bottom" ? `${inset}%` : "0%";
  const right = align === "end" ? "0%" : `${inset}%`;
  const left = align === "end" ? `${inset}%` : "0%";
  return `inset(${top} ${right} ${bottom} ${left} round ${radius}px)`;
}

// El resorte original se conserva en el contenedor, pero el clip-path complejo se interpola con una
// curva para que no salte cuando el resorte resuelve su distancia final.
const MORPH_CLIP_TRANSITION = { duration: 0.32, ease: EASE_OUT } as const;

export interface MorphPopoverContentProps {
  children: ReactNode;
  side?: Side;
  align?: Align;
  /** Separación entre el disparador y el panel, en px. Por omisión 8. */
  sideOffset?: number;
  /** Radio de las esquinas del panel, en px. Por omisión 10. */
  radius?: number;
  /** Dibuja la sombra de la superficie. Por omisión true. */
  shadow?: boolean;
  /** Se ejecuta cuando la superficie del portal ya está posicionada y visible. */
  onOpenAutoFocus?: (content: HTMLDivElement) => void;
  className?: string;
}

// El portal solo existe en el cliente: en el servidor no hay `document.body` donde montarlo.
const noSuscribir = () => () => {};

export function MorphPopoverContent(props: MorphPopoverContentProps) {
  const ctx = useMorphContext("MorphPopoverContent");
  const montado = useSyncExternalStore(
    noSuscribir,
    () => true,
    () => false,
  );
  if (!montado) return null;
  return createPortal(
    <AnimatePresence>
      {ctx.open && <MorphPopoverSurface {...props} />}
    </AnimatePresence>,
    document.body,
  );
}

// La medición pertenece a la sesión del portal montado: al reabrir, la entrada no debe comenzar en
// las coordenadas de la sesión anterior antes de medir la actual.
function MorphPopoverSurface({
  children,
  side = "bottom",
  align = "end",
  sideOffset = 8,
  radius = 10,
  shadow = true,
  onOpenAutoFocus,
  className,
}: MorphPopoverContentProps) {
  const ctx = useMorphContext("MorphPopoverContent");
  const { contentId, contentRef, triggerId, triggerRef } = ctx;
  const reduce = useReducedMotion() ?? false;
  const [isPresent, safeToRemove] = usePresence();
  const layout = usePopoverPortalPosition(triggerRef, contentRef, isPresent);

  const preferredLeft = layout
    ? align === "end"
      ? layout.trigger.left + layout.trigger.width - layout.content.width
      : layout.trigger.left
    : 0;
  // Mantiene dentro del viewport las composiciones grandes. Se prefiere el lado pedido y se usa el
  // más amplio cuando no cabe en él.
  const gutter = 12;
  const below = layout
    ? Math.max(
        0,
        window.innerHeight -
          layout.trigger.top -
          layout.trigger.height -
          sideOffset -
          gutter,
      )
    : 0;
  const above = layout
    ? Math.max(0, layout.trigger.top - sideOffset - gutter)
    : 0;
  const requestedSpace = side === "bottom" ? below : above;
  const otherSpace = side === "bottom" ? above : below;
  const resolvedSide =
    layout &&
    layout.content.height > requestedSpace &&
    otherSpace > requestedSpace
      ? side === "bottom"
        ? "top"
        : "bottom"
      : side;
  const availableHeight = resolvedSide === "bottom" ? below : above;
  const left = layout
    ? Math.max(
        gutter,
        Math.min(
          preferredLeft,
          window.innerWidth - layout.content.width - gutter,
        ),
      )
    : 0;
  const top = layout
    ? resolvedSide === "bottom"
      ? layout.trigger.top + layout.trigger.height + sideOffset
      : Math.max(
          gutter,
          layout.trigger.top - layout.content.height - sideOffset,
        )
    : 0;

  // Ambas direcciones viajan entre los mismos estados oculto/visible. La salida apunta directo a
  // "hidden" en lugar de introducir otra coreografía.
  const wrap = reduce
    ? undefined
    : {
        hidden: { scale: 0.96, transition: SPRING_PANEL },
        show: { scale: 1, transition: SPRING_PANEL },
      };
  const clip = reduce
    ? undefined
    : {
        hidden: {
          clipPath: clipAt(resolvedSide, align, radius, 92),
          transition: MORPH_CLIP_TRANSITION,
        },
        show: {
          clipPath: clipAt(resolvedSide, align, radius, 0),
          transition: MORPH_CLIP_TRANSITION,
        },
      };
  // Se anima el valor directamente para que la opacidad permanezca en el estilo en línea durante la
  // entrada. Una animación nativa de opacidad puede mostrar el 0 inicial un cuadro al terminar, antes
  // de que Motion escriba el valor final.
  const opacity = useMotionValue(0);
  const ready = layout !== null;
  const focusedOnOpen = useRef(false);
  useEffect(() => {
    if (!ready || !isPresent || focusedOnOpen.current || !contentRef.current)
      return;
    focusedOnOpen.current = true;
    onOpenAutoFocus?.(contentRef.current);
  }, [ready, isPresent, contentRef, onOpenAutoFocus]);
  useEffect(() => {
    if (!ready) {
      if (!isPresent) safeToRemove?.();
      return;
    }
    const animation = animate(opacity, isPresent ? 1 : 0, {
      ...(reduce ? { duration: 0.12 } : SPRING_PANEL),
      onComplete: () => {
        if (!isPresent) safeToRemove?.();
      },
    });
    return () => animation.stop();
  }, [opacity, ready, isPresent, reduce, safeToRemove]);

  return (
    <motion.div
      data-morph-popover-portal=""
      inert={!isPresent}
      // El contenedor lleva la sombra como filtro drop-shadow, que sigue la forma recortada de abajo
      // (un box-shadow quedaría recortado).
      variants={wrap}
      initial="hidden"
      animate={layout ? "show" : "hidden"}
      exit="hidden"
      style={{
        left,
        top,
        opacity,
        pointerEvents: isPresent ? "auto" : "none",
        visibility: layout ? "visible" : "hidden",
        transformOrigin: originFor(resolvedSide, align),
      }}
      className={cn(
        "fixed z-50",
        shadow && "[filter:drop-shadow(0_10px_18px_rgba(0,0,0,0.14))]",
      )}
    >
      <motion.div
        ref={contentRef}
        id={contentId}
        role="dialog"
        aria-labelledby={triggerId}
        variants={clip}
        style={{
          borderRadius: radius,
          maxHeight: layout ? availableHeight : undefined,
          overflowY: "auto",
        }}
        className={cn(
          "overflow-hidden border border-border bg-popover text-popover-foreground",
          className,
        )}
      >
        {children}
      </motion.div>
    </motion.div>
  );
}
