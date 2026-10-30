import { Pagination } from "@/components/ui/pagination";
import { PaginationSummary } from "@/components/ui/pagination-summary";

type UsuariosPaginacionProps = {
  pagina: number;
  totalPaginas: number;
  tamanoPagina: number;
  total: number;
  onPaginaChange: (pagina: number) => void;
  onTamanoPaginaChange: (tamano: number) => void;
};

export function UsuariosPaginacion({
  pagina,
  totalPaginas,
  tamanoPagina,
  total,
  onPaginaChange,
  onTamanoPaginaChange,
}: UsuariosPaginacionProps) {
  return (
    // Fijo al borde inferior; los márgenes negativos compensan el p-6 del layout.
    <div className="sticky bottom-0 z-10 -mx-6 mt-auto -mb-6 flex flex-wrap items-center justify-between gap-4 px-6 pt-14 pb-6 before:absolute before:inset-0 before:-z-10 before:bg-background before:[mask-image:linear-gradient(to_top,black_55%,transparent)]">
      <PaginationSummary
        pageSize={tamanoPagina}
        total={total}
        onPageSizeChange={onTamanoPaginaChange}
      />
      <Pagination
        page={pagina}
        pageCount={totalPaginas}
        onPageChange={onPaginaChange}
      />
    </div>
  );
}
