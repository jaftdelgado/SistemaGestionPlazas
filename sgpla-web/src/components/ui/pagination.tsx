import { ArrowLeft01Icon, ArrowRight01Icon } from "@hugeicons/core-free-icons";
import { HugeiconsIcon } from "@hugeicons/react";
import { LayoutGroup, motion, useReducedMotion } from "motion/react";
import * as React from "react";
import { Button } from "@/components/ui/button";
import { SPRING_LAYOUT } from "@/lib/ease";
import { cn } from "@/lib/utils";

const MAXIMO_SIN_ELIPSIS = 7;
const BLOQUE_EXTREMO = 5;
const UMBRAL_EXTREMO = 4;

type PaginationProps = {
  page: number;
  pageCount: number;
  onPageChange: (page: number) => void;
  label?: string;
  className?: string;
};

type ElementoPagina = number | "elipsis-inicio" | "elipsis-fin";

function rango(desde: number, hasta: number) {
  return Array.from({ length: hasta - desde + 1 }, (_, i) => desde + i);
}

// La clave es la posición del hueco, no el número: así los botones no se
// reordenan en el DOM al cambiar de tramo y la marca no se anima sobre un
// layout que se movió. Solo cambia la etiqueta de cada hueco.
function paginasVisibles(page: number, pageCount: number) {
  return elementosVisibles(page, pageCount).map((elemento, posicion) => ({
    clave: `hueco-${posicion}`,
    elemento,
  }));
}

// Siempre se muestran la primera y la última página.
function elementosVisibles(page: number, pageCount: number): ElementoPagina[] {
  if (pageCount <= MAXIMO_SIN_ELIPSIS) return rango(1, pageCount);
  if (page <= UMBRAL_EXTREMO) {
    return [...rango(1, BLOQUE_EXTREMO), "elipsis-fin", pageCount];
  }
  if (page >= pageCount - UMBRAL_EXTREMO + 1) {
    return [
      1,
      "elipsis-inicio",
      ...rango(pageCount - BLOQUE_EXTREMO + 1, pageCount),
    ];
  }
  return [
    1,
    "elipsis-inicio",
    ...rango(page - 1, page + 1),
    "elipsis-fin",
    pageCount,
  ];
}

function Pagination({
  page,
  pageCount,
  onPageChange,
  label = "Paginación",
  className,
}: PaginationProps) {
  const markId = React.useId();
  const reducirMovimiento = useReducedMotion();

  if (pageCount < 1) {
    return <nav aria-label={label} className={className} />;
  }

  const actual = Math.min(Math.max(page, 1), pageCount);

  return (
    <nav
      aria-label={label}
      className={cn("flex flex-wrap items-center gap-1", className)}
    >
      <Button
        variant="ghost"
        size="sm"
        aria-label="Página anterior"
        disabled={actual === 1}
        onClick={() => onPageChange(actual - 1)}
      >
        <HugeiconsIcon icon={ArrowLeft01Icon} data-icon="inline-start" />
        Anterior
      </Button>
      <LayoutGroup id={markId}>
        {paginasVisibles(actual, pageCount).map(
          ({ clave, elemento: numero }) => {
            if (typeof numero === "string") {
              return (
                <span
                  key={clave}
                  aria-hidden
                  className="flex size-8 items-center justify-center text-muted-foreground"
                >
                  …
                </span>
              );
            }
            const esActual = numero === actual;
            return (
              <Button
                key={clave}
                variant="ghost"
                size="icon-sm"
                aria-label={`Página ${numero}`}
                aria-current={esActual ? "page" : undefined}
                onClick={() => onPageChange(numero)}
                className={cn(
                  "relative tabular-nums",
                  esActual &&
                    "text-primary-foreground hover:text-primary-foreground",
                )}
              >
                {esActual && (
                  <motion.span
                    layoutId="pagination-mark"
                    aria-hidden
                    className="absolute inset-0 rounded-full bg-primary"
                    transition={
                      reducirMovimiento ? { duration: 0 } : SPRING_LAYOUT
                    }
                  />
                )}
                <span className="relative">{numero}</span>
              </Button>
            );
          },
        )}
      </LayoutGroup>
      <Button
        variant="ghost"
        size="sm"
        aria-label="Página siguiente"
        disabled={actual === pageCount}
        onClick={() => onPageChange(actual + 1)}
      >
        Siguiente
        <HugeiconsIcon icon={ArrowRight01Icon} data-icon="inline-end" />
      </Button>
    </nav>
  );
}

export { Pagination };
