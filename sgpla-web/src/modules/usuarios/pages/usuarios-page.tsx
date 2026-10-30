import {
  PageHeader,
  PageHeaderActions,
  PageHeaderTitle,
} from "@/components/page-header/page-header";
import { Button } from "@/components/ui/button";
import { useUsuariosListado } from "../application/use-usuarios-listado";
import { UsuariosFiltros } from "../components/usuarios-filtros";
import { UsuariosPaginacion } from "../components/usuarios-paginacion";
import { UsuariosTabla } from "../components/usuarios-tabla";

export function UsuariosPage() {
  const listado = useUsuariosListado();

  return (
    <section className="flex flex-1 flex-col gap-4">
      <PageHeader>
        <PageHeaderTitle>Usuarios</PageHeaderTitle>
        <PageHeaderActions>
          <Button>Registrar usuario</Button>
        </PageHeaderActions>
      </PageHeader>
      <UsuariosFiltros
        textoBusqueda={listado.textoBusqueda}
        onBusquedaChange={listado.cambiarBusqueda}
        filtros={listado.filtros}
        onFiltrosChange={listado.cambiarFiltros}
      />
      <UsuariosTabla
        usuarios={listado.usuarios}
        cargando={listado.isPending}
        filasCarga={listado.tamanoPagina}
        hayError={listado.isError}
        error={listado.error}
        onReintentar={() => void listado.refetch()}
        hayCriterios={listado.hayCriterios}
        onLimpiarCriterios={listado.limpiarCriterios}
      />
      <UsuariosPaginacion
        pagina={listado.pagina}
        totalPaginas={listado.totalPaginas}
        tamanoPagina={listado.tamanoPagina}
        total={listado.total}
        onPaginaChange={listado.setPagina}
        onTamanoPaginaChange={listado.cambiarTamanoPagina}
      />
    </section>
  );
}
