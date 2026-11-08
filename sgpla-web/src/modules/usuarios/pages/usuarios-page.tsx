import { Button } from "@/shared/ui/button";
import {
  PageHeader,
  PageHeaderActions,
  PageHeaderTitle,
} from "@/shared/ui/page-header";
import { useUsuariosPage } from "../../hooks/use-usuarios-page";
import { UsuariosDataGrid } from "../components/usuarios-data-grid";
import { UsuariosFilters } from "../components/usuarios-filters";
import { UsuariosPagination } from "../components/usuarios-pagination";

export function UsuariosPage() {
  const page = useUsuariosPage();

  return (
    <section className="flex flex-1 flex-col gap-4">
      <PageHeader>
        <PageHeaderTitle>Usuarios</PageHeaderTitle>
        <PageHeaderActions>
          <Button>Registrar usuario</Button>
        </PageHeaderActions>
      </PageHeader>
      <UsuariosFilters {...page.filters} />
      <UsuariosDataGrid {...page.grid} />
      <UsuariosPagination {...page.pagination} />
    </section>
  );
}
