import { ArrowLeft01Icon, ArrowRight01Icon } from "@hugeicons/core-free-icons";
import { HugeiconsIcon } from "@hugeicons/react";
import { LayoutGroup, motion, useReducedMotion } from "motion/react";
import * as React from "react";
import { Button } from "@/components/ui/button";
import { SPRING_LAYOUT } from "@/lib/ease";
import { cn } from "@/lib/utils";

const MAX_WITHOUT_ELLIPSIS = 7;
const EDGE_BLOCK = 5;
const EDGE_THRESHOLD = 4;

type PaginationProps = {
  page: number;
  pageCount: number;
  onPageChange: (page: number) => void;
  label?: string;
  className?: string;
};

type PageItem = number | "elipsis-inicio" | "elipsis-fin";

function range(from: number, to: number) {
  return Array.from({ length: to - from + 1 }, (_, i) => from + i);
}

// La clave es la posición del hueco, no el número: así los botones no se
// reordenan en el DOM al cambiar de tramo y la marca no se anima sobre un
// layout que se movió. Solo cambia la etiqueta de cada hueco.
function visiblePages(page: number, pageCount: number) {
  return visibleItems(page, pageCount).map((item, position) => ({
    itemKey: `hueco-${position}`,
    item,
  }));
}

// Siempre se muestran la primera y la última página.
function visibleItems(page: number, pageCount: number): PageItem[] {
  if (pageCount <= MAX_WITHOUT_ELLIPSIS) return range(1, pageCount);
  if (page <= EDGE_THRESHOLD) {
    return [...range(1, EDGE_BLOCK), "elipsis-fin", pageCount];
  }
  if (page >= pageCount - EDGE_THRESHOLD + 1) {
    return [
      1,
      "elipsis-inicio",
      ...range(pageCount - EDGE_BLOCK + 1, pageCount),
    ];
  }
  return [
    1,
    "elipsis-inicio",
    ...range(page - 1, page + 1),
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
  const reduceMotion = useReducedMotion();

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
        {visiblePages(actual, pageCount).map(
          ({ itemKey, item: pageNumber }) => {
            if (typeof pageNumber === "string") {
              return (
                <span
                  key={itemKey}
                  aria-hidden
                  className="flex size-8 items-center justify-center text-muted-foreground"
                >
                  …
                </span>
              );
            }
            const esActual = pageNumber === actual;
            return (
              <Button
                key={itemKey}
                variant="ghost"
                size="icon-sm"
                aria-label={`Página ${pageNumber}`}
                aria-current={esActual ? "page" : undefined}
                onClick={() => onPageChange(pageNumber)}
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
                    transition={reduceMotion ? { duration: 0 } : SPRING_LAYOUT}
                  />
                )}
                <span className="relative">{pageNumber}</span>
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
