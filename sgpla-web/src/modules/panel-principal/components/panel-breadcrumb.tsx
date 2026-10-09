import { Link } from "@tanstack/react-router";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
import { useNavegacion } from "../hooks/use-navegacion";

export function PanelBreadcrumb() {
  const { elementos, activo } = useNavegacion();
  const pagina =
    activo === "inicio"
      ? "Panel principal"
      : elementos.find((e) => e.id === activo)?.titulo;
  return (
    <Breadcrumb>
      <BreadcrumbList>
        <BreadcrumbItem>
          <BreadcrumbLink render={(props) => <Link to="/" {...props} />}>
            Inicio
          </BreadcrumbLink>
        </BreadcrumbItem>
        <BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbPage>{pagina}</BreadcrumbPage>
        </BreadcrumbItem>
      </BreadcrumbList>
    </Breadcrumb>
  );
}
