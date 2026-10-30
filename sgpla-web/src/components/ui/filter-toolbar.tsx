import {
  ArrowLeft01Icon,
  ArrowRight01Icon,
  FilterHorizontalIcon,
  Tick01Icon,
} from "@hugeicons/core-free-icons";
import { HugeiconsIcon, type IconSvgElement } from "@hugeicons/react";
import { Popover as PopoverPrimitive } from "@base-ui/react/popover";
import {
  AnimatePresence,
  motion,
  useReducedMotion,
  type Variants,
} from "motion/react";
import * as React from "react";
import { Button } from "@/components/ui/button";
import { EASE_OUT, SPRING_SWAP } from "@/lib/ease";
import { cn } from "@/lib/utils";

export type FilterChip = {
  id: string;
  label: string;
  value?: string;
};

export type FilterFieldOption = { value: string; label: string };

export type FilterField = {
  id: string;
  label: string;
  icon?: IconSvgElement;
  options: FilterFieldOption[];
};

export type FilterToolbarProps = {
  /** Filtros aplicados; su cantidad se muestra en la insignia del botón. */
  filters: FilterChip[];
  fields: FilterField[];
  /** Recibe un chip con el id del campo; reemplaza al anterior del mismo campo. */
  onAdd: (filter: FilterChip) => void;
  onRemove: (id: string) => void;
  onClearAll?: () => void;
  size?: "default" | "sm";
  /** Texto del botón que abre el panel. */
  addLabel?: string;
  /** Nombre accesible del grupo. */
  label?: string;
  className?: string;
};

const STEP_OFFSET = 18;
const INSTANT = { duration: 0 };
const EASE = [...EASE_OUT] as [number, number, number, number];

const ITEM_CLASS =
  "relative flex min-h-8 w-full cursor-default items-center gap-2 rounded-full py-1 pr-8 pl-3 text-left text-base outline-none select-none hover:bg-muted focus-visible:bg-muted md:text-sm";

// Los pasos viajan en la dirección del movimiento: avanzar entra por el borde final, volver por el inicial.
function stepVariants(reduceMotion: boolean): Variants {
  return {
    enter: (direction: number) =>
      reduceMotion
        ? { opacity: 0 }
        : {
            opacity: 0,
            x: direction * STEP_OFFSET,
          },
    center: {
      opacity: 1,
      x: 0,
      transition: reduceMotion ? { duration: 0.1 } : { duration: 0.22, ease: EASE },
    },
    exit: (direction: number) =>
      reduceMotion
        ? { opacity: 0, transition: INSTANT }
        : {
            opacity: 0,
            x: direction * -STEP_OFFSET,
            transition: { duration: 0.12, ease: EASE },
          },
  };
}

function FilterToolbar({
  filters,
  fields,
  onAdd,
  onRemove,
  onClearAll,
  size = "default",
  addLabel = "Filtros",
  label = "Filtros activos",
  className,
}: FilterToolbarProps) {
  const isSmall = size === "sm";
  const reduceMotion = useReducedMotion() ?? false;
  const [isOpen, setIsOpen] = React.useState(false);
  const [activeFieldId, setActiveFieldId] = React.useState<string | null>(null);
  const [direction, setDirection] = React.useState(1);
  // Alto del paso visible; el contenedor lo sigue con un resorte al cambiar de paso.
  const [contentHeight, setContentHeight] = React.useState<number | null>(null);
  const listRef = React.useRef<HTMLDivElement>(null);
  // Fila que recibe el foco tras cambiar de paso; null si no hay cambio pendiente.
  const pendingFocus = React.useRef<string | null>(null);

  const activeField = fields.find((f) => f.id === activeFieldId) ?? null;

  React.useLayoutEffect(() => {
    if (!isOpen || !listRef.current) return;
    setContentHeight(listRef.current.offsetHeight);
  }, [isOpen, activeFieldId]);

  React.useEffect(() => {
    const itemKey = pendingFocus.current;
    if (itemKey === null) return;
    pendingFocus.current = null;
    const rows = Array.from(
      listRef.current?.querySelectorAll<HTMLElement>("[data-row]") ?? [],
    );
    const row =
      itemKey === "primera"
        ? rows[0]
        : rows.find((f) => f.dataset.row === itemKey);
    (row ?? rows[0])?.focus();
  }, [activeFieldId]);

  function changeOpen(next: boolean) {
    setIsOpen(next);
    if (next) {
      setActiveFieldId(null);
      setContentHeight(null);
    }
  }

  function enterField(id: string) {
    pendingFocus.current = "primera";
    setDirection(1);
    setActiveFieldId(id);
  }

  function backToFields() {
    if (!activeField) return;
    pendingFocus.current = activeField.id;
    setDirection(-1);
    setActiveFieldId(null);
  }

  // Elegir de nuevo el valor aplicado lo quita.
  function selectValue(option: FilterFieldOption) {
    if (!activeField) return;
    const applied = filters.some(
      (f) => f.id === activeField.id && f.value === option.value,
    );
    if (applied) onRemove(activeField.id);
    else onAdd({ id: activeField.id, label: activeField.label, value: option.value });
    setIsOpen(false);
  }

  function onKeyDown(event: React.KeyboardEvent<HTMLDivElement>) {
    const rows = Array.from(
      listRef.current?.querySelectorAll<HTMLElement>("[data-row]") ?? [],
    );
    const index = rows.indexOf(document.activeElement as HTMLElement);
    function ir(next: number) {
      event.preventDefault();
      rows[next]?.focus();
    }
    if (event.key === "ArrowDown") return ir((index + 1) % rows.length);
    if (event.key === "ArrowUp") {
      return ir((index - 1 + rows.length) % rows.length);
    }
    if (event.key === "Home") return ir(0);
    if (event.key === "End") return ir(rows.length - 1);
    const currentRow = rows[index]?.dataset.row ?? "";
    if (
      event.key === "ArrowRight" &&
      !activeField &&
      fields.some((f) => f.id === currentRow)
    ) {
      event.preventDefault();
      enterField(currentRow);
    }
    if (event.key === "ArrowLeft" && activeField) {
      event.preventDefault();
      backToFields();
    }
  }

  return (
    <div
      role="group"
      aria-label={label}
      data-slot="filter-toolbar"
      data-size={size}
      className={cn("flex flex-wrap items-center gap-2", className)}
    >
      {filters.length > 0 && onClearAll && (
        <Button
          type="button"
          variant="ghost"
          size={isSmall ? "sm" : "default"}
          onClick={onClearAll}
        >
          Limpiar filtros
        </Button>
      )}
      <PopoverPrimitive.Root open={isOpen} onOpenChange={changeOpen}>
        <PopoverPrimitive.Trigger
          data-slot="filter-toolbar-trigger"
          className={cn(
            "relative inline-flex h-10 w-32 items-center gap-2 rounded-full border border-transparent bg-input px-4 py-1 text-base font-medium shadow-border transition-colors outline-none hover:bg-muted focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 md:text-sm [&_svg]:size-4 [&_svg]:shrink-0",
            isSmall && "h-8 w-32 px-3",
            filters.length > 0 ? "justify-start" : "justify-center",
          )}
        >
          {/* Al aparecer la insignia el grupo pasa del centro al inicio con un resorte ligero. La insignia está fuera
              del flujo para que su salida no retrase el regreso del grupo al centro. */}
          <motion.span
            layout="position"
            transition={reduceMotion ? INSTANT : SPRING_SWAP}
            className="inline-flex items-center gap-2"
          >
            <HugeiconsIcon icon={FilterHorizontalIcon} aria-hidden="true" />
            {addLabel}
          </motion.span>
          <AnimatePresence initial={false}>
            {filters.length > 0 && (
              <motion.span
                key="insignia"
                aria-label={`${filters.length} filtros aplicados`}
                initial={reduceMotion ? false : { opacity: 0, scale: 0.6 }}
                animate={{ opacity: 1, scale: 1 }}
                exit={
                  reduceMotion
                    ? { opacity: 0, transition: INSTANT }
                    : {
                        opacity: 0,
                        scale: 0.6,
                        transition: { duration: 0.12, ease: EASE },
                      }
                }
                transition={reduceMotion ? INSTANT : SPRING_SWAP}
                className={cn(
                  "absolute top-1/2 -mt-2.5 flex h-5 min-w-5 items-center justify-center rounded-full bg-primary px-1.5 text-xs font-medium text-primary-foreground tabular-nums",
                  isSmall ? "right-1.5" : "right-2.5",
                )}
              >
                {filters.length}
              </motion.span>
            )}
          </AnimatePresence>
        </PopoverPrimitive.Trigger>
        <PopoverPrimitive.Portal>
          <PopoverPrimitive.Positioner
            sideOffset={4}
            align="end"
            className="isolate z-50"
          >
            <PopoverPrimitive.Popup
              data-slot="filter-toolbar-content"
              className="max-h-(--available-height) w-68 origin-(--transform-origin) overflow-y-auto rounded-2xl bg-popover p-1 text-popover-foreground shadow-border outline-none data-open:animate-in data-open:fade-in-0 data-open:zoom-in-95 data-closed:animate-out data-closed:fade-out-0 data-closed:zoom-out-95"
            >
              <motion.div
                className="relative overflow-hidden"
                initial={false}
                animate={contentHeight === null ? undefined : { height: contentHeight }}
                transition={
                  reduceMotion ? INSTANT : { duration: 0.22, ease: EASE }
                }
              >
                <AnimatePresence
                  mode="popLayout"
                  initial={false}
                  custom={direction}
                >
                  <motion.div
                    key={activeField?.id ?? "campos"}
                    ref={listRef}
                    role="menu"
                    aria-label={activeField ? activeField.label : addLabel}
                    custom={direction}
                    variants={stepVariants(reduceMotion)}
                    initial="enter"
                    animate="center"
                    exit="exit"
                    onKeyDown={onKeyDown}
                  >
                    {activeField && (
                      <button
                        type="button"
                        aria-label="Volver a los campos"
                        onClick={backToFields}
                        className="mb-1 flex min-h-8 w-full items-center gap-2 rounded-full py-1 pr-3 pl-2 text-left text-base font-medium outline-none hover:bg-muted focus-visible:bg-muted md:text-sm"
                      >
                        <HugeiconsIcon
                          icon={ArrowLeft01Icon}
                          aria-hidden="true"
                          className="size-4"
                        />
                        {activeField.label}
                      </button>
                    )}
                    {activeField
                      ? activeField.options.map((option) => {
                          const applied = filters.some(
                            (f) =>
                              f.id === activeField.id && f.value === option.value,
                          );
                          return (
                            <button
                              key={option.value}
                              type="button"
                              role="menuitemradio"
                              aria-checked={applied}
                              data-row={option.value}
                              className={ITEM_CLASS}
                              onClick={() => selectValue(option)}
                            >
                              {option.label}
                              {applied && (
                                <HugeiconsIcon
                                  icon={Tick01Icon}
                                  aria-hidden="true"
                                  className="absolute right-3 size-4"
                                />
                              )}
                            </button>
                          );
                        })
                      : fields.map((f) => {
                          const value = filters.find(
                            (x) => x.id === f.id,
                          )?.value;
                          const labelText =
                            f.options.find((o) => o.value === value)?.label ??
                            value;
                          return (
                            <button
                              key={f.id}
                              type="button"
                              role="menuitem"
                              data-row={f.id}
                              className={cn(ITEM_CLASS, "pr-3")}
                              onClick={() => enterField(f.id)}
                            >
                              {f.icon && (
                                <HugeiconsIcon
                                  icon={f.icon}
                                  aria-hidden="true"
                                  className="size-4 shrink-0"
                                />
                              )}
                              <span className="shrink-0">{f.label}</span>
                              <span className="flex min-w-0 flex-1 items-center justify-end gap-1 text-muted-foreground">
                                <span className="truncate" title={labelText}>
                                  {labelText}
                                </span>
                                <HugeiconsIcon
                                  icon={ArrowRight01Icon}
                                  aria-hidden="true"
                                  className="size-4 shrink-0"
                                />
                              </span>
                            </button>
                          );
                        })}
                  </motion.div>
                </AnimatePresence>
              </motion.div>
            </PopoverPrimitive.Popup>
          </PopoverPrimitive.Positioner>
        </PopoverPrimitive.Portal>
      </PopoverPrimitive.Root>
    </div>
  );
}

export { FilterToolbar };
