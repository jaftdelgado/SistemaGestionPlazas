import {
  ArrowLeft01Icon,
  ArrowRight01Icon,
  FilterHorizontalIcon,
  Tick01Icon,
} from "@hugeicons/core-free-icons";
import { HugeiconsIcon } from "@hugeicons/react";
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

const DESPLAZAMIENTO_PASO = 18;
const INSTANTANEO = { duration: 0 };
const EASE = [...EASE_OUT] as [number, number, number, number];

const ITEM_CLASS =
  "relative flex min-h-8 w-full cursor-default items-center gap-2 rounded-full py-1 pr-8 pl-3 text-left text-base outline-none select-none hover:bg-muted focus-visible:bg-muted md:text-sm";

// Los pasos viajan en la dirección del movimiento: avanzar entra por el borde final, volver por el inicial.
function pasos(reducir: boolean): Variants {
  return {
    entra: (direccion: number) =>
      reducir
        ? { opacity: 0 }
        : {
            opacity: 0,
            x: direccion * DESPLAZAMIENTO_PASO,
          },
    centro: {
      opacity: 1,
      x: 0,
      transition: reducir ? { duration: 0.1 } : { duration: 0.22, ease: EASE },
    },
    sale: (direccion: number) =>
      reducir
        ? { opacity: 0, transition: INSTANTANEO }
        : {
            opacity: 0,
            x: direccion * -DESPLAZAMIENTO_PASO,
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
  const esSm = size === "sm";
  const reducir = useReducedMotion() ?? false;
  const [abierto, setAbierto] = React.useState(false);
  const [campoId, setCampoId] = React.useState<string | null>(null);
  const [direccion, setDireccion] = React.useState(1);
  // Alto del paso visible; el contenedor lo sigue con un resorte al cambiar de paso.
  const [alto, setAlto] = React.useState<number | null>(null);
  const lista = React.useRef<HTMLDivElement>(null);
  // Fila que recibe el foco tras cambiar de paso; null si no hay cambio pendiente.
  const enfocar = React.useRef<string | null>(null);

  const campo = fields.find((f) => f.id === campoId) ?? null;

  React.useLayoutEffect(() => {
    if (!abierto || !lista.current) return;
    setAlto(lista.current.offsetHeight);
  }, [abierto, campoId]);

  React.useEffect(() => {
    const clave = enfocar.current;
    if (clave === null) return;
    enfocar.current = null;
    const filas = Array.from(
      lista.current?.querySelectorAll<HTMLElement>("[data-row]") ?? [],
    );
    const fila =
      clave === "primera"
        ? filas[0]
        : filas.find((f) => f.dataset.row === clave);
    (fila ?? filas[0])?.focus();
  }, [campoId]);

  function cambiarApertura(siguiente: boolean) {
    setAbierto(siguiente);
    if (siguiente) {
      setCampoId(null);
      setAlto(null);
    }
  }

  function entrarEnCampo(id: string) {
    enfocar.current = "primera";
    setDireccion(1);
    setCampoId(id);
  }

  function volverACampos() {
    if (!campo) return;
    enfocar.current = campo.id;
    setDireccion(-1);
    setCampoId(null);
  }

  // Elegir de nuevo el valor aplicado lo quita.
  function elegirValor(opcion: FilterFieldOption) {
    if (!campo) return;
    const aplicado = filters.some(
      (f) => f.id === campo.id && f.value === opcion.value,
    );
    if (aplicado) onRemove(campo.id);
    else onAdd({ id: campo.id, label: campo.label, value: opcion.value });
    setAbierto(false);
  }

  function onKeyDown(evento: React.KeyboardEvent<HTMLDivElement>) {
    const filas = Array.from(
      lista.current?.querySelectorAll<HTMLElement>("[data-row]") ?? [],
    );
    const indice = filas.indexOf(document.activeElement as HTMLElement);
    function ir(siguiente: number) {
      evento.preventDefault();
      filas[siguiente]?.focus();
    }
    if (evento.key === "ArrowDown") return ir((indice + 1) % filas.length);
    if (evento.key === "ArrowUp") {
      return ir((indice - 1 + filas.length) % filas.length);
    }
    if (evento.key === "Home") return ir(0);
    if (evento.key === "End") return ir(filas.length - 1);
    const filaActual = filas[indice]?.dataset.row ?? "";
    if (
      evento.key === "ArrowRight" &&
      !campo &&
      fields.some((f) => f.id === filaActual)
    ) {
      evento.preventDefault();
      entrarEnCampo(filaActual);
    }
    if (evento.key === "ArrowLeft" && campo) {
      evento.preventDefault();
      volverACampos();
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
          size={esSm ? "sm" : "default"}
          onClick={onClearAll}
        >
          Limpiar filtros
        </Button>
      )}
      <PopoverPrimitive.Root open={abierto} onOpenChange={cambiarApertura}>
        <PopoverPrimitive.Trigger
          data-slot="filter-toolbar-trigger"
          className={cn(
            "relative inline-flex h-10 w-32 items-center gap-2 rounded-full border border-transparent bg-input px-4 py-1 text-base font-medium shadow-border transition-colors outline-none hover:bg-muted focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 md:text-sm [&_svg]:size-4 [&_svg]:shrink-0",
            esSm && "h-8 w-32 px-3",
            filters.length > 0 ? "justify-start" : "justify-center",
          )}
        >
          {/* Al aparecer la insignia el grupo pasa del centro al inicio con un resorte ligero. La insignia está fuera
              del flujo para que su salida no retrase el regreso del grupo al centro. */}
          <motion.span
            layout="position"
            transition={reducir ? INSTANTANEO : SPRING_SWAP}
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
                initial={reducir ? false : { opacity: 0, scale: 0.6 }}
                animate={{ opacity: 1, scale: 1 }}
                exit={
                  reducir
                    ? { opacity: 0, transition: INSTANTANEO }
                    : {
                        opacity: 0,
                        scale: 0.6,
                        transition: { duration: 0.12, ease: EASE },
                      }
                }
                transition={reducir ? INSTANTANEO : SPRING_SWAP}
                className={cn(
                  "absolute top-1/2 -mt-2.5 flex h-5 min-w-5 items-center justify-center rounded-full bg-primary px-1.5 text-xs font-medium text-primary-foreground tabular-nums",
                  esSm ? "right-1.5" : "right-2.5",
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
                animate={alto === null ? undefined : { height: alto }}
                transition={
                  reducir ? INSTANTANEO : { duration: 0.22, ease: EASE }
                }
              >
                <AnimatePresence
                  mode="popLayout"
                  initial={false}
                  custom={direccion}
                >
                  <motion.div
                    key={campo?.id ?? "campos"}
                    ref={lista}
                    role="menu"
                    aria-label={campo ? campo.label : addLabel}
                    custom={direccion}
                    variants={pasos(reducir)}
                    initial="entra"
                    animate="centro"
                    exit="sale"
                    onKeyDown={onKeyDown}
                  >
                    {campo && (
                      <button
                        type="button"
                        aria-label="Volver a los campos"
                        onClick={volverACampos}
                        className="mb-1 flex min-h-8 w-full items-center gap-2 rounded-full py-1 pr-3 pl-2 text-left text-base font-medium outline-none hover:bg-muted focus-visible:bg-muted md:text-sm"
                      >
                        <HugeiconsIcon
                          icon={ArrowLeft01Icon}
                          aria-hidden="true"
                          className="size-4"
                        />
                        {campo.label}
                      </button>
                    )}
                    {campo
                      ? campo.options.map((opcion) => {
                          const aplicado = filters.some(
                            (f) =>
                              f.id === campo.id && f.value === opcion.value,
                          );
                          return (
                            <button
                              key={opcion.value}
                              type="button"
                              role="menuitemradio"
                              aria-checked={aplicado}
                              data-row={opcion.value}
                              className={ITEM_CLASS}
                              onClick={() => elegirValor(opcion)}
                            >
                              {opcion.label}
                              {aplicado && (
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
                          const valor = filters.find(
                            (x) => x.id === f.id,
                          )?.value;
                          const etiqueta =
                            f.options.find((o) => o.value === valor)?.label ??
                            valor;
                          return (
                            <button
                              key={f.id}
                              type="button"
                              role="menuitem"
                              data-row={f.id}
                              className={cn(ITEM_CLASS, "pr-3")}
                              onClick={() => entrarEnCampo(f.id)}
                            >
                              <span className="shrink-0">{f.label}</span>
                              <span className="flex min-w-0 flex-1 items-center justify-end gap-1 text-muted-foreground">
                                <span className="truncate" title={etiqueta}>
                                  {etiqueta}
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
