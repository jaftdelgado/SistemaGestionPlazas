import {
  PageHeader,
  PageHeaderActions,
  PageHeaderTitle,
} from "@/components/page-header/page-header";
import { Button } from "@/components/ui/button";
import { SearchInput } from "@/components/ui/search-input";

export function UsuariosPage() {
  return (
    <section className="flex flex-col gap-4">
      <PageHeader>
        <PageHeaderTitle>Usuarios</PageHeaderTitle>
        <PageHeaderActions>
          <Button>Registrar usuario</Button>
        </PageHeaderActions>
      </PageHeader>
      <SearchInput
        aria-label="Buscar usuarios"
        placeholder="Buscar usuarios"
        className="max-w-xs"
      />
    </section>
  );
}
