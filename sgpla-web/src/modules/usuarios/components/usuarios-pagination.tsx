import { Pagination } from "@/components/ui/pagination";
import { PaginationSummary } from "@/components/ui/pagination-summary";

type UsuariosPaginationProps = {
  page: number;
  totalPages: number;
  pageSize: number;
  total: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;
};

export function UsuariosPagination({
  page,
  totalPages,
  pageSize,
  total,
  onPageChange,
  onPageSizeChange,
}: UsuariosPaginationProps) {
  return (
    // Fijo al borde inferior; los márgenes negativos compensan el p-6 del layout.
    <div className="sticky bottom-0 z-10 -mx-6 mt-auto -mb-6 flex flex-wrap items-center justify-between gap-4 px-6 pt-14 pb-6 before:absolute before:inset-0 before:-z-10 before:bg-background before:[mask-image:linear-gradient(to_top,black_55%,transparent)]">
      <PaginationSummary
        pageSize={pageSize}
        total={total}
        onPageSizeChange={onPageSizeChange}
      />
      <Pagination
        page={page}
        pageCount={totalPages}
        onPageChange={onPageChange}
      />
    </div>
  );
}
