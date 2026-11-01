import { Select } from "@/shared/ui/select";
import { PAGE_SIZE_OPTIONS } from "@/shared/lib/pagination";
import { cn } from "@/shared/lib/utils";

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
        options={pageSizeOptions.map((option) => ({
          value: String(option),
          label: String(option),
        }))}
        value={String(pageSize)}
        onValueChange={(value) => {
          if (value !== null) onPageSizeChange(Number(value));
        }}
      />
      <span>de {total} items</span>
    </div>
  );
}

export { PaginationSummary };
