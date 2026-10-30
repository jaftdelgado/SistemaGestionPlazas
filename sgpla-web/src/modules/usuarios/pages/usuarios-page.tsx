import {
  PageHeader,
  PageHeaderActions,
  PageHeaderTitle,
} from "@/components/page-header/page-header";
import { Button } from "@/components/ui/button";
import { useUsuariosListing } from "../application/use-usuarios-listing";
import { UsuariosDataGrid } from "../components/usuarios-data-grid";
import { UsuariosFilters } from "../components/usuarios-filters";
import { UsuariosPagination } from "../components/usuarios-pagination";

export function UsuariosPage() {
  const listing = useUsuariosListing();

  return (
    <section className="flex flex-1 flex-col gap-4">
      <PageHeader>
        <PageHeaderTitle>Usuarios</PageHeaderTitle>
        <PageHeaderActions>
          <Button>Registrar usuario</Button>
        </PageHeaderActions>
      </PageHeader>
      <UsuariosFilters
        searchText={listing.searchText}
        onSearchChange={listing.changeSearch}
        filters={listing.filters}
        onFiltersChange={listing.changeFilters}
      />
      <UsuariosDataGrid
        usuarios={listing.usuarios}
        loading={listing.isPending}
        loadingRows={listing.pageSize}
        hasError={listing.isError}
        error={listing.error}
        onRetry={() => void listing.refetch()}
        hasCriteria={listing.hasCriteria}
        onClearCriteria={listing.clearCriteria}
      />
      <UsuariosPagination
        page={listing.page}
        totalPages={listing.totalPages}
        pageSize={listing.pageSize}
        total={listing.total}
        onPageChange={listing.setPage}
        onPageSizeChange={listing.changePageSize}
      />
    </section>
  );
}
