import { Select } from "@/components/ui/select";
import { cn } from "@/lib/utils";

export const PAGE_SIZE_OPTIONS = [5, 10, 25, 50] as const;

type PaginationSummaryProps = {
  /** Registros por página; debe ser una de las opciones. */
  pageSize: number;
  /** Total de registros, no solo los de la página. */
  total: number;
  onPageSizeChange: (pageSize: number) => void;
  pageSizeOptions?: readonly number[];
  className?: string;
};

function PaginationSummary({
  pageSize,
  total,
  onPageSizeChange,
  pageSizeOptions = PAGE_SIZE_OPTIONS,
  className,
}: PaginationSummaryProps) {
  return (
    <div
      data-slot="pagination-summary"
      className={cn(
        "flex flex-wrap items-center gap-2 text-sm text-muted-foreground",
        className,
      )}
    >
      <span>Mostrando</span>
      <Select
        size="sm"
        aria-label="Registros por página"
        className="w-18"
        options={pageSizeOptions.map((opcion) => ({
          value: String(opcion),
          label: String(opcion),
        }))}
        value={String(pageSize)}
        onValueChange={(valor) => {
          if (valor !== null) onPageSizeChange(Number(valor));
        }}
      />
      <span>de {total} elementos</span>
    </div>
  );
}

export { PaginationSummary };
